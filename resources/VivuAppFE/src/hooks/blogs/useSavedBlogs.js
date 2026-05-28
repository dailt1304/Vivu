import useSWR from "swr";
import blogApi from "../../api/blogApi";

export function useSavedBlogs(pageNumber = 1, pageSize = 12) {
  return useSWR(
    ["saved-blogs", pageNumber, pageSize],
    () => blogApi.getMyBookmarks({ pageNumber, pageSize }).then((res) => res.data)
  );
}
