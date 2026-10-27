using CSharpFunctionalExtensions;
using Library.Domain.UnitOfWork;
using Library.Domain.UserModule;
using MediatR;

namespace Library.Application.UserModule.Commands;

public sealed class CreateUserCommand : IRequest<Result>
{ }

public sealed class CreateUserCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork) : IRequestHandler<CreateUserCommand, Result>
{
    public async Task<Result> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        await userRepository.CreateAsync(new User(), cancellationToken);

        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
