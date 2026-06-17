using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.DataCollections.Exceptions;
using Domain.DataCollections;
using LanguageExt;
using MediatR;

namespace Application.DataCollections.Commands;

public record DuplicateDataCollectionCommand : IRequest<Either<DataCollectionException, DataCollection>>
{
    public Guid SourceId { get; init; }
    public string? NewName { get; init; }
}

public class DuplicateDataCollectionCommandHandler(
    PermissionsService permissionsService,
    IDataCollectionRepository repository)
    : IRequestHandler<DuplicateDataCollectionCommand, Either<DataCollectionException, DataCollection>>
{
    public async Task<Either<DataCollectionException, DataCollection>> Handle(
        DuplicateDataCollectionCommand request,
        CancellationToken cancellationToken)
    {
        var sourceId = new DataCollectionId(request.SourceId);
        var source = await repository.GetByIdWithLocalAuthoritiesAsync(sourceId, cancellationToken);

        return await source.MatchAsync(
            dc => DuplicateDataCollection(dc, request.NewName, cancellationToken),
            () => new DataCollectionDoesNotExistException(sourceId));
    }

    private async Task<Either<DataCollectionException, DataCollection>> DuplicateDataCollection(
        DataCollection source,
        string? newName,
        CancellationToken cancellationToken)
    {
        var userIdResult = permissionsService.GetUserId();

        return await userIdResult.MatchAsync(
            userId => CreateDuplicate(source, newName, userId, cancellationToken),
            e => new DataCollectionUnknownException(DataCollectionId.Empty(), new Exception(e.Message)));
    }

    private async Task<Either<DataCollectionException, DataCollection>> CreateDuplicate(
        DataCollection source,
        string? newName,
        Domain.Users.UserId userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var duplicateName = newName ?? $"{source.Name} (Copy)";

            var existingWithName = await repository.FindByNameAsync(duplicateName, cancellationToken);
            if (existingWithName.IsSome)
            {
                duplicateName = $"{duplicateName} - {DateTime.UtcNow:yyyyMMddHHmmss}";
            }

            var duplicate = DataCollection.New(
                DataCollectionId.New(),
                duplicateName,
                source.Description,
                source.StartDate,
                source.EndDate,
                source.IsSubmittedByAllLocalAuthorities);

            if (!source.IsSubmittedByAllLocalAuthorities)
            {
                foreach (var la in source.LocalAuthorities)
                {
                    duplicate.AssignLocalAuthority(la.LocalAuthorityId, userId);
                }
            }

            return await repository.AddAsync(duplicate, cancellationToken);
        }
        catch (Exception exception)
        {
            return new DataCollectionUnknownException(DataCollectionId.Empty(), exception);
        }
    }
}