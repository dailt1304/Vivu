import React, { useState, useMemo, useEffect } from "react";
import { motion } from "framer-motion";
import { Check, ChevronsUpDown, Edit, MapPin, Map, Tag, Search as SearchIcon, Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "@/components/ui/command";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";
import DataTable from "@/components/cms/common/DataTable";
import StatusBadge from "@/components/cms/common/StatusBadge";
import LocationEditSheet from "@/components/cms/locations/LocationEditSheet";
import LocationCreateSheet from "@/components/cms/locations/LocationCreateSheet";
import locationApi from "@/api/locationApi";
import cityApi from "@/api/cityApi";
import locationCategoryApi from "@/api/locationCategoryApi";
import useDebounce from "@/hooks/utils/useDebounce";
import useSWR from "swr";
import toast from "@/utils/toast";
import { useLocation } from "react-router-dom";

const LocationListTab = () => {
  const [searchQuery, setSearchQuery] = useState("");
  const debouncedSearch = useDebounce(searchQuery, 300);

  const [cityOpen, setCityOpen] = useState(false);
  const [selectedCityId, setSelectedCityId] = useState(null);
  const [selectedCategoryId, setSelectedCategoryId] = useState(null);
  const [selectedLocation, setSelectedLocation] = useState(null);
  const [sheetOpen, setSheetOpen] = useState(false);
  const [createSheetOpen, setCreateSheetOpen] = useState(false);
  const [pageNumber, setPageNumber] = useState(1);
  const pageSize = 10;

  const loc = useLocation();
  const isAdminTheme = ['/cms/dashboard', '/cms/users', '/cms/subscriptions'].some(p => loc.pathname.includes(p));
  const focusBorder = isAdminTheme ? "focus:border-emerald-500 data-[state=open]:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20" : "focus:border-blue-500 data-[state=open]:border-blue-500 focus:ring-2 focus:ring-blue-500/20";


  const [sortConfig, setSortConfig] = useState({
    key: "recent",
    direction: "desc",
  });

  // Fetch all cities for the city selector dropdown
  const { data: citiesResponse, isLoading: isLoadingCities } = useSWR(
    ["all-cities-for-filter", { pageNumber: 1, pageSize: 999 }],
    ([, params]) => cityApi.getAll(params),
    { revalidateOnFocus: false },
  );

  const cities = useMemo(
    () => citiesResponse?.data?.items || [],
    [citiesResponse],
  );

  // Fetch all categories for the category filter dropdown
  const { data: categoriesResponse } = useSWR(
    ["all-categories-for-filter", { pageNumber: 1, pageSize: 100 }],
    ([, params]) => locationCategoryApi.getAll(params),
    { revalidateOnFocus: false },
  );

  const categories = useMemo(
    () => categoriesResponse?.data?.items || [],
    [categoriesResponse],
  );

  // Map frontend column keys to backend-accepted SortBy values
  // Backend only accepts: 'rating', 'distance', 'recent', 'name'
  const sortKeyMap = {
    name: "name",
    ratingAverage: "rating",
    createdAt: "recent",
    recent: "recent",
    categoryName: "name",
    isVerified: "recent",
  };

  // Only fetch locations when a city is selected
  const filterParams = useMemo(() => {
    if (!selectedCityId) return null;
    return {
      CityId: selectedCityId,
      PageNumber: pageNumber,
      PageSize: pageSize,
      SortBy: sortKeyMap[sortConfig.key] || "recent",
      IsDescending: sortConfig.direction === "desc",
      ...(debouncedSearch ? { SearchText: debouncedSearch } : {}),
      ...(selectedCategoryId ? { CategoryId: selectedCategoryId } : {}),
    };
  }, [selectedCityId, selectedCategoryId, pageNumber, sortConfig, debouncedSearch]);

  const {
    data: swrResponse,
    error,
    isLoading,
    mutate,
  } = useSWR(
    filterParams
      ? ["locations-by-city", JSON.stringify(filterParams)]
      : null,
    () => locationApi.getByFilter(filterParams),
    {
      revalidateOnFocus: false,
    },
  );

  useEffect(() => {
    if (error) {
      console.error("Failed to fetch locations", error);
      toast.error("Không thể tải danh sách địa điểm");
    }
  }, [error]);

  const items = useMemo(() => swrResponse?.data?.items || [], [swrResponse]);
  const totalCount = swrResponse?.data?.totalCount || 0;
  const totalPages = Math.ceil(totalCount / pageSize) || 1;

  // Reset page when city, category or search changes
  useEffect(() => {
    setPageNumber(1);
  }, [selectedCityId, selectedCategoryId, debouncedSearch]);

  const columns = [
    {
      key: "name",
      label: "Địa điểm",
      sortable: true,
      render: (value, row) => (
        <div className="flex items-center gap-3">
          <div className="h-10 w-10 rounded-lg bg-gray-100 flex items-center justify-center shrink-0 overflow-hidden">
            {row.locationDetail?.imageUrls?.[0] ? (
              <img
                src={row.locationDetail.imageUrls[0]}
                alt={value}
                className="h-full w-full object-cover"
              />
            ) : (
              <MapPin className="h-5 w-5 text-gray-500" />
            )}
          </div>
          <div className="min-w-0">
            <p className="font-medium text-gray-900 truncate max-w-[250px]">
              {value}
            </p>
            <p className="text-xs text-gray-500 truncate max-w-[200px]">
              {row.address}
            </p>
          </div>
        </div>
      ),
    },
    {
      key: "categoryName",
      label: "Danh mục",
      sortable: true,
      render: (_, row) => (
        <span className="text-xs font-medium px-2 py-1 rounded bg-blue-50 text-blue-700">
          {row.category?.name || row.categoryName || "Chưa phân loại"}
        </span>
      ),
    },
    {
      key: "ratingAverage",
      label: "Đánh giá",
      sortable: true,
      render: (value) => (
        <span className="text-sm font-medium">
          ⭐ {value ? Number(value).toFixed(1) : "N/A"}
        </span>
      ),
    },
    {
      key: "isVerified",
      label: "Trạng thái",
      sortable: true,
      render: (value) => (
        <StatusBadge status={value ? "APPROVED" : "PENDING"} />
      ),
    },
  ];

  const handleSort = (key) => {
    setSortConfig((prev) => ({
      key,
      direction: prev.key === key && prev.direction === "asc" ? "desc" : "asc",
    }));
    setPageNumber(1);
  };

  const selectedCityName = useMemo(() => {
    if (!selectedCityId) return null;
    return cities.find((c) => c.id === selectedCityId)?.name || "";
  }, [selectedCityId, cities]);

  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3 }}
      className="space-y-5"
    >
      {/* Header with Add button */}
      <div className="flex items-center justify-between pb-4 border-b border-zinc-100">
        <div className="flex flex-wrap items-center gap-4">
        {/* City Filter */}
        <div className="flex items-center gap-2">
          <div className="flex items-center gap-1.5 text-sm font-medium text-zinc-700">
            <Map className="h-4 w-4 text-emerald-500" />
            Khu vực:
          </div>
          {isLoadingCities ? (
            <Skeleton className="h-10 w-[220px] rounded-md bg-zinc-100" />
          ) : (
            <Popover open={cityOpen} onOpenChange={setCityOpen}>
              <PopoverTrigger asChild>
                <Button
                  variant="outline"
                  role="combobox"
                  aria-expanded={cityOpen}
                  className={`w-[220px] justify-between font-normal hover:bg-white bg-white hover:text-zinc-900 transition-colors ${focusBorder}`}
                >
                  <span className="truncate">
                    {selectedCityId
                      ? cities.find((city) => city.id === selectedCityId)?.name
                      : "Chọn Tỉnh/TP..."}
                  </span>
                  <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
                </Button>
              </PopoverTrigger>
              <PopoverContent className="w-[220px] p-0" align="start">
                <Command>
                  <CommandInput placeholder="Tìm nhanh Tỉnh/TP..." />
                  <CommandList>
                    <CommandEmpty>Không tìm thấy Tỉnh/TP nào.</CommandEmpty>
                    <CommandGroup>
                      {cities.map((city) => (
                        <CommandItem
                          key={city.id}
                          value={city.name}
                          onSelect={() => {
                            setSelectedCityId(city.id === selectedCityId ? null : city.id);
                            setCityOpen(false);
                          }}
                        >
                          <Check
                            className={cn(
                              "mr-2 h-4 w-4",
                              selectedCityId === city.id ? "opacity-100" : "opacity-0"
                            )}
                          />
                          {city.name}
                        </CommandItem>
                      ))}
                    </CommandGroup>
                  </CommandList>
                </Command>
              </PopoverContent>
            </Popover>
          )}
        </div>

        {/* Category Filter */}
        <div className="flex items-center gap-2">
          <div className="flex items-center gap-1.5 text-sm font-medium text-zinc-700">
            <Tag className="h-4 w-4 text-blue-500" />
            Danh mục:
          </div>
          <Select
            value={selectedCategoryId || ""}
            onValueChange={(val) => setSelectedCategoryId(val)}
          >
            <SelectTrigger className="w-[200px] bg-white">
              <SelectValue placeholder="Tất cả danh mục" />
            </SelectTrigger>
            <SelectContent>
              {categories.map((cat) => (
                <SelectItem key={cat.id} value={cat.id}>
                  {cat.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {/* Clear filters */}
        {(selectedCityId || selectedCategoryId) && (
          <Button
            variant="ghost"
            size="sm"
            className="text-xs text-zinc-400 hover:text-zinc-600"
            onClick={() => {
              setSelectedCityId(null);
              setSelectedCategoryId(null);
            }}
          >
            Xóa bộ lọc
          </Button>
        )}
        </div>
        <Button
          onClick={() => setCreateSheetOpen(true)}
          className="bg-blue-500 hover:bg-blue-600 text-white shrink-0 shadow-sm"
        >
          <Plus className="h-4 w-4 mr-2" />
          Thêm địa điểm
        </Button>
      </div>

      {/* Content */}
      {!selectedCityId ? (
        // Empty state: no city selected
        <div className="flex flex-col items-center justify-center py-20 text-center">
          <div className="w-16 h-16 rounded-2xl bg-zinc-100 flex items-center justify-center mb-5">
            <SearchIcon className="h-7 w-7 text-zinc-400" />
          </div>
          <h3 className="text-lg font-semibold text-zinc-800 mb-2">
            Chọn khu vực để xem địa điểm
          </h3>
          <p className="text-sm text-zinc-500 max-w-md leading-relaxed">
            Vui lòng chọn một Tỉnh/Thành phố từ bộ lọc phía trên để hiển thị
            danh sách các địa điểm thuộc khu vực đó.
          </p>
        </div>
      ) : (
        <>
          {selectedCityName && (
            <p className="text-xs text-zinc-500">
              Đang hiển thị địa điểm thuộc{" "}
              <span className="font-semibold text-zinc-700">
                {selectedCityName}
              </span>
            </p>
          )}
          <DataTable
            columns={columns}
            data={items}
            loading={isLoading}
            searchPlaceholder="Tìm kiếm địa điểm (tên hoặc địa chỉ)..."
            onSearch={(val) => {
              setSearchQuery(val);
            }}
            sortConfig={sortConfig}
            onSort={handleSort}
            pagination={{
              currentPage: pageNumber,
              totalPages: totalPages,
              from: totalCount === 0 ? 0 : (pageNumber - 1) * pageSize + 1,
              to: Math.min(pageNumber * pageSize, totalCount),
              total: totalCount,
            }}
            onPageChange={setPageNumber}
            actions={(row) => (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => {
                  setSelectedLocation(row);
                  setSheetOpen(true);
                }}
                className="text-blue-600 hover:text-blue-800 hover:bg-blue-50"
              >
                <Edit className="h-4 w-4 mr-2" />
                Sửa
              </Button>
            )}
            emptyMessage="Không có địa điểm nào trong khu vực này"
          />
        </>
      )}

      <LocationEditSheet
        open={sheetOpen}
        onOpenChange={setSheetOpen}
        location={selectedLocation}
        onMutate={() => mutate()}
      />

      <LocationCreateSheet
        open={createSheetOpen}
        onOpenChange={setCreateSheetOpen}
        onSuccess={() => mutate()}
      />
    </motion.div>
  );
};

export default LocationListTab;
