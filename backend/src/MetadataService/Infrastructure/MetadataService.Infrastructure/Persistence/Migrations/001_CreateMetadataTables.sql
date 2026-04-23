-- =============================================================================
-- IOC Platform — Metadata Service Schema
-- Migration: 001_CreateMetadataTables
-- =============================================================================

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- =============================================================================
-- DATASETS
-- =============================================================================
CREATE TABLE IF NOT EXISTS datasets (
    id              UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID          NOT NULL,
    name            VARCHAR(255)  NOT NULL,
    description     TEXT,
    source_type     VARCHAR(50)   NOT NULL CHECK (source_type IN ('postgresql', 'view', 'custom_sql')),
    schema_name     VARCHAR(255),
    table_name      VARCHAR(255),
    custom_sql      TEXT,
    is_active       BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_by      UUID          NOT NULL,
    CONSTRAINT uq_dataset_tenant_name UNIQUE (tenant_id, name)
);

CREATE INDEX IF NOT EXISTS idx_datasets_tenant_id    ON datasets (tenant_id);
CREATE INDEX IF NOT EXISTS idx_datasets_is_active    ON datasets (tenant_id, is_active);

-- =============================================================================
-- DIMENSIONS
-- =============================================================================
CREATE TABLE IF NOT EXISTS dimensions (
    id                   UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    dataset_id           UUID          NOT NULL REFERENCES datasets(id) ON DELETE CASCADE,
    tenant_id            UUID          NOT NULL,
    name                 VARCHAR(255)  NOT NULL,
    display_name         VARCHAR(255)  NOT NULL,
    description          TEXT,
    column_name          VARCHAR(255)  NOT NULL,
    custom_sql_expression TEXT,
    data_type            VARCHAR(50)   NOT NULL DEFAULT 'string'
                             CHECK (data_type IN ('string','number','integer','decimal','date','datetime','boolean','json')),
    format               VARCHAR(255),
    is_time_dimension    BOOLEAN       NOT NULL DEFAULT FALSE,
    default_granularity  VARCHAR(20)   CHECK (default_granularity IN ('hour','day','week','month','quarter','year')),
    sort_order           INTEGER       NOT NULL DEFAULT 0,
    is_active            BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at           TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_dimension_dataset_name UNIQUE (dataset_id, name)
);

CREATE INDEX IF NOT EXISTS idx_dimensions_dataset_id ON dimensions (dataset_id);
CREATE INDEX IF NOT EXISTS idx_dimensions_tenant_id  ON dimensions (tenant_id);

-- =============================================================================
-- MEASURES
-- =============================================================================
CREATE TABLE IF NOT EXISTS measures (
    id                   UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    dataset_id           UUID          NOT NULL REFERENCES datasets(id) ON DELETE CASCADE,
    tenant_id            UUID          NOT NULL,
    name                 VARCHAR(255)  NOT NULL,
    display_name         VARCHAR(255)  NOT NULL,
    description          TEXT,
    column_name          VARCHAR(255)  NOT NULL,
    custom_sql_expression TEXT,
    aggregation_type     VARCHAR(50)   NOT NULL DEFAULT 'sum'
                             CHECK (aggregation_type IN ('sum','average','count','count_distinct','min','max')),
    data_type            VARCHAR(50)   NOT NULL DEFAULT 'decimal'
                             CHECK (data_type IN ('number','integer','decimal')),
    format               VARCHAR(255),
    filter_expression    TEXT,
    sort_order           INTEGER       NOT NULL DEFAULT 0,
    is_active            BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at           TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_measure_dataset_name UNIQUE (dataset_id, name)
);

CREATE INDEX IF NOT EXISTS idx_measures_dataset_id ON measures (dataset_id);
CREATE INDEX IF NOT EXISTS idx_measures_tenant_id  ON measures (tenant_id);

-- =============================================================================
-- METRICS (derived / computed measures)
-- =============================================================================
CREATE TABLE IF NOT EXISTS metrics (
    id                   UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    dataset_id           UUID          NOT NULL REFERENCES datasets(id) ON DELETE CASCADE,
    tenant_id            UUID          NOT NULL,
    name                 VARCHAR(255)  NOT NULL,
    display_name         VARCHAR(255)  NOT NULL,
    description          TEXT,
    expression           TEXT          NOT NULL,    -- SQL with {{measure_name}} placeholders
    data_type            VARCHAR(50)   NOT NULL DEFAULT 'decimal',
    format               VARCHAR(255),
    depends_on_measures  TEXT[]        NOT NULL DEFAULT '{}',
    sort_order           INTEGER       NOT NULL DEFAULT 0,
    is_active            BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at           TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_metric_dataset_name UNIQUE (dataset_id, name)
);

CREATE INDEX IF NOT EXISTS idx_metrics_dataset_id ON metrics (dataset_id);
CREATE INDEX IF NOT EXISTS idx_metrics_tenant_id  ON metrics (tenant_id);

-- =============================================================================
-- DASHBOARD CONFIGS (used in STEP 5)
-- =============================================================================
CREATE TABLE IF NOT EXISTS dashboard_configs (
    id           UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id    UUID          NOT NULL,
    name         VARCHAR(255)  NOT NULL,
    description  TEXT,
    config       JSONB         NOT NULL DEFAULT '{}',
    is_published BOOLEAN       NOT NULL DEFAULT FALSE,
    created_at   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_by   UUID          NOT NULL,
    CONSTRAINT uq_dashboard_tenant_name UNIQUE (tenant_id, name)
);

CREATE INDEX IF NOT EXISTS idx_dashboard_configs_tenant ON dashboard_configs (tenant_id);

-- =============================================================================
-- CHART CONFIGS (saved chart builder states)
-- =============================================================================
CREATE TABLE IF NOT EXISTS chart_configs (
    id           UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id    UUID          NOT NULL,
    dataset_id   UUID          NOT NULL REFERENCES datasets(id),
    name         VARCHAR(255)  NOT NULL,
    description  TEXT,
    query_input  JSONB         NOT NULL DEFAULT '{}',   -- serialized QueryInput
    chart_type   VARCHAR(50)   NOT NULL DEFAULT 'bar',  -- 'bar','line','pie','area','scatter'
    chart_options JSONB        NOT NULL DEFAULT '{}',   -- ECharts option overrides
    created_at   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_by   UUID          NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_chart_configs_tenant    ON chart_configs (tenant_id);
CREATE INDEX IF NOT EXISTS idx_chart_configs_dataset   ON chart_configs (dataset_id);

-- =============================================================================
-- QUERY CACHE LOG (audit)
-- =============================================================================
CREATE TABLE IF NOT EXISTS query_logs (
    id              UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID          NOT NULL,
    dataset_id      UUID          NOT NULL,
    cache_key       TEXT          NOT NULL,
    generated_sql   TEXT,
    execution_ms    INTEGER,
    row_count       INTEGER,
    from_cache      BOOLEAN       NOT NULL DEFAULT FALSE,
    error_message   TEXT,
    executed_at     TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_query_logs_tenant     ON query_logs (tenant_id, executed_at DESC);
CREATE INDEX IF NOT EXISTS idx_query_logs_dataset    ON query_logs (dataset_id, executed_at DESC);
