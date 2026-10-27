using Library.Application.Interfaces;
using Microsoft.Extensions.Primitives;

namespace Library.API.Services;

public sealed class CurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    private const string CorrelationHeader = "X-Correlation-Id";
    private const string ActorHeader = "X-Actor";

    public string CorrelationId
    {
        get
        {
            HttpContext? context = httpContextAccessor.HttpContext;
            if (context is null)
                return Guid.NewGuid().ToString();

            if (context.Response.Headers.TryGetValue(CorrelationHeader, out StringValues responseId) && !string.IsNullOrWhiteSpace(responseId))
                return responseId.ToString();

            if (context.Request.Headers.TryGetValue(CorrelationHeader, out StringValues requestId) && !string.IsNullOrWhiteSpace(requestId))
                return requestId.ToString();

            return context.Items.TryGetValue("CorrelationId", out var itemCorrId) && itemCorrId is string corrStr
                ? corrStr
                : context.TraceIdentifier;
        }
    }

    public string CurrentUser
    {
        get
        {
            HttpContext? context = httpContextAccessor.HttpContext;
            if (context is null)
                return "System";

            if (context.User?.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(context.User.Identity.Name))
                return context.User.Identity.Name;

            return context.Request.Headers.TryGetValue(ActorHeader, out StringValues actor) && !string.IsNullOrWhiteSpace(actor)
                ? actor.ToString()
                : "Anonymous/System";
        }
    }
}
