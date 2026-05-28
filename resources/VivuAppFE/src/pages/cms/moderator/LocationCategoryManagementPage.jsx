import React, { useState, useMemo, useCallback } from "react";
import { Plus, Edit, Image as ImageIcon, Layers, MoreHorizontal, Trash2, Power, PowerOff } from "lucide-react";
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

import CategoryFormSheet from "@/components/cms/categories/CategoryFormSheet";
import ConfirmDialog from "@/components/cms/common/ConfirmDialog";

import { useAllCategories, useDeleteCategory, useChangeCategoryStatus } from "@/hooks/locations/useCategoryManagement";

// Mapping with backend Enum
const CATEGORY_TYPES = {
  0: { label: "Tham quan", color: "bg-blue-100 text-blue-800" },
  1: { label: "Ăn uống", color: "bg-orange-100 text-orange-800" },
  2: { label: "Lưu trú", color: "bg-purple-100 text-purple-800" },
  3: { label: "Giải trí", color: "bg-pink-100 text-pink-800" },
  4: { label: "Khác", color: "bg-gray-100 text-gray-800" },
  5: { label: "Thiên nhiên", color: "bg-emerald-100 text-emerald-800" }
};

const LocationCategoryManagementPage = () => {
  const [pageNumber, setPageNumber] = useState(1);
  const [searchQuery, setSearchQuery] = useState("");
  const debouncedSearch = useDebounce(searchQuery, 400);

  const loc = useLocation();
  const isAdminTheme = ['/cms/dashboard', '/cms/users', '/cms/subscriptions'].some(p => loc.pathname.includes(p));
  const iconThemeClass = isAdminTheme ? "text-emerald-400" : "text-blue-400";
  const btnThemeClass = isAdminTheme ? "bg-emerald-400 hover:bg-emerald-500 text-white font-medium shadow-md shadow-emerald-400/20" : "bg-blue-400 hover:bg-blue-500 text-white font-medium shadow-md shadow-blue-400/20";

  const [sortConfig, setSortConfig] = useState({
    key: "name",
    direction: "asc",
  });

  const { data: categoriesRes, isLoading, mutate } = useAllCategories({
     pageNumber,
     pageSize: 20,
     sortColumn: sortConfig.key,
     sortDescending: sortConfig.direction === "desc"
  });
  
  const { trigger: deleteCategory, isMutating: isDeleting } = useDeleteCategory();
  const { trigger: changeStatus } = useChangeCategoryStatus();

  // Modals state
  const [formSheetOpen, setFormSheetOpen] = useState(false);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [selectedCategory, setSelectedCategory] = useState(null);

  const categoriesData = useMemo(() => categoriesRes?.data?.items || categoriesRes?.items || [], [categoriesRes]);

  const columns = useMemo(() => [
    {
      key: "name",
      label: "Tên danh mục",
      sortable: true,
      render: (value, row) => {
         const isUrl = row.iconUrl && (row.iconUrl.startsWith('http') || row.iconUrl.startsWith('/'));
         return (
         <div className="flex items-center gap-3">
           <div className="w-10 h-10 rounded-xl bg-zinc-100 flex items-center justify-center border border-zinc-200/60 overflow-hidden shrink-0">
             {row.iconUrl ? (
               isUrl ? (
                 <img src={row.iconUrl} alt={value} className="w-6 h-6 object-contain" />
               ) : (
                 <span className="text-xl leading-none">{row.iconUrl}</span>
               )
             ) : (
               <ImageIcon className="w-4 h-4 text-zinc-400" />
             )}
           </div>
           <span className="font-semibold text-zinc-900">{value}</span>
         </div>
      )},
    },
    {
      key: "categoryType",
      label: "Phân loại",
      render: (value) => {
         const typeInfo = CATEGORY_TYPES[value] || CATEGORY_TYPES[4];
         return (
           <span className={`text-xs font-medium px-2.5 py-1 rounded-full ${typeInfo.color}`}>
              {typeInfo.label}
           </span>
         );
      },
    },
    {
      key: "locationCount",
      label: "Số lượng địa điểm",
      sortable: true,
      render: (value) => (
        <span className="font-[family-name:var(--font-cms-mono)] font-medium tabular-nums text-zinc-700 bg-zinc-100 px-2.5 py-1 rounded-md">
           {(value ?? 0).toLocaleString()} <span className="text-zinc-400 text-xs font-sans ml-1">địa điểm</span>
        </span>
      ),
    },
    {
      key: "isActive",
      label: "Trạng thái",
      render: (value) => (
         <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium border ${
            value 
              ? 'bg-emerald-50 text-emerald-700 border-emerald-200/60' 
              : 'bg-zinc-100 text-zinc-600 border-zinc-200/60'
          }`}>
            <span className={`w-1.5 h-1.5 rounded-full ${value ? 'bg-emerald-500' : 'bg-zinc-400'}`}></span>
            {value ? 'Đang hoạt động' : 'Đang ẩn'}
         </span>
      ),
    },
  ], []);

  const filteredData = useMemo(() => {
    let result = [...categoriesData];

    if (debouncedSearch) {
      const q = debouncedSearch.toLowerCase();
      result = result.filter(
        (cat) => cat.name?.toLowerCase().includes(q)
      );
    }

    return result;
  }, [debouncedSearch, categoriesData]);

  const handleSort = useCallback((key) => {
    setSortConfig((prev) => ({
      key,
      direction: prev.key === key && prev.direction === "asc" ? "desc" : "asc",
    }));
  }, []);

  const handleOpenAdd = useCallback(() => {
    setSelectedCategory(null);
    setFormSheetOpen(true);
  }, []);

  const handleOpenEdit = useCallback((cat) => {
    setSelectedCategory(cat);
    setFormSheetOpen(true);
  }, []);

  const handleToggleStatus = useCallback(async (cat) => {
     try {
        await changeStatus({ id: cat.id, data: { isActive: !cat.isActive } });
        toast.success(`Đã ${!cat.isActive ? 'hiện' : 'ẩn'} danh mục ${cat.name}`);
        mutate();
     } catch(e) {
        toast.error("Có lỗi xảy ra khi đổi trạng thái");
     }
  }, [changeStatus, mutate]);

  const handleDeleteConfirm = async () => {
     try {
        await deleteCategory(selectedCategory.id);
        toast.success(`Đã xóa danh mục ${selectedCategory.name} thành công!`);
        setConfirmOpen(false);
        mutate();
     } catch(e) {
        toast.error(e?.response?.data?.message || "Có lỗi khi xóa danh mục. Có thể danh mục này đang có địa điểm sử dụng.");
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
        <DropdownMenuItem onClick={() => handleOpenEdit(row)} className="cursor-pointer">
          <Edit className="h-4 w-4 mr-2 text-zinc-500" />
          Chỉnh sửa thông tin
        </DropdownMenuItem>
        
        <DropdownMenuItem onClick={() => handleToggleStatus(row)} className="cursor-pointer">
          {row.isActive ? (
             <>
                <PowerOff className="h-4 w-4 mr-2 text-orange-500" />
                Ẩn danh mục
             </>
          ) : (
             <>
                <Power className="h-4 w-4 mr-2 text-emerald-500" />
                Hiện danh mục
             </>
          )}
        </DropdownMenuItem>

        <DropdownMenuSeparator className="bg-zinc-100" />
        
        <DropdownMenuItem
            onClick={() => {
                setSelectedCategory(row);
                setConfirmOpen(true);
            }}
            className="text-red-600 focus:text-red-700 focus:bg-red-50 cursor-pointer"
        >
        <Trash2 className="h-4 w-4 mr-2" />
        Xóa danh mục
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  ), [handleOpenEdit, handleToggleStatus]);

  return (
    <div className="cms-animate-page">
      <div className="cms-animate-stagger space-y-6">
        <div className="flex flex-col sm:flex-row items-baseline justify-between gap-4 border-b border-zinc-200 pb-6">
          <div>
            <h1 className="text-2xl font-semibold tracking-tight text-zinc-900 flex items-center gap-2">
              <Layers className={`h-6 w-6 ${iconThemeClass}`} />
              Quản lý Danh mục
            </h1>
            <p className="text-sm text-zinc-500 mt-1.5 leading-relaxed">
              Quản lý các danh mục và phân loại dùng chung cho địa điểm.
            </p>
          </div>
          <Button onClick={handleOpenAdd} className={`cms-btn-interactive shadow-sm border-none ${btnThemeClass}`}>
            <Plus className="h-4 w-4 mr-2" />
            Thêm Danh mục mới
          </Button>
        </div>

        <div>
          <DataTable
            columns={columns}
            data={filteredData}
            loading={isLoading}
            searchPlaceholder="Tìm kiếm theo tên danh mục..."
            onSearch={(v) => { setSearchQuery(v); setPageNumber(1); }}
            sortConfig={sortConfig}
            onSort={handleSort}
            actions={renderActions}
            pagination={{
              currentPage: categoriesRes?.data?.pageNumber || categoriesRes?.pageNumber || pageNumber,
              totalPages: categoriesRes?.data?.totalPages || categoriesRes?.totalPages || 1,
              from: ((pageNumber - 1) * 20) + 1,
              to: Math.min(pageNumber * 20, categoriesRes?.data?.totalCount || categoriesRes?.totalCount || filteredData.length),
              total: categoriesRes?.data?.totalCount || categoriesRes?.totalCount || filteredData.length,
            }}
            onPageChange={setPageNumber}
            emptyMessage="Không tìm thấy danh mục nào trong hệ thống."
          />
        </div>

      </div>

      {/* Sheets & Modals */}
      <CategoryFormSheet 
         open={formSheetOpen}
         onOpenChange={setFormSheetOpen}
         category={selectedCategory}
         onMutate={mutate}
      />

      <ConfirmDialog
         open={confirmOpen}
         onOpenChange={setConfirmOpen}
         title="Xóa danh mục?"
         description={`Bạn có chắc chắn muốn xóa danh mục "${selectedCategory?.name}"? Hệ thống sẽ không cho phép xoá nếu đang có địa điểm thuộc danh mục này.`}
         onConfirm={handleDeleteConfirm}
         loading={isDeleting}
         isDestructive={true}
      />
    </div>
  );
};

export default LocationCategoryManagementPage;
