namespace IOC.Kafka;

// ─────────────────────────────────────────────────────────────────────────────
// KafkaTopics — single source of truth for topic names
// Convention: ioc.{domain}.{event-verb}
// ─────────────────────────────────────────────────────────────────────────────

public static class KafkaTopics
{
    // ── Metrics / Query engine ────────────────────────────────────────────────
    /// <summary>Fired when a dataset's metric values change (e.g. after ETL refresh).</summary>
    public const string MetricUpdated = "ioc.metrics.updated";

    /// <summary>Fired after a semantic query executes successfully.</summary>
    public const string QueryExecuted = "ioc.query.executed";

    // ── Dashboard ─────────────────────────────────────────────────────────────
    public const string DashboardSaved   = "ioc.dashboard.saved";
    public const string DashboardDeleted = "ioc.dashboard.deleted";

    // ── Finance plugin ────────────────────────────────────────────────────────
    public const string FinanceBudgetUpdated  = "ioc.finance.budget-updated";
    public const string FinanceInvoiceCreated = "ioc.finance.invoice-created";

    // ── HR plugin ─────────────────────────────────────────────────────────────
    public const string HrEmployeeJoined  = "ioc.hr.employee-joined";
    public const string HrLeaveApproved   = "ioc.hr.leave-approved";

    // ── Marketing plugin ──────────────────────────────────────────────────────
    public const string MarketingCampaignLaunched = "ioc.marketing.campaign-launched";

    // ── System ────────────────────────────────────────────────────────────────
    public const string SystemErrors = "ioc.system.errors";
    public const string SystemAlerts = "ioc.system.alert";
}
