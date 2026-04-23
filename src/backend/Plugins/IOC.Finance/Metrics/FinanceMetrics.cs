namespace IOC.Finance.Metrics;

// ─────────────────────────────────────────────────────────────────────────────
// FinanceMetricDefinition — declares which metrics the Finance plugin exposes
//
// These mirror the SemanticLayer metric definitions but live here so the
// Finance plugin owns its own metric catalogue.
// ─────────────────────────────────────────────────────────────────────────────

public static class FinanceMetrics
{
    public const string Revenue       = "revenue";
    public const string Cost          = "cost";
    public const string GrossProfit   = "gross_profit";
    public const string BudgetUsage   = "budget_usage_pct";
    public const string InvoiceCount  = "invoice_count";
    public const string OverdueAmount = "overdue_amount";

    public const string Domain    = "finance";
    public const string DatasetId = "finance-main";
    public const string Unit      = "VND";
    public const string UnitPct   = "%";
    public const string UnitCount = "invoice";
}
