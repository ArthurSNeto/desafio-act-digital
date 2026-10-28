using CSharpFunctionalExtensions;
using Library.Application.BookModule.Commands;
using Library.Application.BookModule.Models;
using Library.Application.BookModule.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers;

[Route("books")]
[Produces("application/json")]
public sealed class BooksController : BaseController
{
    public BooksController(IMediator mediator)
        : base(mediator)
    { }

    [HttpPost]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateBookCommand command, CancellationToken ct)
    {
        Result<BookResponse> result = await Mediator.Send(command, ct);

        return HandleCreated(result, nameof(GetById), book => new { id = book.Id });
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BookResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] bool onlyActive = true, CancellationToken ct = default)
    {
        Result<IReadOnlyList<BookResponse>> result = await Mediator.Send(new ListBooksQuery(onlyActive), ct);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        Result<BookResponse> result = await Mediator.Send(new GetBookByIdQuery(id), ct);
        return HandleResult(result);
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateBookDto dto, CancellationToken ct)
    {
        Result<BookResponse> result = await Mediator.Send(new UpdateBookCommand(id, dto.Title, dto.Author, dto.Isbn, dto.TotalCopies), ct);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        Result<BookResponse> result = await Mediator.Send(new DeleteBookCommand(id), ct);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}/availability")]
    [ProducesResponseType(typeof(BookAvailableResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckBookAvailability([FromRoute] Guid id, CancellationToken ct)
    {
        Result<BookAvailableResponse> result = await Mediator.Send(new CheckBookAvailabilityQuery(id), ct);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(BookLoanHistoryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBookLoanHistory([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetBookLoanHistoryQuery(id), ct);
        return HandleResult(result);
    }
}

public record UpdateBookDto(string? Title, string? Author, string? Isbn, int TotalCopies);
