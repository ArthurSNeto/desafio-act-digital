using CSharpFunctionalExtensions;
using Library.Domain.UnitOfWork;
using Library.Domain.UserModule;
using MediatR;

namespace Library.Application.UserModule.Commands;
    
public sealed class CreateUserCommand : IRequest<Result<User>>
{ }

public sealed class CreateUserCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork) : IRequestHandler<CreateUserCommand, Result<User>>
{
    public async Task<Result<User>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User();
        await userRepository.CreateAsync(user, cancellationToken);

        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(user);
    }
}
