namespace Api.Services.Abstract;

public interface IServicesBulkUploadTemplateService
{
    Task<byte[]> GenerateCsvTemplateAsync(CancellationToken cancellationToken = default);
    Task<byte[]> GenerateExcelTemplateAsync(CancellationToken cancellationToken = default);
}