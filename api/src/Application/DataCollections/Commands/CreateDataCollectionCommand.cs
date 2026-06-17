using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.DataCollections.Exceptions;
using Domain.DataCollections;
using Domain.Organisations;
using LanguageExt;
using MediatR;

namespace Application.DataCollections.Commands;

public record CreateDataCollectionCommand : IRequest<Either<DataCollectionException, DataCollection>>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public bool IsSubmittedByAllLocalAuthorities { get; init; } = true;
    public bool SaveAsDraft { get; init; } = false;
    public IReadOnlyList<Guid> LocalAuthorityIds { get; init; } = [];
    public IReadOnlyList<Guid> FormModuleIds { get; init; } = [];
}

public class CreateDataCollectionCommandHandler(
    PermissionsService permissionsService,
    IDataCollectionRepository repository)
    : IRequestHandler<CreateDataCollectionCommand, Either<DataCollectionException, DataCollection>>
{
    public async Task<Either<DataCollectionException, DataCollection>> Handle(
        CreateDataCollectionCommand request,
        CancellationToken cancellationToken)
    {
        var dataCollectionWithName = await repository.FindByNameAsync(
            request.Name,
            cancellationToken);

        return await dataCollectionWithName.MatchAsync(
            dc => new DataCollectionAlreadyExistsException(dc.Id, request.Name),
            () => CreateDataCollection(request, cancellationToken));
    }

    private async Task<Either<DataCollectionException, DataCollection>> CreateDataCollection(
        CreateDataCollectionCommand request,
        CancellationToken cancellationToken)
    {
        var userIdResult = permissionsService.GetUserId();

        return await userIdResult.MatchAsync(
            userId => CreateDataCollectionWithUser(request, userId, cancellationToken),
            e => new DataCollectionUnknownException(DataCollectionId.Empty(), new Exception(e.Message)));
    }

    private async Task<Either<DataCollectionException, DataCollection>> CreateDataCollectionWithUser(
        CreateDataCollectionCommand request,
        Domain.Users.UserId userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var dataCollection = DataCollection.New(
                DataCollectionId.New(),
                request.Name,
                request.Description?.Trim(),
                request.StartDate,
                request.EndDate,
                request.IsSubmittedByAllLocalAuthorities,
                request.SaveAsDraft);

            if (!request.IsSubmittedByAllLocalAuthorities)
            {
                foreach (var laId in request.LocalAuthorityIds)
                {
                    dataCollection.AssignLocalAuthority(new OrganisationId(laId), userId);
                }
            }

            foreach (var formModuleId in request.FormModuleIds)
            {
                dataCollection.AssignFormModule(new DataCollectionFormModuleId(formModuleId));
            }

            var result = await repository.AddAsync(dataCollection, cancellationToken);
            return result;
        }
        catch (Exception exception)
        {
            return new DataCollectionUnknownException(DataCollectionId.Empty(), exception);
        }
    }
}