using Domain;
using Domain.Common;

namespace Infrastructure.Persistence.Repositories;

public abstract class RepositoryBase<T, TKey>(ApplicationDbContext dbContext) : IAsyncDisposable
    where T : class, IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    protected readonly ApplicationDbContext Context = dbContext;

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        Guard.NotNull(entity, nameof(entity), "Empty entity received");

        try
        {
            Context.Set<T>().Add(entity);
            await Context.SaveChangesAsync(cancellationToken);
            return entity;
        }
        catch (Exception ex)
        {
            var entityType = Context.Model.FindRuntimeEntityType(entity.GetType());
            throw new Exception(
                $"Unexpected failure while trying to add entity of type '{entityType}' with id '{entity.Id}'", ex);
        }
    }

    public async Task AddRangeAsync(
      IReadOnlyList<T> entities,
      CancellationToken cancellationToken = default,
      bool saveChanges = true)
    {
        Guard.NotEmpty(entities, nameof(entities));

        try
        {
            await Context.Set<T>().AddRangeAsync(entities, cancellationToken);

            if (saveChanges)
            {
                await Context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            throw new Exception(
                $"Unexpected failure while trying to add entities of type '{typeof(T)}'", ex);
        }
    }

    public virtual async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        Guard.NotNull(entity, nameof(entity), "Empty entity received");

        try
        {
            var entry = Context.Entry(entity);
            if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Detached)
            {
                Context.Set<T>().Update(entity);
            }

            await Context.SaveChangesAsync(cancellationToken);
            return entity;
        }
        catch (Exception ex)
        {
            var entityType = Context.Model.FindRuntimeEntityType(entity.GetType());
            throw new Exception(
                $"Unexpected failure while trying to update entity of type '{entityType}' with id '{entity.Id}'", ex);
        }
    }

    public async Task<T> RemoveAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            Context.Set<T>().Remove(entity);
            await Context.SaveChangesAsync(cancellationToken);
            Context.ChangeTracker.Clear();

            return entity;
        }
        catch (Exception ex)
        {
            var entityType = Context.Model.FindRuntimeEntityType(entity.GetType());
            throw new Exception(
                $"Unexpected failure while trying to delete entity of type '{entityType}' with id '{entity.Id}'", ex);
        }
    }

    public async Task<int> RemoveRangeAsync(
        IReadOnlyList<T> entities,
        bool saveChanges = true,
        CancellationToken cancellationToken = default)
    {
        Guard.NotEmpty(entities, nameof(entities));

        try
        {
            var idsToRemove = entities.Select(x => x.Id).ToList();

            Context.Set<T>().RemoveRange(Context.Set<T>().Where(x => idsToRemove.Contains(x.Id)));
            if (saveChanges)
            {
                await Context.SaveChangesAsync(cancellationToken);
            }

            return entities.Count;
        }
        catch (Exception ex)
        {
            throw new Exception(
                $"Unexpected failure while trying to remove entities of type '{typeof(T)}'", ex);
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await Context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new Exception(
                $"Unexpected failure while trying to save changes", ex);
        }
    }

    public ValueTask DisposeAsync()
    {
        return Context.DisposeAsync();
    }
}