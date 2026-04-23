import { useCallback, useMemo } from 'react';
import { useMutation, useQuery, useApolloClient } from '@apollo/client';
import {
  LIST_DASHBOARDS,
  GET_DASHBOARD,
  CREATE_DASHBOARD,
  UPDATE_DASHBOARD,
  DELETE_DASHBOARD,
} from '@/graphql/queries';
import type { DashboardGql, DashboardSummaryGql } from '@/graphql/types';
import type { DashboardConfig } from './types';

// ─────────────────────────────────────────────────────────────────────────────
// useDashboardPersistence — Apollo mutations + LocalStorage fallback
// ─────────────────────────────────────────────────────────────────────────────

const LS_KEY_PREFIX = 'ioc:dashboard:';
const LS_INDEX_KEY = 'ioc:dashboard:_index';

// ── LocalStorage helpers ──────────────────────────────────────────────────────

function lsSave(config: DashboardConfig): void {
  try {
    localStorage.setItem(`${LS_KEY_PREFIX}${config.id}`, JSON.stringify(config));
    const index = lsIndex();
    if (!index.includes(config.id)) {
      localStorage.setItem(LS_INDEX_KEY, JSON.stringify([...index, config.id]));
    }
  } catch {
    // Silently ignore storage quota errors
  }
}

function lsGet(id: string): DashboardConfig | null {
  try {
    const raw = localStorage.getItem(`${LS_KEY_PREFIX}${id}`);
    return raw ? (JSON.parse(raw) as DashboardConfig) : null;
  } catch {
    return null;
  }
}

function lsRemove(id: string): void {
  try {
    localStorage.removeItem(`${LS_KEY_PREFIX}${id}`);
    const index = lsIndex().filter((i) => i !== id);
    localStorage.setItem(LS_INDEX_KEY, JSON.stringify(index));
  } catch {
    // ignore
  }
}

function lsIndex(): string[] {
  try {
    const raw = localStorage.getItem(LS_INDEX_KEY);
    return raw ? (JSON.parse(raw) as string[]) : [];
  } catch {
    return [];
  }
}

function lsListAll(): DashboardConfig[] {
  return lsIndex()
    .map(lsGet)
    .filter((d): d is DashboardConfig => d !== null);
}

// ── Apollo response types ─────────────────────────────────────────────────────

interface ListDashboardsData {
  dashboards: DashboardSummaryGql[];
}
interface GetDashboardData {
  dashboard: DashboardGql;
}
interface CreateDashboardData {
  createDashboard: DashboardGql;
}
interface UpdateDashboardData {
  updateDashboard: DashboardGql;
}
interface DeleteDashboardData {
  deleteDashboard: boolean;
}

// ── GQL → DashboardConfig mapping ────────────────────────────────────────────

function parseDashboardConfig(gql: DashboardGql): DashboardConfig {
  try {
    const config = JSON.parse(gql.configJson) as DashboardConfig;
    // Always override id/timestamps with the backend's canonical values.
    // configJson may still contain an old temp- id (e.g. right after createDashboard),
    // so gql.id is the authoritative source.
    return {
      ...config,
      id: gql.id,
      createdAt: gql.createdAt,
      updatedAt: gql.updatedAt,
    };
  } catch {
    // If JSON is malformed, return a minimal placeholder
    return {
      id: gql.id,
      title: gql.title,
      description: gql.description,
      widgets: [],
      createdAt: gql.createdAt,
      updatedAt: gql.updatedAt,
    };
  }
}

// ── Hook: list dashboards ─────────────────────────────────────────────────────

export interface UseDashboardListResult {
  summaries: DashboardSummaryGql[];
  loading: boolean;
  error: string | undefined;
  refetch: () => void;
}

export function useDashboardList(includeInactive = false): UseDashboardListResult {
  const { data, loading, error, refetch } = useQuery<ListDashboardsData>(LIST_DASHBOARDS, {
    variables: { includeInactive },
    fetchPolicy: 'cache-and-network',
  });

  return {
    summaries: data?.dashboards ?? [],
    loading,
    error: error?.message,
    refetch: () => void refetch(),
  };
}

// ── Hook: load single dashboard ───────────────────────────────────────────────

export interface UseDashboardLoadResult {
  config: DashboardConfig | null;
  loading: boolean;
  error: string | undefined;
}

