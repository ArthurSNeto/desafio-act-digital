using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected readonly IMediator Mediator;

    protected BaseController(IMediator mediator)
    {
        Mediator = mediator;
    }

    protected IActionResult HandleResult<T>(Result<T> result)
    {
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { Errors = result.Error.Split('\n') });
    }

    protected IActionResult HandleResult(Result result)
    {
        return result.IsSuccess ? Ok() : BadRequest(new { Errors = result.Error.Split('\n') });
    }
}
