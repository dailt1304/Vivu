import useSWR from "swr";
import useSWRMutation from "swr/mutation";
import locationReportApi from "../../api/locationReportApi";

// 1. Hook lấy danh sách reports
export function useLocationReports(params) {
  // SWR key array includes pagination and status filter for caching
  const key = [
    "location-reports",
    params?.pageNumber,
    params?.pageSize,
    params?.status,
  ];

  return useSWR(key, () =>
    locationReportApi.getAll(params).then((res) => res.data),
  );
}

// 2. Hook xử lý report (Approve/Reject)
export function useReviewReport() {
  return useSWRMutation("review-report", (_, { arg: data }) =>
    locationReportApi.review(data).then((res) => res.data),
  );
}

// 3. Hook lấy danh sách reports của user hiện tại
export function useMyLocationReports(params) {
  const key = params
    ? ["my-location-reports", JSON.stringify(params)]
    : null;
  return useSWR(key, () =>
    locationReportApi.getMyReports(params).then((res) => res.data),
  );
}

// 4. Hook cập nhật report (cho user sỡ hữu report)
export function useUpdateLocationReport() {
  return useSWRMutation("update-location-report", (_, { arg }) =>
    locationReportApi.update(arg.id, arg.formData).then((res) => res.data),
  );
}
