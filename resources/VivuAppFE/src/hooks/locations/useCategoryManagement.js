import useSWR from "swr";
import useSWRMutation from "swr/mutation";
import locationCategoryApi from "../../api/locationCategoryApi";

/** Paginated list for CMS DataTable */
export function useAllCategories(params) {
  const key = params ? ["all-categories", params] : null;
  return useSWR(key, () => locationCategoryApi.getAll(params).then((res) => res.data));
}

export function useCreateCategory() {
  return useSWRMutation("create-category", (_, { arg: data }) =>
    locationCategoryApi.create(data).then((res) => res.data),
  );
}

export function useUpdateCategory() {
  return useSWRMutation("update-category", (_, { arg: { id, data } }) =>
    locationCategoryApi.update(id, data).then((res) => res.data),
  );
}

export function useDeleteCategory() {
  return useSWRMutation("delete-category", (_, { arg: id }) =>
    locationCategoryApi.delete(id).then((res) => res.data),
  );
}

export function useChangeCategoryStatus() {
  return useSWRMutation("change-category-status", (_, { arg: { id, data } }) =>
    locationCategoryApi.changeStatus(id, data).then((res) => res.data),
  );
}
