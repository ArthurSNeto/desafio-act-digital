namespace Library.Domain.UserModule;

public class User : Entity
{
    public User()
    {
        Id = Guid.NewGuid();
    }
}
