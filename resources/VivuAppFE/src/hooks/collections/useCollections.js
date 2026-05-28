import useSWR from "swr";
import useSWRMutation from "swr/mutation";
import collectionApi from "../../api/collectionApi";

export function useUserCollections(pageNumber = 1, pageSize = 10, searchText = "") {
  return useSWR(["user-collections", pageNumber, pageSize, searchText], () =>
    collectionApi.getAll({ pageNumber, pageSize, searchText }).then((res) => res.data),
  );
}

export function useCollectionsSummary() {
  return useSWR("collections-summary", () =>
    collectionApi.getSummary().then((res) => res.data),
  );
}

export function useCreateCollection() {
  return useSWRMutation("create-collection", (_, { arg: formData }) =>
    collectionApi.create(formData).then((res) => res.data),
  );
}

export function useUpdateCollection() {
  return useSWRMutation("update-collection", (_, { arg }) =>
    collectionApi.update(arg.id, arg.formData).then((res) => res.data),
  );
}

export function useDeleteCollection() {
  return useSWRMutation("delete-collection", (_, { arg: id }) =>
    collectionApi.delete(id).then((res) => res.data),
  );
}

export function useAddLocationToCollection() {
  return useSWRMutation("add-location-collection", (_, { arg }) =>
    collectionApi
      .addLocation(arg.collectionId, {
        locationId: arg.locationId,
        note: arg.note,
      })
      .then((res) => res.data),
  );
}

export function useRemoveLocationFromCollection() {
  return useSWRMutation("remove-location-collection", (_, { arg }) =>
    collectionApi
      .removeLocation(arg.collectionId, arg.locationId)
      .then((res) => res.data),
  );
}
