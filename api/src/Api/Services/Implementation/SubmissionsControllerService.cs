using Api.Services.Abstract;
using Application.Common;
using Application.Common.FileValidation;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
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
    IDataCollectionFormModuleQueries formModuleQueries,
    IFormSubmissionRepository formSubmissionRepository,
    IServiceQueries serviceQueries,
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
        DataCollectionSubmissionStatus.Rejected => "Rejected",
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
                dc.IsClosedFor(orgId)));
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
                dc.IsClosedFor(orgId)));
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
            submission.DataCollection!.FormModuleAssignments, orgId, cancellationToken);

        return Option<SubmissionDetailDto>.Some(new SubmissionDetailDto(
            submission.DataCollectionId.Value,
            submission.DataCollection!.Name,
            submission.DataCollection.Description,
            submission.DataCollection.StartDate,
            endDate,
            overallStatus,
            Math.Max(0, daysRemaining),
            submission.DataCollection.IsClosedFor(orgId),
            formModules));
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
            dataCollection.FormModuleAssignments, orgId, cancellationToken);

        return new SubmissionDetailDto(
            dataCollection.Id.Value,
            dataCollection.Name,
            dataCollection.Description,
            dataCollection.StartDate,
            endDate,
            "Not started",
            Math.Max(0, daysRemaining),
            dataCollection.IsClosedFor(orgId),
            formModules);
    }

    private async Task<List<SubmissionFormModuleDto>> BuildFormModuleDtos(
        IEnumerable<DataCollectionFormModuleAssignment> assignments,
        OrganisationId organisationId,
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
                new List<SubmissionSectionDto>()));
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
                .OrderBy(f => f.DisplayOrder)
                .Select(f =>
                {
                    int? stepNumber = f.FormSectionId != null
                        ? mod.Sections.FirstOrDefault(s => s.Id == f.FormSectionId)?.SectionNumber
                        : null;

                    var savedValue = savedValues.TryGetValue(f.Id, out var val) ? val : f.DefaultValue;

                    return new FormModuleFieldDto(
                        f.Id.Value,
                        f.FieldKey,
                        f.Label,
                        f.HelpText,
                        f.FieldType.ToString(),
                        f.IsRequired,
                        f.DisplayOrder,
                        stepNumber,
                        savedValue,
                        savedFieldValues.TryGetValue(f.Id, out var fv) ? AnswerSnapshotResolver.ResolveDisplayValue(fv, f) : null,
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
            fields);
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
            .OrderBy(f => f.DisplayOrder)
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

                return new FormModuleFieldDto(
                    f.Id.Value,
                    f.FieldKey,
                    f.Label,
                    f.HelpText,
                    f.FieldType.ToString(),
                    f.IsRequired,
                    f.DisplayOrder,
                    stepNumber,
                    savedValue,
                    savedFieldValues.TryGetValue(f.Id, out var fv) ? AnswerSnapshotResolver.ResolveDisplayValue(fv, f) : null,
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
            .OrderBy(f => f.DisplayOrder)
            .Select(f =>
            {
                int? stepNumber = f.FormSectionId != null
                    ? formModule.Sections.FirstOrDefault(s => s.Id == f.FormSectionId)?.SectionNumber
                    : null;

                var savedValue = savedValues.TryGetValue(f.Id, out var val) ? val : f.DefaultValue;

                return new FormModuleFieldDto(
                    f.Id.Value,
                    f.FieldKey,
                    f.Label,
                    f.HelpText,
                    f.FieldType.ToString(),
                    f.IsRequired,
                    f.DisplayOrder,
                    stepNumber,
                    savedValue,
                    savedFieldValues.TryGetValue(f.Id, out var fv) ? AnswerSnapshotResolver.ResolveDisplayValue(fv, f) : null,
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
            .OrderBy(f => f.DisplayOrder)
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
            .OrderBy(f => f.DisplayOrder)
            .Select(f =>
            {
                int? stepNumber = f.FormSectionId != null
                    ? formModule.Sections.FirstOrDefault(s => s.Id == f.FormSectionId)?.SectionNumber
                    : null;

                var savedValue = savedValues.TryGetValue(f.Id, out var val) ? val : f.DefaultValue;

                return new FormModuleFieldDto(
                    f.Id.Value,
                    f.FieldKey,
                    f.Label,
                    f.HelpText,
                    f.FieldType.ToString(),
                    f.IsRequired,
                    f.DisplayOrder,
                    stepNumber,
                    savedValue,
                    savedFieldValues.TryGetValue(f.Id, out var fv) ? AnswerSnapshotResolver.ResolveDisplayValue(fv, f) : null,
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
                formSubmission.SetFieldValue(field, fieldEntry.Value);
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

        // A record counts as complete for submission when it is "approved" (the pre-submission
        // terminal state) or "submitted". After a quarter is submitted MarkAsSubmitted() flips every
        // record to "submitted"; reopening the quarter resets the DataCollectionSubmission but leaves
        // those records at "submitted", so a re-submit must accept both states - otherwise the UI shows
        // every section "Completed" (its status calc counts both) while this gate rejects with a 400.
        static bool IsComplete(FormSubmission submission) =>
            submission.Status.Value == "approved" || submission.Status.Value == "submitted";

        // Check if all assigned modules are complete
        foreach (var module in assignedFormModules)
        {
            var moduleSubmissions = dcSubmissions.Where(s => s.FormModuleId == module.Id).ToList();

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

                var serviceSubmissions = moduleSubmissions.Where(s => s.EntityType == "Service" && IsComplete(s)).ToList();
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
                var categorySubmissions = moduleSubmissions.Where(s => s.EntityType == "WiderServiceCategory" && IsComplete(s)).ToList();
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
                var outcomeSubmissions = moduleSubmissions.Where(s => s.EntityType == "OutcomeScore" && IsComplete(s)).ToList();
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

            var allComplete = moduleSubmissions.All(IsComplete);
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

        // Create or update the DataCollectionSubmission record for admin visibility
        var submittedById = new UserId(userId);
        var existingSubmission = await dataCollectionSubmissionRepository.GetByDataCollectionAndOrganisationAsync(
            dcId, orgId, cancellationToken);

        if (existingSubmission.IsSome)
        {
            var dcSubmission = existingSubmission.Match(s => s, () => null!);
            dcSubmission.Submit(submittedById);
            await dataCollectionSubmissionRepository.UpdateAsync(dcSubmission, cancellationToken, saveChanges: false);
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
            activities);
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

        var formSubmission = FormSubmission.Create(modId, orgId, dcId, "Activity", null, null);

        var idField = formModule.Fields.FirstOrDefault(f => f.FieldType == FieldType.ReadOnly && f.DisplayOrder == 1);
        if (idField != null)
        {
            var generatedCode = "ACT-" + Guid.NewGuid().ToString("N")[..8].ToUpper();
            formSubmission.SetFieldValue(idField, generatedCode);
        }

        await formSubmissionRepository.AddAsync(formSubmission, cancellationToken);

        var savedValues = formSubmission.FieldValues.ToDictionary(fv => fv.FormFieldId, fv => fv.Value);
        var savedFieldValues = formSubmission.FieldValues.ToDictionary(fv => fv.FormFieldId);
        var (sections, fields) = BuildFormDto(formModule, savedValues, savedFieldValues);

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
            fields));
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
            fields));
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

        if (submission.OrganisationId != orgId)
        {
            return Option<ActivityFormDetailDto>.None;
        }

        var savedValues = submission.FieldValues.ToDictionary(fv => fv.FormFieldId, fv => fv.Value);
        var savedFieldValues = submission.FieldValues.ToDictionary(fv => fv.FormFieldId);
        var (sections, fields) = BuildFormDto(formModule, savedValues, savedFieldValues);

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
            fields);
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

        foreach (var fieldEntry in request.FieldValues)
        {
            var field = formModule.Fields.FirstOrDefault(f => f.FieldKey == fieldEntry.Key);
            if (field != null)
            {
                formSubmission.SetFieldValue(field, fieldEntry.Value);
            }
        }

        // Backfill Activity ID if not yet set (e.g. activity created before auto-generate was in place)
        var activityIdField = formModule.Fields.FirstOrDefault(f => f.FieldType == FieldType.ReadOnly && f.DisplayOrder == 1);
        if (activityIdField != null && !formSubmission.FieldValues.Any(fv => fv.FormFieldId == activityIdField.Id && !string.IsNullOrWhiteSpace(fv.Value)))
        {
            formSubmission.SetFieldValue(activityIdField, "ACT-" + Guid.NewGuid().ToString("N")[..8].ToUpper());
        }

        // Derive display name from the first user-editable field by display order
        var nameField = formModule.Fields
            .Where(f => f.FieldType.Value != FieldTypeConstants.ReadOnly && f.FieldType.Value != FieldTypeConstants.Calculation)
            .OrderBy(f => f.DisplayOrder)
            .FirstOrDefault();
        if (nameField != null && request.FieldValues.TryGetValue(nameField.FieldKey, out var nameValue) && !string.IsNullOrWhiteSpace(nameValue))
        {
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

        await formSubmissionRepository.DeleteAsync(submissionId, cancellationToken);

        return Either<string, bool>.Right(true);
    }

    private (IReadOnlyList<FormModuleSectionDto> Sections, IReadOnlyList<FormModuleFieldDto> Fields) BuildFormDto(
        DataCollectionFormModule formModule,
        Dictionary<FormFieldId, string?>? savedValues,
        Dictionary<FormFieldId, FormFieldValue>? savedFieldValues = null)
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
            .OrderBy(f => f.DisplayOrder)
            .Select(f =>
            {
                int? stepNumber = f.FormSectionId != null
                    ? formModule.Sections.FirstOrDefault(s => s.Id == f.FormSectionId)?.SectionNumber
                    : null;

                var savedValue = savedValues != null && savedValues.TryGetValue(f.Id, out var val) ? val : f.DefaultValue;

                return new FormModuleFieldDto(
                    f.Id.Value,
                    f.FieldKey,
                    f.Label,
                    f.HelpText,
                    f.FieldType.ToString(),
                    f.IsRequired,
                    f.DisplayOrder,
                    stepNumber,
                    savedValue,
                    savedFieldValues != null && savedFieldValues.TryGetValue(f.Id, out var fv) ? AnswerSnapshotResolver.ResolveDisplayValue(fv, f) : null,
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
}