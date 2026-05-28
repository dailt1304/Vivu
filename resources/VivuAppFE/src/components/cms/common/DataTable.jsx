import React, { useState } from "react";
import { cn } from "@/lib/utils";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import ErrorBanner from "./ErrorBanner";
import EmptyState from "./EmptyState";
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
  Search,
  SlidersHorizontal,
  ArrowUpDown,
  ArrowUp,
  ArrowDown,
} from "lucide-react";

const DataTable = ({
  columns,
  data,
  loading = false,
  error = null,
  onRetry = null,
  searchPlaceholder = "Tìm kiếm...",
  onSearch,
  pagination,
  onPageChange,
  onSort,
  sortConfig,
  actions,
  emptyMessage = "Không có dữ liệu",
}) => {
  const [searchValue, setSearchValue] = useState("");
  const [visibleColumns, setVisibleColumns] = useState(
    columns.map((col) => col.key),
  );

  const handleSearch = (value) => {
    setSearchValue(value);
    onSearch?.(value);
  };

  const toggleColumn = (key) => {
    setVisibleColumns((prev) =>
      prev.includes(key) ? prev.filter((k) => k !== key) : [...prev, key],
    );
  };

  const getSortIcon = (key) => {
    const isActive = sortConfig?.key === key;
    return (
      <ArrowUp className={cn(
        "h-3.5 w-3.5 transition-transform duration-200 ease-out",
        isActive 
          ? (sortConfig.direction === "desc" ? "rotate-180 text-emerald-600" : "text-emerald-600") 
          : "text-zinc-300 opacity-0 group-hover:opacity-100"
      )} />
    );
  };

  const visibleColumnsData = columns.filter((col) =>
    visibleColumns.includes(col.key),
  );

  return (
    <div className="space-y-4">
      {/* Toolbar */}
      <div className="flex items-center justify-between gap-4">
        <div className="flex items-center gap-2 flex-1">
          <div className="relative max-w-sm flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-500" />
            <Input
              placeholder={searchPlaceholder}
              value={searchValue}
              onChange={(e) => handleSearch(e.target.value)}
              className="pl-9"
            />
          </div>
        </div>

        <div className="flex items-center gap-2">
          {/* Column visibility */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline" size="sm">
                <SlidersHorizontal className="h-4 w-4 mr-2" />
                Cột
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              {columns.map((column) => (
                <DropdownMenuCheckboxItem
                  key={column.key}
                  checked={visibleColumns.includes(column.key)}
                  onCheckedChange={() => toggleColumn(column.key)}
                >
                  {column.label}
                </DropdownMenuCheckboxItem>
              ))}
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>

      {error ? (
        <ErrorBanner 
           title="Lỗi tải dữ liệu" 
           description={typeof error === 'string' ? error : "Có lỗi xảy ra khi gọi dữ liệu từ máy chủ."} 
           onRetry={onRetry} 
        />
      ) : (
      <div className="rounded-md border bg-white">
        <Table>
          <TableHeader>
            <TableRow>
              {visibleColumnsData.map((column) => (
                <TableHead
                  key={column.key}
                  className={cn(
                    "group text-xs text-zinc-500 font-medium uppercase tracking-wider",
                    column.sortable && "cursor-pointer select-none hover:text-zinc-900 transition-colors",
                  )}
                  onClick={() => column.sortable && onSort?.(column.key)}
                >
                  <div className="flex items-center gap-1.5">
                    {column.label}
                    {column.sortable && getSortIcon(column.key)}
                  </div>
                </TableHead>
              ))}
              {actions && <TableHead className="w-[100px]">Thao tác</TableHead>}
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              // Loading skeleton
              [...Array(5)].map((_, i) => (
                <TableRow key={i}>
                  {visibleColumnsData.map((column) => (
                    <TableCell key={column.key}>
                       <Skeleton className="h-5 w-full bg-zinc-100" />
                    </TableCell>
                  ))}
                  {actions && (
                    <TableCell>
                       <Skeleton className="h-8 w-8 bg-zinc-100" />
                    </TableCell>
                  )}
                </TableRow>
              ))
            ) : data.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={visibleColumnsData.length + (actions ? 1 : 0)}
                  className="h-64 p-0"
                >
                  <EmptyState 
                     title="Không có dữ liệu" 
                     description={emptyMessage} 
                     inline={true} 
                  />
                </TableCell>
              </TableRow>
            ) : (
              data.map((row, index) => (
                <TableRow 
                  key={row.id || index}
                  className="hover:bg-zinc-50/50 transition-colors duration-150 group"
                >
                  {visibleColumnsData.map((column) => (
                    <TableCell key={column.key}>
                      {column.render
                        ? column.render(row[column.key], row)
                        : row[column.key]}
                    </TableCell>
                  ))}
                  {actions && <TableCell>{actions(row)}</TableCell>}
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>
      )}

      {/* Pagination */}
      {pagination && data.length > 0 && !error && !loading && (
        <div className="flex items-center justify-between mt-4">
          <p className="text-sm text-gray-500">
            Hiển thị {pagination.from} - {pagination.to} trên {pagination.total}
          </p>
          <div className="flex items-center gap-1">
            <Button
              variant="outline"
              size="icon"
              onClick={() => onPageChange?.(1)}
              disabled={pagination.currentPage === 1}
              className="h-8 w-8 hover:border-emerald-200 hover:text-emerald-600 transition-colors"
            >
              <ChevronsLeft className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              size="icon"
              onClick={() => onPageChange?.(pagination.currentPage - 1)}
              disabled={pagination.currentPage === 1}
              className="h-8 w-8 hover:border-emerald-200 hover:text-emerald-600 transition-colors"
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <span className="px-3 text-sm font-medium font-[family-name:var(--font-cms-mono)] text-zinc-600">
              Trang {pagination.currentPage} / {pagination.totalPages}
            </span>
            <Button
              variant="outline"
              size="icon"
              onClick={() => onPageChange?.(pagination.currentPage + 1)}
              disabled={pagination.currentPage === pagination.totalPages}
              className="h-8 w-8 hover:border-emerald-200 hover:text-emerald-600 transition-colors"
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              size="icon"
              onClick={() => onPageChange?.(pagination.totalPages)}
              disabled={pagination.currentPage === pagination.totalPages}
              className="h-8 w-8 hover:border-emerald-200 hover:text-emerald-600 transition-colors"
            >
              <ChevronsRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
};

export default DataTable;
