using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using Application.DataCollections.Dtos;
using Domain.DataCollections;
using Domain.Organisations;
using LanguageExt;

namespace Api.Services.Implementation;

public class DataCollectionsControllerService(
    IDataCollectionQueries dataCollectionQueries,
    IDataCollectionFormModuleQueries formModuleQueries,
    IDataCollectionSubmissionQueries submissionQueries,
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
                return DataCollectionDto.FromDomainModelWithSubmissions(dc, submissions);
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

        // Get the data collection
        var dataCollectionOption = await dataCollectionQueries.GetByIdAsync(dcId, cancellationToken);
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
        var submissionOption = await submissionQueries.GetByDataCollectionAndOrganisationAsync(dcId, orgId, cancellationToken);

        var submission = submissionOption.Match(s => s, () => (DataCollectionSubmission?)null);

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

        // For now, return empty files list - this can be extended later
        var files = new List<LocalAuthorityFileDto>();

        // If submission exists and has data, we could add file download links here
        if (submission != null && submission.Status == DataCollectionSubmissionStatus.Submitted)
        {
            files.Add(new LocalAuthorityFileDto(
                Name: "Local Authority Management Information data",
                Description: $"for {dataCollection.Name}",
                CsvUrl: $"/admin/data-collections/{dataCollectionId}/local-authorities/{localAuthorityId}/download?format=csv",
                CsvSize: null,
                JsonUrl: $"/admin/data-collections/{dataCollectionId}/local-authorities/{localAuthorityId}/download?format=json",
                JsonSize: null));
        }

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
            Files: files);
    }
}