namespace DashboardService.Domain;

public sealed class DashboardNotFoundException(Guid id)
    : Exception($"Dashboard '{id}' not found.");

public sealed class DashboardAccessDeniedException(Guid id)
    : Exception($"Access denied to dashboard '{id}'.");
