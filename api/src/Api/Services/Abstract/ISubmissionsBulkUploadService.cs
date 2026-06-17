namespace Api.Services.Abstract;

public interface ISubmissionsBulkUploadService
{
    Task<byte[]> GenerateCsvTemplateAsync(Guid organisationId, Guid moduleId, Guid submissionId, Guid dataCollectionId, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateExcelTemplateAsync(Guid organisationId, Guid moduleId, Guid submissionId, Guid dataCollectionId, CancellationToken cancellationToken = default);
}