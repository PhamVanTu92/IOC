-- ─────────────────────────────────────────────────────────────────────────────
-- V007__modules_charts.sql — Modules, Charts, Permissions, Layouts
-- ─────────────────────────────────────────────────────────────────────────────

-- IOC Modules (Sales, Finance, HR, etc.)
CREATE TABLE IF NOT EXISTS modules (
    id          UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID         NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    name        VARCHAR(200) NOT NULL,
    slug        VARCHAR(100) NOT NULL,
    description TEXT,
    icon        VARCHAR(100) DEFAULT '◫',
    color       VARCHAR(20)  DEFAULT '#0ea5e9',
    is_active   BOOLEAN      NOT NULL DEFAULT true,
    sort_order  INT          NOT NULL DEFAULT 0,
    created_by  UUID         REFERENCES users(id),
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    UNIQUE(tenant_id, slug)
);

-- Chart definitions (JSON config per chart)
CREATE TABLE IF NOT EXISTS charts (
    id          UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    module_id   UUID         NOT NULL REFERENCES modules(id) ON DELETE CASCADE,
    tenant_id   UUID         NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    name        VARCHAR(200) NOT NULL,
    description TEXT,
    chart_type  VARCHAR(50)  NOT NULL CHECK (chart_type IN ('line','bar','pie','table','kpi','area','scatter')),
    config_json TEXT         NOT NULL DEFAULT '{}',
    is_active   BOOLEAN      NOT NULL DEFAULT true,
    sort_order  INT          NOT NULL DEFAULT 0,
    created_by  UUID         REFERENCES users(id),
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

-- Which users can view/edit which modules
CREATE TABLE IF NOT EXISTS user_module_permissions (
    id         UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id    UUID        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    module_id  UUID        NOT NULL REFERENCES modules(id) ON DELETE CASCADE,
    can_view   BOOLEAN     NOT NULL DEFAULT true,
    can_edit   BOOLEAN     NOT NULL DEFAULT false,
    granted_by UUID        REFERENCES users(id),
    granted_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(user_id, module_id)
);

-- Which users can edit specific charts (override)
CREATE TABLE IF NOT EXISTS user_chart_permissions (
    id         UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id    UUID        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    chart_id   UUID        NOT NULL REFERENCES charts(id) ON DELETE CASCADE,
    can_view   BOOLEAN     NOT NULL DEFAULT true,
    can_edit   BOOLEAN     NOT NULL DEFAULT false,
    granted_by UUID        REFERENCES users(id),
    granted_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(user_id, chart_id)
);

-- Per-user dashboard layout (per module or global)
CREATE TABLE IF NOT EXISTS dashboard_layouts (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id     UUID        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    module_id   UUID        REFERENCES modules(id) ON DELETE CASCADE,
    layout_json TEXT        NOT NULL DEFAULT '[]',
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(user_id, module_id)
);

CREATE INDEX IF NOT EXISTS idx_charts_module_id ON charts(module_id);
CREATE INDEX IF NOT EXISTS idx_user_module_perm_user ON user_module_permissions(user_id);
CREATE INDEX IF NOT EXISTS idx_user_chart_perm_user  ON user_chart_permissions(user_id);
CREATE INDEX IF NOT EXISTS idx_layouts_user_module   ON dashboard_layouts(user_id, module_id);
