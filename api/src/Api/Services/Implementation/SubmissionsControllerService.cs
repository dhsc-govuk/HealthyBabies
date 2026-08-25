using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Api.Services.Abstract;
using Application.Common;
using Application.Common.FileValidation;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.DataCollections.Dtos;
using Application.Organisations;
using Application.Organisations.Helpers;
using Application.Submissions.Dtos;
using Application.Submissions.Helpers;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using Domain.Organisations;
using Domain.Services;
using Domain.Users;
using LanguageExt;

namespace Api.Services.Implementation;

public class SubmissionsControllerService(
    IDataCollectionQueries dataCollectionQueries,
    IDataCollectionSubmissionQueries submissionQueries,
    IDataCollectionSubmissionRepository dataCollectionSubmissionRepository,
    IDataCollectionSubmissionVersionRepository versionRepository,
    IDataCollectionFormModuleQueries formModuleQueries,
    IFormSubmissionRepository formSubmissionRepository,
    IServiceQueries serviceQueries,
    IOrganisationFinancialDataQueries financialDataQueries,
    IOrganisationQueries organisationQueries,
    IBlobService blobService,
    IFileValidationService fileValidationService)
    : ISubmissionsControllerService
{
    private const string SubmissionFilesContainer = "submission-files";
    private static string GetOverallStatus(DataCollectionSubmissionStatus status) => status switch
    {
        DataCollectionSubmissionStatus.NotStarted => "Not started",
        DataCollectionSubmissionStatus.InProgress => "In progress",
        DataCollectionSubmissionStatus.Submitted => "Submitted",
        DataCollectionSubmissionStatus.Approved => "Approved",
        // Rejected is the recorded decision; "Reopened" is the wording users see.
        DataCollectionSubmissionStatus.Rejected => "Reopened",
        DataCollectionSubmissionStatus.RequiresChanges => "Requires changes",
        _ => "Unknown"
    };

    private static string GetFormSubmissionStatus(SubmissionStatus status) => status.Value switch
    {
        "draft" => "In progress",
        "submitted" => "Submitted",
        "under_review" => "Under review",
        "approved" => "Completed",
        "rejected" => "Rejected",
        "requires_changes" => "Requires changes",
        "cancelled" => "Cancelled",
        _ => "Not started"
    };

    public async Task<IEnumerable<SubmissionDto>> GetByOrganisationId(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var dataCollections = await dataCollectionQueries.GetByOrganisationIdAsync(orgId, cancellationToken);

        // Get all form submissions for this organisation to check progress
        var allSubmissions = await formSubmissionRepository.GetByOrganisationIdAsync(orgId, cancellationToken);

        // Get all data collection submissions to check overall status
        var dcSubmissions = await submissionQueries.GetByOrganisationIdAsync(orgId, cancellationToken);

        var result = new List<SubmissionDto>();

        foreach (var dc in dataCollections)
        {
            var endDate = dc.GetEffectiveEndDate(orgId);
            var daysRemaining = dc.GetDaysRemaining(orgId);
            string status;

            if (dc.IsClosedFor(orgId))
            {
                status = "Closed";
            }
            else
            {
                // First check if there's a DataCollectionSubmission record with a status
                var dcSubmission = dcSubmissions.FirstOrDefault(s => s.DataCollectionId == dc.Id);
                if (dcSubmission != null)
                {
                    status = GetOverallStatus(dcSubmission.Status);
                }
                else
                {
                    // Check if any form submissions exist for this data collection
                    var hasAnyProgress = allSubmissions.Any(s => s.DataCollectionId == dc.Id);
                    status = hasAnyProgress ? "In progress" : "Incomplete";
                }
            }

            result.Add(new SubmissionDto(
                dc.Id.Value,
                dc.Name,
                dc.Description,
                dc.StartDate,
                endDate,
                status,
                Math.Max(0, daysRemaining),
                dc.IsClosedFor(orgId),
                dc.Type.Value));
        }

        return result;
    }

    public async Task<IEnumerable<SubmissionDto>> GetByOrganisationIdAndType(
        Guid organisationId,
        string type,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var dataCollectionType = DataCollectionType.From(type);
        var dataCollections = await dataCollectionQueries.GetByOrganisationIdAndTypeAsync(orgId, dataCollectionType, cancellationToken);

        var allSubmissions = await formSubmissionRepository.GetByOrganisationIdAsync(orgId, cancellationToken);
        var dcSubmissions = await submissionQueries.GetByOrganisationIdAsync(orgId, cancellationToken);

        var result = new List<SubmissionDto>();

        foreach (var dc in dataCollections)
        {
            var endDate = dc.GetEffectiveEndDate(orgId);
            var daysRemaining = dc.GetDaysRemaining(orgId);
            string status;

            if (dc.IsClosedFor(orgId))
            {
                status = "Closed";
            }
            else
            {
                var dcSubmission = dcSubmissions.FirstOrDefault(s => s.DataCollectionId == dc.Id);
                if (dcSubmission != null)
                {
                    status = GetOverallStatus(dcSubmission.Status);
                }
                else
                {
                    var hasAnyProgress = allSubmissions.Any(s => s.DataCollectionId == dc.Id);
                    status = hasAnyProgress ? "In progress" : "Incomplete";
                }
            }

            result.Add(new SubmissionDto(
                dc.Id.Value,
                dc.Name,
                dc.Description,
                dc.StartDate,
                endDate,
                status,
                Math.Max(0, daysRemaining),
                dc.IsClosedFor(orgId),
                dc.Type.Value));
        }

        return result;
    }

    public async Task<Option<SubmissionDetailDto>> GetById(
        Guid organisationId,
        Guid dataCollectionId,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var dcId = new DataCollectionId(dataCollectionId);

        var submissionOption = await submissionQueries.GetByDataCollectionAndOrganisationWithSectionsAsync(
            dcId, orgId, cancellationToken);

        if (submissionOption.IsNone)
        {
            return await GetSubmissionFromDataCollection(orgId, dcId, cancellationToken);
        }

        var submission = submissionOption.Match(s => s, () => null!);
        var endDate = submission.DataCollection!.GetEffectiveEndDate(orgId);
        var daysRemaining = submission.DataCollection.GetDaysRemaining(orgId);
        var overallStatus = GetOverallStatus(submission.Status);

        var formModules = await BuildFormModuleDtos(
            submission.DataCollection!.FormModuleAssignments, orgId, submission.SectionsToReview.ToHashSet(), cancellationToken);

        var previousVersionCount = await versionRepository.GetCountByDataCollectionSubmissionIdAsync(submission.Id, cancellationToken);

        var isDeliveryPlan = submission.DataCollection.Type.Value == DataCollectionTypeConstants.DeliveryPlan;
        var sections = isDeliveryPlan
            ? submission.DataCollection.FormModuleAssignments
                .Where(fm => fm.FormModule is { IsActive: true })
                .OrderBy(fm => fm.FormModule.SectionNumber)
                .Select(fm => new ReviewSectionSummaryDto(
                    FormModuleId: fm.FormModuleId.Value,
                    Code: fm.FormModule.Code,
                    SectionNumber: fm.FormModule.SectionNumber,
                    Name: fm.FormModule.Name,
                    AllowsMultipleActivities: fm.FormModule.AllowsMultipleActivities))
                .ToList()
            : (IReadOnlyList<ReviewSectionSummaryDto>)[];

        return Option<SubmissionDetailDto>.Some(new SubmissionDetailDto(
            submission.DataCollectionId.Value,
            submission.DataCollection!.Name,
            submission.DataCollection.Description,
            submission.DataCollection.StartDate,
            endDate,
            overallStatus,
            Math.Max(0, daysRemaining),
            submission.DataCollection.IsClosedFor(orgId),
            submission.DataCollection.Type.Value,
            formModules,
            previousVersionCount,
            submission.ReviewedAt,
            submission.ReviewNotes,
            submission.ReviewedBy?.Name.ToString(),
            submission.SubmittedBy?.Name.ToString(),
            submission.SubmittedBy?.Email,
            submission.SubmittedAt,
            sections));
    }

    private async Task<Option<SubmissionDetailDto>> GetSubmissionFromDataCollection(
        OrganisationId orgId,
        DataCollectionId dcId,
        CancellationToken cancellationToken)
    {
        var dataCollections = await dataCollectionQueries.GetByOrganisationIdAsync(orgId, cancellationToken);
        var dataCollection = dataCollections.FirstOrDefault(dc => dc.Id == dcId);

        if (dataCollection == null)
        {
            return Option<SubmissionDetailDto>.None;
        }

        var endDate = dataCollection.GetEffectiveEndDate(orgId);
        var daysRemaining = dataCollection.GetDaysRemaining(orgId);

        var formModules = await BuildFormModuleDtos(
            dataCollection.FormModuleAssignments, orgId, Array.Empty<Guid>().ToHashSet(), cancellationToken);

        return new SubmissionDetailDto(
            dataCollection.Id.Value,
            dataCollection.Name,
            dataCollection.Description,
            dataCollection.StartDate,
            endDate,
            "Not started",
            Math.Max(0, daysRemaining),
            dataCollection.IsClosedFor(orgId),
            dataCollection.Type.Value,
            formModules,
            PreviousVersionCount: 0);
    }

    private async Task<List<SubmissionFormModuleDto>> BuildFormModuleDtos(
        IEnumerable<DataCollectionFormModuleAssignment> assignments,
        OrganisationId organisationId,
        IReadOnlySet<Guid> sectionsToReview,
        CancellationToken cancellationToken)
    {
        var result = new List<SubmissionFormModuleDto>();

        var activeAssignments = assignments
            .Where(fma => fma.FormModule != null && fma.FormModule.IsActive)
            .OrderBy(fma => fma.FormModule!.SectionNumber)
            .ToList();

        foreach (var fma in activeAssignments)
        {
            var moduleId = fma.FormModule!.Id.Value;
            var moduleCode = fma.FormModule.Code;
            string moduleStatus;

            if (moduleCode == DataCollectionFormModuleCodes.ServiceUsers)
            {
                moduleStatus = await GetServiceUsersModuleStatus(fma.FormModule.Id, organisationId, fma.DataCollectionId, cancellationToken);
            }
            else if (moduleCode == DataCollectionFormModuleCodes.WiderServiceUsers)
            {
                moduleStatus = await GetWiderServiceUsersModuleStatus(fma.FormModule.Id, organisationId, fma.DataCollectionId, cancellationToken);
            }
            else if (moduleCode == DataCollectionFormModuleCodes.OutcomeScores)
            {
                moduleStatus = await GetOutcomeScoresModuleStatus(fma.FormModule.Id, organisationId, fma.DataCollectionId, cancellationToken);
            }
            else
            {
                // Use OrganisationId and DataCollectionId to scope submissions
                var submissions = await formSubmissionRepository.GetByFormModuleOrganisationAndDataCollectionAsync(
                    fma.FormModule.Id, organisationId, fma.DataCollectionId, cancellationToken);

                var formSubmission = submissions.FirstOrDefault();
                moduleStatus = formSubmission != null
                    ? GetFormSubmissionStatus(formSubmission.Status)
                    : "Not started";
            }

            result.Add(new SubmissionFormModuleDto(
                moduleId,
                fma.FormModule.Code,
                fma.FormModule.SectionNumber,
                fma.FormModule.Name,
                fma.FormModule.Description,
                moduleStatus,
                fma.FormModule.AllowsMultipleActivities,
                new List<SubmissionSectionDto>(),
                sectionsToReview.Contains(moduleId)));
        }

        return result;
    }

    private async Task<string> GetServiceUsersModuleStatus(
        DataCollectionFormModuleId moduleId,
        OrganisationId organisationId,
        DataCollectionId dataCollectionId,
        CancellationToken cancellationToken)
    {
        var dataCollectionOption = await dataCollectionQueries.GetByIdAsync(dataCollectionId, cancellationToken);
        if (dataCollectionOption.IsNone)
        {
            return "Not started";
        }

        var dataCollection = dataCollectionOption.Match(dc => dc, () => null!);
        var allServices = await serviceQueries.GetByOrganisationIdAsync(organisationId, cancellationToken);
        var serviceSubmissions = await formSubmissionRepository.GetByFormModuleOrganisationDataCollectionAndEntityTypeAsync(
            moduleId, organisationId, dataCollectionId, "Service", cancellationToken);

        return ComputeServiceUsersModuleStatus(dataCollection, allServices, serviceSubmissions);
    }

    private static string ComputeServiceUsersModuleStatus(
        DataCollection dataCollection,
        IReadOnlyList<Service> allServices,
        IReadOnlyList<FormSubmission> serviceSubmissions)
    {
        var services = allServices
            .Where(s => ServiceQuarterlyInclusion.IsIncludedInQuarterlyServiceUsers(s, dataCollection.StartDate))
            .ToList();

        if (!services.Any())
        {
            return "Not started";
        }

        var completedCount = 0;
        var inProgressCount = 0;
        var submittedCount = 0;

        foreach (var service in services)
        {
            var submission = serviceSubmissions.FirstOrDefault(s => s.EntityId == service.Id.Value);
            if (submission != null)
            {
                var status = GetFormSubmissionStatus(submission.Status);
                if (status == "Completed")
                {
                    completedCount++;
                }
                else if (status == "Submitted")
                {
                    submittedCount++;
                }
                else if (status == "In progress")
                {
                    inProgressCount++;
                }
            }
        }

        if (completedCount == services.Count)
        {
            return "Completed";
        }

        if (submittedCount == services.Count)
        {
            return "Submitted";
        }

        if (completedCount > 0 || submittedCount > 0 || inProgressCount > 0)
        {
            return "In progress";
        }

        return "Not started";
    }

    private async Task<string> GetWiderServiceUsersModuleStatus(
        DataCollectionFormModuleId moduleId,
        OrganisationId organisationId,
        DataCollectionId dataCollectionId,
        CancellationToken cancellationToken)
    {
        var allServiceCategories = await serviceQueries.GetServiceCategoriesByOrganisationIdAsync(organisationId, cancellationToken);

        // Filter out categories where WSDM01 = "no" (not part of Start for Life Offer)
        var serviceCategories = allServiceCategories
            .Where(c =>
            {
                var wsdm01Answer = c.Answers.FirstOrDefault(a => a.QuestionCode == "WSDM01");
                return wsdm01Answer == null || wsdm01Answer.Value != "no";
            })
            .ToList();

        if (!serviceCategories.Any())
        {
            // No wider service categories selected in master data, so this module is considered complete
            return "Completed";
        }

        var categorySubmissions = await formSubmissionRepository.GetByFormModuleOrganisationDataCollectionAndEntityTypeAsync(
            moduleId, organisationId, dataCollectionId, "WiderServiceCategory", cancellationToken);

        var completedCount = 0;
        var inProgressCount = 0;
        var submittedCount = 0;

        foreach (var category in serviceCategories)
        {
            var submission = categorySubmissions.FirstOrDefault(s => s.EntityId == category.Id.Value);
            if (submission != null)
            {
                var status = GetFormSubmissionStatus(submission.Status);
                if (status == "Completed")
                {
                    completedCount++;
                }
                else if (status == "Submitted")
                {
                    submittedCount++;
                }
                else if (status == "In progress")
                {
                    inProgressCount++;
                }
            }
        }

        if (completedCount == serviceCategories.Count)
        {
            return "Completed";
        }

        if (submittedCount == serviceCategories.Count)
        {
            return "Submitted";
        }

        if (completedCount > 0 || submittedCount > 0 || inProgressCount > 0)
        {
            return "In progress";
        }

        return "Not started";
    }

    private async Task<string> GetOutcomeScoresModuleStatus(
        DataCollectionFormModuleId moduleId,
        OrganisationId organisationId,
        DataCollectionId dataCollectionId,
        CancellationToken cancellationToken)
    {
        // First check if Service Users module is completed - Outcome Scores depends on it
        var serviceUsersModuleOption = await formModuleQueries.GetByCodeWithFieldsAsync(
            DataCollectionFormModuleCodes.ServiceUsers, cancellationToken);

        string serviceUsersStatus = "Not started";
        if (serviceUsersModuleOption.IsSome)
        {
            var serviceUsersModule = serviceUsersModuleOption.Match(m => m, () => null!);
            serviceUsersStatus = await GetServiceUsersModuleStatus(
                serviceUsersModule.Id, organisationId, dataCollectionId, cancellationToken);

            // If Service Users is not completed or submitted, Outcome Scores cannot be determined yet
            if (serviceUsersStatus != "Completed" && serviceUsersStatus != "Submitted")
            {
                return "Not started";
            }
        }

        // Get all outcome score records for this organisation and data collection
        var allRecords = await formSubmissionRepository.GetByFormModuleOrganisationDataCollectionAndEntityTypeAsync(
            moduleId, organisationId, dataCollectionId, "OutcomeScore", cancellationToken);

        // Get services that require outcome scores and their expected record counts (from QSU14)
        var serviceRequirements = await GetServiceOutcomeScoreRequirementsAsync(
            organisationId, dataCollectionId, cancellationToken);

        return ComputeOutcomeScoresModuleStatus(serviceUsersStatus, serviceRequirements, allRecords);
    }

    /// <summary>
    /// Section 4 reaches a terminal state ("Completed" pre-submit, "Submitted" post-submit) once
    /// Section 2 (Service Users) is in a terminal state and each service requiring outcome scores has
    /// at least one record. The record check is aligned with the SubmitSubmission validation so the UI
    /// never reports "In progress" for a submission the backend would accept.
    /// </summary>
    private static string ComputeOutcomeScoresModuleStatus(
        string serviceUsersStatus,
        Dictionary<Guid, int> serviceRequirements,
        IReadOnlyList<FormSubmission> allRecords)
    {
        // If no services require outcome scores, return same status as Service Users
        if (serviceRequirements.Count == 0)
        {
            return serviceUsersStatus == "Submitted" ? "Submitted" : "Completed";
        }

        // If no records exist but services require them, not started
        if (allRecords.Count == 0)
        {
            return "Not started";
        }

        // Count completed records per service. "approved" is the pre-submission terminal state;
        // after final submission MarkAsSubmitted() flips every record to "submitted", so both count.
        var completedRecordsPerService = allRecords
            .Where(r => (r.Status.Value == "approved" || r.Status.Value == "submitted") && r.EntityId.HasValue && r.EntityId.Value != Guid.Empty)
            .GroupBy(r => r.EntityId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        // Section 4 reaches a terminal state when:
        // 1. Section 2 (Service Users) is in a terminal state ("Completed" pre-submit or "Submitted" post-submit)
        // 2. Each service requiring outcome scores has at least one record - aligned with the
        //    SubmitSubmission validation so the UI never blocks a submit the backend would accept.
        var isSection2Complete = serviceUsersStatus == "Completed" || serviceUsersStatus == "Submitted";

        var allServicesHaveRecords = serviceRequirements.All(kvp =>
            completedRecordsPerService.GetValueOrDefault(kvp.Key, 0) >= 1);

        if (isSection2Complete && allServicesHaveRecords)
        {
            // Mirror Section 2's terminal state: "Submitted" after final submission, otherwise "Completed".
            return serviceUsersStatus == "Submitted" ? "Submitted" : "Completed";
        }

        // If any records exist (even incomplete), status is "In progress"
        return "In progress";
    }

    private async Task<List<Guid>> GetServicesRequiringOutcomeScoresAsync(
        OrganisationId organisationId,
        DataCollectionId dataCollectionId,
        CancellationToken cancellationToken)
    {
        var serviceRequirements = await GetServiceOutcomeScoreRequirementsAsync(
            organisationId, dataCollectionId, cancellationToken);
        return serviceRequirements.Keys.ToList();
    }

    /// <summary>
    /// Gets the expected number of outcome score records per service based on QSU13 and QSU14 values.
    /// Returns a dictionary where key is ServiceId and value is the expected record count from QSU14.
    /// Only includes services where QSU13 = "yes".
    /// </summary>
    private async Task<Dictionary<Guid, int>> GetServiceOutcomeScoreRequirementsAsync(
        OrganisationId organisationId,
        DataCollectionId dataCollectionId,
        CancellationToken cancellationToken)
    {
        var serviceRequirements = new Dictionary<Guid, int>();

        // Get the Service Users module to find the QSU13 and QSU14 fields
        var serviceUsersModuleOption = await formModuleQueries.GetByCodeWithFieldsAsync(
            DataCollectionFormModuleCodes.ServiceUsers, cancellationToken);

        if (serviceUsersModuleOption.IsNone)
        {
            return serviceRequirements;
        }

        var serviceUsersModule = serviceUsersModuleOption.Match(m => m, () => null!);

        // Get all Service Users form submissions for this organisation and data collection
        var serviceSubmissions = await formSubmissionRepository.GetByFormModuleOrganisationDataCollectionAndEntityTypeAsync(
            serviceUsersModule.Id, organisationId, dataCollectionId, "Service", cancellationToken);

        return ComputeServiceOutcomeScoreRequirements(serviceUsersModule, serviceSubmissions);
    }

    /// <summary>
    /// Gets the expected number of outcome score records per service based on QSU13 and QSU14 values.
    /// Returns a dictionary where key is ServiceId and value is the expected record count from QSU14.
    /// Only includes services where QSU13 = "yes".
    /// </summary>
    private static Dictionary<Guid, int> ComputeServiceOutcomeScoreRequirements(
        DataCollectionFormModule serviceUsersModule,
        IReadOnlyList<FormSubmission> serviceSubmissions)
    {
        var serviceRequirements = new Dictionary<Guid, int>();

        // Find the QSU13 field (outcome scores collected question) and QSU14 field (number of users with outcome scores)
        var qsu13Field = serviceUsersModule.Fields.FirstOrDefault(f => f.FieldKey == "QSU13");
        var qsu14Field = serviceUsersModule.Fields.FirstOrDefault(f => f.FieldKey == "QSU14");

        if (qsu13Field == null)
        {
            return serviceRequirements;
        }

        // Check each service submission for QSU13 = "yes" and get QSU14 value
        foreach (var submission in serviceSubmissions)
        {
            var qsu13Value = submission.FieldValues.FirstOrDefault(fv => fv.FormFieldId == qsu13Field.Id);
            if (string.Equals(qsu13Value?.Value, "yes", StringComparison.OrdinalIgnoreCase) && submission.EntityId.HasValue)
            {
                var expectedCount = 1; // Default to 1 if QSU14 is not set or invalid

                if (qsu14Field != null)
                {
                    var qsu14Value = submission.FieldValues.FirstOrDefault(fv => fv.FormFieldId == qsu14Field.Id);
                    if (qsu14Value?.Value != null && int.TryParse(qsu14Value.Value, out var count) && count > 0)
                    {
                        expectedCount = count;
                    }
                }

                serviceRequirements[submission.EntityId.Value] = expectedCount;
            }
        }

        return serviceRequirements;
    }

    public Task<Option<SectionDetailDto>> GetSectionById(
        Guid organisationId,
        Guid dataCollectionId,
        Guid sectionId,
        CancellationToken cancellationToken)
    {
        // Sections are no longer used - FormModules link directly to FormDefinitions
        return Task.FromResult(Option<SectionDetailDto>.None);
    }

    public Task<Either<string, SectionDetailDto>> SaveSection(
        Guid organisationId,
        Guid dataCollectionId,
        Guid sectionId,
        SaveSectionRequest request,
        CancellationToken cancellationToken)
    {
        // Sections are no longer used - FormModules link directly to FormDefinitions
        return Task.FromResult(Either<string, SectionDetailDto>.Left("Sections are no longer supported. Use form modules instead."));
    }

    public async Task<Option<FormModuleDetailDto>> GetFormModuleById(
        Guid organisationId,
        Guid dataCollectionId,
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var dcId = new DataCollectionId(dataCollectionId);
        var modId = new DataCollectionFormModuleId(moduleId);

        var dataCollections = await dataCollectionQueries.GetByOrganisationIdAsync(orgId, cancellationToken);
        var dataCollection = dataCollections.FirstOrDefault(dc => dc.Id == dcId);

        if (dataCollection == null)
        {
            return Option<FormModuleDetailDto>.None;
        }

        var formModuleAssignment = dataCollection.FormModuleAssignments
            .FirstOrDefault(fma => fma.FormModuleId == modId);

        if (formModuleAssignment?.FormModule == null)
        {
            return Option<FormModuleDetailDto>.None;
        }

        var formModule = formModuleAssignment.FormModule;
        var moduleStatus = "Not started";
        var sections = new List<FormModuleSectionDto>();
        var fields = new List<FormModuleFieldDto>();
        var currentStep = 1;
        var totalSteps = 1;

        // Get saved field values for this module (scoped by organisation and data collection)
        var existingSubmissions = await formSubmissionRepository.GetByFormModuleOrganisationAndDataCollectionAsync(
            modId, orgId, dcId, cancellationToken);
        var formSubmission = existingSubmissions.FirstOrDefault();
        var savedValues = formSubmission?.FieldValues.ToDictionary(fv => fv.FormFieldId, fv => fv.Value)
            ?? new Dictionary<FormFieldId, string?>();
        var savedFieldValues = formSubmission?.FieldValues.ToDictionary(fv => fv.FormFieldId)
            ?? new Dictionary<FormFieldId, FormFieldValue>();

        if (formSubmission != null)
        {
            moduleStatus = GetFormSubmissionStatus(formSubmission.Status);
        }

        // Doubles as IsReadOnly below. Hoisted because the projection runs inside a synchronous
        // IfSome, and because a locked module is still rendered through the editable wizard - the
        // client only routes to the read-only summary on "Completed", never on "Submitted".
        var isLocked = await IsSubmissionLockedAsync(orgId, dcId, cancellationToken);

        // Get form module with sections and fields directly
        var formModuleWithDetails = await formModuleQueries.GetByIdWithFieldsAsync(formModule.Id, cancellationToken);

        formModuleWithDetails.IfSome(mod =>
        {
            totalSteps = mod.Sections.Count > 0 ? mod.Sections.Count : 1;

            sections.AddRange(mod.Sections
                .Where(s => s.IsActive)
                .OrderBy(s => s.SectionNumber)
                .Select(s => new FormModuleSectionDto(
                    s.Id.Value,
                    s.SectionNumber,
                    s.Title,
                    s.Description,
                    s.HelpText,
                    s.HelpUrl)));

            fields.AddRange(mod.Fields
                .Where(f => f.IsActive)
                .OrderBy(f => mod.Sections.FirstOrDefault(s => s.Id == f.FormSectionId)?.SectionNumber ?? 0)
                .ThenBy(f => f.DisplayOrder)
                .Select(f =>
                {
                    int? stepNumber = f.FormSectionId != null
                        ? mod.Sections.FirstOrDefault(s => s.Id == f.FormSectionId)?.SectionNumber
                        : null;

                    var savedValue = savedValues.TryGetValue(f.Id, out var val) ? val : f.DefaultValue;

                    var fieldValue = savedFieldValues.TryGetValue(f.Id, out var fv) ? fv : null;

                    return new FormModuleFieldDto(
                        f.Id.Value,
                        f.FieldKey,
                        AnswerSnapshotResolver.ResolveDisplayLabel(fieldValue, f, isLocked),
                        f.HelpText,
                        f.FieldType.ToString(),
                        f.IsRequired,
                        f.DisplayOrder,
                        stepNumber,
                        savedValue,
                        AnswerSnapshotResolver.ResolveDisplayValue(fieldValue, f),
                        AnswerSnapshotResolver.ResolveQuestionLabel(fieldValue),
                        f.ConditionalRules,
                        f.Configuration,
                        f.Placeholder,
                        f.Options.OrderBy(o => o.DisplayOrder).Select(o => new SectionFieldOptionDto(
                            o.Value,
                            o.Label,
                            o.DisplayOrder)).ToList());
                }));
        });

        return new FormModuleDetailDto(
            formModule.Id.Value,
            dataCollectionId,
            dataCollection.Name,
            formModule.Code,
            formModule.SectionNumber,
            formModule.Name,
            formModule.Description,
            moduleStatus,
            dataCollection.StartDate,
            dataCollection.GetEffectiveEndDate(orgId),
            currentStep,
            totalSteps,
            formModule.AllowsMultipleActivities,
            sections,
            fields,
            isLocked);
    }

    public async Task<Either<string, FormModuleDetailDto>> SaveFormModule(
        Guid organisationId,
        Guid dataCollectionId,
        Guid moduleId,
        SaveFormModuleRequest request,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var dcId = new DataCollectionId(dataCollectionId);
        var modId = new DataCollectionFormModuleId(moduleId);

        if (await IsSubmissionLockedAsync(orgId, dcId, cancellationToken))
        {
            return Either<string, FormModuleDetailDto>.Left(SubmissionLockedMessage);
        }

        var dataCollections = await dataCollectionQueries.GetByOrganisationIdAsync(orgId, cancellationToken);
        var dataCollection = dataCollections.FirstOrDefault(dc => dc.Id == dcId);

        if (dataCollection == null)
        {
            return Either<string, FormModuleDetailDto>.Left("Data collection not found");
        }

        var formModuleAssignment = dataCollection.FormModuleAssignments
            .FirstOrDefault(fma => fma.FormModuleId == modId);

        if (formModuleAssignment?.FormModule == null)
        {
            return Either<string, FormModuleDetailDto>.Left("Form module not found");
        }

        var formModule = formModuleAssignment.FormModule;

        // Get form module with fields
        var formModuleOption = await formModuleQueries.GetByIdWithFieldsAsync(modId, cancellationToken);

        if (formModuleOption.IsNone)
        {
            return Either<string, FormModuleDetailDto>.Left("Form module not found");
        }

        var formModuleWithFields = formModuleOption.Match(fm => fm, () => null!);

        // Get or create FormSubmission for this form module (scoped by organisation and data collection)
        var existingSubmissions = await formSubmissionRepository.GetByFormModuleOrganisationAndDataCollectionAsync(
            modId, orgId, dcId, cancellationToken);

        var existingSubmission = existingSubmissions.FirstOrDefault();

        FormSubmission formSubmission;
        if (existingSubmission != null)
        {
            formSubmission = existingSubmission;
        }
        else
        {
            formSubmission = FormSubmission.Create(modId, orgId, dcId);
            await formSubmissionRepository.AddAsync(formSubmission, cancellationToken);
        }

        // Save field values
        foreach (var fieldEntry in request.FieldValues)
        {
            var field = formModuleWithFields.Fields.FirstOrDefault(f => f.FieldKey == fieldEntry.Key);
            if (field != null)
            {
                formSubmission.SetFieldValue(field, Utility.HtmlDecodeNullableField(fieldEntry.Value));
            }
        }

        // Update status based on MarkComplete flag
        if (request.MarkComplete)
        {
            formSubmission.MarkAsComplete();
        }
        else
        {
            formSubmission.SaveAsDraft();
        }

        // Persist changes
        await formSubmissionRepository.UpdateAsync(formSubmission, cancellationToken);

        var result = await GetFormModuleById(organisationId, dataCollectionId, moduleId, cancellationToken);

        return result.Match<Either<string, FormModuleDetailDto>>(
            dto => Either<string, FormModuleDetailDto>.Right(dto),
            () => Either<string, FormModuleDetailDto>.Left("Failed to retrieve updated form module"));
    }

    public async Task<Either<string, bool>> DeleteFormModule(
        Guid organisationId,
        Guid dataCollectionId,
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var dcId = new DataCollectionId(dataCollectionId);
        var modId = new DataCollectionFormModuleId(moduleId);

        if (await IsSubmissionLockedAsync(orgId, dcId, cancellationToken))
        {
            return Either<string, bool>.Left(SubmissionLockedMessage);
        }

        // Get existing submissions for this form module (scoped by organisation and data collection)
        var existingSubmissions = await formSubmissionRepository.GetByFormModuleOrganisationAndDataCollectionAsync(
            modId, orgId, dcId, cancellationToken);

        var formSubmission = existingSubmissions.FirstOrDefault();

        if (formSubmission == null)
        {
            return Either<string, bool>.Left("No submission found to delete");
        }

        // Delete the form submission
        await formSubmissionRepository.DeleteAsync(formSubmission.Id, cancellationToken);

        return Either<string, bool>.Right(true);
    }

    public async Task<Either<string, FileUploadResultDto>> UploadFile(
        Guid organisationId,
        Guid submissionId,
        Guid moduleId,
        Microsoft.AspNetCore.Http.IFormFile file,
        CancellationToken cancellationToken)
    {
        if (await IsSubmissionLockedAsync(new OrganisationId(organisationId), new DataCollectionId(submissionId), cancellationToken))
        {
            return Either<string, FileUploadResultDto>.Left(SubmissionLockedMessage);
        }

        var validationResult = await fileValidationService.ValidateAsync(
            file,
            FileUploadProfile.SubmissionAttachment,
            cancellationToken);

        return await validationResult.MatchAsync<Either<string, FileUploadResultDto>>(
            async validated =>
            {
                await using var content = validated.Content;
                var blobPath = $"{organisationId}/{submissionId}/{moduleId}/{validated.SafeFileName}";

                var uploadOptions = new BlobWriteOptions(
                    ContentType: validated.ContentType,
                    ContentDisposition: $"attachment; filename=\"{validated.SafeFileName}\"",
                    Metadata: new Dictionary<string, string>
                    {
                        ["originalFileName"] = validated.OriginalFileName,
                    });

                var streamResult = await blobService.OpenWriteStream(
                    SubmissionFilesContainer,
                    blobPath,
                    uploadOptions,
                    cancellationToken);

                return await streamResult.MatchAsync<Either<string, FileUploadResultDto>>(
                    async writeStream =>
                    {
                        await using (writeStream)
                        {
                            await content.CopyToAsync(writeStream, cancellationToken);
                        }

                        return new FileUploadResultDto(validated.OriginalFileName, blobPath);
                    },
                    ex => Either<string, FileUploadResultDto>.Left($"Failed to upload file: {ex.Message}"));
            },
            error => error.Message);
    }

    public async Task<Option<ServiceUsersModuleDetailDto>> GetServiceUsersModule(
        Guid organisationId,
        Guid dataCollectionId,
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var modId = new DataCollectionFormModuleId(moduleId);
        var dcId = new DataCollectionId(dataCollectionId);

        var dataCollectionOption = await dataCollectionQueries.GetByIdAsync(dcId, cancellationToken);
        if (dataCollectionOption.IsNone)
        {
            return Option<ServiceUsersModuleDetailDto>.None;
        }

        var dataCollection = dataCollectionOption.Match(dc => dc, () => null!);

        var formModuleOption = await formModuleQueries.GetByIdWithFieldsAsync(modId, cancellationToken);
        if (formModuleOption.IsNone)
        {
            return Option<ServiceUsersModuleDetailDto>.None;
        }

        var formModule = formModuleOption.Match(fm => fm, () => null!);

        var allServices = await serviceQueries.GetByOrganisationIdAsync(orgId, cancellationToken);
        var services = allServices
            .Where(s => ServiceQuarterlyInclusion.IsIncludedInQuarterlyServiceUsers(s, dataCollection.StartDate))
            .ToList();

        var serviceSubmissions = await formSubmissionRepository.GetByFormModuleOrganisationDataCollectionAndEntityTypeAsync(
            modId, orgId, dcId, "Service", cancellationToken);

        var serviceStatusList = new List<ServiceFormStatusDto>();
        var completedCount = 0;

        foreach (var service in services)
        {
            var submission = serviceSubmissions.FirstOrDefault(s => s.EntityId == service.Id.Value);
            var status = submission != null ? GetFormSubmissionStatus(submission.Status) : "Not started";

            if (status == "Completed")
            {
                completedCount++;
            }

            var fundingAnswer = service.Answers.FirstOrDefault(a => a.QuestionCode == "SMD02");
            var fundingType = fundingAnswer?.DisplayValue ?? GetFundingTypeLabel(fundingAnswer?.Value);

            serviceStatusList.Add(new ServiceFormStatusDto(
                service.Id.Value,
                service.Name,
                fundingType,
                status));
        }

        var overallStatus = completedCount == 0 ? "Not started" :
            completedCount == services.Count ? "Completed" : "In progress";

        return new ServiceUsersModuleDetailDto(
            formModule.Id.Value,
            dataCollectionId,
            dataCollection.Name,
            formModule.Code,
            formModule.SectionNumber,
            formModule.Name,
            formModule.Description,
            overallStatus,
            services.Count,
            completedCount,
            serviceStatusList);
    }

    public async Task<Option<ServiceFormDetailDto>> GetServiceForm(
        Guid organisationId,
        Guid dataCollectionId,
        Guid moduleId,
        Guid serviceId,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var modId = new DataCollectionFormModuleId(moduleId);
        var dcId = new DataCollectionId(dataCollectionId);

        var dataCollectionOption = await dataCollectionQueries.GetByIdAsync(dcId, cancellationToken);
        if (dataCollectionOption.IsNone)
        {
            return Option<ServiceFormDetailDto>.None;
        }

        var dataCollection = dataCollectionOption.Match(dc => dc, () => null!);

        var formModuleOption = await formModuleQueries.GetByIdWithFieldsAsync(modId, cancellationToken);
        if (formModuleOption.IsNone)
        {
            return Option<ServiceFormDetailDto>.None;
        }

        var formModule = formModuleOption.Match(fm => fm, () => null!);

        var services = await serviceQueries.GetByOrganisationIdAsync(orgId, cancellationToken);
        var service = services.FirstOrDefault(s => s.Id.Value == serviceId);

        if (service == null)
        {
            return Option<ServiceFormDetailDto>.None;
        }

        if (!ServiceQuarterlyInclusion.IsIncludedInQuarterlyServiceUsers(service, dataCollection.StartDate))
        {
            return Option<ServiceFormDetailDto>.None;
        }

        var submissionOption = await formSubmissionRepository.GetByFormModuleDataCollectionAndServiceAsync(modId, dcId, serviceId, cancellationToken);
        var savedValues = submissionOption.Match(
            s => s.FieldValues.ToDictionary(fv => fv.FormFieldId, fv => fv.Value),
            () => new Dictionary<FormFieldId, string?>());

        var savedFieldValues = submissionOption.Match(
            s => s.FieldValues.ToDictionary(fv => fv.FormFieldId),
            () => new Dictionary<FormFieldId, FormFieldValue>());

        var status = submissionOption.Match(
            s => GetFormSubmissionStatus(s.Status),
            () => "Not started");

        var fundingAnswer = service.Answers.FirstOrDefault(a => a.QuestionCode == "SMD02");
        var fundingType = fundingAnswer?.DisplayValue ?? GetFundingTypeLabel(fundingAnswer?.Value);
        var isFunded = fundingAnswer?.Value == "0" || fundingAnswer?.Value == "1";

        // Once the collection is locked its answers are history and the question has to read as it
        // did when it was answered, whichever screen renders it.
        var isLocked = await IsSubmissionLockedAsync(orgId, dcId, cancellationToken);

        var sections = formModule.Sections
            .Where(s => s.IsActive)
            .OrderBy(s => s.SectionNumber)
            .Select(s => new FormModuleSectionDto(
                s.Id.Value,
                s.SectionNumber,
                s.Title,
                s.Description,
                s.HelpText,
                s.HelpUrl))
            .ToList();

        var fields = formModule.Fields
            .Where(f => f.IsActive)
            .OrderBy(f => formModule.Sections.FirstOrDefault(s => s.Id == f.FormSectionId)?.SectionNumber ?? 0)
            .ThenBy(f => f.DisplayOrder)
            .Select(f =>
            {
                int? stepNumber = f.FormSectionId != null
                    ? formModule.Sections.FirstOrDefault(s => s.Id == f.FormSectionId)?.SectionNumber
                    : null;

                var savedValue = savedValues.TryGetValue(f.Id, out var val) ? val : f.DefaultValue;

                var conditionalRules = f.ConditionalRules;
                if (!isFunded && conditionalRules != null && conditionalRules.Contains("\"fundingRequired\":true"))
                {
                    savedValue = "n/a";
                }

                var fieldValue = savedFieldValues.TryGetValue(f.Id, out var fv) ? fv : null;

                return new FormModuleFieldDto(
                    f.Id.Value,
                    f.FieldKey,
                    AnswerSnapshotResolver.ResolveDisplayLabel(fieldValue, f, isLocked),
                    f.HelpText,
                    f.FieldType.ToString(),
                    f.IsRequired,
                    f.DisplayOrder,
                    stepNumber,
                    savedValue,
                    AnswerSnapshotResolver.ResolveDisplayValue(fieldValue, f),
                    AnswerSnapshotResolver.ResolveQuestionLabel(fieldValue),
                    conditionalRules,
                    f.Configuration,
                    f.Placeholder,
                    f.Options.OrderBy(o => o.DisplayOrder).Select(o => new SectionFieldOptionDto(
                        o.Value,
                        o.Label,
                        o.DisplayOrder)).ToList());
            })
            .ToList();

        var serviceAnswers = service.Answers.ToDictionary(a => a.QuestionCode, a => a.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        return new ServiceFormDetailDto(
            service.Id.Value,
            service.Name,
            fundingType,
            formModule.Id.Value,
            status,
            sections,
            fields,
            serviceAnswers);
    }

    public async Task<Either<string, ServiceFormDetailDto>> SaveServiceForm(
        Guid organisationId,
        Guid dataCollectionId,
        Guid moduleId,
        Guid serviceId,
        SaveServiceFormRequest request,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var dcId = new DataCollectionId(dataCollectionId);
        var modId = new DataCollectionFormModuleId(moduleId);

        if (await IsSubmissionLockedAsync(orgId, dcId, cancellationToken))
        {
            return Either<string, ServiceFormDetailDto>.Left(SubmissionLockedMessage);
        }

        var dataCollectionOption = await dataCollectionQueries.GetByIdAsync(dcId, cancellationToken);
        if (dataCollectionOption.IsNone)
        {
            return Either<string, ServiceFormDetailDto>.Left("Data collection not found");
        }

        var dataCollection = dataCollectionOption.Match(dc => dc, () => null!);

        var formModuleOption = await formModuleQueries.GetByIdWithFieldsAsync(modId, cancellationToken);
        if (formModuleOption.IsNone)
        {
            return Either<string, ServiceFormDetailDto>.Left("Form module not found");
        }

        var formModule = formModuleOption.Match(fm => fm, () => null!);

        var services = await serviceQueries.GetByOrganisationIdAsync(orgId, cancellationToken);
        var service = services.FirstOrDefault(s => s.Id.Value == serviceId);

        if (service == null)
        {
            return Either<string, ServiceFormDetailDto>.Left("Service not found");
        }

        if (!ServiceQuarterlyInclusion.IsIncludedInQuarterlyServiceUsers(service, dataCollection.StartDate))
        {
            return Either<string, ServiceFormDetailDto>.Left("Service is not included in this quarterly collection");
        }

        var submissionOption = await formSubmissionRepository.GetByFormModuleDataCollectionAndServiceAsync(modId, dcId, serviceId, cancellationToken);

        FormSubmission formSubmission;
        if (submissionOption.IsSome)
        {
            formSubmission = submissionOption.Match(s => s, () => null!);
        }
        else
        {
            formSubmission = FormSubmission.Create(modId, orgId, dcId, "Service", serviceId);
            await formSubmissionRepository.AddAsync(formSubmission, cancellationToken);
        }

        foreach (var fieldEntry in request.FieldValues)
        {
            var field = formModule.Fields.FirstOrDefault(f => f.FieldKey == fieldEntry.Key);
            if (field != null)
            {
                formSubmission.SetFieldValue(field, Utility.HtmlDecodeNullableField(fieldEntry.Value));
            }
        }

        if (request.MarkComplete)
        {
            formSubmission.MarkAsComplete();
        }
        else
        {
            formSubmission.SaveAsDraft();
        }

        await formSubmissionRepository.UpdateAsync(formSubmission, cancellationToken);

        var result = await GetServiceForm(organisationId, dataCollectionId, moduleId, serviceId, cancellationToken);

        return result.Match<Either<string, ServiceFormDetailDto>>(
            dto => Either<string, ServiceFormDetailDto>.Right(dto),
            () => Either<string, ServiceFormDetailDto>.Left("Failed to retrieve updated service form"));
    }

    // Removes a service's answers for this quarter, leaving the Service itself and its answers in
    // every other collection untouched - the same record SaveServiceForm writes, keyed the same way.
    public async Task<Either<string, bool>> DeleteServiceForm(
        Guid organisationId,
        Guid dataCollectionId,
        Guid moduleId,
        Guid serviceId,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var dcId = new DataCollectionId(dataCollectionId);
        var modId = new DataCollectionFormModuleId(moduleId);

        var submissionOption = await formSubmissionRepository.GetByFormModuleDataCollectionAndServiceAsync(modId, dcId, serviceId, cancellationToken);
        if (submissionOption.IsNone)
        {
            return Either<string, bool>.Left("Record not found");
        }

        var submission = submissionOption.Match(s => s, () => null!);

        // That lookup is keyed on module, collection and service but not on organisation, so without
        // an ownership check an organisation admin could delete another organisation's answers by
        // guessing a service id. Ownership is settled on the Service, which always carries an
        // organisation, rather than on FormSubmission.OrganisationId, which is nullable and was added
        // by 20260309161714 without a backfill - keying on that alone would make a record created
        // before then undeletable by the very organisation that owns it. Same wording as the missing
        // case, so neither check can be used to probe for records.
        var services = await serviceQueries.GetByOrganisationIdAsync(orgId, cancellationToken);
        if (services.All(s => s.Id.Value != serviceId))
        {
            return Either<string, bool>.Left("Record not found");
        }

        if (submission.OrganisationId != null && submission.OrganisationId != orgId)
        {
            return Either<string, bool>.Left("Record not found");
        }

        if (await IsSubmissionLockedAsync(orgId, dcId, cancellationToken))
        {
            return Either<string, bool>.Left(SubmissionLockedMessage);
        }

        await formSubmissionRepository.DeleteAsync(submission.Id, cancellationToken);

        return Either<string, bool>.Right(true);
    }

    private static string GetFundingTypeLabel(string? value) => value switch
    {
        "0" => "Funded",
        "1" => "Partially funded",
        "2" => "Not programme funded",
        _ => "Unknown"
    };

    public async Task<Option<WiderServiceUsersModuleDetailDto>> GetWiderServiceUsersModule(
        Guid organisationId,
        Guid dataCollectionId,
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var dcId = new DataCollectionId(dataCollectionId);
        var modId = new DataCollectionFormModuleId(moduleId);

        var dataCollectionOption = await dataCollectionQueries.GetByIdWithLocalAuthoritiesAsync(dcId, cancellationToken);
        if (dataCollectionOption.IsNone)
        {
            return Option<WiderServiceUsersModuleDetailDto>.None;
        }

        var dataCollection = dataCollectionOption.Match(dc => dc, () => null!);

        var formModuleOption = await formModuleQueries.GetByIdWithFieldsAsync(modId, cancellationToken);
        if (formModuleOption.IsNone)
        {
            return Option<WiderServiceUsersModuleDetailDto>.None;
        }

        var formModule = formModuleOption.Match(fm => fm, () => null!);

        var allServiceCategories = await serviceQueries.GetServiceCategoriesByOrganisationIdAsync(orgId, cancellationToken);

        // Filter out categories where WSDM01 = "no" (not part of Start for Life Offer)
        var serviceCategories = allServiceCategories
            .Where(c =>
            {
                var wsdm01Answer = c.Answers.FirstOrDefault(a => a.QuestionCode == "WSDM01");
                return wsdm01Answer == null || wsdm01Answer.Value != "no";
            })
            .ToList();

        var categoryStatuses = new List<WiderServiceCategoryStatusDto>();
        var completedCount = 0;

        foreach (var category in serviceCategories)
        {
            var submissionOption = await formSubmissionRepository.GetByFormModuleDataCollectionAndServiceCategoryAsync(
                modId, dcId, category.Id.Value, cancellationToken);

            var status = "Not started";
            int? userCount = null;

            if (submissionOption.IsSome)
            {
                var submission = submissionOption.Match(s => s, () => null!);
                status = GetFormSubmissionStatus(submission.Status);

                var userCountField = submission.FieldValues.FirstOrDefault(fv =>
                    formModule.Fields.Any(f => f.Id == fv.FormFieldId && f.FieldKey == "QWSU01"));

                if (userCountField != null && int.TryParse(userCountField.Value, out var count))
                {
                    userCount = count;
                }

                if (submission.Status.Value == "approved")
                {
                    completedCount++;
                }
            }

            categoryStatuses.Add(new WiderServiceCategoryStatusDto(
                category.Id.Value,
                category.CategoryName,
                status,
                userCount));
        }

        return Option<WiderServiceUsersModuleDetailDto>.Some(new WiderServiceUsersModuleDetailDto(
            formModule.Id.Value,
            dataCollectionId,
            dataCollection.Name,
            formModule.Code,
            formModule.SectionNumber,
            formModule.Name,
            formModule.Description,
            serviceCategories.Count == 0 || completedCount == serviceCategories.Count ? "Completed" : "In progress",
            dataCollection.StartDate,
            dataCollection.GetEffectiveEndDate(orgId),
            serviceCategories.Count,
            completedCount,
            categoryStatuses));
    }

    public async Task<Option<WiderServiceCategoryFormDto>> GetWiderServiceCategoryForm(
        Guid organisationId,
        Guid dataCollectionId,
        Guid moduleId,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var dcId = new DataCollectionId(dataCollectionId);
        var modId = new DataCollectionFormModuleId(moduleId);

        var dataCollectionOption = await dataCollectionQueries.GetByIdWithLocalAuthoritiesAsync(dcId, cancellationToken);
        if (dataCollectionOption.IsNone)
        {
            return Option<WiderServiceCategoryFormDto>.None;
        }

        var dataCollection = dataCollectionOption.Match(dc => dc, () => null!);

        var formModuleOption = await formModuleQueries.GetByIdWithFieldsAsync(modId, cancellationToken);
        if (formModuleOption.IsNone)
        {
            return Option<WiderServiceCategoryFormDto>.None;
        }

        var formModule = formModuleOption.Match(fm => fm, () => null!);

        var serviceCategories = await serviceQueries.GetServiceCategoriesByOrganisationIdAsync(orgId, cancellationToken);
        var category = serviceCategories.FirstOrDefault(c => c.Id.Value == categoryId);

        if (category == null)
        {
            return Option<WiderServiceCategoryFormDto>.None;
        }

        var submissionOption = await formSubmissionRepository.GetByFormModuleDataCollectionAndServiceCategoryAsync(
            modId, dcId, categoryId, cancellationToken);

        var status = submissionOption.Match(
            s => GetFormSubmissionStatus(s.Status),
            () => "Not started");

        var savedValues = submissionOption.Match(
            s => s.FieldValues.ToDictionary(fv => fv.FormFieldId, fv => fv.Value),
            () => new Dictionary<FormFieldId, string?>());

        var savedFieldValues = submissionOption.Match(
            s => s.FieldValues.ToDictionary(fv => fv.FormFieldId),
            () => new Dictionary<FormFieldId, FormFieldValue>());

        // Once the collection is locked its answers are history and the question has to read as it
        // did when it was answered, whichever screen renders it.
        var isLocked = await IsSubmissionLockedAsync(orgId, dcId, cancellationToken);

        var sections = formModule.Sections
            .Where(s => s.IsActive)
            .OrderBy(s => s.SectionNumber)
            .Select(s => new FormModuleSectionDto(
                s.Id.Value,
                s.SectionNumber,
                s.Title,
                s.Description,
                s.HelpText,
                s.HelpUrl))
            .ToList();

        var fields = formModule.Fields
            .Where(f => f.IsActive)
            .OrderBy(f => formModule.Sections.FirstOrDefault(s => s.Id == f.FormSectionId)?.SectionNumber ?? 0)
            .ThenBy(f => f.DisplayOrder)
            .Select(f =>
            {
                int? stepNumber = f.FormSectionId != null
                    ? formModule.Sections.FirstOrDefault(s => s.Id == f.FormSectionId)?.SectionNumber
                    : null;

                var savedValue = savedValues.TryGetValue(f.Id, out var val) ? val : f.DefaultValue;

                var fieldValue = savedFieldValues.TryGetValue(f.Id, out var fv) ? fv : null;

                return new FormModuleFieldDto(
                    f.Id.Value,
                    f.FieldKey,
                    AnswerSnapshotResolver.ResolveDisplayLabel(fieldValue, f, isLocked),
                    f.HelpText,
                    f.FieldType.ToString(),
                    f.IsRequired,
                    f.DisplayOrder,
                    stepNumber,
                    savedValue,
                    AnswerSnapshotResolver.ResolveDisplayValue(fieldValue, f),
                    AnswerSnapshotResolver.ResolveQuestionLabel(fieldValue),
                    f.ConditionalRules,
                    f.Configuration,
                    f.Placeholder,
                    f.Options.OrderBy(o => o.DisplayOrder).Select(o => new SectionFieldOptionDto(
                        o.Value,
                        o.Label,
                        o.DisplayOrder)).ToList());
            })
            .ToList();

        return Option<WiderServiceCategoryFormDto>.Some(new WiderServiceCategoryFormDto(
            category.Id.Value,
            category.CategoryName,
            status,
            dataCollection.StartDate.ToString("d MMMM yyyy"),
            dataCollection.GetEffectiveEndDate(orgId).ToString("d MMMM yyyy"),
            sections,
            fields));
    }

    public async Task<Either<string, WiderServiceCategoryFormDto>> SaveWiderServiceCategoryForm(
        Guid organisationId,
        Guid dataCollectionId,
        Guid moduleId,
        Guid categoryId,
        SaveWiderServiceCategoryFormRequest request,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var dcId = new DataCollectionId(dataCollectionId);
        var modId = new DataCollectionFormModuleId(moduleId);

        if (await IsSubmissionLockedAsync(orgId, dcId, cancellationToken))
        {
            return Either<string, WiderServiceCategoryFormDto>.Left(SubmissionLockedMessage);
        }

        var formModuleOption = await formModuleQueries.GetByIdWithFieldsAsync(modId, cancellationToken);
        if (formModuleOption.IsNone)
        {
            return Either<string, WiderServiceCategoryFormDto>.Left("Form module not found");
        }

        var formModule = formModuleOption.Match(fm => fm, () => null!);

        var serviceCategories = await serviceQueries.GetServiceCategoriesByOrganisationIdAsync(orgId, cancellationToken);
        var category = serviceCategories.FirstOrDefault(c => c.Id.Value == categoryId);

        if (category == null)
        {
            return Either<string, WiderServiceCategoryFormDto>.Left("Service category not found");
        }

        var submissionOption = await formSubmissionRepository.GetByFormModuleDataCollectionAndServiceCategoryAsync(
            modId, dcId, categoryId, cancellationToken);

        FormSubmission formSubmission;
        if (submissionOption.IsSome)
        {
            formSubmission = submissionOption.Match(s => s, () => null!);
        }
        else
        {
            formSubmission = FormSubmission.Create(modId, orgId, dcId, "WiderServiceCategory", categoryId);
            await formSubmissionRepository.AddAsync(formSubmission, cancellationToken);
        }

        foreach (var fieldEntry in request.FieldValues)
        {
            var field = formModule.Fields.FirstOrDefault(f => f.FieldKey == fieldEntry.Key);
            if (field != null)
            {
                formSubmission.SetFieldValue(field, Utility.HtmlDecodeNullableField(fieldEntry.Value));
            }
        }

        if (request.MarkComplete)
        {
            formSubmission.MarkAsComplete();
        }
        else
        {
            formSubmission.SaveAsDraft();
        }

        await formSubmissionRepository.UpdateAsync(formSubmission, cancellationToken);

        var result = await GetWiderServiceCategoryForm(organisationId, dataCollectionId, moduleId, categoryId, cancellationToken);

        return result.Match<Either<string, WiderServiceCategoryFormDto>>(
            dto => Either<string, WiderServiceCategoryFormDto>.Right(dto),
            () => Either<string, WiderServiceCategoryFormDto>.Left("Failed to retrieve updated form"));
    }

    public async Task<Option<OutcomeScoresModuleDetailDto>> GetOutcomeScoresModule(
        Guid organisationId,
        Guid dataCollectionId,
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var dcId = new DataCollectionId(dataCollectionId);
        var modId = new DataCollectionFormModuleId(moduleId);

        var dataCollectionOption = await dataCollectionQueries.GetByIdAsync(dcId, cancellationToken);
        if (dataCollectionOption.IsNone)
        {
            return Option<OutcomeScoresModuleDetailDto>.None;
        }

        var dataCollection = dataCollectionOption.Match(dc => dc, () => null!);

        var formModuleOption = await formModuleQueries.GetByIdWithFieldsAsync(modId, cancellationToken);
        if (formModuleOption.IsNone)
        {
            return Option<OutcomeScoresModuleDetailDto>.None;
        }

        var formModule = formModuleOption.Match(fm => fm, () => null!);

        var services = await serviceQueries.GetByOrganisationIdAsync(orgId, cancellationToken);

        var allRecords = await formSubmissionRepository.GetByFormModuleOrganisationDataCollectionAndEntityTypeAsync(
            modId, orgId, dcId, "OutcomeScore", cancellationToken);

        // Clean up abandoned "Add record" stubs (no service ever selected) once they're stale enough that
        // nobody is still actively filling the form in, rather than letting them accumulate indefinitely.
        var staleAbandonedStubs = allRecords
            .Where(r => (!r.EntityId.HasValue || r.EntityId.Value == Guid.Empty) && r.CreatedAt < DateTime.UtcNow.AddHours(-24))
            .ToList();

        if (staleAbandonedStubs.Count > 0)
        {
            foreach (var stub in staleAbandonedStubs)
            {
                await formSubmissionRepository.DeleteAsync(stub.Id, cancellationToken);
            }

            var staleIds = staleAbandonedStubs.Select(s => s.Id).ToHashSet();
            allRecords = allRecords.Where(r => !staleIds.Contains(r.Id)).ToList();
        }

        // Filter out records with no service selected (incomplete/abandoned records)
        var validRecords = allRecords
            .Where(r => r.EntityId.HasValue && r.EntityId.Value != Guid.Empty)
            .ToList();

        var records = validRecords
            .OrderByDescending(r => r.DraftSavedAt)
            .Select((r, index) =>
            {
                var service = services.FirstOrDefault(s => s.Id.Value == r.EntityId);
                var serviceName = service?.Name;
                var anonymisedId = $"PPS_ID_{validRecords.Count - index:D5}";

                return new OutcomeScoreRecordDto(
                    r.Id.Value,
                    anonymisedId,
                    r.EntityId ?? Guid.Empty,
                    serviceName ?? string.Empty,
                    GetFormSubmissionStatus(r.Status),
                    r.DraftSavedAt);
            })
            .ToList();

        // Check Section 2 (Service Users) status and requirements once, and reuse below for
        // availability/isSection2Complete/overallStatus instead of each re-fetching the same data.
        var serviceUsersModuleOption = await formModuleQueries.GetByCodeWithFieldsAsync(
            DataCollectionFormModuleCodes.ServiceUsers, cancellationToken);

        var serviceUsersStatus = "Not started";
        var serviceRequirements = new Dictionary<Guid, int>();

        if (serviceUsersModuleOption.IsSome)
        {
            var serviceUsersModule = serviceUsersModuleOption.Match(m => m, () => null!);
            var serviceUsersSubmissions = await formSubmissionRepository.GetByFormModuleOrganisationDataCollectionAndEntityTypeAsync(
                serviceUsersModule.Id, orgId, dcId, "Service", cancellationToken);

            serviceUsersStatus = ComputeServiceUsersModuleStatus(dataCollection, services, serviceUsersSubmissions);
            serviceRequirements = ComputeServiceOutcomeScoreRequirements(serviceUsersModule, serviceUsersSubmissions);
        }

        // A submitted Section 2 is a terminal state too - treat it as complete so Section 4 doesn't
        // regress to "In progress" after the quarterly return is submitted.
        var isSection2Complete = serviceUsersStatus == "Completed" || serviceUsersStatus == "Submitted";

        // Only show services that have QSU13 = "yes" (collect pre/post outcome scores)
        var availableServices = services
            .Where(s => serviceRequirements.ContainsKey(s.Id.Value))
            .Select(s => new AvailableServiceDto(s.Id.Value, s.Name))
            .ToList();

        // Count completed records per service. Include "submitted" so the per-service actual counts
        // still reflect the data after final submission (MarkAsSubmitted flips approved -> submitted).
        var completedRecordsPerService = validRecords
            .Where(r => r.Status.Value == "approved" || r.Status.Value == "submitted")
            .GroupBy(r => r.EntityId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        // Build service requirements list with actual vs expected counts
        var serviceRequirementsList = serviceRequirements
            .Select(kvp =>
            {
                var service = services.FirstOrDefault(s => s.Id.Value == kvp.Key);
                var actualCount = completedRecordsPerService.GetValueOrDefault(kvp.Key, 0);
                return new ServiceRecordRequirementDto(
                    kvp.Key,
                    service?.Name ?? "Unknown Service",
                    kvp.Value,
                    actualCount,
                    actualCount == kvp.Value);
            })
            .ToList();

        // Calculate total expected records
        var totalExpectedRecords = serviceRequirements.Values.Sum();

        // Determine overall status
        var overallStatus = serviceUsersModuleOption.IsSome && serviceUsersStatus != "Completed" && serviceUsersStatus != "Submitted"
            ? "Not started"
            : ComputeOutcomeScoresModuleStatus(serviceUsersStatus, serviceRequirements, allRecords);

        return new OutcomeScoresModuleDetailDto(
            formModule.Id.Value,
            dataCollectionId,
            dataCollection.Name,
            formModule.Code,
            formModule.SectionNumber,
            formModule.Name,
            formModule.Description,
            overallStatus,
            records.Count,
            totalExpectedRecords,
            isSection2Complete,
            records,
            availableServices,
            serviceRequirementsList);
    }

    public async Task<Either<string, OutcomeScoreFormDetailDto>> CreateOutcomeScoreRecord(
        Guid organisationId,
        Guid dataCollectionId,
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var dcId = new DataCollectionId(dataCollectionId);
        var modId = new DataCollectionFormModuleId(moduleId);

        if (await IsSubmissionLockedAsync(orgId, dcId, cancellationToken))
        {
            return Either<string, OutcomeScoreFormDetailDto>.Left(SubmissionLockedMessage);
        }

        var formModuleOption = await formModuleQueries.GetByIdWithFieldsAsync(modId, cancellationToken);
        if (formModuleOption.IsNone)
        {
            return Either<string, OutcomeScoreFormDetailDto>.Left("Form module not found");
        }

        var formModule = formModuleOption.Match(fm => fm, () => null!);

        var formSubmission = FormSubmission.Create(modId, orgId, dcId, "OutcomeScore", Guid.Empty);
        await formSubmissionRepository.AddAsync(formSubmission, cancellationToken);

        var services = await serviceQueries.GetByOrganisationIdAsync(orgId, cancellationToken);

        // Get services that require outcome scores based on QSU13 answer in Service Users module
        var servicesRequiringOutcomeScores = await GetServicesRequiringOutcomeScoresAsync(
            orgId, dcId, cancellationToken);

        // Only show services that have QSU13 = "yes" (collect pre/post outcome scores)
        var availableServices = services
            .Where(s => servicesRequiringOutcomeScores.Contains(s.Id.Value))
            .Select(s => new AvailableServiceDto(s.Id.Value, s.Name))
            .ToList();

        var sections = formModule.Sections
            .Where(s => s.IsActive)
            .OrderBy(s => s.SectionNumber)
            .Select(s => new FormModuleSectionDto(
                s.Id.Value,
                s.SectionNumber,
                s.Title,
                s.Description,
                s.HelpText,
                s.HelpUrl))
            .ToList();

        var fields = formModule.Fields
            .Where(f => f.IsActive)
            .OrderBy(f => formModule.Sections.FirstOrDefault(s => s.Id == f.FormSectionId)?.SectionNumber ?? 0)
            .ThenBy(f => f.DisplayOrder)
            .Select(f =>
            {
                int? stepNumber = f.FormSectionId != null
                    ? formModule.Sections.FirstOrDefault(s => s.Id == f.FormSectionId)?.SectionNumber
                    : null;

                return new FormModuleFieldDto(
                    f.Id.Value,
                    f.FieldKey,
                    f.Label,
                    f.HelpText,
                    f.FieldType.ToString(),
                    f.IsRequired,
                    f.DisplayOrder,
                    stepNumber,
                    f.DefaultValue,
                    null,
                    null,
                    f.ConditionalRules,
                    f.Configuration,
                    f.Placeholder,
                    f.Options.OrderBy(o => o.DisplayOrder).Select(o => new SectionFieldOptionDto(
                        o.Value,
                        o.Label,
                        o.DisplayOrder)).ToList());
            })
            .ToList();

        return Either<string, OutcomeScoreFormDetailDto>.Right(new OutcomeScoreFormDetailDto(
            formSubmission.Id.Value,
            formModule.Id.Value,
            null,
            null,
            "Draft",
            sections,
            fields,
            availableServices));
    }

    public async Task<Option<OutcomeScoreFormDetailDto>> GetOutcomeScoreRecord(
        Guid organisationId,
        Guid dataCollectionId,
        Guid moduleId,
        Guid recordId,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var modId = new DataCollectionFormModuleId(moduleId);
        var submissionId = new FormSubmissionId(recordId);

        var formModuleOption = await formModuleQueries.GetByIdWithFieldsAsync(modId, cancellationToken);
        if (formModuleOption.IsNone)
        {
            return Option<OutcomeScoreFormDetailDto>.None;
        }

        var formModule = formModuleOption.Match(fm => fm, () => null!);

        var submissionOption = await formSubmissionRepository.GetByIdWithValuesAsync(submissionId, cancellationToken);
        if (submissionOption.IsNone)
        {
            return Option<OutcomeScoreFormDetailDto>.None;
        }

        var submission = submissionOption.Match(s => s, () => null!);

        // Verify the record belongs to the requesting organisation
        if (submission.OrganisationId != orgId)
        {
            return Option<OutcomeScoreFormDetailDto>.None;
        }

        var savedValues = submission.FieldValues.ToDictionary(fv => fv.FormFieldId, fv => fv.Value);
        var savedFieldValues = submission.FieldValues.ToDictionary(fv => fv.FormFieldId);

        var services = await serviceQueries.GetByOrganisationIdAsync(orgId, cancellationToken);
        var service = services.FirstOrDefault(s => s.Id.Value == submission.EntityId);

        // Get services that require outcome scores based on QSU13 answer in Service Users module
        var servicesRequiringOutcomeScores = await GetServicesRequiringOutcomeScoresAsync(
            orgId, new DataCollectionId(dataCollectionId), cancellationToken);

        // Only show services that have QSU13 = "yes" (collect pre/post outcome scores)
        var availableServices = services
            .Where(s => servicesRequiringOutcomeScores.Contains(s.Id.Value))
            .Select(s => new AvailableServiceDto(s.Id.Value, s.Name))
            .ToList();

        // Once the collection is locked its answers are history and the question has to read as it
        // did when it was answered, whichever screen renders it.
        var isLocked = await IsSubmissionLockedAsync(orgId, new DataCollectionId(dataCollectionId), cancellationToken);

        var sections = formModule.Sections
            .Where(s => s.IsActive)
            .OrderBy(s => s.SectionNumber)
            .Select(s => new FormModuleSectionDto(
                s.Id.Value,
                s.SectionNumber,
                s.Title,
                s.Description,
                s.HelpText,
                s.HelpUrl))
            .ToList();

        var fields = formModule.Fields
            .Where(f => f.IsActive)
            .OrderBy(f => formModule.Sections.FirstOrDefault(s => s.Id == f.FormSectionId)?.SectionNumber ?? 0)
            .ThenBy(f => f.DisplayOrder)
            .Select(f =>
            {
                int? stepNumber = f.FormSectionId != null
                    ? formModule.Sections.FirstOrDefault(s => s.Id == f.FormSectionId)?.SectionNumber
                    : null;

                var savedValue = savedValues.TryGetValue(f.Id, out var val) ? val : f.DefaultValue;

                var fieldValue = savedFieldValues.TryGetValue(f.Id, out var fv) ? fv : null;

                return new FormModuleFieldDto(
                    f.Id.Value,
                    f.FieldKey,
                    AnswerSnapshotResolver.ResolveDisplayLabel(fieldValue, f, isLocked),
                    f.HelpText,
                    f.FieldType.ToString(),
                    f.IsRequired,
                    f.DisplayOrder,
                    stepNumber,
                    savedValue,
                    AnswerSnapshotResolver.ResolveDisplayValue(fieldValue, f),
                    AnswerSnapshotResolver.ResolveQuestionLabel(fieldValue),
                    f.ConditionalRules,
                    f.Configuration,
                    f.Placeholder,
                    f.Options.OrderBy(o => o.DisplayOrder).Select(o => new SectionFieldOptionDto(
                        o.Value,
                        o.Label,
                        o.DisplayOrder)).ToList());
            })
            .ToList();

        return new OutcomeScoreFormDetailDto(
            submission.Id.Value,
            formModule.Id.Value,
            submission.EntityId != Guid.Empty ? submission.EntityId : null,
            service?.Name,
            GetFormSubmissionStatus(submission.Status),
            sections,
            fields,
            availableServices);
    }

    public async Task<Either<string, OutcomeScoreFormDetailDto>> SaveOutcomeScoreRecord(
        Guid organisationId,
        Guid dataCollectionId,
        Guid moduleId,
        Guid recordId,
        SaveOutcomeScoreFormRequest request,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var modId = new DataCollectionFormModuleId(moduleId);
        var submissionId = new FormSubmissionId(recordId);

        var formModuleOption = await formModuleQueries.GetByIdWithFieldsAsync(modId, cancellationToken);
        if (formModuleOption.IsNone)
        {
            return Either<string, OutcomeScoreFormDetailDto>.Left("Form module not found");
        }

        var formModule = formModuleOption.Match(fm => fm, () => null!);

        var submissionOption = await formSubmissionRepository.GetByIdWithValuesAsync(submissionId, cancellationToken);
        if (submissionOption.IsNone)
        {
            return Either<string, OutcomeScoreFormDetailDto>.Left("Record not found");
        }

        var formSubmission = submissionOption.Match(s => s, () => null!);

        // Verify the record belongs to the requesting organisation
        if (formSubmission.OrganisationId != orgId)
        {
            return Either<string, OutcomeScoreFormDetailDto>.Left("Record not found");
        }

        if (formSubmission.DataCollectionId != null &&
            await IsSubmissionLockedAsync(orgId, formSubmission.DataCollectionId, cancellationToken))
        {
            return Either<string, OutcomeScoreFormDetailDto>.Left(SubmissionLockedMessage);
        }

        if (request.FieldValues.TryGetValue("PPS01", out var serviceIdStr) && Guid.TryParse(serviceIdStr, out var serviceId))
        {
            formSubmission.LinkToEntity("OutcomeScore", serviceId);
        }

        foreach (var fieldEntry in request.FieldValues)
        {
            var field = formModule.Fields.FirstOrDefault(f => f.FieldKey == fieldEntry.Key);
            if (field != null)
            {
                formSubmission.SetFieldValue(field, Utility.HtmlDecodeNullableField(fieldEntry.Value));
            }
        }

        if (request.MarkComplete)
        {
            formSubmission.MarkAsComplete();
        }
        else
        {
            formSubmission.SaveAsDraft();
        }

        await formSubmissionRepository.UpdateAsync(formSubmission, cancellationToken);

        var result = await GetOutcomeScoreRecord(organisationId, dataCollectionId, moduleId, recordId, cancellationToken);

        return result.Match<Either<string, OutcomeScoreFormDetailDto>>(
            dto => Either<string, OutcomeScoreFormDetailDto>.Right(dto),
            () => Either<string, OutcomeScoreFormDetailDto>.Left("Failed to retrieve updated outcome score record"));
    }

    public async Task<Either<string, bool>> DeleteOutcomeScoreRecord(
        Guid organisationId,
        Guid dataCollectionId,
        Guid moduleId,
        Guid recordId,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var submissionId = new FormSubmissionId(recordId);

        var submissionOption = await formSubmissionRepository.GetByIdAsync(submissionId, cancellationToken);
        if (submissionOption.IsNone)
        {
            return Either<string, bool>.Left("Record not found");
        }

        var submission = submissionOption.Match(s => s, () => null!);

        // Verify the record belongs to the requesting organisation
        if (submission.OrganisationId != orgId)
        {
            return Either<string, bool>.Left("Record not found");
        }

        if (submission.DataCollectionId != null &&
            await IsSubmissionLockedAsync(orgId, submission.DataCollectionId, cancellationToken))
        {
            return Either<string, bool>.Left(SubmissionLockedMessage);
        }

        await formSubmissionRepository.DeleteAsync(submissionId, cancellationToken);

        return Either<string, bool>.Right(true);
    }

    public async Task<Either<string, int>> PurgeAllSubmissions(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        var count = await formSubmissionRepository.DeleteAllAsync(cancellationToken);
        return Either<string, int>.Right(count);
    }

    private const string SubmissionLockedMessage = "This quarter can no longer be edited - it has either already been submitted or its submission window has closed. Reopen it to make changes.";

    private async Task<bool> IsDataCollectionClosedAsync(
        OrganisationId organisationId,
        DataCollectionId dataCollectionId,
        CancellationToken cancellationToken)
    {
        var dataCollectionOption = await dataCollectionQueries.GetByIdWithLocalAuthoritiesAsync(dataCollectionId, cancellationToken);
        return dataCollectionOption.Match(
            dc => dc.IsClosedFor(organisationId),
            () => false);
    }

    private async Task<bool> IsSubmissionLockedAsync(
        OrganisationId organisationId,
        DataCollectionId dataCollectionId,
        CancellationToken cancellationToken)
    {
        if (await IsDataCollectionClosedAsync(organisationId, dataCollectionId, cancellationToken))
        {
            return true;
        }

        var existingSubmission = await dataCollectionSubmissionRepository.GetByDataCollectionAndOrganisationAsync(
            dataCollectionId, organisationId, cancellationToken);

        return existingSubmission.Match(s => s.IsLockedForEditing(), () => false);
    }

    public async Task<Either<string, SubmissionDetailDto>> ReopenSubmission(
        Guid organisationId,
        Guid dataCollectionId,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var dcId = new DataCollectionId(dataCollectionId);

        var existingSubmission = await dataCollectionSubmissionRepository.GetByDataCollectionAndOrganisationAsync(
            dcId, orgId, cancellationToken);

        if (existingSubmission.IsNone)
        {
            return Either<string, SubmissionDetailDto>.Left("This data collection has not been submitted yet");
        }

        var dcSubmission = existingSubmission.Match(s => s, () => null!);

        var canReopen = dcSubmission.Status == DataCollectionSubmissionStatus.Submitted ||
            dcSubmission.Status == DataCollectionSubmissionStatus.Rejected ||
            dcSubmission.Status == DataCollectionSubmissionStatus.RequiresChanges;

        if (!canReopen)
        {
            return Either<string, SubmissionDetailDto>.Left("Only a submitted, rejected, or requires-changes data collection can be reopened");
        }

        if (await IsDataCollectionClosedAsync(orgId, dcId, cancellationToken))
        {
            return Either<string, SubmissionDetailDto>.Left("This quarter's submission window has already closed and cannot be reopened");
        }

        dcSubmission.Reopen();
        await dataCollectionSubmissionRepository.UpdateAsync(dcSubmission, cancellationToken);

        var result = await GetById(organisationId, dataCollectionId, cancellationToken);
        return result.Match<Either<string, SubmissionDetailDto>>(
            dto => Either<string, SubmissionDetailDto>.Right(dto),
            () => Either<string, SubmissionDetailDto>.Left("Failed to retrieve updated submission"));
    }

    public async Task<Either<string, SubmissionDetailDto>> SubmitSubmission(
        Guid organisationId,
        Guid dataCollectionId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var dcId = new DataCollectionId(dataCollectionId);

        // Get the data collection with its assigned form modules
        var dataCollectionOption = await dataCollectionQueries.GetByIdWithLocalAuthoritiesAsync(dcId, cancellationToken);
        if (dataCollectionOption.IsNone)
        {
            return Either<string, SubmissionDetailDto>.Left("Data collection not found");
        }

        var dataCollection = dataCollectionOption.Match(dc => dc, () => null!);

        // Get only the form modules assigned to this specific data collection
        var assignedFormModules = dataCollection.FormModuleAssignments
            .Where(fma => fma.FormModule != null && fma.FormModule.IsActive)
            .Select(fma => fma.FormModule!)
            .ToList();

        // Get all form submissions for this organisation and data collection
        var allSubmissions = await formSubmissionRepository.GetByOrganisationIdAsync(orgId, cancellationToken);
        var dcSubmissions = allSubmissions.Where(s => s.DataCollectionId == dcId).ToList();

        // Fetched once and reused below when creating/updating the parent record after the
        // completeness checks pass, so this doesn't cost a second round trip.
        var dcSubmissionOption = await dataCollectionSubmissionRepository.GetByDataCollectionAndOrganisationAsync(
            dcId, orgId, cancellationToken);
        var existingDcSubmission = dcSubmissionOption.MatchUnsafe(s => s, () => (DataCollectionSubmission?)null);

        // Sections DHSC flagged on the reopen that produced the current round - advisory everywhere
        // else (the local authority can still edit any section), but resubmitting must not be
        // possible while a flagged section is still just the carried-over "submitted" state from
        // before the rejection. Empty once the submission is no longer Rejected, since a flag from
        // an earlier round no longer applies to the round being submitted now.
        var sectionsRequiringCompletion = existingDcSubmission?.Status == DataCollectionSubmissionStatus.Rejected
            ? existingDcSubmission.SectionsToReview.ToHashSet()
            : [];

        // A record counts as complete for submission when it is "approved" (the pre-submission
        // terminal state) or "submitted". After a quarter is submitted MarkAsSubmitted() flips every
        // record to "submitted"; reopening the quarter resets the DataCollectionSubmission but leaves
        // those records at "submitted", so a re-submit must accept both states - otherwise the UI shows
        // every section "Completed" (its status calc counts both) while this gate rejects with a 400.
        // The one exception is a module DHSC specifically flagged for review: that one must actually
        // be re-completed (approved), not just still sitting at its pre-rejection "submitted" state.
        static bool IsComplete(FormSubmission submission, bool mustBeReCompleted) =>
            submission.Status.Value == "approved" || (!mustBeReCompleted && submission.Status.Value == "submitted");

        // Check if all assigned modules are complete
        foreach (var module in assignedFormModules)
        {
            var moduleSubmissions = dcSubmissions.Where(s => s.FormModuleId == module.Id).ToList();
            var moduleMustBeReCompleted = sectionsRequiringCompletion.Contains(module.Id.Value);

            // Special handling for Service Users module - every included service needs its own approved submission,
            // not just whatever submissions happen to already exist (which is all the generic fallback checks below).
            if (module.Code == DataCollectionFormModuleCodes.ServiceUsers)
            {
                var allServices = await serviceQueries.GetByOrganisationIdAsync(orgId, cancellationToken);
                var includedServices = allServices
                    .Where(s => ServiceQuarterlyInclusion.IsIncludedInQuarterlyServiceUsers(s, dataCollection.StartDate))
                    .ToList();

                if (includedServices.Count == 0)
                {
                    // No services in scope this quarter, so this module is considered complete
                    continue;
                }

                var serviceSubmissions = moduleSubmissions.Where(s => s.EntityType == "Service" && IsComplete(s, moduleMustBeReCompleted)).ToList();
                var servicesWithSubmissions = serviceSubmissions.Where(s => s.EntityId.HasValue).Select(s => s.EntityId!.Value).Distinct().ToHashSet();
                var allServicesHaveSubmissions = includedServices.All(s => servicesWithSubmissions.Contains(s.Id.Value));

                if (!allServicesHaveSubmissions)
                {
                    return Either<string, SubmissionDetailDto>.Left($"Module '{module.Name}' is not complete - not all services have been submitted");
                }

                continue;
            }

            // Special handling for Wider Service Users module - check if any wider service categories exist
            if (module.Code == DataCollectionFormModuleCodes.WiderServiceUsers)
            {
                var allServiceCategories = await serviceQueries.GetServiceCategoriesByOrganisationIdAsync(orgId, cancellationToken);

                // Filter out categories where WSDM01 = "no" (not part of Start for Life Offer)
                var widerServiceCategories = allServiceCategories
                    .Where(c =>
                    {
                        var wsdm01Answer = c.Answers.FirstOrDefault(a => a.QuestionCode == "WSDM01");
                        return wsdm01Answer == null || wsdm01Answer.Value != "no";
                    })
                    .ToList();

                if (widerServiceCategories.Count == 0)
                {
                    // No wider service categories selected, so this module is considered complete
                    continue;
                }

                // For Wider Service Users, check that all categories have approved submissions
                var categorySubmissions = moduleSubmissions.Where(s => s.EntityType == "WiderServiceCategory" && IsComplete(s, moduleMustBeReCompleted)).ToList();
                var categoriesWithSubmissions = categorySubmissions.Where(s => s.EntityId.HasValue).Select(s => s.EntityId!.Value).Distinct().ToHashSet();
                var allCategoriesHaveSubmissions = widerServiceCategories.All(c => categoriesWithSubmissions.Contains(c.Id.Value));

                if (!allCategoriesHaveSubmissions)
                {
                    return Either<string, SubmissionDetailDto>.Left($"Module '{module.Name}' is not complete - not all wider service categories have been submitted");
                }

                continue;
            }

            // Special handling for Outcome Scores module - check if any services require scores
            if (module.Code == DataCollectionFormModuleCodes.OutcomeScores)
            {
                var servicesRequiringScores = await GetServicesRequiringOutcomeScoresAsync(orgId, dcId, cancellationToken);
                if (servicesRequiringScores.Count == 0)
                {
                    // No services require outcome scores, so this module is considered complete
                    continue;
                }

                // For Outcome Scores, check that all services requiring scores have at least one approved submission
                var outcomeSubmissions = moduleSubmissions.Where(s => s.EntityType == "OutcomeScore" && IsComplete(s, moduleMustBeReCompleted)).ToList();
                var servicesWithScores = outcomeSubmissions.Where(s => s.EntityId.HasValue).Select(s => s.EntityId!.Value).Distinct().ToHashSet();
                var allServicesHaveScores = servicesRequiringScores.All(serviceId => servicesWithScores.Contains(serviceId));

                if (!allServicesHaveScores)
                {
                    return Either<string, SubmissionDetailDto>.Left($"Module '{module.Name}' is not complete - not all services have outcome scores");
                }

                continue;
            }

            if (!moduleSubmissions.Any())
            {
                return Either<string, SubmissionDetailDto>.Left($"Module '{module.Name}' has not been started");
            }

            var allComplete = moduleSubmissions.All(s => IsComplete(s, moduleMustBeReCompleted));
            if (!allComplete)
            {
                return Either<string, SubmissionDetailDto>.Left($"Module '{module.Name}' is not complete");
            }
        }

        // Mark all form submissions as submitted (they're already approved/complete, this is the final submission).
        // Skip orphaned records with no entity attached (e.g. an Outcome Scores "Add record" click that was
        // never followed through to a service selection) - there's nothing real to submit for those.
        // All writes below are staged (saveChanges: false) and committed in one SaveChangesAsync call at the
        // end, so the whole submission is atomic: a failure/timeout part-way through leaves nothing committed,
        // rather than some records flipping to "submitted" while others are left behind.
        foreach (var submission in dcSubmissions.Where(s => !s.EntityId.HasValue || s.EntityId.Value != Guid.Empty))
        {
            submission.MarkAsSubmitted();
            await formSubmissionRepository.UpdateAsync(submission, cancellationToken, saveChanges: false);
        }

        // Create or update the DataCollectionSubmission record for admin visibility. Reuses the
        // tracked entity fetched above for the SectionsToReview check rather than querying again.
        var submittedById = new UserId(userId);

        if (existingDcSubmission != null)
        {
            existingDcSubmission.Submit(submittedById);
            await dataCollectionSubmissionRepository.UpdateAsync(existingDcSubmission, cancellationToken, saveChanges: false);
        }
        else
        {
            var dcSubmission = DataCollectionSubmission.Create(dcId, orgId);
            dcSubmission.Submit(submittedById);
            await dataCollectionSubmissionRepository.AddAsync(dcSubmission, cancellationToken, saveChanges: false);
        }

        await formSubmissionRepository.SaveChangesAsync(cancellationToken);

        // Return the updated submission detail
        var result = await GetById(organisationId, dataCollectionId, cancellationToken);
        return result.Match<Either<string, SubmissionDetailDto>>(
            dto => Either<string, SubmissionDetailDto>.Right(dto),
            () => Either<string, SubmissionDetailDto>.Left("Failed to retrieve updated submission"));
    }

    // ── Admin support tools (BLUM-7047) ─────────────────────────────────────────
    // Detects/repairs a submission left orphaned by a SubmitSubmission call that was cut off
    // before it reached the parent DataCollectionSubmission write. Detection is deliberately
    // narrow: a legitimately reopened submission also has "submitted" children with no parent in
    // Submitted/Approved status (Reopen() flips only the parent, never the children), so we key
    // off the parent record's *existence*, not its status - a missing parent row can only happen
    // if the loop never reached the end of SubmitSubmission, which is exactly the crash scenario.

    private const string OrphanedSubmitRevertNote =
        "Reverted by admin reset: SubmitSubmission was cut off before the parent DataCollectionSubmission was created (BLUM-7047).";

    public async Task<SubmissionIntegrityDto> GetSubmissionIntegrity(
        Guid organisationId,
        Guid dataCollectionId,
        CancellationToken cancellationToken)
    {
        var orphaned = await GetOrphanedSubmittedFormSubmissions(organisationId, dataCollectionId, cancellationToken);

        var orphanedIds = orphaned.Select(s => s.Id.Value).ToList();
        return new SubmissionIntegrityDto(orphanedIds.Count > 0, orphanedIds.Count, orphanedIds);
    }

    public async Task<ResetCorruptedSubmissionResultDto> ResetCorruptedSubmission(
        Guid organisationId,
        Guid dataCollectionId,
        CancellationToken cancellationToken)
    {
        var orphaned = await GetOrphanedSubmittedFormSubmissions(organisationId, dataCollectionId, cancellationToken);

        if (orphaned.Count == 0)
        {
            return new ResetCorruptedSubmissionResultDto(false, 0);
        }

        foreach (var submission in orphaned)
        {
            submission.RevertOrphanedSubmit(OrphanedSubmitRevertNote);
            await formSubmissionRepository.UpdateAsync(submission, cancellationToken, saveChanges: false);
        }

        await formSubmissionRepository.SaveChangesAsync(cancellationToken);

        return new ResetCorruptedSubmissionResultDto(true, orphaned.Count);
    }

    private async Task<IReadOnlyList<FormSubmission>> GetOrphanedSubmittedFormSubmissions(
        Guid organisationId,
        Guid dataCollectionId,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var dcId = new DataCollectionId(dataCollectionId);

        var allSubmissions = await formSubmissionRepository.GetByOrganisationIdAsync(orgId, cancellationToken);
        var submittedForThisDataCollection = allSubmissions
            .Where(s => s.DataCollectionId == dcId && s.Status.Value == "submitted")
            .ToList();

        if (submittedForThisDataCollection.Count == 0)
        {
            return Array.Empty<FormSubmission>();
        }

        var existingParent = await dataCollectionSubmissionRepository.GetByDataCollectionAndOrganisationAsync(
            dcId, orgId, cancellationToken);

        // Parent exists (regardless of its status) => some past submit attempt completed the loop in
        // full, so "submitted" children with no further action are the normal, un-corrupted state.
        return existingParent.IsSome ? Array.Empty<FormSubmission>() : submittedForThisDataCollection;
    }

    // ── Activity module methods ────────────────────────────────────────────────

    public async Task<Option<ActivitiesModuleDetailDto>> GetActivitiesModule(
        Guid organisationId,
        Guid dataCollectionId,
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var dcId = new DataCollectionId(dataCollectionId);
        var modId = new DataCollectionFormModuleId(moduleId);

        var dataCollectionOption = await dataCollectionQueries.GetByIdAsync(dcId, cancellationToken);
        if (dataCollectionOption.IsNone)
        {
            return Option<ActivitiesModuleDetailDto>.None;
        }

        var dataCollection = dataCollectionOption.Match(dc => dc, () => null!);

        var formModuleOption = await formModuleQueries.GetByIdWithFieldsAsync(modId, cancellationToken);
        if (formModuleOption.IsNone)
        {
            return Option<ActivitiesModuleDetailDto>.None;
        }

        var formModule = formModuleOption.Match(fm => fm, () => null!);

        var submissions = await formSubmissionRepository.GetByFormModuleOrganisationDataCollectionAndEntityTypeAsync(
            modId, orgId, dcId, "Activity", cancellationToken);

        var activities = submissions
            .OrderBy(s => s.CreatedAt)
            .Select(s => new ActivityRecordDto(
                s.Id.Value,
                s.Name ?? "(untitled)",
                GetFormSubmissionStatus(s.Status),
                s.UpdatedAt ?? s.CreatedAt))
            .ToList();

        var overallStatus = activities.Count == 0 ? "Not started"
            : activities.All(a => a.Status == "Completed") ? "Completed"
            : "In progress";

        return new ActivitiesModuleDetailDto(
            formModule.Id.Value,
            dataCollectionId,
            dataCollection.Name,
            formModule.Code,
            formModule.SectionNumber,
            formModule.Name,
            formModule.Description,
            overallStatus,
            formModule.AllowsMultipleActivities,
            activities,
            await IsSubmissionLockedAsync(orgId, dcId, cancellationToken));
    }

    /// <summary>
    /// Every service belonging to the organisation, for fields configured with
    /// {"dynamicOptions":"services"}. Delivery-plan activities can fund any service the authority
    /// runs, so this is deliberately unfiltered — unlike Outcome Scores (QSU13 = "yes" only) and
    /// Service Users (quarterly inclusion), whose filters answer different questions.
    /// </summary>
    /// <summary>
    /// Blocks marking a delivery-plan strand complete while its forecast spend sits outside the CFD
    /// tolerance. Returns the user-facing message, or null when the strand is compliant, isn't a
    /// delivery-plan strand, or has no allocation set yet.
    ///
    /// The rule is defined on the strand total, not the individual activity, so every activity in
    /// the same round is summed — including the one being saved, whose incoming values are merged
    /// over whatever is currently stored.
    /// </summary>
    private async Task<IReadOnlyList<StrandTolerance>> ReconcileStrandAsync(
        DataCollectionFormModule formModule,
        OrganisationId orgId,
        DataCollectionId dcId,
        FormSubmissionId? submissionBeingSaved,
        IReadOnlyDictionary<string, string?>? incomingValues,
        CancellationToken cancellationToken,
        FormSubmissionId? excludeSubmission = null)
    {
        var strand = DeliveryPlanStrandFields.All.FirstOrDefault(x => x.ModuleCode == formModule.Code);
        if (strand is null)
        {
            return [];
        }

        var allocations = await financialDataQueries.GetAllByOrganisationIdAsync(orgId, cancellationToken);
        decimal AllocationFor(string? module, string year) =>
            module is null ? 0m : allocations.Where(a => a.Module.Value == module && a.YearRange == year).Sum(a => a.Amount);

        var fieldKeyById = formModule.Fields.ToDictionary(f => f.Id, f => f.FieldKey);
        var siblings = await formSubmissionRepository.GetByFormModuleOrganisationAndDataCollectionAsync(
            formModule.Id, orgId, dcId, cancellationToken);

        var activities = new List<IReadOnlyDictionary<string, string?>>();
        var savedIncluded = false;

        foreach (var sibling in siblings)
        {
            if (excludeSubmission is not null && sibling.Id == excludeSubmission)
            {
                continue;
            }

            var answers = sibling.FieldValues
                .Where(v => fieldKeyById.ContainsKey(v.FormFieldId))
                .ToDictionary(v => fieldKeyById[v.FormFieldId], v => Utility.HtmlDecodeNullableField(v.Value));

            if (submissionBeingSaved is not null && sibling.Id == submissionBeingSaved)
            {
                savedIncluded = true;
                if (incomingValues is not null)
                {
                    foreach (var incoming in incomingValues)
                    {
                        answers[incoming.Key] = incoming.Value;
                    }
                }
            }

            activities.Add(answers);
        }

        // A brand-new activity has no stored row yet, so its figures are only in this request.
        if (!savedIncluded && incomingValues is not null)
        {
            activities.Add(new Dictionary<string, string?>(incomingValues));
        }

        return DeliveryPlanSpendReconciler.Summarise(strand, activities, AllocationFor);
    }

    /// <summary>
    /// The strand's spend position with <paramref name="excludeSubmission"/> left out, so the form
    /// can add the figure the user is currently typing and show the remaining headroom live.
    /// </summary>
    private async Task<IReadOnlyList<StrandSpendSummaryDto>> BuildStrandSpendAsync(
        DataCollectionFormModule formModule,
        OrganisationId orgId,
        DataCollectionId dcId,
        FormSubmissionId? excludeSubmission,
        CancellationToken cancellationToken)
    {
        var results = await ReconcileStrandAsync(
            formModule, orgId, dcId, null, null, cancellationToken, excludeSubmission);

        return results
            .Select(r => new StrandSpendSummaryDto(
                r.YearRange,
                r.ForecastFieldKey,
                r.Allocation,
                r.Spend,
                r.LowerBound,
                r.UpperBound,
                r.RemainingToUpperBound,
                r.IsCap))
            .ToList();
    }

    /// <summary>
    /// Blocks a save that would push a strand's total forecast over its CFD allocation. Checked on
    /// every activity save rather than only at module completion, so an LA User is stopped at the
    /// activity that breaks the budget instead of discovering it later against the whole strand.
    ///
    /// Only the upper bound is enforced here. A strand part-way through data entry is legitimately
    /// below its allocation, so the "more than 10% under" half is checked when the strand is marked
    /// complete (<see cref="FindStrandCompletionBreachAsync"/>).
    /// </summary>
    private async Task<string?> FindOverspendAsync(
        DataCollectionFormModule formModule,
        OrganisationId orgId,
        DataCollectionId dcId,
        FormSubmissionId? submissionBeingSaved,
        IReadOnlyDictionary<string, string?>? incomingValues,
        CancellationToken cancellationToken)
    {
        var results = await ReconcileStrandAsync(formModule, orgId, dcId, submissionBeingSaved, incomingValues, cancellationToken);
        var breach = results.FirstOrDefault(r => r.ExceedsUpperBound);
        return breach is null ? null : DeliveryPlanSpendReconciler.Describe(breach);
    }

    /// <summary>
    /// The full ±10% check, for the point at which the strand is declared finished. Adds the
    /// "more than 10% under the allocation" half that cannot be judged mid-entry.
    /// </summary>
    private async Task<string?> FindStrandCompletionBreachAsync(
        DataCollectionFormModule formModule,
        OrganisationId orgId,
        DataCollectionId dcId,
        FormSubmissionId? submissionBeingSaved,
        IReadOnlyDictionary<string, string?>? incomingValues,
        CancellationToken cancellationToken)
    {
        var results = await ReconcileStrandAsync(formModule, orgId, dcId, submissionBeingSaved, incomingValues, cancellationToken);
        var breach = results.FirstOrDefault(r => !r.IsWithinTolerance);
        return breach is null ? null : DeliveryPlanSpendReconciler.Describe(breach);
    }

    private async Task<IReadOnlyList<AvailableServiceDto>> LoadAvailableServicesAsync(
        OrganisationId orgId,
        CancellationToken cancellationToken)
    {
        var services = await serviceQueries.GetByOrganisationIdAsync(orgId, cancellationToken);
        return services
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .Select(s => new AvailableServiceDto(s.Id.Value, s.Name))
            .ToList();
    }

    public async Task<Either<string, ActivityFormDetailDto>> CreateActivity(
        Guid organisationId,
        Guid dataCollectionId,
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var dcId = new DataCollectionId(dataCollectionId);
        var modId = new DataCollectionFormModuleId(moduleId);

        var dataCollectionOption = await dataCollectionQueries.GetByIdAsync(dcId, cancellationToken);
        if (dataCollectionOption.IsNone)
        {
            return Either<string, ActivityFormDetailDto>.Left("Data collection not found");
        }

        var dataCollection = dataCollectionOption.Match(dc => dc, () => null!);

        var formModuleOption = await formModuleQueries.GetByIdWithFieldsAsync(modId, cancellationToken);
        if (formModuleOption.IsNone)
        {
            return Either<string, ActivityFormDetailDto>.Left("Form module not found");
        }

        var formModule = formModuleOption.Match(fm => fm, () => null!);

        if (await IsSubmissionLockedAsync(orgId, dcId, cancellationToken))
        {
            return Either<string, ActivityFormDetailDto>.Left(SubmissionLockedMessage);
        }

        // Refuse a new activity when the strand's existing activities already exhaust the allocation —
        // there is no figure the user could enter that wouldn't breach it, so stopping here is kinder
        // than letting them fill in a form that can never be saved.
        var headroomError = await FindOverspendAsync(formModule, orgId, dcId, null, null, cancellationToken);
        if (headroomError is not null)
        {
            return Either<string, ActivityFormDetailDto>.Left(
                $"{headroomError} Adjust the existing activities before adding another.");
        }

        var formSubmission = FormSubmission.Create(modId, orgId, dcId, "Activity", null, null);

        var idField = formModule.Fields.FirstOrDefault(f => f.FieldType.Value == FieldTypeConstants.ReadOnly && f.DisplayOrder == 1);
        if (idField != null)
        {
            var generatedCode = "ACT-" + Guid.NewGuid().ToString("N")[..8].ToUpper();
            formSubmission.SetFieldValue(idField, generatedCode);
        }

        await formSubmissionRepository.AddAsync(formSubmission, cancellationToken);

        var savedValues = formSubmission.FieldValues.ToDictionary(fv => fv.FormFieldId, fv => fv.Value);
        var savedFieldValues = formSubmission.FieldValues.ToDictionary(fv => fv.FormFieldId);
        var (sections, fields) = BuildFormDto(formModule, savedValues, savedFieldValues, isLocked: false);

        return Either<string, ActivityFormDetailDto>.Right(new ActivityFormDetailDto(
            formSubmission.Id.Value,
            dataCollectionId,
            dataCollection.Name,
            formModule.Id.Value,
            formModule.Name,
            formModule.Code,
            formModule.SectionNumber,
            null!,
            "Draft",
            sections,
            fields,
            await LoadAvailableServicesAsync(orgId, cancellationToken),
            await BuildStrandSpendAsync(formModule, orgId, dcId, formSubmission.Id, cancellationToken),
            // Not read-only: the lock was checked above, so reaching here means the round is open.
            IsReadOnly: false));
    }

    public async Task<Option<ActivityFormDetailDto>> GetActivityTemplate(
        Guid organisationId,
        Guid dataCollectionId,
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var dcId = new DataCollectionId(dataCollectionId);
        var modId = new DataCollectionFormModuleId(moduleId);

        var dataCollectionOption = await dataCollectionQueries.GetByIdAsync(dcId, cancellationToken);
        if (dataCollectionOption.IsNone)
        {
            return Option<ActivityFormDetailDto>.None;
        }

        var dataCollection = dataCollectionOption.Match(dc => dc, () => null!);

        var formModuleOption = await formModuleQueries.GetByIdWithFieldsAsync(modId, cancellationToken);
        if (formModuleOption.IsNone)
        {
            return Option<ActivityFormDetailDto>.None;
        }

        var formModule = formModuleOption.Match(fm => fm, () => null!);

        var (sections, fields) = BuildFormDto(formModule, null);

        return Option<ActivityFormDetailDto>.Some(new ActivityFormDetailDto(
            Guid.Empty,
            dataCollectionId,
            dataCollection.Name,
            formModule.Id.Value,
            formModule.Name,
            formModule.Code,
            formModule.SectionNumber,
            null!,
            "Draft",
            sections,
            fields,
            await LoadAvailableServicesAsync(new OrganisationId(organisationId), cancellationToken),
            await BuildStrandSpendAsync(formModule, new OrganisationId(organisationId), dcId, null, cancellationToken),
            await IsSubmissionLockedAsync(new OrganisationId(organisationId), dcId, cancellationToken)));
    }

    public async Task<Option<ActivityFormDetailDto>> GetActivityForm(
        Guid organisationId,
        Guid dataCollectionId,
        Guid moduleId,
        Guid activityId,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var dcId = new DataCollectionId(dataCollectionId);
        var modId = new DataCollectionFormModuleId(moduleId);
        var submissionId = new FormSubmissionId(activityId);

        var dataCollectionOption = await dataCollectionQueries.GetByIdAsync(dcId, cancellationToken);
        if (dataCollectionOption.IsNone)
        {
            return Option<ActivityFormDetailDto>.None;
        }

        var dataCollection = dataCollectionOption.Match(dc => dc, () => null!);

        var formModuleOption = await formModuleQueries.GetByIdWithFieldsAsync(modId, cancellationToken);
        if (formModuleOption.IsNone)
        {
            return Option<ActivityFormDetailDto>.None;
        }

        var formModule = formModuleOption.Match(fm => fm, () => null!);

        var submissionOption = await formSubmissionRepository.GetByIdWithValuesAsync(submissionId, cancellationToken);
        if (submissionOption.IsNone)
        {
            return Option<ActivityFormDetailDto>.None;
        }

        var submission = submissionOption.Match(s => s, () => null!);

        if (submission.OrganisationId != orgId || submission.FormModuleId != modId || submission.DataCollectionId != dcId)
        {
            return Option<ActivityFormDetailDto>.None;
        }

        var savedValues = submission.FieldValues.ToDictionary(fv => fv.FormFieldId, fv => fv.Value);
        var savedFieldValues = submission.FieldValues.ToDictionary(fv => fv.FormFieldId);

        // Doubles as IsReadOnly below.
        var isLocked = await IsSubmissionLockedAsync(orgId, dcId, cancellationToken);
        var (sections, fields) = BuildFormDto(formModule, savedValues, savedFieldValues, isLocked);

        return new ActivityFormDetailDto(
            submission.Id.Value,
            dataCollectionId,
            dataCollection.Name,
            formModule.Id.Value,
            formModule.Name,
            formModule.Code,
            formModule.SectionNumber,
            submission.Name ?? "(untitled)",
            GetFormSubmissionStatus(submission.Status),
            sections,
            fields,
            await LoadAvailableServicesAsync(new OrganisationId(organisationId), cancellationToken),
            await BuildStrandSpendAsync(formModule, new OrganisationId(organisationId), dcId, submission.Id, cancellationToken),
            isLocked);
    }

    public async Task<Either<string, ActivityFormDetailDto>> SaveActivityForm(
        Guid organisationId,
        Guid dataCollectionId,
        Guid moduleId,
        Guid activityId,
        SaveActivityFormRequest request,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var modId = new DataCollectionFormModuleId(moduleId);
        var submissionId = new FormSubmissionId(activityId);

        var formModuleOption = await formModuleQueries.GetByIdWithFieldsAsync(modId, cancellationToken);
        if (formModuleOption.IsNone)
        {
            return Either<string, ActivityFormDetailDto>.Left("Form module not found");
        }

        var formModule = formModuleOption.Match(fm => fm, () => null!);

        if (await IsSubmissionLockedAsync(orgId, new DataCollectionId(dataCollectionId), cancellationToken))
        {
            return Either<string, ActivityFormDetailDto>.Left(SubmissionLockedMessage);
        }

        var submissionOption = await formSubmissionRepository.GetByIdWithValuesAsync(submissionId, cancellationToken);
        if (submissionOption.IsNone)
        {
            return Either<string, ActivityFormDetailDto>.Left("Activity not found");
        }

        var formSubmission = submissionOption.Match(s => s, () => null!);

        if (formSubmission.OrganisationId != orgId)
        {
            return Either<string, ActivityFormDetailDto>.Left("Activity not found");
        }

        // Validate date answers against the whole post-save picture — a cross-field rule such as
        // "planned completion cannot precede planned start" needs both values, and either may be
        // arriving in this request or already be on the submission from an earlier save.
        var dateAnswers = formModule.Fields
            .Where(f => f.FieldType.Value == FieldTypeConstants.Date)
            .ToDictionary(
                f => f.FieldKey,
                f => request.FieldValues.TryGetValue(f.FieldKey, out var incoming)
                    ? Utility.HtmlDecodeNullableField(incoming)
                    : formSubmission.FieldValues.FirstOrDefault(fv => fv.FormFieldId == f.Id)?.Value);

        var dateError = DeliveryPlanDateAnswerValidator.Validate(dateAnswers, formModule.Fields);
        if (dateError is not null)
        {
            return Either<string, ActivityFormDetailDto>.Left(dateError);
        }

        // Spend cross-validation runs against the whole strand, with this activity's incoming
        // figures merged in, so the running total is judged as it will stand after the save.
        var incomingFieldValues = request.FieldValues.ToDictionary(
            kv => kv.Key,
            kv => Utility.HtmlDecodeNullableField(kv.Value));

        var dcIdForSpend = new DataCollectionId(dataCollectionId);
        var spendError = request.MarkComplete
            ? await FindStrandCompletionBreachAsync(formModule, orgId, dcIdForSpend, submissionId, incomingFieldValues, cancellationToken)
            : await FindOverspendAsync(formModule, orgId, dcIdForSpend, submissionId, incomingFieldValues, cancellationToken);

        if (spendError is not null)
        {
            return Either<string, ActivityFormDetailDto>.Left(spendError);
        }

        foreach (var fieldEntry in request.FieldValues)
        {
            var field = formModule.Fields.FirstOrDefault(f => f.FieldKey == fieldEntry.Key);
            if (field != null)
            {
                formSubmission.SetFieldValue(field, Utility.HtmlDecodeNullableField(fieldEntry.Value));
            }
        }

        // Backfill Activity ID if not yet set (e.g. activity created before auto-generate was in place)
        var activityIdField = formModule.Fields.FirstOrDefault(f => f.FieldType.Value == FieldTypeConstants.ReadOnly && f.DisplayOrder == 1);
        if (activityIdField != null && !formSubmission.FieldValues.Any(fv => fv.FormFieldId == activityIdField.Id && !string.IsNullOrWhiteSpace(fv.Value)))
        {
            formSubmission.SetFieldValue(activityIdField, "ACT-" + Guid.NewGuid().ToString("N")[..8].ToUpper());
        }

        // Prefer the strand's configured name field (e.g. Rollover Spend's RO02) over guessing by
        // position — its first editable field, RO01, is a strand-selector dropdown, not the name.
        var configuredNameFieldKey = DeliveryPlanStrandFields.ResolveActivityNameFieldKey(formModule.Code);
        var nameField = (configuredNameFieldKey is not null
            ? formModule.Fields.FirstOrDefault(f => f.FieldKey == configuredNameFieldKey)
            : null)
            ?? formModule.Fields
                .Where(f => f.FieldType.Value != FieldTypeConstants.ReadOnly && f.FieldType.Value != FieldTypeConstants.Calculation)
                .OrderBy(f => formModule.Sections.FirstOrDefault(s => s.Id == f.FormSectionId)?.SectionNumber ?? 0)
                .ThenBy(f => f.DisplayOrder)
                .FirstOrDefault();
        if (nameField != null && request.FieldValues.TryGetValue(nameField.FieldKey, out var rawNameValue) && !string.IsNullOrWhiteSpace(rawNameValue))
        {
            var nameValue = Utility.HtmlDecodeField(rawNameValue);
            formSubmission.SetName(nameValue.Length > 500 ? nameValue[..500] : nameValue);
        }

        if (request.MarkComplete)
        {
            formSubmission.MarkAsComplete();
        }
        else
        {
            formSubmission.SaveAsDraft();
        }

        await formSubmissionRepository.UpdateAsync(formSubmission, cancellationToken);

        var result = await GetActivityForm(organisationId, dataCollectionId, moduleId, activityId, cancellationToken);

        return result.Match<Either<string, ActivityFormDetailDto>>(
            dto => Either<string, ActivityFormDetailDto>.Right(dto),
            () => Either<string, ActivityFormDetailDto>.Left("Failed to retrieve updated activity"));
    }

    public async Task<Either<string, bool>> DeleteActivity(
        Guid organisationId,
        Guid dataCollectionId,
        Guid moduleId,
        Guid activityId,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var submissionId = new FormSubmissionId(activityId);

        var submissionOption = await formSubmissionRepository.GetByIdAsync(submissionId, cancellationToken);
        if (submissionOption.IsNone)
        {
            return Either<string, bool>.Left("Activity not found");
        }

        var submission = submissionOption.Match(s => s, () => null!);

        if (submission.OrganisationId != orgId)
        {
            return Either<string, bool>.Left("Activity not found");
        }

        if (await IsSubmissionLockedAsync(orgId, new DataCollectionId(dataCollectionId), cancellationToken))
        {
            return Either<string, bool>.Left(SubmissionLockedMessage);
        }

        await formSubmissionRepository.DeleteAsync(submissionId, cancellationToken);

        return Either<string, bool>.Right(true);
    }

    private (IReadOnlyList<FormModuleSectionDto> Sections, IReadOnlyList<FormModuleFieldDto> Fields) BuildFormDto(
        DataCollectionFormModule formModule,
        Dictionary<FormFieldId, string?>? savedValues,
        Dictionary<FormFieldId, FormFieldValue>? savedFieldValues = null,
        bool isLocked = false)
    {
        var sections = formModule.Sections
            .Where(s => s.IsActive)
            .OrderBy(s => s.SectionNumber)
            .Select(s => new FormModuleSectionDto(
                s.Id.Value,
                s.SectionNumber,
                s.Title,
                s.Description,
                s.HelpText,
                s.HelpUrl))
            .ToList();

        var fields = formModule.Fields
            .Where(f => f.IsActive)
            .OrderBy(f => formModule.Sections.FirstOrDefault(s => s.Id == f.FormSectionId)?.SectionNumber ?? 0)
            .ThenBy(f => f.DisplayOrder)
            .Select(f =>
            {
                int? stepNumber = f.FormSectionId != null
                    ? formModule.Sections.FirstOrDefault(s => s.Id == f.FormSectionId)?.SectionNumber
                    : null;

                var savedValue = savedValues != null && savedValues.TryGetValue(f.Id, out var val) ? val : f.DefaultValue;

                var fieldValue = savedFieldValues != null && savedFieldValues.TryGetValue(f.Id, out var fv) ? fv : null;

                return new FormModuleFieldDto(
                    f.Id.Value,
                    f.FieldKey,
                    AnswerSnapshotResolver.ResolveDisplayLabel(fieldValue, f, isLocked),
                    f.HelpText,
                    f.FieldType.ToString(),
                    f.IsRequired,
                    f.DisplayOrder,
                    stepNumber,
                    savedValue,
                    AnswerSnapshotResolver.ResolveDisplayValue(fieldValue, f),
                    AnswerSnapshotResolver.ResolveQuestionLabel(fieldValue),
                    f.ConditionalRules,
                    f.Configuration,
                    f.Placeholder,
                    f.Options.OrderBy(o => o.DisplayOrder).Select(o => new SectionFieldOptionDto(
                        o.Value,
                        o.Label,
                        o.DisplayOrder)).ToList());
            })
            .ToList();

        return (sections, fields);
    }

    public async Task<IReadOnlyList<SubmissionVersionSummaryDto>> GetSubmissionVersions(
        Guid organisationId,
        Guid dataCollectionId,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var dcId = new DataCollectionId(dataCollectionId);

        var submissionOption = await submissionQueries.GetByDataCollectionAndOrganisationAsync(dcId, orgId, cancellationToken);

        return await submissionOption.MatchAsync(
            async submission =>
            {
                var versions = await versionRepository.GetByDataCollectionSubmissionIdAsync(submission.Id, cancellationToken);
                return (IReadOnlyList<SubmissionVersionSummaryDto>)versions
                    .Select(v => new SubmissionVersionSummaryDto(v.VersionNumber, v.SubmittedBy?.Name.ToString(), v.SubmittedAt))
                    .ToList();
            },
            () => (IReadOnlyList<SubmissionVersionSummaryDto>)Array.Empty<SubmissionVersionSummaryDto>());
    }

    public async Task<Option<SubmissionVersionDetailDto>> GetSubmissionVersionDetail(
        Guid organisationId,
        Guid dataCollectionId,
        int versionNumber,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var dcId = new DataCollectionId(dataCollectionId);

        var submissionOption = await submissionQueries.GetByDataCollectionAndOrganisationAsync(dcId, orgId, cancellationToken);
        var submission = submissionOption.MatchUnsafe(s => s, () => (DataCollectionSubmission?)null);
        if (submission is null)
        {
            return Option<SubmissionVersionDetailDto>.None;
        }

        var versionOption = await versionRepository.GetByDataCollectionSubmissionIdAndVersionNumberAsync(
            submission.Id, versionNumber, cancellationToken);
        var version = versionOption.MatchUnsafe(v => v, () => (DataCollectionSubmissionVersion?)null);
        if (version is null)
        {
            return Option<SubmissionVersionDetailDto>.None;
        }

        var dataCollectionOption = await dataCollectionQueries.GetByIdWithLocalAuthoritiesAsync(dcId, cancellationToken);
        var dataCollection = dataCollectionOption.MatchUnsafe(dc => dc, () => (DataCollection?)null);
        var liveModulesByCode = dataCollection?.FormModuleAssignments
            .Where(fm => fm.FormModule is { IsActive: true })
            .ToDictionary(fm => fm.FormModule.Code, fm => fm.FormModule)
            ?? new Dictionary<string, DataCollectionFormModule>();
        var liveModulesById = liveModulesByCode.Values.ToDictionary(m => m.Id.Value);

        var sections = ParseVersionSections(version.Content)
            .Select(s => liveModulesByCode.TryGetValue(s.ModuleCode, out var module)
                ? new ReviewSectionSummaryDto(module.Id.Value, module.Code, module.SectionNumber, module.Name, module.AllowsMultipleActivities)
                : new ReviewSectionSummaryDto(Guid.Empty, s.ModuleCode, s.SectionNumber, s.ModuleName, false))
            .OrderBy(s => s.SectionNumber)
            .ToList();

        var sectionsToReview = version.SectionsToReview
            .Where(id => liveModulesById.ContainsKey(id))
            .Select(id => liveModulesById[id])
            .Select(module => new ReviewSectionSummaryDto(module.Id.Value, module.Code, module.SectionNumber, module.Name, module.AllowsMultipleActivities))
            .OrderBy(s => s.SectionNumber)
            .ToList();

        return new SubmissionVersionDetailDto(
            version.VersionNumber,
            version.SubmittedBy?.Name.ToString(),
            version.SubmittedBy?.Email,
            version.SubmittedAt,
            version.ReviewNotes,
            sectionsToReview,
            sections);
    }

    /// <summary>
    /// One zip containing the same per-module CSVs SubmissionExportService's live download builds,
    /// plus a submission.json holding the version's own stored Content - built from the version's
    /// frozen snapshot rather than live DB entities. Only "answers" and "activities" section shapes
    /// are handled - a version is Delivery Plan only, so "records" (Service Users, Wider Service
    /// Users, Outcome Scores) never occurs here.
    /// </summary>
    public async Task<Option<byte[]>> GetSubmissionVersionZip(
        Guid organisationId,
        Guid dataCollectionId,
        int versionNumber,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var dcId = new DataCollectionId(dataCollectionId);

        var submissionOption = await submissionQueries.GetByDataCollectionAndOrganisationAsync(dcId, orgId, cancellationToken);
        var submission = submissionOption.MatchUnsafe(s => s, () => (DataCollectionSubmission?)null);
        if (submission is null)
        {
            return Option<byte[]>.None;
        }

        var versionOption = await versionRepository.GetByDataCollectionSubmissionIdAndVersionNumberAsync(
            submission.Id, versionNumber, cancellationToken);
        var version = versionOption.MatchUnsafe(v => v, () => (DataCollectionSubmissionVersion?)null);
        if (version is null)
        {
            return Option<byte[]>.None;
        }

        var organisationOption = await organisationQueries.GetOrganisationById(orgId, cancellationToken);
        var organisation = organisationOption.MatchUnsafe(o => o, () => (Organisation?)null);
        if (organisation is null)
        {
            return Option<byte[]>.None;
        }

        var dataCollectionOption = await dataCollectionQueries.GetByIdWithLocalAuthoritiesAsync(dcId, cancellationToken);
        var dataCollectionName = dataCollectionOption.MatchUnsafe(dc => dc.Name, () => string.Empty) ?? string.Empty;

        var sections = ParseVersionSections(version.Content).OrderBy(s => s.SectionNumber).ToList();

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var section in sections)
            {
                var csvContent = BuildCsvForVersionSection(section, organisation.Name, organisation.ONSCode, dataCollectionName);
                var fileName = SanitizeFileName($"{section.SectionNumber}_{section.ModuleCode}.csv");
                var entry = archive.CreateEntry(fileName);
                using var entryStream = entry.Open();
                var bytes = Encoding.UTF8.GetBytes(csvContent);
                await entryStream.WriteAsync(bytes, cancellationToken);
            }

            var jsonEntry = archive.CreateEntry("submission.json");
            using var jsonEntryStream = jsonEntry.Open();
            var jsonBytes = Encoding.UTF8.GetBytes(version.Content);
            await jsonEntryStream.WriteAsync(jsonBytes, cancellationToken);
        }

        memoryStream.Position = 0;
        return Option<byte[]>.Some(memoryStream.ToArray());
    }

    public async Task<Option<SubmissionVersionSectionDetailDto>> GetSubmissionVersionSection(
        Guid organisationId,
        Guid dataCollectionId,
        int versionNumber,
        Guid formModuleId,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var dcId = new DataCollectionId(dataCollectionId);

        var submissionOption = await submissionQueries.GetByDataCollectionAndOrganisationAsync(dcId, orgId, cancellationToken);
        var submission = submissionOption.MatchUnsafe(s => s, () => (DataCollectionSubmission?)null);
        if (submission is null)
        {
            return Option<SubmissionVersionSectionDetailDto>.None;
        }

        var versionOption = await versionRepository.GetByDataCollectionSubmissionIdAndVersionNumberAsync(
            submission.Id, versionNumber, cancellationToken);
        var version = versionOption.MatchUnsafe(v => v, () => (DataCollectionSubmissionVersion?)null);
        if (version is null)
        {
            return Option<SubmissionVersionSectionDetailDto>.None;
        }

        var moduleOption = await formModuleQueries.GetByIdWithFieldsAsync(new DataCollectionFormModuleId(formModuleId), cancellationToken);
        var module = moduleOption.MatchUnsafe(m => m, () => (DataCollectionFormModule?)null);
        if (module is null)
        {
            return Option<SubmissionVersionSectionDetailDto>.None;
        }

        var dataCollectionOption = await dataCollectionQueries.GetByIdWithLocalAuthoritiesAsync(dcId, cancellationToken);
        var dataCollection = dataCollectionOption.MatchUnsafe(dc => dc, () => (DataCollection?)null);
        var liveModulesByCode = dataCollection?.FormModuleAssignments
            .Where(fm => fm.FormModule is { IsActive: true })
            .ToDictionary(fm => fm.FormModule.Code, fm => fm.FormModule)
            ?? new Dictionary<string, DataCollectionFormModule>();

        var parsedSections = ParseVersionSections(version.Content)
            .OrderBy(s => s.SectionNumber)
            .ToList();

        var index = parsedSections.FindIndex(s => s.ModuleCode == module.Code);
        if (index < 0)
        {
            return Option<SubmissionVersionSectionDetailDto>.None;
        }

        var section = parsedSections[index];
        var isActivitiesSection = section.Raw.TryGetProperty("activities", out _);
        IReadOnlyList<ReviewAnswerGroupDto> answerGroups = isActivitiesSection ? [] : BuildAnswerGroupsFromVersionSection(section);
        IReadOnlyList<ReviewActivityRowDto> activities = isActivitiesSection ? BuildActivityRowsFromVersionSection(section, module.Code) : [];

        ReviewNavigationLinkDto? ToLink(int i) => liveModulesByCode.TryGetValue(parsedSections[i].ModuleCode, out var linkedModule)
            ? new ReviewNavigationLinkDto(linkedModule.Id.Value, parsedSections[i].ModuleName, i + 1)
            : null;

        var navigation = new ReviewNavigationDto(
            index + 1,
            parsedSections.Count,
            index > 0 ? ToLink(index - 1) : null,
            index < parsedSections.Count - 1 ? ToLink(index + 1) : null);

        return new SubmissionVersionSectionDetailDto(
            module.Id.Value,
            module.Code,
            module.SectionNumber,
            module.Name,
            dataCollection?.Name ?? string.Empty,
            module.AllowsMultipleActivities,
            answerGroups,
            activities,
            navigation);
    }

    public async Task<Option<SubmissionVersionActivityDetailDto>> GetSubmissionVersionActivity(
        Guid organisationId,
        Guid dataCollectionId,
        int versionNumber,
        Guid formModuleId,
        Guid activityId,
        CancellationToken cancellationToken)
    {
        var orgId = new OrganisationId(organisationId);
        var dcId = new DataCollectionId(dataCollectionId);

        var submissionOption = await submissionQueries.GetByDataCollectionAndOrganisationAsync(dcId, orgId, cancellationToken);
        var submission = submissionOption.MatchUnsafe(s => s, () => (DataCollectionSubmission?)null);
        if (submission is null)
        {
            return Option<SubmissionVersionActivityDetailDto>.None;
        }

        var versionOption = await versionRepository.GetByDataCollectionSubmissionIdAndVersionNumberAsync(
            submission.Id, versionNumber, cancellationToken);
        var version = versionOption.MatchUnsafe(v => v, () => (DataCollectionSubmissionVersion?)null);
        if (version is null)
        {
            return Option<SubmissionVersionActivityDetailDto>.None;
        }

        var moduleOption = await formModuleQueries.GetByIdWithFieldsAsync(new DataCollectionFormModuleId(formModuleId), cancellationToken);
        var module = moduleOption.MatchUnsafe(m => m, () => (DataCollectionFormModule?)null);
        if (module is null || !module.AllowsMultipleActivities)
        {
            return Option<SubmissionVersionActivityDetailDto>.None;
        }

        var dataCollectionOption = await dataCollectionQueries.GetByIdAsync(dcId, cancellationToken);
        var dataCollection = dataCollectionOption.MatchUnsafe(dc => dc, () => (DataCollection?)null);

        var parsedSections = ParseVersionSections(version.Content);
        var section = parsedSections.FirstOrDefault(s => s.ModuleCode == module.Code);
        if (section is null || !section.Raw.TryGetProperty("activities", out var activitiesElement))
        {
            return Option<SubmissionVersionActivityDetailDto>.None;
        }

        var activities = activitiesElement.EnumerateArray().ToList();
        var activityIndex = activities.FindIndex(a => a.GetProperty("activityId").GetGuid() == activityId);
        if (activityIndex < 0)
        {
            return Option<SubmissionVersionActivityDetailDto>.None;
        }

        var activity = activities[activityIndex];
        var activityName = activity.TryGetProperty("activityName", out var name) ? name.GetString() ?? string.Empty : string.Empty;
        var answerGroups = new List<ReviewAnswerGroupDto> { new(null, null, ParseAnswers(activity.GetProperty("answers"))) };

        string ActivityNameAt(int i) => activities[i].TryGetProperty("activityName", out var n) ? n.GetString() ?? "(untitled)" : "(untitled)";
        Guid ActivityIdAt(int i) => activities[i].GetProperty("activityId").GetGuid();

        var navigation = new ReviewNavigationDto(
            activityIndex + 1,
            activities.Count,
            activityIndex > 0 ? new ReviewNavigationLinkDto(ActivityIdAt(activityIndex - 1), ActivityNameAt(activityIndex - 1), activityIndex) : null,
            activityIndex < activities.Count - 1 ? new ReviewNavigationLinkDto(ActivityIdAt(activityIndex + 1), ActivityNameAt(activityIndex + 1), activityIndex + 2) : null);

        return new SubmissionVersionActivityDetailDto(
            activityId,
            string.IsNullOrWhiteSpace(activityName) ? "(untitled)" : activityName,
            module.Id.Value,
            module.Name,
            module.SectionNumber,
            dataCollection?.Name ?? string.Empty,
            answerGroups,
            navigation);
    }

    private sealed record VersionSection(string ModuleCode, string ModuleName, int SectionNumber, JsonElement Raw);

    private static List<VersionSection> ParseVersionSections(string content)
    {
        using var document = JsonDocument.Parse(content);
        if (!document.RootElement.TryGetProperty("sections", out var sectionsElement))
        {
            return [];
        }

        return sectionsElement.EnumerateArray()
            .Select(s => new VersionSection(
                s.GetProperty("moduleCode").GetString() ?? string.Empty,
                s.GetProperty("moduleName").GetString() ?? string.Empty,
                s.GetProperty("sectionNumber").GetInt32(),
                s.Clone()))
            .ToList();
    }

    private static IReadOnlyList<ReviewAnswerGroupDto> BuildAnswerGroupsFromVersionSection(VersionSection section)
    {
        var raw = section.Raw;

        if (raw.TryGetProperty("answers", out var answersElement))
        {
            return [new ReviewAnswerGroupDto(null, null, ParseAnswers(answersElement))];
        }

        // "activities"-shaped sections are handled by BuildActivityRowsFromVersionSection instead —
        // the caller never reaches here for one, this guards only against an unexpected shape.
        if (raw.TryGetProperty("records", out var recordsElement))
        {
            return recordsElement.EnumerateArray()
                .Select((r, i) => new ReviewAnswerGroupDto(
                    null,
                    ResolveRecordTitle(r, i),
                    ParseAnswers(r.GetProperty("answers"))))
                .ToList();
        }

        return [];
    }

    /// <summary>
    /// Activity and delivery status are answers, not workflow state — mirrors
    /// DeliveryPlanReviewService.BuildActivityRows, but reads the snapshot's own stored "value"
    /// (the raw option value, kept alongside the resolved "answer" text) instead of a live
    /// FormFieldValue, so the tag colour is still keyed off the option rather than its English label.
    /// </summary>
    private static IReadOnlyList<ReviewActivityRowDto> BuildActivityRowsFromVersionSection(VersionSection section, string moduleCode)
    {
        if (!section.Raw.TryGetProperty("activities", out var activitiesElement))
        {
            return [];
        }

        var statusFields = DeliveryPlanActivityStatusFields.ForModule(moduleCode);

        return activitiesElement.EnumerateArray()
            .Select(a =>
            {
                var activityName = a.TryGetProperty("activityName", out var name) ? name.GetString() : null;
                var answers = a.GetProperty("answers");
                var (activityStatus, activityStatusValue) = FindAnswerValue(answers, statusFields?.ActivityStatusFieldKey);
                var (deliveryStatus, deliveryStatusValue) = FindAnswerValue(answers, statusFields?.DeliveryStatusFieldKey);

                return new ReviewActivityRowDto(
                    ActivityId: a.GetProperty("activityId").GetGuid(),
                    ActivityName: string.IsNullOrWhiteSpace(activityName) ? "(untitled)" : activityName!,
                    ActivityStatus: activityStatus,
                    ActivityStatusValue: activityStatusValue,
                    DeliveryStatus: deliveryStatus,
                    DeliveryStatusValue: deliveryStatusValue);
            })
            .ToList();
    }

    private static (string? Answer, string? Value) FindAnswerValue(JsonElement answersElement, string? questionCode)
    {
        if (questionCode is null)
        {
            return (null, null);
        }

        foreach (var answer in answersElement.EnumerateArray())
        {
            if (answer.GetProperty("questionCode").GetString() != questionCode)
            {
                continue;
            }

            var answerText = answer.TryGetProperty("answer", out var a) ? a.GetString() : null;
            var value = answer.TryGetProperty("value", out var v) ? v.GetString() : null;
            return (string.IsNullOrEmpty(answerText) ? null : answerText, string.IsNullOrEmpty(value) ? null : value);
        }

        return (null, null);
    }

    private static string ResolveRecordTitle(JsonElement record, int index)
    {
        if (record.TryGetProperty("serviceName", out var serviceName) && serviceName.GetString() is { Length: > 0 } sName)
        {
            return sName;
        }

        if (record.TryGetProperty("categoryName", out var categoryName) && categoryName.GetString() is { Length: > 0 } cName)
        {
            return cName;
        }

        return $"Record {index + 1}";
    }

    private static IReadOnlyList<ReviewAnswerDto> ParseAnswers(JsonElement answersElement)
    {
        return answersElement.EnumerateArray()
            .Select(a => new ReviewAnswerDto(
                a.GetProperty("questionCode").GetString() ?? string.Empty,
                a.GetProperty("question").GetString() ?? string.Empty,
                a.TryGetProperty("answer", out var answer) ? answer.GetString() : null,
                string.Empty))
            .ToList();
    }

    /// <summary>
    /// Same header/row shape as SubmissionExportService's GenerateStandardFormCsvAsync /
    /// GenerateActivitiesCsvAsync, rebuilt from the stored snapshot instead of live FormFieldValues.
    /// </summary>
    private static string BuildCsvForVersionSection(VersionSection section, string laName, string onsCode, string collectionPeriod)
    {
        var sb = new StringBuilder();
        var raw = section.Raw;

        if (raw.TryGetProperty("activities", out var activitiesElement))
        {
            var headers = new[] { "LAName", "ONSCode", "CollectionPeriod", "ActivityId", "ActivityName", "Status", "QuestionCode", "Question", "Answer" };
            sb.AppendLine(string.Join(",", headers.Select(EscapeCsvField)));

            foreach (var activity in activitiesElement.EnumerateArray())
            {
                var activityId = activity.GetProperty("activityId").GetGuid().ToString();
                var activityName = activity.TryGetProperty("activityName", out var name) ? name.GetString() ?? string.Empty : string.Empty;
                var status = activity.TryGetProperty("status", out var s) ? s.GetString() ?? string.Empty : string.Empty;

                foreach (var answer in activity.GetProperty("answers").EnumerateArray())
                {
                    var row = new[]
                    {
                        laName,
                        onsCode,
                        collectionPeriod,
                        activityId,
                        activityName,
                        status,
                        answer.GetProperty("questionCode").GetString() ?? string.Empty,
                        answer.GetProperty("question").GetString() ?? string.Empty,
                        answer.TryGetProperty("answer", out var a) ? a.GetString() ?? string.Empty : string.Empty
                    };
                    sb.AppendLine(string.Join(",", row.Select(EscapeCsvField)));
                }
            }

            return sb.ToString();
        }

        if (raw.TryGetProperty("answers", out var answersElement))
        {
            var headers = new[] { "LAName", "ONSCode", "CollectionPeriod", "QuestionCode", "Question", "Answer" };
            sb.AppendLine(string.Join(",", headers.Select(EscapeCsvField)));

            foreach (var answer in answersElement.EnumerateArray())
            {
                var row = new[]
                {
                    laName,
                    onsCode,
                    collectionPeriod,
                    answer.GetProperty("questionCode").GetString() ?? string.Empty,
                    answer.GetProperty("question").GetString() ?? string.Empty,
                    answer.TryGetProperty("answer", out var a) ? a.GetString() ?? string.Empty : string.Empty
                };
                sb.AppendLine(string.Join(",", row.Select(EscapeCsvField)));
            }

            return sb.ToString();
        }

        return sb.ToString();
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return field;
        }

        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }
}