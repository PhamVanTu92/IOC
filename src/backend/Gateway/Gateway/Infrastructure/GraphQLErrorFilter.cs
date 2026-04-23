using DashboardService.Domain;
using HotChocolate;
using Microsoft.Extensions.Logging;

namespace Gateway.Infrastructure;

// ─────────────────────────────────────────────────────────────────────────────
// GraphQLErrorFilter — maps domain exceptions to structured GraphQL errors
// and logs unexpected exceptions for debugging
// ─────────────────────────────────────────────────────────────────────────────

public sealed class GraphQLErrorFilter(ILogger<GraphQLErrorFilter> logger) : IErrorFilter
{
    public IError OnError(IError error)
    {
        return error.Exception switch
        {
            DashboardNotFoundException ex =>
                error.WithMessage(ex.Message)
                     .WithCode("DASHBOARD_NOT_FOUND")
                     .RemoveException(),

            DashboardAccessDeniedException ex =>
                error.WithMessage(ex.Message)
                     .WithCode("ACCESS_DENIED")
                     .RemoveException(),

            ArgumentException ex =>
                error.WithMessage(ex.Message)
                     .WithCode("INVALID_ARGUMENT")
                     .RemoveException(),

            // Log all unexpected exceptions — visible in docker logs
            Exception ex =>
                LogAndReturn(error, ex),

            _ => error
        };
    }

    private IError LogAndReturn(IError error, Exception ex)
    {
        logger.LogError(ex,
            "Unexpected GraphQL execution error at path [{Path}]: {Message}",
            string.Join(".", error.Path ?? []),
            ex.Message);
        return error;
    }
}
