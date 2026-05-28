import React, { useState, useMemo, useCallback } from "react";
import { Plus, Edit, Eye, Map, MoreHorizontal, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  DropdownMenuSeparator
} from "@/components/ui/dropdown-menu";
import DataTable from "@/components/cms/common/DataTable";
import toast from "@/utils/toast";
import useDebounce from "@/hooks/utils/useDebounce";
import { useLocation } from "react-router-dom";

import CityFormSheet from "@/components/cms/cities/CityFormSheet";
import CityDetailSheet from "@/components/cms/cities/CityDetailSheet";
import ConfirmDialog from "@/components/cms/common/ConfirmDialog";

import { useAllCities, useDeleteCity } from "@/hooks/cities/useCities";

const CityManagementPage = () => {
  const [pageNumber, setPageNumber] = useState(1);
  const [searchQuery, setSearchQuery] = useState("");
  const debouncedSearch = useDebounce(searchQuery, 400);

  const loc = useLocation();
  const isAdminTheme = ['/cms/dashboard', '/cms/users', '/cms/subscriptions'].some(p => loc.pathname.includes(p));
  const iconThemeClass = isAdminTheme ? "text-emerald-400" : "text-blue-400";
  const btnThemeClass = isAdminTheme ? "bg-emerald-400 hover:bg-emerald-500 text-white font-medium shadow-md shadow-emerald-400/20" : "bg-blue-400 hover:bg-blue-500 text-white font-medium shadow-md shadow-blue-400/20";

  const [sortConfig, setSortConfig] = useState({
    key: "createdAt",
    direction: "desc",
  });

  const { data: citiesRes, isLoading, mutate } = useAllCities({
    pageNumber,
    pageSize: 20
    // If backend supports sortColumn: sortConfig.key, sortDescending: sortConfig.direction === "desc"
  });
  
  const { trigger: deleteCity, isMutating: isDeleting } = useDeleteCity();

  // Modals state
  const [formSheetOpen, setFormSheetOpen] = useState(false);
  const [detailSheetOpen, setDetailSheetOpen] = useState(false);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [selectedCity, setSelectedCity] = useState(null);

  const citiesData = useMemo(() => citiesRes?.data?.items || citiesRes?.items || [], [citiesRes]);

  const columns = useMemo(() => [
    {
      key: "name",
      label: "Thành phố / Tỉnh",
      sortable: true,
      render: (value) => (
        <span className="font-semibold text-zinc-900">{value}</span>
      ),
    },
    {
      key: "country",
      label: "Quốc gia",
      render: (value) => (
        <span className="text-sm text-zinc-500 font-medium">
            {value?.name || "Việt Nam"}
        </span>
      ),
    },
    {
      key: "locationCount",
      label: "Số địa điểm",
      sortable: true,
      render: (value) => (
        <span className="font-[family-name:var(--font-cms-mono)] font-medium tabular-nums text-zinc-700 bg-zinc-100 px-2 py-0.5 rounded">
           {(value ?? 0).toLocaleString()}
        </span>
      ),
    },
    {
      key: "createdDate",
      label: "Ngày tạo",
      sortable: true,
      render: (value) => (
        <span className="font-[family-name:var(--font-cms-mono)] text-sm text-zinc-500">
            {value ? new Date(value).toLocaleDateString("vi-VN") : "N/A"}
        </span>
      ),
    },
  ], []);

  const filteredData = useMemo(() => {
    let result = [...citiesData];

    if (debouncedSearch) {
      const q = debouncedSearch.toLowerCase();
      result = result.filter(
        (city) => city.name?.toLowerCase().includes(q)
      );
    }

    if (sortConfig) {
      result.sort((a, b) => {
        const aVal = a[sortConfig.key] || "";
        const bVal = b[sortConfig.key] || "";
        if (sortConfig.direction === "asc") {
          return aVal > bVal ? 1 : -1;
        }
        return aVal < bVal ? 1 : -1;
      });
    }

    return result;
  }, [debouncedSearch, sortConfig, citiesData]);

  const handleSort = useCallback((key) => {
    setSortConfig((prev) => ({
      key,
      direction: prev.key === key && prev.direction === "asc" ? "desc" : "asc",
    }));
  }, []);

  const handleOpenAdd = useCallback(() => {
    setSelectedCity(null);
    setFormSheetOpen(true);
  }, []);

  const handleOpenEdit = useCallback((city) => {
    setSelectedCity(city);
    setFormSheetOpen(true);
  }, []);

  const handleOpenDetail = useCallback((city) => {
    setSelectedCity(city);
    setDetailSheetOpen(true);
  }, []);

  // No useCallback — only used in ConfirmDialog's onConfirm, not in DataTable
  const handleDeleteConfirm = async () => {
     try {
        await deleteCity(selectedCity.id);
        toast.success(`Đã xóa thành phố ${selectedCity.name} thành công!`);
        setConfirmOpen(false);
        mutate();
     } catch(e) {
        toast.error(e?.response?.data?.message || "Có lỗi khi xóa thành phố");
     }
  };

  const renderActions = useCallback((row) => (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" className="group-hover:bg-zinc-200/50">
          <MoreHorizontal className="h-4 w-4 text-zinc-500" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-48 shadow-[var(--shadow-cms-card-hover)] border-zinc-100 rounded-xl">
        <DropdownMenuItem onClick={() => handleOpenDetail(row)} className="cursor-pointer">
          <Eye className="h-4 w-4 mr-2 text-zinc-500" />
          Xem chi tiết
        </DropdownMenuItem>
        <DropdownMenuSeparator className="bg-zinc-100" />
        <DropdownMenuItem onClick={() => handleOpenEdit(row)} className="cursor-pointer">
          <Edit className="h-4 w-4 mr-2 text-zinc-500" />
          Chỉnh sửa thông tin
        </DropdownMenuItem>
        <DropdownMenuItem
            onClick={() => {
                setSelectedCity(row);
                setConfirmOpen(true);
            }}
            className="text-red-600 focus:text-red-700 focus:bg-red-50 cursor-pointer mt-1"
        >
        <Trash2 className="h-4 w-4 mr-2" />
        Xóa khu vực
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  ), [handleOpenDetail, handleOpenEdit]);

  return (
    <div className="cms-animate-page">
      <div className="cms-animate-stagger space-y-6">
        <div className="flex flex-col sm:flex-row items-baseline justify-between gap-4 border-b border-zinc-200 pb-6">
          <div>
            <h1 className="text-2xl font-semibold tracking-tight text-zinc-900 flex items-center gap-2">
              <Map className={`h-6 w-6 ${iconThemeClass}`} />
              Quản lý Tỉnh/Thành phố
            </h1>
            <p className="text-sm text-zinc-500 mt-1.5 leading-relaxed">
              Quản lý danh sách các Tỉnh/Thành phổ hỗ trợ được lấy qua API thực tế.
            </p>
          </div>
          <Button onClick={handleOpenAdd} className={`cms-btn-interactive shadow-sm border-none ${btnThemeClass}`}>
            <Plus className="h-4 w-4 mr-2" />
            Thêm Khu vực mới
          </Button>
        </div>

        <div>
          <DataTable
            columns={columns}
            data={filteredData}
            loading={isLoading}
            searchPlaceholder="Tìm kiếm theo tên hoặc tỉnh thành..."
            onSearch={(v) => { setSearchQuery(v); setPageNumber(1); }}
            sortConfig={sortConfig}
            onSort={handleSort}
            actions={renderActions}
            pagination={{
              currentPage: citiesRes?.data?.pageNumber || citiesRes?.pageNumber || pageNumber,
              totalPages: citiesRes?.data?.totalPages || citiesRes?.totalPages || 1,
              from: ((pageNumber - 1) * 20) + 1,
              to: Math.min(pageNumber * 20, citiesRes?.data?.totalCount || citiesRes?.totalCount || filteredData.length),
              total: citiesRes?.data?.totalCount || citiesRes?.totalCount || filteredData.length,
            }}
            onPageChange={setPageNumber}
            emptyMessage="Không tìm thấy khu vực nào trong hệ thống."
          />
        </div>

      </div>

      {/* Sheets & Modals */}
      <CityFormSheet 
         open={formSheetOpen}
         onOpenChange={setFormSheetOpen}
         city={selectedCity}
         onMutate={mutate}
      />

      <CityDetailSheet
         open={detailSheetOpen}
         onOpenChange={setDetailSheetOpen}
         city={selectedCity}
      />

      <ConfirmDialog
         open={confirmOpen}
         onOpenChange={setConfirmOpen}
         title="Xóa khu vực?"
         description={`Bạn có chắc chắn muốn xóa thành phố ${selectedCity?.name}? Hành động này không thể hoàn tác.`}
         onConfirm={handleDeleteConfirm}
         loading={isDeleting}
         isDestructive={true}
      />
    </div>
  );
};

export default CityManagementPage;
