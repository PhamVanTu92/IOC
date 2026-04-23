using DashboardService.Domain;
using HotChocolate;

namespace Gateway.Infrastructure;

// ─────────────────────────────────────────────────────────────────────────────
// GraphQLErrorFilter — maps domain exceptions to structured GraphQL errors
// ─────────────────────────────────────────────────────────────────────────────

public sealed class GraphQLErrorFilter : IErrorFilter
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

            _ => error
        };
    }
}
