import useSWR from "swr";
import userApi from "../../api/userApi";

export function useSavedTrips(pageNumber = 1, pageSize = 12) {
  return useSWR(["saved-trips", pageNumber, pageSize], () =>
    userApi
      .getMyFavoriteTrips({ pageNumber, pageSize })
      .then((res) => res.data),
  );
}
