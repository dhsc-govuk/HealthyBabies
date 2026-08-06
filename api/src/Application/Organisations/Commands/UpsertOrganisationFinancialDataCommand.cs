using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.Organisations;
using LanguageExt;
using MediatR;

namespace Application.Organisations.Commands;

public record FinancialDataEntryCommand(string Module, string YearRange, decimal Amount);

public record UpsertOrganisationFinancialDataCommand : IRequest<Either<Exception, IReadOnlyList<OrganisationFinancialData>>>
{
    public required Guid OrganisationId { get; init; }
    public required IReadOnlyList<FinancialDataEntryCommand> Entries { get; init; }
}

public class UpsertOrganisationFinancialDataCommandHandler(
    IOrganisationFinancialDataRepository repository,
    IOrganisationFinancialDataQueries queries)
    : IRequestHandler<UpsertOrganisationFinancialDataCommand, Either<Exception, IReadOnlyList<OrganisationFinancialData>>>
{
    public async Task<Either<Exception, IReadOnlyList<OrganisationFinancialData>>> Handle(
        UpsertOrganisationFinancialDataCommand request,
        CancellationToken cancellationToken)
    {
        var organisationId = new OrganisationId(request.OrganisationId);
        var existing = await queries.GetAllByOrganisationIdAsync(organisationId, cancellationToken);

        var results = new List<OrganisationFinancialData>();

        foreach (var entry in request.Entries)
        {
            var module = FinancialDataModule.From(entry.Module);
            var match = existing.FirstOrDefault(x => x.Module.Value == entry.Module && x.YearRange == entry.YearRange);

            if (match is not null)
            {
                match.UpdateAmount(entry.Amount);
                var updated = await repository.UpdateAsync(match, cancellationToken);
                results.Add(updated);
            }
            else
            {
                var newEntry = OrganisationFinancialData.New(
                    OrganisationFinancialDataId.New(),
                    organisationId,
                    module,
                    entry.YearRange,
                    entry.Amount);
                var created = await repository.AddAsync(newEntry, cancellationToken);
                results.Add(created);
            }
        }

        return Either<Exception, IReadOnlyList<OrganisationFinancialData>>.Right(results);
    }
}