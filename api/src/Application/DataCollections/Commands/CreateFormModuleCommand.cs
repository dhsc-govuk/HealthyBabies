using System.Text.RegularExpressions;
using Application.Common.Interfaces.Repositories;
using Application.DataCollections.Exceptions;
using Domain.DataCollections;
using LanguageExt;
using MediatR;

namespace Application.DataCollections.Commands;

public record CreateFormModuleCommand : IRequest<Either<FormModuleException, DataCollectionFormModule>>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Type { get; init; } = string.Empty;
}

public class CreateFormModuleCommandHandler(
    IDataCollectionFormModuleRepository moduleRepository)
    : IRequestHandler<CreateFormModuleCommand, Either<FormModuleException, DataCollectionFormModule>>
{
    private static readonly Regex NonAlphanumeric = new(@"[^a-z0-9]+", RegexOptions.Compiled);

    public async Task<Either<FormModuleException, DataCollectionFormModule>> Handle(
        CreateFormModuleCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Type != DataCollectionTypeConstants.DeliveryPlan
            && request.Type != DataCollectionTypeConstants.ManagementInformation)
        {
            return new FormModuleInvalidTypeException(request.Type);
        }

        var code = await GenerateUniqueCode(request.Name, cancellationToken);

        var sectionNumber = await moduleRepository.GetNextSectionNumber(cancellationToken);

        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(),
            code,
            sectionNumber,
            request.Name,
            request.Description,
            type: DataCollectionType.From(request.Type));

        await moduleRepository.AddAsync(module, cancellationToken);

        return module;
    }

    // The Code is the module's system-managed identifier - never accepted from the client (BLUM-7067).
    // Slugified from Name, with a numeric suffix appended on collision so it stays unique.
    private async Task<string> GenerateUniqueCode(string name, CancellationToken cancellationToken)
    {
        var baseCode = NonAlphanumeric.Replace(name.Trim().ToLowerInvariant(), "-").Trim('-');

        var code = baseCode;
        var suffix = 2;
        while (await moduleRepository.ExistsByCode(code, cancellationToken))
        {
            code = $"{baseCode}-{suffix}";
            suffix++;
        }

        return code;
    }
}