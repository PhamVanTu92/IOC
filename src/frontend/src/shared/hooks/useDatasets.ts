import { useQuery } from '@apollo/client';
import { LIST_DATASETS, GET_DATASET } from '@/graphql/queries';
import type { DatasetGql } from '@/graphql/types';

// ─────────────────────────────────────────────────────────────────────────────
// useDatasets — load danh sách datasets cho selector
// ─────────────────────────────────────────────────────────────────────────────

interface ListDatasetsData {
  datasets: Pick<DatasetGql, 'id' | 'name' | 'description' | 'sourceType' | 'isActive' | 'updatedAt'>[];
}

export function useDatasets(includeInactive = false) {
  const { data, loading, error, refetch } = useQuery<ListDatasetsData>(LIST_DATASETS, {
    variables: { includeInactive },
    fetchPolicy: 'cache-and-network',
  });

  return {
    datasets: data?.datasets ?? [],
    loading,
    error: error?.message,
    refetch: () => void refetch(),
  };
}

// ─────────────────────────────────────────────────────────────────────────────
// useDataset — load chi tiết một dataset với dimensions/measures/metrics
// ─────────────────────────────────────────────────────────────────────────────

interface GetDatasetData {
  dataset: DatasetGql | null;
}

export function useDataset(datasetId: string | undefined) {
  const { data, loading, error, refetch } = useQuery<GetDatasetData>(GET_DATASET, {
    variables: { id: datasetId },
    skip: !datasetId,
    fetchPolicy: 'cache-and-network',
  });

  return {
    dataset: data?.dataset ?? null,
    loading,
    error: error?.message,
    refetch: () => void refetch(),
  };
}
