using Application.Common.Permissions;
using Application.Services.Dtos;
using LanguageExt;

namespace Api.Services.Abstract;

public interface IServicesControllerService
{
    Task<IEnumerable<ServiceListDto>> GetAll(
        Permission permission,
        CancellationToken cancellationToken);

    Task<Option<ServiceDto>> Get(
        Permission permission,
        Guid serviceId,
        CancellationToken cancellationToken);

    Task<string> GenerateBulkUploadTemplate(CancellationToken cancellationToken);
}