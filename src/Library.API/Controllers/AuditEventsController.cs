using CSharpFunctionalExtensions;
using Library.Application.AuditModule.Queries;
using Library.Domain.AuditModule;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers;

[Route("audit-events")]
[Produces("application/json")]
public sealed class AuditEventsController : BaseController
{
    public AuditEventsController(IMediator mediator)
        : base(mediator)
    { }

    [HttpGet]
    [ProducesResponseType(typeof(List<AuditEvent>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditEvents(CancellationToken ct)
    {
        Result<List<AuditEvent>> result = await Mediator.Send(new GetAuditEventsQuery(), ct);

        return HandleResult(result);
    }
}