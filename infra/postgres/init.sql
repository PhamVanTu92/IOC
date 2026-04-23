-- ─────────────────────────────────────────────────────────────────────────────
-- IOC Platform — PostgreSQL Init Script
-- Chạy tự động khi container khởi động lần đầu
-- ─────────────────────────────────────────────────────────────────────────────

-- Extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";   -- full-text search sau này

-- ─────────────────────────────────────────────────────────────────────────────
-- Tenants
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tenants (
    id          UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    name        VARCHAR(100) NOT NULL UNIQUE,
    slug        VARCHAR(50)  NOT NULL UNIQUE,
    is_active   BOOLEAN      NOT NULL DEFAULT true,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

-- ─────────────────────────────────────────────────────────────────────────────
-- Dashboards
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS dashboards (
    id          UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID         NOT NULL,
    created_by  UUID         NOT NULL,
    title       VARCHAR(200) NOT NULL,
    description TEXT,
    config_json JSONB        NOT NULL DEFAULT '{}',
    is_active   BOOLEAN      NOT NULL DEFAULT true,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_dashboards_tenant_active
    ON dashboards (tenant_id, updated_at DESC)
    WHERE is_active = true;

CREATE INDEX IF NOT EXISTS ix_dashboards_config_gin
    ON dashboards USING GIN (config_json);

-- ─────────────────────────────────────────────────────────────────────────────
-- Datasets metadata (semantic layer registry)
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS datasets (
    id          UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID,                          -- NULL = shared/global
    name        VARCHAR(100) NOT NULL,
    description TEXT,
    config_json JSONB        NOT NULL DEFAULT '{}',
    is_active   BOOLEAN      NOT NULL DEFAULT true,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

-- ─────────────────────────────────────────────────────────────────────────────
-- Query history (audit + cache warming)
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS query_history (
    id               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id        UUID,
    user_id          UUID,
    dataset_id       UUID,
    dimensions       TEXT[]      NOT NULL DEFAULT '{}',
    measures         TEXT[]      NOT NULL DEFAULT '{}',
    row_count        INT,
    execution_ms     INT,
    from_cache       BOOLEAN     NOT NULL DEFAULT false,
    generated_sql    TEXT,
    executed_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_query_history_tenant_date
    ON query_history (tenant_id, executed_at DESC);

-- ─────────────────────────────────────────────────────────────────────────────
-- Auto-update updated_at trigger
-- ─────────────────────────────────────────────────────────────────────────────
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

-- ─────────────────────────────────────────────────────────────────────────────
-- Seed data (dev / staging)
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO tenants (id, name, slug) VALUES
    ('00000000-0000-0000-0000-000000000001', 'IOC Demo', 'ioc-demo'),
    ('00000000-0000-0000-0000-000000000002', 'Finance Dept', 'finance')
ON CONFLICT (slug) DO NOTHING;

INSERT INTO datasets (tenant_id, name, description, config_json) VALUES
    (NULL, 'Finance', 'Dữ liệu tài chính tổng hợp',
     '{"domain":"finance","refreshInterval":300}'),
    (NULL, 'HR', 'Dữ liệu nhân sự',
     '{"domain":"hr","refreshInterval":600}')
ON CONFLICT DO NOTHING;
