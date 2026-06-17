using Application.Submissions.Dtos;
using LanguageExt;

namespace Api.Services.Abstract;

public interface ISubmissionsControllerService
{
    Task<IEnumerable<SubmissionDto>> GetByOrganisationId(Guid organisationId, CancellationToken cancellationToken);
    Task<Option<SubmissionDetailDto>> GetById(Guid organisationId, Guid dataCollectionId, CancellationToken cancellationToken);
    Task<Option<SectionDetailDto>> GetSectionById(Guid organisationId, Guid dataCollectionId, Guid sectionId, CancellationToken cancellationToken);
    Task<Either<string, SectionDetailDto>> SaveSection(Guid organisationId, Guid dataCollectionId, Guid sectionId, SaveSectionRequest request, CancellationToken cancellationToken);
    Task<Option<FormModuleDetailDto>> GetFormModuleById(Guid organisationId, Guid dataCollectionId, Guid moduleId, CancellationToken cancellationToken);
    Task<Either<string, FormModuleDetailDto>> SaveFormModule(Guid organisationId, Guid dataCollectionId, Guid moduleId, SaveFormModuleRequest request, CancellationToken cancellationToken);
    Task<Either<string, bool>> DeleteFormModule(Guid organisationId, Guid dataCollectionId, Guid moduleId, CancellationToken cancellationToken);
    Task<Either<string, FileUploadResultDto>> UploadFile(Guid organisationId, Guid submissionId, Guid moduleId, IFormFile file, CancellationToken cancellationToken);

    // Service Users module methods
    Task<Option<ServiceUsersModuleDetailDto>> GetServiceUsersModule(Guid organisationId, Guid dataCollectionId, Guid moduleId, CancellationToken cancellationToken);
    Task<Option<ServiceFormDetailDto>> GetServiceForm(Guid organisationId, Guid dataCollectionId, Guid moduleId, Guid serviceId, CancellationToken cancellationToken);
    Task<Either<string, ServiceFormDetailDto>> SaveServiceForm(Guid organisationId, Guid dataCollectionId, Guid moduleId, Guid serviceId, SaveServiceFormRequest request, CancellationToken cancellationToken);

    // Wider Service Users module methods
    Task<Option<WiderServiceUsersModuleDetailDto>> GetWiderServiceUsersModule(Guid organisationId, Guid dataCollectionId, Guid moduleId, CancellationToken cancellationToken);
    Task<Option<WiderServiceCategoryFormDto>> GetWiderServiceCategoryForm(Guid organisationId, Guid dataCollectionId, Guid moduleId, Guid categoryId, CancellationToken cancellationToken);
    Task<Either<string, WiderServiceCategoryFormDto>> SaveWiderServiceCategoryForm(Guid organisationId, Guid dataCollectionId, Guid moduleId, Guid categoryId, SaveWiderServiceCategoryFormRequest request, CancellationToken cancellationToken);

    // Outcome Scores module methods
    Task<Option<OutcomeScoresModuleDetailDto>> GetOutcomeScoresModule(Guid organisationId, Guid dataCollectionId, Guid moduleId, CancellationToken cancellationToken);
    Task<Either<string, OutcomeScoreFormDetailDto>> CreateOutcomeScoreRecord(Guid organisationId, Guid dataCollectionId, Guid moduleId, CancellationToken cancellationToken);
    Task<Option<OutcomeScoreFormDetailDto>> GetOutcomeScoreRecord(Guid organisationId, Guid dataCollectionId, Guid moduleId, Guid recordId, CancellationToken cancellationToken);
    Task<Either<string, OutcomeScoreFormDetailDto>> SaveOutcomeScoreRecord(Guid organisationId, Guid dataCollectionId, Guid moduleId, Guid recordId, SaveOutcomeScoreFormRequest request, CancellationToken cancellationToken);
    Task<Either<string, bool>> DeleteOutcomeScoreRecord(Guid organisationId, Guid dataCollectionId, Guid moduleId, Guid recordId, CancellationToken cancellationToken);

    // Admin methods
    Task<Either<string, int>> PurgeAllSubmissions(Guid organisationId, CancellationToken cancellationToken);

    // Submit submission
    Task<Either<string, SubmissionDetailDto>> SubmitSubmission(Guid organisationId, Guid dataCollectionId, Guid userId, CancellationToken cancellationToken);
}