using Domain.Organisations;

namespace Api.Services.Abstract;

public interface ICoreDataExportService
{
    Task<byte[]> ExportSitesAsCsvAsync(CancellationToken cancellationToken = default);
    Task<byte[]> ExportServicesAsCsvAsync(CancellationToken cancellationToken = default);

    // BLUM-7251: LA self-serve equivalents - same CSV shape, scoped to one organisation and
    // without the LAName column (redundant once every row is already the same LA).
    Task<byte[]> ExportSitesAsCsvAsync(OrganisationId organisationId, CancellationToken cancellationToken = default);
    Task<byte[]> ExportServicesAsCsvAsync(OrganisationId organisationId, CancellationToken cancellationToken = default);
}