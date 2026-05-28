import { useSearchParams } from "react-router-dom";
import { useCallback, useMemo } from "react";

const PAGE_SIZE = 20;

export function useExploreFilters() {
  const [searchParams, setSearchParams] = useSearchParams();

  // Đọc state từ URL (single source of truth)
  const filters = useMemo(() => {
    const rawSort = searchParams.get("sort") || "rating";
    let sortBy = rawSort;
    let sortColumn = undefined;

    if (rawSort === "popular") {
      sortBy = undefined;
      sortColumn = "RatingCount";
    }

    return {
      cityId: searchParams.get("cityId") || null,
      categoryId: searchParams.get("categoryId") || null,
      sortBy,
      sortColumn,
      isDescending: searchParams.get("desc") !== "false",
      pageNumber: Number(searchParams.get("page")) || 1,
      pageSize: PAGE_SIZE,
      isVerifiedOnly: true,
      searchText: searchParams.get("q") || null,
    };
  }, [searchParams]);

  // Derived state cho UI hiển thị
  const cityName = searchParams.get("cityName") || "";
  const activeSortOption = searchParams.get("sort") || "rating";
  const currentPage = filters.pageNumber;

  const setCity = useCallback((cityId, name) => {
    setSearchParams(prev => {
      if (cityId) {
        prev.set("cityId", cityId);
        prev.set("cityName", name);
      } else {
        prev.delete("cityId");
        prev.delete("cityName");
      }
      prev.set("page", "1"); // Reset page khi đổi filter
      return prev;
    }, { replace: true });
  }, [setSearchParams]);

  const setCategory = useCallback((categoryId) => {
    setSearchParams(prev => {
      if (categoryId) prev.set("categoryId", categoryId);
      else prev.delete("categoryId");
      prev.set("page", "1"); // Reset page khi đổi filter
      return prev;
    }, { replace: true });
  }, [setSearchParams]);

  const setSort = useCallback((sort, isDescending = true) => {
    setSearchParams(prev => {
      prev.set("sort", sort);
      if (!isDescending) prev.set("desc", "false");
      else prev.delete("desc");
      prev.set("page", "1"); // Reset page khi đổi filter
      return prev;
    }, { replace: true });
  }, [setSearchParams]);

  const setPage = useCallback((page) => {
    setSearchParams(prev => {
      prev.set("page", String(page));
      return prev;
    }, { replace: false }); // replace: false để có thể Back lại trang trước
  }, [setSearchParams]);

  const setSearchText = useCallback((text) => {
    setSearchParams(prev => {
      if (text && text.trim()) {
        prev.set("q", text.trim());
      } else {
        prev.delete("q");
      }
      prev.set("page", "1");
      return prev;
    }, { replace: true });
  }, [setSearchParams]);

  const resetFilters = useCallback(() => {
    setSearchParams({}, { replace: true });
  }, [setSearchParams]);

  return {
    filters,
    cityName,
    activeSortOption,
    currentPage,
    setCity,
    setCategory,
    setSort,
    setPage,
    setSearchText,
    resetFilters,
  };
}
