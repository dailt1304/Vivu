import useSWRInfinite from "swr/infinite";
import tripApi from "../../api/tripApi";

/**
 * Hook for fetching and paginating trip chat history
 * @param {string} tripId - The ID of the trip
 * @param {number} pageSize - Number of messages per page
 */
const useChatHistory = (tripId, pageSize = 50) => {
  const getKey = (pageIndex, previousPageData) => {
    if (!tripId) return null;
    // backend pageNumber is 1-based
    if (
      previousPageData &&
      (!previousPageData.items || previousPageData.items.length === 0)
    )
      return null; // reached the end
    return ["chat-history", tripId, pageIndex + 1, pageSize];
  };

  const fetcher = async ([_, id, pageNumber, size]) => {
    const response = await tripApi.getChatHistory(id, {
      pageNumber,
      pageSize: size,
    });
    if (response.success && response.data) {
      return response.data;
    }
    throw new Error(response.message || "Failed to fetch chat history");
  };

  const { data, error, size, setSize, isValidating, mutate } = useSWRInfinite(
    getKey,
    fetcher,
    {
      revalidateOnFocus: false, // Chat history is generally static unless new messages arrive via push/socket
    },
  );

  // Accumulate all messages from all pages
  const messages = data ? data.flatMap((page) => page.items || []) : [];

  const isLoadingInitialData = !data && !error;
  const isLoadingMore =
    isLoadingInitialData ||
    (size > 0 && data && typeof data[size - 1] === "undefined");
  const isEmpty = data?.[0]?.items?.length === 0;
  const isReachingEnd =
    isEmpty || (data && data[data.length - 1]?.items?.length < pageSize);

  const fetchMore = () => {
    if (!isReachingEnd && !isLoadingMore) {
      setSize(size + 1);
    }
  };

  return {
    messages,
    error,
    isLoading: isLoadingInitialData,
    isLoadingMore,
    isReachingEnd,
    fetchMore,
    mutate,
  };
};

export default useChatHistory;
