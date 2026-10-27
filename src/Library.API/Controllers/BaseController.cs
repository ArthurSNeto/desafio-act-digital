using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers;

[ApiController]
public abstract class BaseController(IMediator mediator) : ControllerBase
{
    protected readonly IMediator Mediator = mediator;

    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return HandleFailure(result.Error);
    }

    protected IActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
        {
            return NoContent();
        }

        return HandleFailure(result.Error);
    }

    protected IActionResult HandleCreated<T>(
        Result<T> result,
        string actionName,
        Func<T, object> routeValuesFactory)
    {
        if (result.IsSuccess)
        {
            var routeValues = routeValuesFactory(result.Value);
            return CreatedAtAction(actionName, routeValues, result.Value);
        }

        return HandleFailure(result.Error);
    }

    private IActionResult HandleFailure(string error)
    {
        var errors = error.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return ValidationProblem(new ValidationProblemDetails(
            new Dictionary<string, string[]> { { "General", errors } })
        {
            Title = "Requisição Inválida",
            Status = StatusCodes.Status400BadRequest,
            Detail = errors.FirstOrDefault() ?? "Ocorreu um erro ao processar a operação.",
            Instance = HttpContext.Request.Path
        });
    }
}
