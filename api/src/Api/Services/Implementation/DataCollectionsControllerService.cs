using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.DataCollections.Dtos;
using Domain.DataCollections;
using Domain.Organisations;
using LanguageExt;

namespace Api.Services.Implementation;

public class DataCollectionsControllerService(
    IDataCollectionQueries dataCollectionQueries,
    IDataCollectionFormModuleQueries formModuleQueries,
    IDataCollectionSubmissionQueries submissionQueries,
    IDataCollectionSubmissionVersionRepository versionRepository,
    IOrganisationQueries organisationQueries,
    IOrganisationContactQueries organisationContactQueries)
    : IDataCollectionsControllerService
{
    public async Task<IEnumerable<DataCollectionDto>> GetAll(CancellationToken cancellationToken)
    {
        var dataCollections = await dataCollectionQueries.GetAllAsync(cancellationToken);

        return dataCollections.Select(DataCollectionDto.FromDomainModel);
    }

    public async Task<Option<DataCollectionDto>> Get(Guid dataCollectionId, CancellationToken cancellationToken)
    {
        var entityId = new DataCollectionId(dataCollectionId);
        var dataCollection = await dataCollectionQueries.GetByIdAsync(entityId, cancellationToken);
        return dataCollection.Match(
            dc => DataCollectionDto.FromDomainModel(dc),
            () => Option<DataCollectionDto>.None);
    }

    public async Task<Option<DataCollectionDto>> GetWithLocalAuthorities(Guid dataCollectionId, CancellationToken cancellationToken)
    {
        var entityId = new DataCollectionId(dataCollectionId);
        var dataCollection = await dataCollectionQueries.GetByIdWithLocalAuthoritiesAsync(entityId, cancellationToken);
        return dataCollection.Match(
            dc => DataCollectionDto.FromDomainModelWithLocalAuthorities(dc),
            () => Option<DataCollectionDto>.None);
    }

    public async Task<Option<DataCollectionDto>> GetWithSubmissions(Guid dataCollectionId, CancellationToken cancellationToken)
    {
        var entityId = new DataCollectionId(dataCollectionId);
        var dataCollection = await dataCollectionQueries.GetByIdWithLocalAuthoritiesAsync(entityId, cancellationToken);

        return await dataCollection.MatchAsync<DataCollection, Option<DataCollectionDto>>(
            async dc =>
            {
                var submissions = await submissionQueries.GetByDataCollectionIdAsync(entityId, cancellationToken);

                // A collection open to every local authority has no assignments to enumerate, so the
                // reviewer's progress list is built from the organisations instead (BLUM-7626). Only
                // fetch them for that case - a scoped collection lists its own assignments.
                IReadOnlyList<Organisation> allLocalAuthorities = dc.IsSubmittedByAllLocalAuthorities
                    ? await organisationQueries.GetOrganisations(cancellationToken)
                    : [];

                return DataCollectionDto.FromDomainModelWithSubmissions(dc, submissions, allLocalAuthorities);
            },
            () => Option<DataCollectionDto>.None);
    }

    public async Task<IEnumerable<DataCollectionFormModuleDto>> GetAllFormModules(CancellationToken cancellationToken)
    {
        var formModules = await formModuleQueries.GetAllActiveAsync(cancellationToken);
        return formModules.Select(DataCollectionFormModuleDto.FromDomainModel);
    }

    public async Task<Option<LocalAuthoritySubmissionDetailDto>> GetLocalAuthoritySubmission(
        Guid dataCollectionId,
        Guid localAuthorityId,
        CancellationToken cancellationToken)
    {
        var dcId = new DataCollectionId(dataCollectionId);
        var orgId = new OrganisationId(localAuthorityId);

        // Loaded with local authorities so the eager-loaded FormModuleAssignments are available for
        // the Delivery Plan section list below.
        var dataCollectionOption = await dataCollectionQueries.GetByIdWithLocalAuthoritiesAsync(dcId, cancellationToken);
        if (dataCollectionOption.IsNone)
        {
            return Option<LocalAuthoritySubmissionDetailDto>.None;
        }

        var dataCollection = dataCollectionOption.Match(dc => dc, () => null!);

        // Get the organisation (local authority)
        var organisationOption = await organisationQueries.GetOrganisationById(orgId, cancellationToken);
        if (organisationOption.IsNone)
        {
            return Option<LocalAuthoritySubmissionDetailDto>.None;
        }

        var organisation = organisationOption.Match(o => o, () => null!);

        // Get the submission if it exists
        var submissionOption = await submissionQueries.GetByDataCollectionAndOrganisationWithUsersAsync(dcId, orgId, cancellationToken);

        var submission = submissionOption.MatchUnsafe(s => s, () => (DataCollectionSubmission?)null);

        // Get organisation contacts to find email
        var contacts = await organisationContactQueries.GetByOrganisationIdAsync(orgId, cancellationToken);
        var primaryContact = contacts.FirstOrDefault();

        // Build contact info from organisation and contact data
        var contact = new LocalAuthorityContactDto(
            OrganisationName: organisation.Name,
            AddressLine1: string.Empty,
            AddressLine2: null,
            City: string.Empty,
            Postcode: string.Empty,
            Country: "England",
            Phone: null,
            Email: primaryContact?.Email);

        var isDeliveryPlan = dataCollection.Type.Value == DataCollectionTypeConstants.DeliveryPlan;

        // Section links are a Delivery Plan review feature only. Management information collections
        // keep their existing behaviour of details plus file downloads, so they get an empty list.
        var sections = isDeliveryPlan
            ? dataCollection.FormModuleAssignments
                .Where(fm => fm.FormModule is { IsActive: true })
                .OrderBy(fm => fm.FormModule.SectionNumber)
                .Select(fm => new ReviewSectionSummaryDto(
                    FormModuleId: fm.FormModuleId.Value,
                    Code: fm.FormModule.Code,
                    SectionNumber: fm.FormModule.SectionNumber,
                    Name: fm.FormModule.Name,
                    AllowsMultipleActivities: fm.FormModule.AllowsMultipleActivities))
                .ToList()
            : [];

        // The subset of sections the reviewer asked the local authority to focus on when reopening.
        var sectionsToReview = submission != null && submission.SectionsToReview.Count > 0
            ? sections.Where(s => submission.SectionsToReview.Contains(s.FormModuleId)).ToList()
            : [];

        // For now, return empty files list - this can be extended later
        var files = new List<LocalAuthorityFileDto>();

        // If submission exists and has data, we could add file download links here
        var hasSubmittedData = submission != null && submission.Status is
            DataCollectionSubmissionStatus.Submitted or
            DataCollectionSubmissionStatus.Approved or
            DataCollectionSubmissionStatus.Rejected;

        if (hasSubmittedData)
        {
            // Reopened means the previous data is being amended, not ready to download again
            // until it is resubmitted.
            var isPending = submission!.Status == DataCollectionSubmissionStatus.Rejected;

            files.Add(new LocalAuthorityFileDto(
                Name: isDeliveryPlan
                    ? "Local Authority Delivery Plan"
                    : "Local Authority Management Information",
                Status: isPending ? "Submission pending" : null,
                Description: isPending
                    ? "The file will be available to download once the local authority has completed their data submission."
                    : $"for {dataCollection.Name}",
                DownloadUrl: isPending ? null : $"/admin/data-collections/{dataCollectionId}/local-authorities/{localAuthorityId}/download",
                DownloadSize: null));
        }

        var previousVersionCount = submission != null
            ? await versionRepository.GetCountByDataCollectionSubmissionIdAsync(submission.Id, cancellationToken)
            : 0;

        return new LocalAuthoritySubmissionDetailDto(
            Id: submission?.Id.Value ?? Guid.Empty,
            DataCollectionId: dataCollectionId,
            DataCollectionName: dataCollection.Name,
            LocalAuthorityId: localAuthorityId,
            LocalAuthorityName: organisation.Name,
            Status: submission?.Status.ToString() ?? DataCollectionSubmissionStatus.NotStarted.ToString(),
            SubmittedAt: submission?.SubmittedAt,
            SubmittedBy: submission?.SubmittedBy?.Name.ToString(),
            SubmittedByEmail: submission?.SubmittedBy?.Email,
            Contact: contact,
            Files: files,
            Type: dataCollection.Type.Value,
            Sections: sections,
            ReviewedAt: submission?.ReviewedAt,
            ReviewedBy: submission?.ReviewedBy?.Name.ToString(),
            ReviewNotes: submission?.ReviewNotes,
            DueDate: dataCollection.GetEffectiveEndDate(orgId),
            SectionsToReview: sectionsToReview,
            PreviousVersionCount: previousVersionCount);
    }
}