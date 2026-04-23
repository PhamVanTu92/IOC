-- ─────────────────────────────────────────────────────────────────────────────
-- V004 — Dimensions, Measures, Metrics tables + seed data
-- Run manually: docker exec -i ioc-postgres psql -U postgres -d ioc_dev < V004__dimensions_measures_metrics.sql
-- ─────────────────────────────────────────────────────────────────────────────

-- ── Dimensions ────────────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS dimensions (
    id                   UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    dataset_id           UUID         NOT NULL REFERENCES datasets(id) ON DELETE CASCADE,
    name                 VARCHAR(100) NOT NULL,
    display_name         VARCHAR(200) NOT NULL,
    description          TEXT,
    column_name          VARCHAR(200) NOT NULL,
    custom_sql_expression TEXT,
    data_type            VARCHAR(50)  NOT NULL DEFAULT 'string',
    format               VARCHAR(100),
    is_time_dimension    BOOLEAN      NOT NULL DEFAULT false,
    default_granularity  VARCHAR(50),
    sort_order           INT          NOT NULL DEFAULT 0,
    is_active            BOOLEAN      NOT NULL DEFAULT true,
    created_at           TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_dimensions_dataset ON dimensions(dataset_id) WHERE is_active = true;

-- ── Measures ──────────────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS measures (
    id                   UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    dataset_id           UUID         NOT NULL REFERENCES datasets(id) ON DELETE CASCADE,
    name                 VARCHAR(100) NOT NULL,
    display_name         VARCHAR(200) NOT NULL,
    description          TEXT,
    column_name          VARCHAR(200) NOT NULL,
    custom_sql_expression TEXT,
    aggregation_type     VARCHAR(50)  NOT NULL DEFAULT 'sum',
    data_type            VARCHAR(50)  NOT NULL DEFAULT 'number',
    format               VARCHAR(100),
    filter_expression    TEXT,
    sort_order           INT          NOT NULL DEFAULT 0,
    is_active            BOOLEAN      NOT NULL DEFAULT true,
    created_at           TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_measures_dataset ON measures(dataset_id) WHERE is_active = true;

-- ── Metrics ───────────────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS metrics (
    id                  UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    dataset_id          UUID         NOT NULL REFERENCES datasets(id) ON DELETE CASCADE,
    name                VARCHAR(100) NOT NULL,
    display_name        VARCHAR(200) NOT NULL,
    description         TEXT,
    expression          TEXT         NOT NULL,
    data_type           VARCHAR(50)  NOT NULL DEFAULT 'number',
    format              VARCHAR(100),
    depends_on_measures TEXT[]       NOT NULL DEFAULT '{}',
    sort_order          INT          NOT NULL DEFAULT 0,
    is_active           BOOLEAN      NOT NULL DEFAULT true,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_metrics_dataset ON metrics(dataset_id) WHERE is_active = true;

-- ─────────────────────────────────────────────────────────────────────────────
-- Seed data — Finance dataset
-- ─────────────────────────────────────────────────────────────────────────────

DO $$
DECLARE
    finance_id UUID;
    hr_id      UUID;
BEGIN
    SELECT id INTO finance_id FROM datasets WHERE name = 'Finance' LIMIT 1;
    SELECT id INTO hr_id      FROM datasets WHERE name = 'HR'      LIMIT 1;

    -- ── Finance Dimensions ─────────────────────────────────────────────────────
    IF finance_id IS NOT NULL THEN
        INSERT INTO dimensions (dataset_id, name, display_name, description, column_name, data_type, is_time_dimension, default_granularity, sort_order) VALUES
            (finance_id, 'date',        'Ngày',          'Ngày giao dịch',   'transaction_date', 'date',   true,  'month', 0),
            (finance_id, 'month',       'Tháng',         'Tháng tài chính',  'month',            'string', false, null,    1),
            (finance_id, 'quarter',     'Quý',           'Quý tài chính',    'quarter',          'string', false, null,    2),
            (finance_id, 'year',        'Năm',           'Năm tài chính',    'year',             'number', false, null,    3),
            (finance_id, 'department',  'Phòng ban',     'Phòng ban',        'department',       'string', false, null,    4),
            (finance_id, 'category',    'Danh mục',      'Danh mục chi phí', 'category',         'string', false, null,    5),
            (finance_id, 'cost_center', 'Trung tâm chi phí', 'Cost center',  'cost_center',      'string', false, null,    6)
        ON CONFLICT DO NOTHING;

        -- ── Finance Measures ───────────────────────────────────────────────────
        INSERT INTO measures (dataset_id, name, display_name, description, column_name, aggregation_type, data_type, format, sort_order) VALUES
            (finance_id, 'revenue',      'Doanh thu',      'Tổng doanh thu',      'revenue',      'sum',   'number', '#,##0', 0),
            (finance_id, 'expense',      'Chi phí',        'Tổng chi phí',        'expense',      'sum',   'number', '#,##0', 1),
            (finance_id, 'profit',       'Lợi nhuận',      'Lợi nhuận thuần',     'profit',       'sum',   'number', '#,##0', 2),
            (finance_id, 'budget',       'Ngân sách',      'Ngân sách phân bổ',   'budget',       'sum',   'number', '#,##0', 3),
            (finance_id, 'transaction_count', 'Số giao dịch', 'Số giao dịch',     'id',           'count', 'number', '#,##0', 4),
            (finance_id, 'avg_revenue',  'Doanh thu TB',   'Doanh thu trung bình','revenue',      'avg',   'number', '#,##0', 5)
        ON CONFLICT DO NOTHING;

        -- ── Finance Metrics ────────────────────────────────────────────────────
        INSERT INTO metrics (dataset_id, name, display_name, description, expression, data_type, format, depends_on_measures, sort_order) VALUES
            (finance_id, 'profit_margin', 'Tỷ suất lợi nhuận', 'Profit / Revenue * 100',
             'profit / NULLIF(revenue, 0) * 100', 'number', '#,##0.0"%"', ARRAY['profit','revenue'], 0),
            (finance_id, 'budget_utilization', 'Tỷ lệ sử dụng ngân sách', 'Expense / Budget * 100',
             'expense / NULLIF(budget, 0) * 100', 'number', '#,##0.0"%"', ARRAY['expense','budget'], 1)
        ON CONFLICT DO NOTHING;
    END IF;

    -- ── HR Dimensions ──────────────────────────────────────────────────────────
    IF hr_id IS NOT NULL THEN
        INSERT INTO dimensions (dataset_id, name, display_name, description, column_name, data_type, is_time_dimension, default_granularity, sort_order) VALUES
            (hr_id, 'join_date',    'Ngày vào làm',   'Ngày nhân viên vào làm', 'join_date',    'date',   true,  'month', 0),
            (hr_id, 'department',   'Phòng ban',      'Phòng ban',              'department',   'string', false, null,    1),
            (hr_id, 'position',     'Vị trí',         'Chức vụ',                'position',     'string', false, null,    2),
            (hr_id, 'gender',       'Giới tính',      'Giới tính nhân viên',    'gender',       'string', false, null,    3),
            (hr_id, 'employment_type', 'Loại hợp đồng', 'Hợp đồng lao động',   'employment_type','string',false, null,   4),
            (hr_id, 'location',     'Địa điểm',       'Văn phòng/Chi nhánh',    'location',     'string', false, null,    5)
        ON CONFLICT DO NOTHING;

        -- ── HR Measures ────────────────────────────────────────────────────────
        INSERT INTO measures (dataset_id, name, display_name, description, column_name, aggregation_type, data_type, format, sort_order) VALUES
            (hr_id, 'headcount',        'Nhân viên',         'Tổng số nhân viên',     'id',        'count', 'number', '#,##0',   0),
            (hr_id, 'avg_salary',       'Lương TB',          'Lương trung bình',       'salary',    'avg',   'number', '#,##0',   1),
            (hr_id, 'total_salary',     'Tổng lương',        'Tổng quỹ lương',         'salary',    'sum',   'number', '#,##0',   2),
            (hr_id, 'leave_days',       'Ngày nghỉ',         'Tổng ngày nghỉ phép',    'leave_days','sum',   'number', '#,##0',   3),
            (hr_id, 'new_hires',        'Tuyển mới',         'Số nhân viên tuyển mới', 'id',        'count', 'number', '#,##0',   4),
            (hr_id, 'resigned',         'Nghỉ việc',         'Số nhân viên nghỉ việc', 'id',        'count', 'number', '#,##0',   5)
        ON CONFLICT DO NOTHING;

        -- ── HR Metrics ─────────────────────────────────────────────────────────
        INSERT INTO metrics (dataset_id, name, display_name, description, expression, data_type, format, depends_on_measures, sort_order) VALUES
            (hr_id, 'turnover_rate', 'Tỷ lệ nghỉ việc', 'Resigned / Headcount * 100',
             'resigned / NULLIF(headcount, 0) * 100', 'number', '#,##0.0"%"', ARRAY['resigned','headcount'], 0),
            (hr_id, 'avg_leave_per_person', 'Nghỉ phép TB/người', 'Leave days / Headcount',
             'leave_days / NULLIF(headcount, 0)', 'number', '#,##0.0', ARRAY['leave_days','headcount'], 1)
        ON CONFLICT DO NOTHING;
    END IF;
END $$;
