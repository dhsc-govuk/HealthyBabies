namespace Api.Services.Abstract;

public interface ICoreDataExportService
{
    Task<byte[]> ExportSitesAsCsvAsync(CancellationToken cancellationToken = default);
    Task<byte[]> ExportServicesAsCsvAsync(CancellationToken cancellationToken = default);
}