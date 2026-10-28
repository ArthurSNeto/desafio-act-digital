using CSharpFunctionalExtensions;
using Library.Application.Telemetry;
using Library.Domain.UnitOfWork;
using Library.Domain.UserModule;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Library.Application.UserModule.Commands;
    
public sealed class CreateUserCommand : IRequest<Result<User>>
{ }

public sealed class CreateUserCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreateUserCommandHandler> logger) : IRequestHandler<CreateUserCommand, Result<User>>
{
    public async Task<Result<User>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        using Activity? activity = LibraryTelemetry.ActivitySource.StartActivity("CreateUser");

        var user = new User();
        await userRepository.CreateAsync(user, cancellationToken);

        await unitOfWork.CommitAsync(cancellationToken);

        activity?.SetTag("library.user.id", user.Id.ToString());
        activity?.SetStatus(ActivityStatusCode.Ok);

        logger.LogInformation("Usuário/Leitor {UserId} cadastrado com sucesso.", user.Id);

        return Result.Success(user);
    }
}
