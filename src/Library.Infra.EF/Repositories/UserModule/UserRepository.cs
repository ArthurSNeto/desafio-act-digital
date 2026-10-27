using Library.Domain.UserModule;
using Library.Infra.EF.Context;

namespace Library.Infra.EF.Repositories.UserModule;

public class UserRepository : GenericRepositoryBase<User>, IUserRepository
{
    public UserRepository(IApplicationDbContext dbContext)
        : base(dbContext)
    { }

    public Task<User> CreateAsync(User entity, CancellationToken cancellationToken)
    {
        return GenericRepository.CreateAsync(entity, cancellationToken);
    }
}
