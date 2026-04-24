import { gql } from '@apollo/client';

// ── Modules ───────────────────────────────────────────────────────────────────

export const GET_MODULES = gql`
  query GetModules {
    modules {
      id
      name
      slug
      description
      icon
      color
      sortOrder
      createdAt
    }
  }
`;

export const CREATE_MODULE = gql`
  mutation CreateModule($input: CreateModuleInput!) {
    createModule(input: $input) {
      id
      name
      slug
      icon
      color
      sortOrder
    }
  }
`;

export const UPDATE_MODULE = gql`
  mutation UpdateModule($input: UpdateModuleInput!) {
    updateModule(input: $input) {
      id
      name
      slug
      icon
      color
      sortOrder
    }
  }
`;

export const DELETE_MODULE = gql`
  mutation DeleteModule($id: UUID!) {
    deleteModule(id: $id)
  }
`;

// ── Charts ────────────────────────────────────────────────────────────────────

export const GET_CHARTS_BY_MODULE = gql`
  query GetChartsByModule($moduleId: UUID!) {
    chartsByModule(moduleId: $moduleId) {
      id
      moduleId
      name
      description
      chartType
      configJson
      sortOrder
      createdAt
    }
  }
`;

export const CREATE_CHART = gql`
  mutation CreateChart($input: CreateChartInput!) {
    createChart(input: $input) {
      id
      moduleId
      name
      chartType
      configJson
      sortOrder
    }
  }
`;

export const UPDATE_CHART = gql`
  mutation UpdateChart($input: UpdateChartInput!) {
    updateChart(input: $input) {
      id
      moduleId
      name
      chartType
      configJson
      sortOrder
    }
  }
`;

export const DELETE_CHART = gql`
  mutation DeleteChart($id: UUID!) {
    deleteChart(id: $id)
  }
`;

// ── Permissions ───────────────────────────────────────────────────────────────

export const GET_MODULE_PERMISSIONS = gql`
  query GetModulePermissions($moduleId: UUID!) {
    modulePermissions(moduleId: $moduleId) {
      userId
      moduleId
      canView
      canEdit
      grantedAt
    }
  }
`;

export const ASSIGN_MODULE_PERMISSION = gql`
  mutation AssignModulePermission($input: AssignModulePermissionInput!) {
    assignModulePermission(input: $input)
  }
`;

export const REVOKE_MODULE_PERMISSION = gql`
  mutation RevokeModulePermission($userId: UUID!, $moduleId: UUID!) {
    revokeModulePermission(userId: $userId, moduleId: $moduleId)
  }
`;

// ── Layouts ───────────────────────────────────────────────────────────────────

export const GET_MY_LAYOUT = gql`
  query GetMyLayout($moduleId: UUID) {
    myLayout(moduleId: $moduleId) {
      userId
      moduleId
      layoutJson
      updatedAt
    }
  }
`;

export const SAVE_LAYOUT = gql`
  mutation SaveLayout($moduleId: UUID, $layoutJson: String!) {
    saveLayout(moduleId: $moduleId, layoutJson: $layoutJson)
  }
`;
