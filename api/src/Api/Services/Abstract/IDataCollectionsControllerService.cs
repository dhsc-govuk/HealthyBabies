using Application.DataCollections.Dtos;
using LanguageExt;

namespace Api.Services.Abstract;

public interface IDataCollectionsControllerService
{
    Task<IEnumerable<DataCollectionDto>> GetAll(CancellationToken cancellationToken);
    Task<Option<DataCollectionDto>> Get(Guid dataCollectionId, CancellationToken cancellationToken);
    Task<Option<DataCollectionDto>> GetWithLocalAuthorities(Guid dataCollectionId, CancellationToken cancellationToken);
    Task<Option<DataCollectionDto>> GetWithSubmissions(Guid dataCollectionId, CancellationToken cancellationToken);
    Task<IEnumerable<DataCollectionFormModuleDto>> GetAllFormModules(CancellationToken cancellationToken);
    Task<Option<LocalAuthoritySubmissionDetailDto>> GetLocalAuthoritySubmission(Guid dataCollectionId, Guid localAuthorityId, CancellationToken cancellationToken);
}