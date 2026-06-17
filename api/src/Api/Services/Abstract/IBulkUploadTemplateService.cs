namespace Api.Services.Abstract;

public interface IBulkUploadTemplateService
{
    Task<byte[]> GenerateCsvTemplateAsync(CancellationToken cancellationToken = default);
    Task<byte[]> GenerateExcelTemplateAsync(CancellationToken cancellationToken = default);
}