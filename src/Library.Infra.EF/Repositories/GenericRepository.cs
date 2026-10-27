using Library.Domain;
using Library.Domain.Repository;
using Library.Infra.EF.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Library.Infra.EF.Repositories;

public class GenericRepository<TEntity>
    : ICreateRepository<TEntity>,
      IRetrieveByIDRepository<TEntity>,
      IUpdateRepository<TEntity>,
      IDeleteRepository<TEntity>
    where TEntity : Entity
{
    public IDatabaseContext Context { get; }

    public GenericRepository(IDatabaseContext context)
    {
        Context = context;
    }

    public async Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        EntityEntry<TEntity> entityEntry = await Context.Set<TEntity>().AddAsync(entity, cancellationToken);
        return entityEntry.Entity;
    }

    public async Task<TEntity?> RetrieveByIDAsync(Guid id, CancellationToken cancellationToken)
    {
        return await Context.Set<TEntity>().FindAsync(id, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        Context.Entry(entity).State = EntityState.Modified;
    }

    public void Delete(TEntity entity)
    {
        Context.Set<TEntity>().Remove(entity);
    }
}
