using Library.Application.BookModule.Commands;
using Library.Application.BookModule.Models;
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
        var result = await Mediator.Send(command, ct);

        return HandleResult(result);
    }
}