export function useDashboardLoad(id: string | undefined): UseDashboardLoadResult {
  const { data, loading, error } = useQuery<GetDashboardData>(GET_DASHBOARD, {
    variables: { id },
    skip: !id,
    fetchPolicy: 'cache-and-network',
  });

  // Memoize by configJson string — parseDashboardConfig calls JSON.parse every render,
  // which would produce a new object reference and trigger infinite useEffect loops.
  const config = useMemo(
    () => (data?.dashboard ? parseDashboardConfig(data.dashboard) : null),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [data?.dashboard?.configJson],
  );

  // LocalStorage fallback while loading
  const lsConfig = useMemo(
    () => (!config && !loading ? lsGet(id ?? '') : null),
    [config, loading, id],
  );

  if (!id) return { config: null, loading: false, error: undefined };

  return {
    config: config ?? lsConfig,
    loading,
    error: error?.message,
  };
}

// ── Hook: save / delete dashboard ─────────────────────────────────────────────

export interface UseDashboardSaveResult {
  save: (config: DashboardConfig) => Promise<DashboardConfig>;
  remove: (id: string) => Promise<boolean>;
  saving: boolean;
  deleting: boolean;
  error: string | undefined;
}

export function useDashboardSave(): UseDashboardSaveResult {
  const client = useApolloClient();
  const [createMutation, createState] = useMutation<CreateDashboardData>(CREATE_DASHBOARD);
  const [updateMutation, updateState] = useMutation<UpdateDashboardData>(UPDATE_DASHBOARD);
  const [deleteMutation, deleteState] = useMutation<DeleteDashboardData>(DELETE_DASHBOARD);

  const save = useCallback(
    async (config: DashboardConfig): Promise<DashboardConfig> => {
      const input = {
        title: config.title,
        description: config.description,
        configJson: JSON.stringify(config),
      };

      // Always write to LocalStorage first (offline support + instant feedback)
      lsSave(config);

      const isNew = !config.id || config.id.startsWith('temp-');

      if (isNew) {
        const { data, errors } = await createMutation({ variables: { input } });
        if (errors?.length) throw new Error(errors[0].message);
        if (!data) throw new Error('No data returned from createDashboard');

        // parseDashboardConfig already overrides id/timestamps from gql.
        // But configJson stored in DB still has the old temp- id.
        // Patch it immediately with a follow-up update so subsequent fetches
        // get a consistent configJson (avoids stale temp-id on next load).
        const saved = parseDashboardConfig(data.createDashboard);
        void updateMutation({
          variables: {
            id: saved.id,
            input: { title: saved.title, description: saved.description, configJson: JSON.stringify(saved) },
          },
        });

        // Update LS with the real backend-assigned id
        lsRemove(config.id);
        lsSave(saved);
        // Invalidate list cache
        await client.refetchQueries({ include: [LIST_DASHBOARDS] });
        return saved;
      } else {
        const { data, errors } = await updateMutation({
          variables: { id: config.id, input },
        });
        if (errors?.length) throw new Error(errors[0].message);
        if (!data) throw new Error('No data returned from updateDashboard');

        const saved = parseDashboardConfig(data.updateDashboard);
        lsSave(saved);
        // Update Apollo cache for this specific dashboard
        client.writeQuery<GetDashboardData>({
          query: GET_DASHBOARD,
          variables: { id: saved.id },
          data: { dashboard: data.updateDashboard },
        });
        return saved;
      }
    },
    [createMutation, updateMutation, client]
  );

  const remove = useCallback(
    async (id: string): Promise<boolean> => {
      lsRemove(id);
      const { data, errors } = await deleteMutation({ variables: { id } });
      if (errors?.length) throw new Error(errors[0].message);
      await client.refetchQueries({ include: [LIST_DASHBOARDS] });
      return data?.deleteDashboard ?? false;
    },
    [deleteMutation, client]
  );

  const error =
    createState.error?.message ??
    updateState.error?.message ??
    deleteState.error?.message;

  return {
    save,
    remove,
    saving: createState.loading || updateState.loading,
    deleting: deleteState.loading,
    error,
  };
}

// ── Offline-only: list from LocalStorage ─────────────────────────────────────

export function listLocalDashboards(): DashboardConfig[] {
  return lsListAll();
}
