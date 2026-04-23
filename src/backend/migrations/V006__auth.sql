-- ─────────────────────────────────────────────────────────────────────────────
-- V006__auth.sql — Auth tables: tenants + users
-- ─────────────────────────────────────────────────────────────────────────────

-- Tenants
CREATE TABLE IF NOT EXISTS tenants (
    id          UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    name        VARCHAR(200) NOT NULL,
    slug        VARCHAR(100) NOT NULL UNIQUE,
    is_active   BOOLEAN      NOT NULL DEFAULT true,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

-- Users
CREATE TABLE IF NOT EXISTS users (
    id              UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID         NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    email           VARCHAR(320) NOT NULL,
    password_hash   TEXT         NOT NULL,
    full_name       VARCHAR(200) NOT NULL DEFAULT '',
    role            VARCHAR(50)  NOT NULL DEFAULT 'viewer'
                        CHECK (role IN ('admin', 'editor', 'viewer')),
    is_active       BOOLEAN      NOT NULL DEFAULT true,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_users_tenant_email UNIQUE (tenant_id, email)
);

CREATE INDEX IF NOT EXISTS idx_users_tenant_id  ON users (tenant_id);
CREATE INDEX IF NOT EXISTS idx_users_email       ON users (email);

-- Seed demo tenant
-- NOTE: do NOT seed users here — DataSeeder.cs seeds users at startup
--       with proper BCrypt hashes computed at runtime.
INSERT INTO tenants (id, name, slug, is_active)
VALUES ('00000000-0000-0000-0000-000000000001', 'IOC Demo', 'ioc-demo', true)
ON CONFLICT (id) DO NOTHING;
