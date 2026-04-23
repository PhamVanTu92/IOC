using DashboardService.Application.DTOs;
using MediatR;

namespace DashboardService.Application.Commands.SaveDashboard;

// ─────────────────────────────────────────────────────────────────────────────
// SaveDashboardCommand — upsert semantics
//   • Id == null  → CREATE
//   • Id != null  → UPDATE (must belong to the same tenant)
// ─────────────────────────────────────────────────────────────────────────────

public sealed record SaveDashboardCommand(
    Guid? Id,
    Guid TenantId,
    Guid UserId,
    string Title,
    string ConfigJson,
    string? Description = null) : IRequest<DashboardDto>;
