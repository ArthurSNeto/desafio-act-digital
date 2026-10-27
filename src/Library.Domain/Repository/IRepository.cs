namespace Library.Domain.Repository;

public interface ICreateRepository<TEntity> where TEntity : class
{
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken);
}

public interface IRetrieveByIDRepository<TEntity> where TEntity : Entity
{
    Task<TEntity?> RetrieveByIDAsync(Guid id, CancellationToken cancellationToken);
}

public interface IUpdateRepository<in TEntity> where TEntity : class
{
    void Update(TEntity entity);
}

public interface IDeleteRepository<in TEntity> where TEntity : class
{
    void Delete(TEntity entity);
}
