import useSWR from "swr";
import useSWRMutation from "swr/mutation";
import locationApi from "../../api/locationApi";

// 1. Hook lấy danh sách locations đang chờ duyệt
export function usePendingLocations(params) {
  // SWR key array includes pagination params for caching
  const key = ["pending-locations", params?.pageNumber, params?.pageSize];

  return useSWR(key, () =>
    locationApi.getPending(params).then((res) => res.data),
  );
}

// 2. Hook tìm kiếm locations
export function useSearchLocations() {
  return useSWRMutation("search-locations", (_, { arg: data }) =>
    locationApi.searchTerm(data).then((res) => res.data),
  );
}

// 2.5 Hook lấy tất cả locations cho các tab khác
export function useAllLocations(params) {
  const key = ["all-locations", params?.pageNumber, params?.pageSize];
  return useSWR(key, () => locationApi.getAll(params).then((res) => res.data));
}

// 3. Hook cập nhật location (Moderator)
export function useUpdateLocation() {
  return useSWRMutation("update-location", (_, { arg: { id, data } }) =>
    locationApi.update(id, data).then((res) => res.data),
  );
}

// 3.5 Hook tạo location mới (Moderator/Admin)
export function useCreateLocation() {
  return useSWRMutation("create-location", (_, { arg: formData }) =>
    locationApi.create(formData).then((res) => res.data),
  );
}

// 4. Hook xóa location
export function useDeleteLocation() {
  return useSWRMutation("delete-location", (_, { arg: id }) =>
    locationApi.delete(id).then((res) => res.data),
  );
}

// 5. Hook duyệt location (Approve)
export function useApproveLocation() {
  return useSWRMutation("approve-location", (_, { arg: { id, data } }) =>
    locationApi.approve(id, data).then((res) => res.data),
  );
}

// 6. Hook từ chối đề xuất location (Reject)
export function useRejectLocation() {
  return useSWRMutation("reject-location", (_, { arg: { id, data } }) =>
    locationApi.rejectSuggestion(id, data).then((res) => res.data),
  );
}
