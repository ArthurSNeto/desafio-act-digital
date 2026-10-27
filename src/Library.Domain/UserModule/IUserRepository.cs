using Library.Domain.Repository;

namespace Library.Domain.UserModule;

public interface IUserRepository :
    ICreateRepository<User>,
    IRetrieveByIDRepository<User>
{ }
