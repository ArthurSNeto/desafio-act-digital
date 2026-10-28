using CSharpFunctionalExtensions;
using Library.Application.LoanModule.Models;
using Library.Application.UserModule.Commands;
using Library.Application.UserModule.Queries;
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
        var result = await Mediator.Send(command, ct);

        return HandleCreated(result, nameof(GetLoansByUserId), user => new { id = user.Id });
    }

    [HttpGet("{id:guid}/loans")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLoansByUserId([FromRoute] Guid id, CancellationToken ct)
    {
        var command = new GetLoansByUserIdQuery(id);
        Result<IReadOnlyList<DetailedLoanResponse>> result = await Mediator.Send(command, ct);

        return HandleResult(result);
    }
}