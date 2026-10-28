using CSharpFunctionalExtensions;
using Library.Application.LoanModule.Commands;
using Library.Application.LoanModule.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers;

[Route("loans")]
[Produces("application/json")]
public sealed class LoansController : BaseController
{
    public LoansController(IMediator mediator)
        : base(mediator)
    { }

    [HttpPost]
    [ProducesResponseType(typeof(LoanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, [FromBody] CreateLoanDto dto, CancellationToken ct)
    {
        CreateLoanCommand command = new(dto.BookId, dto.UserId, idempotencyKey ?? string.Empty, dto.ExpectedReturnDateUtc);
        var result = await Mediator.Send(command, ct);

        if (result.IsFailure)
            return HandleResult(Result.Failure<LoanResponse>(result.Error));

        if (result.Value.IsReplay)
            return Ok(result.Value.Data);

        return CreatedAtAction(nameof(Create), new { id = result.Value.Data.Id }, result.Value.Data);
    }

    [HttpPost("{id:guid}/return")]
    [ProducesResponseType(typeof(LoanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Return([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new ReturnLoanCommand(id), ct);

        return HandleResult(result);
    }
}

public record CreateLoanDto(Guid BookId, Guid UserId, DateTime ExpectedReturnDateUtc);
