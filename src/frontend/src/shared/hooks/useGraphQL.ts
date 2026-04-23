import { useQuery, useMutation, useSubscription } from '@apollo/client';
import type {
  DocumentNode,
  OperationVariables,
  QueryHookOptions,
  MutationHookOptions,
  SubscriptionHookOptions,
} from '@apollo/client';

// ─────────────────────────────────────────────────────────────────────────────
// IOC GraphQL hooks — wrapper mỏng với error handling chuẩn
// ─────────────────────────────────────────────────────────────────────────────

export interface IOCQueryResult<TData> {
  data: TData | undefined;
  loading: boolean;
  error: string | undefined;
  refetch: () => void;
}

export function useIOCQuery<TData, TVariables extends OperationVariables = OperationVariables>(
  query: DocumentNode,
  options?: QueryHookOptions<TData, TVariables>
): IOCQueryResult<TData> {
  const { data, loading, error, refetch } = useQuery<TData, TVariables>(query, options);
  return {
    data,
    loading,
    error: error?.message,
    refetch: () => void refetch(),
  };
}

export function useIOCMutation<TData, TVariables extends OperationVariables = OperationVariables>(
  mutation: DocumentNode,
  options?: MutationHookOptions<TData, TVariables>
) {
  const [mutate, { data, loading, error }] = useMutation<TData, TVariables>(mutation, options);
  return {
    mutate,
    data,
    loading,
    error: error?.message,
  };
}

export function useIOCSubscription<TData, TVariables extends OperationVariables = OperationVariables>(
  subscription: DocumentNode,
  options?: SubscriptionHookOptions<TData, TVariables>
): IOCQueryResult<TData> {
  const { data, loading, error } = useSubscription<TData, TVariables>(subscription, options);
  return {
    data,
    loading,
    error: error?.message,
    refetch: () => {/* subscriptions không refetch */},
  };
}
