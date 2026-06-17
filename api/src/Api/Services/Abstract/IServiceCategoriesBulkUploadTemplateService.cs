namespace Api.Services.Abstract;

public interface IServiceCategoriesBulkUploadTemplateService
{
    Task<byte[]> GenerateCsvTemplateAsync(Guid organisationId, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateExcelTemplateAsync(Guid organisationId, CancellationToken cancellationToken = default);
}