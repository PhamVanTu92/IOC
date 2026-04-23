-- ─────────────────────────────────────────────────────────────────────────────
-- V005 — Sample data tables cho Finance và HR
-- Run: docker exec -i ioc-postgres psql -U postgres -d ioc_dev < V005__sample_data.sql
-- ─────────────────────────────────────────────────────────────────────────────

-- ── Finance data table ────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS finance_data (
    id               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_date DATE        NOT NULL,
    month            VARCHAR(7)  NOT NULL,   -- 'YYYY-MM'
    quarter          VARCHAR(6)  NOT NULL,   -- 'YYYY-Q1'
    year             INT         NOT NULL,
    department       VARCHAR(100) NOT NULL,
    category         VARCHAR(100) NOT NULL,
    cost_center      VARCHAR(50),
    revenue          NUMERIC(18,2) NOT NULL DEFAULT 0,
    expense          NUMERIC(18,2) NOT NULL DEFAULT 0,
    profit           NUMERIC(18,2) GENERATED ALWAYS AS (revenue - expense) STORED,
    budget           NUMERIC(18,2) NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS ix_finance_date       ON finance_data(transaction_date);
CREATE INDEX IF NOT EXISTS ix_finance_month      ON finance_data(month);
CREATE INDEX IF NOT EXISTS ix_finance_department ON finance_data(department);

-- Seed: 12 tháng × 4 phòng ban × 3 danh mục = 144 rows
INSERT INTO finance_data (transaction_date, month, quarter, year, department, category, cost_center, revenue, expense, budget)
SELECT
    d::date                                              AS transaction_date,
    TO_CHAR(d, 'YYYY-MM')                               AS month,
    TO_CHAR(d, 'YYYY') || '-Q' || TO_CHAR(d, 'Q')      AS quarter,
    EXTRACT(YEAR FROM d)::INT                            AS year,
    dept.name                                            AS department,
    cat.name                                             AS category,
    dept.code                                            AS cost_center,
    -- Revenue: base + random ±20%
    ROUND((cat.base_revenue * dept.rev_factor * (0.8 + RANDOM()*0.4))::NUMERIC, 2) AS revenue,
    ROUND((cat.base_expense * dept.exp_factor * (0.8 + RANDOM()*0.4))::NUMERIC, 2) AS expense,
    ROUND((cat.base_revenue * dept.rev_factor * 1.1)::NUMERIC, 2)                  AS budget
FROM
    GENERATE_SERIES('2025-01-01'::date, '2025-12-01'::date, '1 month'::interval) AS d,
    (VALUES
        ('Finance',   'FIN', 1.2, 1.0),
        ('HR',        'HR',  0.8, 0.9),
        ('Marketing', 'MKT', 1.5, 1.3),
        ('IT',        'IT',  1.0, 0.8)
    ) AS dept(name, code, rev_factor, exp_factor),
    (VALUES
        ('Doanh thu dịch vụ', 800000000, 400000000),
        ('Doanh thu sản phẩm', 1200000000, 600000000),
        ('Chi phí vận hành',   200000000, 180000000)
    ) AS cat(name, base_revenue, base_expense)
ON CONFLICT DO NOTHING;

-- ── HR data table ─────────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS hr_data (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_code   VARCHAR(20) NOT NULL UNIQUE,
    full_name       VARCHAR(200) NOT NULL,
    join_date       DATE        NOT NULL,
    department      VARCHAR(100) NOT NULL,
    position        VARCHAR(100) NOT NULL,
    gender          VARCHAR(10)  NOT NULL,
    employment_type VARCHAR(50)  NOT NULL,
    location        VARCHAR(100) NOT NULL,
    salary          NUMERIC(18,2) NOT NULL,
    leave_days      INT          NOT NULL DEFAULT 0,
    is_active       BOOLEAN      NOT NULL DEFAULT true
);

CREATE INDEX IF NOT EXISTS ix_hr_department ON hr_data(department);
CREATE INDEX IF NOT EXISTS ix_hr_join_date  ON hr_data(join_date);

-- Seed: 80 nhân viên mẫu
INSERT INTO hr_data (employee_code, full_name, join_date, department, position, gender, employment_type, location, salary, leave_days)
SELECT
    'EMP' || LPAD(n::TEXT, 4, '0')                       AS employee_code,
    'Nhân viên ' || n                                     AS full_name,
    ('2020-01-01'::date + (RANDOM()*1800)::INT * '1 day'::interval)::date AS join_date,
    dept.name                                             AS department,
    pos.name                                              AS position,
    CASE WHEN RANDOM() > 0.45 THEN 'Nam' ELSE 'Nữ' END  AS gender,
    CASE WHEN RANDOM() > 0.15 THEN 'Toàn thời gian' ELSE 'Bán thời gian' END AS employment_type,
    CASE (n % 3) WHEN 0 THEN 'Hà Nội' WHEN 1 THEN 'TP.HCM' ELSE 'Đà Nẵng' END AS location,
    ROUND((dept.base_salary + (RANDOM()-0.5) * dept.base_salary * 0.4)::NUMERIC, -5) AS salary,
    (RANDOM() * 20)::INT                                  AS leave_days
FROM
    GENERATE_SERIES(1, 80) AS n,
    LATERAL (
        SELECT *
        FROM (VALUES
            ('Finance',   18000000),
            ('HR',        15000000),
            ('Marketing', 17000000),
            ('IT',        22000000)
        ) AS t(name, base_salary)
        OFFSET (n % 4) LIMIT 1
    ) AS dept,
    LATERAL (
        SELECT *
        FROM (VALUES
            ('Nhân viên'),
            ('Chuyên viên'),
            ('Trưởng nhóm'),
            ('Quản lý')
        ) AS t(name)
        OFFSET (n % 4) LIMIT 1
    ) AS pos
ON CONFLICT (employee_code) DO NOTHING;

-- ── Cập nhật config_json cho Finance dataset ──────────────────────────────────

UPDATE datasets
SET config_json = jsonb_set(
    jsonb_set(config_json, '{tableName}', '"finance_data"'),
    '{sourceType}', '"postgres"'
)
WHERE name = 'Finance';

-- ── Cập nhật config_json cho HR dataset ──────────────────────────────────────

UPDATE datasets
SET config_json = jsonb_set(
    jsonb_set(config_json, '{tableName}', '"hr_data"'),
    '{sourceType}', '"postgres"'
)
WHERE name = 'HR';

-- Verify
SELECT name, config_json->>'tableName' AS table_name FROM datasets;
