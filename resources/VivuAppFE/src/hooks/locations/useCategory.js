import useSWR from 'swr';
import locationCategoryApi from '../../api/locationCategoryApi';

export const useCategories = () => {
  const { data, error, isLoading, mutate } = useSWR(
    '/location-categories',
    () => locationCategoryApi.getAll({ pageSize: 100 }).then(res => res.data?.items || [])
  );

  return {
    categories: data || [],
    isLoading,
    isError: error,
    mutate
  };
};
