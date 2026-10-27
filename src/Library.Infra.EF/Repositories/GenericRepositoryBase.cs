using Library.Domain;
using Library.Infra.EF.Context;

namespace Library.Infra.EF.Repositories;

public abstract class GenericRepositoryBase<TEntity> where TEntity : Entity
{
    protected GenericRepository<TEntity> GenericRepository;

    protected GenericRepositoryBase(IDatabaseContext context)
    {
        GenericRepository = new GenericRepository<TEntity>(context);
    }
}
