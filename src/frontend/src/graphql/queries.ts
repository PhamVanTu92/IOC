import { gql } from '@apollo/client';

// ─────────────────────────────────────────────────────────────────────────────
// GraphQL Documents — dùng với Apollo Client hooks
// ─────────────────────────────────────────────────────────────────────────────

// ── Dataset queries ────────────────────────────────────────────────────────────

/** Lấy danh sách datasets (không có children — dùng cho list/selector) */
export const LIST_DATASETS = gql`
  query ListDatasets($includeInactive: Boolean) {
    datasets(includeInactive: $includeInactive) {
      id
      name
      description
      sourceType
      isActive
      updatedAt
    }
  }
`;

/** Lấy chi tiết một dataset kèm dimensions, measures, metrics */
export const GET_DATASET = gql`
  query GetDataset($id: UUID!) {
    dataset(id: $id) {
      id
      name
      description
      sourceType
      schemaName
      tableName
      isActive
      createdAt
      updatedAt
      dimensions {
        id
        name
        displayName
        description
        dataType
        format
        isTimeDimension
        defaultGranularity
        sortOrder
        isActive
      }
      measures {
        id
        name
        displayName
        description
        aggregationType
        dataType
        format
        filterExpression
        sortOrder
        isActive
      }
      metrics {
        id
        name
        displayName
        description
        expression
        dataType
        format
        dependsOnMeasures
        sortOrder
        isActive
      }
    }
  }
`;

// ── Query execution ────────────────────────────────────────────────────────────

/** Thực thi dynamic query qua Semantic Layer */
export const EXECUTE_QUERY = gql`
  query ExecuteQuery($input: QueryRequestInput!) {
    executeQuery(input: $input) {
      columns {
        name
        displayName
        dataType
        format
        fieldType
      }
      rows
      metadata {
        generatedSql
        executionTimeMs
        totalRows
        fromCache
        cacheKey
        executedAt
        errorMessage
      }
    }
  }
`;

// ── Dataset mutations ──────────────────────────────────────────────────────────

export const CREATE_DATASET = gql`
  mutation CreateDataset($input: CreateDatasetInput!) {
    createDataset(input: $input) {
      id
      name
      sourceType
      isActive
      createdAt
    }
  }
`;

export const CREATE_DIMENSION = gql`
  mutation CreateDimension($input: CreateDimensionInput!) {
    createDimension(input: $input) {
      id
      name
      displayName
      dataType
      isTimeDimension
    }
  }
`;

export const CREATE_MEASURE = gql`
  mutation CreateMeasure($input: CreateMeasureInput!) {
    createMeasure(input: $input) {
      id
      name
      displayName
      aggregationType
      dataType
    }
  }
`;

export const CREATE_METRIC = gql`
  mutation CreateMetric($input: CreateMetricInput!) {
    createMetric(input: $input) {
      id
      name
      displayName
      expression
    }
  }
`;

// ── Dashboard queries ──────────────────────────────────────────────────────────

/** Shared fragment for full dashboard payload */
const DASHBOARD_FIELDS = `
  id
  tenantId
  createdBy
  title
  description
  configJson
  isActive
  createdAt
  updatedAt
`;

export const LIST_DASHBOARDS = gql`
  query ListDashboards($includeInactive: Boolean) {
    dashboards(includeInactive: $includeInactive) {
      id
      title
      description
      isActive
      updatedAt
      widgetCount
    }
  }
`;

export const GET_DASHBOARD = gql`
  query GetDashboard($id: UUID!) {
    dashboard(id: $id) {
      ${DASHBOARD_FIELDS}
    }
  }
`;

// ── Dashboard mutations ────────────────────────────────────────────────────────

export const CREATE_DASHBOARD = gql`
  mutation CreateDashboard($input: SaveDashboardInput!) {
    createDashboard(input: $input) {
      ${DASHBOARD_FIELDS}
    }
  }
`;

export const UPDATE_DASHBOARD = gql`
  mutation UpdateDashboard($id: UUID!, $input: SaveDashboardInput!) {
    updateDashboard(id: $id, input: $input) {
      ${DASHBOARD_FIELDS}
    }
  }
`;

export const DELETE_DASHBOARD = gql`
  mutation DeleteDashboard($id: UUID!) {
    deleteDashboard(id: $id)
  }
`;

// ── Auth mutations & queries ───────────────────────────────────────────────────

export const LOGIN = gql`
  mutation Login($email: String!, $password: String!) {
    login(email: $email, password: $password) {
      token
      expiresAt
      user {
        id
        email
        fullName
        role
        tenantId
      }
    }
  }
`;

export const REGISTER = gql`
  mutation Register($email: String!, $password: String!, $fullName: String!) {
    register(email: $email, password: $password, fullName: $fullName) {
      token
      expiresAt
      user {
        id
        email
        fullName
        role
        tenantId
      }
    }
  }
`;

export const ME = gql`
  query Me {
    me {
      id
      email
      fullName
      role
      tenantId
    }
  }
`;
