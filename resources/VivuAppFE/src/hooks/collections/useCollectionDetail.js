import useSWR from "swr";
import collectionApi from "../../api/collectionApi";

export function useCollectionDetail(collectionId) {
  return useSWR(collectionId ? ["collection-detail", collectionId] : null, () =>
    collectionApi.getById(collectionId).then((res) => res.data),
  );
}
