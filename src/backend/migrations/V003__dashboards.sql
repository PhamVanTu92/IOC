-- ─────────────────────────────────────────────────────────────────────────────
-- V003 — Dashboards table
-- Flyway migration; run after V001 (initial schema) and V002 (datasets/dims/...)
-- ─────────────────────────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS dashboards (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID        NOT NULL,
    created_by  UUID        NOT NULL,
    title       VARCHAR(200) NOT NULL,
    description TEXT,
    -- Full serialized frontend DashboardConfig as JSONB for schema flexibility
    config_json JSONB       NOT NULL DEFAULT '{}',
    is_active   BOOLEAN     NOT NULL DEFAULT true,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ── Indexes ──────────────────────────────────────────────────────────────────

-- Primary access pattern: list dashboards by tenant (active only)
CREATE INDEX IF NOT EXISTS ix_dashboards_tenant_active
    ON dashboards (tenant_id, updated_at DESC)
    WHERE is_active = true;

-- For querying widgets within config_json (optional — for future analytics)
-- CREATE INDEX IF NOT EXISTS ix_dashboards_config_gin
--     ON dashboards USING GIN (config_json);

-- ── Trigger: auto-update updated_at ──────────────────────────────────────────

CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_dashboards_updated_at ON dashboards;
CREATE TRIGGER trg_dashboards_updated_at
    BEFORE UPDATE ON dashboards
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- ── Comments ──────────────────────────────────────────────────────────────────

COMMENT ON TABLE  dashboards            IS 'Saved IOC dashboard configurations, one per user-defined dashboard.';
COMMENT ON COLUMN dashboards.config_json IS 'Serialized frontend DashboardConfig JSON (widgets, layouts, chart configs).';
COMMENT ON COLUMN dashboards.is_active   IS 'Soft-delete flag — false means logically deleted.';
