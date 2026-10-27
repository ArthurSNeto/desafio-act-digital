using CSharpFunctionalExtensions;
using Library.Application.UserModule.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers;

[Route("users")]
[Produces("application/json")]
public sealed class UsersController : BaseController
{
    public UsersController(IMediator mediator)
        : base(mediator)
    { }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateUserCommand command, CancellationToken ct)
    {
        Result result = await Mediator.Send(command, ct);

        return HandleResult(result);
    }
}