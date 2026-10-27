using Library.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using System.Diagnostics;

namespace Library.API.Configurations;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        (int statusCode, string? title, string? detail, string? type) = MapException(exception);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Exceção não tratada capturada pelo GlobalExceptionHandler: {Message}", exception.Message);
        }
        else
        {
            logger.LogWarning("Falha tratada [{StatusCode}]: {Message}", statusCode, exception.Message);
        }

        httpContext.Response.StatusCode = statusCode;

        string traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        ProblemDetails problemDetails = new()
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = traceId;
        problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;

        if (httpContext.Request.Headers.TryGetValue("X-Correlation-Id", out StringValues correlationId))
        {
            problemDetails.Extensions["correlationId"] = correlationId.ToString();
        }

        if (exception is FluentValidation.ValidationException fluentValidationException)
        {
            Dictionary<string, string[]> validationErrors = fluentValidationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(e => e.ErrorMessage).ToArray()
                );

            problemDetails.Extensions["errors"] = validationErrors;
        }
        else if (exception is ValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors;
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }

    private static (int StatusCode, string Title, string Detail, string Type) MapException(Exception exception)
    {
        return exception switch
        {
            FluentValidation.ValidationException => (
                StatusCodes.Status400BadRequest,
                "Falha de Validação",
                "Um ou mais erros de validação ocorreram nos campos enviados.",
                "https://tools.ietf.org/html/rfc9110#section-15.5.1"
            ),
            ValidationException ex => (
                StatusCodes.Status400BadRequest,
                "Falha de Validação",
                ex.Message,
                "https://tools.ietf.org/html/rfc9110#section-15.5.1"
            ),
            EntityNotFoundException ex => (
                StatusCodes.Status404NotFound,
                "Recurso Não Encontrado",
                ex.Message,
                "https://tools.ietf.org/html/rfc9110#section-15.5.5"
            ),
            ConcurrencyConflictException or DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "Conflito de Concorrência",
                exception.Message,
                "https://tools.ietf.org/html/rfc9110#section-15.5.10"
            ),
            BusinessRuleException ex => (
                StatusCodes.Status422UnprocessableEntity,
                "Regra de Negócio Violada",
                ex.Message,
                "https://tools.ietf.org/html/rfc9110#section-15.5.21"
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Erro Interno no Servidor",
                "Ocorreu um erro interno inesperado ao processar a requisição.",
                "https://tools.ietf.org/html/rfc9110#section-15.6.1"
            )
        };
    }
}