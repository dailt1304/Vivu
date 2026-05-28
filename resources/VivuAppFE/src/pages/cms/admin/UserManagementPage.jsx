import React, { useState, useMemo, useCallback, useRef } from "react";
import {
  Eye,
  MoreHorizontal,
  Ban,
  CheckCircle,
  UserPlus,
  Users,
  UserCheck,
  UserX,
} from "lucide-react";
import { toast } from "sonner";
import {
  useAllUsers,
  useUserActions,
  useUserManagementStats,
} from "@/hooks/users/useUsers";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import DataTable from "@/components/cms/common/DataTable";
import StatusBadge from "@/components/cms/common/StatusBadge";
import DetailSheet from "@/components/cms/common/DetailSheet";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";

const UserManagementPage = () => {
  const [activeTab, setActiveTab] = useState("all");
  const [roleFilter, setRoleFilter] = useState("all");
  const [selectedUser, setSelectedUser] = useState(null);
  const [sheetOpen, setSheetOpen] = useState(false);

  // Ban dialog state
  const [banDialogOpen, setBanDialogOpen] = useState(false);
  const [banReason, setBanReason] = useState("");
  const [userToBan, setUserToBan] = useState(null);

  const [searchQuery, setSearchQuery] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [sortConfig, setSortConfig] = useState({
    key: "createdAt",
    direction: "desc",
  });
  const [pagination, setPagination] = useState({
    pageNumber: 1,
    pageSize: 10,
  });

  const queryParams = useMemo(() => ({
    pageNumber: pagination.pageNumber,
    pageSize: pagination.pageSize,
    status: activeTab === "all" ? undefined : activeTab,
    role: roleFilter === "all" ? undefined : roleFilter,
    searchQuery: debouncedSearch,
    sortColumn: sortConfig.key,
    sortDescending: sortConfig.direction === "desc",
  }), [pagination.pageNumber, pagination.pageSize, activeTab, roleFilter, debouncedSearch, sortConfig.key, sortConfig.direction]);

  const {
    users,
    pagination: apiPagination,
    isLoading,
    mutate,
  } = useAllUsers(queryParams);

  const { usage, isLoading: usageLoading } = useUserManagementStats();
  const { ban, unban, isBanning, isUnbanning } = useUserActions();

  // Ref to avoid selectedUser in handleUnban deps (keeps renderActions stable)
  const selectedUserRef = useRef(selectedUser);
  selectedUserRef.current = selectedUser;

  const columns = useMemo(() => [
    {
      key: "fullName",
      label: "Người dùng",
      sortable: true,
      render: (value, row) => (
        <div className="flex items-center gap-3 py-1">
          <Avatar className="h-9 w-9 border border-zinc-200 shadow-sm">
            <AvatarImage src={row.avatarUrl} />
            <AvatarFallback className="bg-emerald-50 text-emerald-600 font-medium">
              {value?.charAt(0)}
            </AvatarFallback>
          </Avatar>
          <div className="flex flex-col">
            <span className="font-medium text-zinc-900 group-hover:text-emerald-600 transition-colors">
              {value}
            </span>
            <span className="text-xs text-zinc-500">{row.email}</span>
          </div>
        </div>
      ),
    },
    {
      key: "phone",
      label: "Số điện thoại",
      render: (value) => (
        <span className="font-[family-name:var(--font-cms-mono)] tabular-nums">
          {value || "N/A"}
        </span>
      ),
    },
    {
      key: "status",
      label: "Trạng thái",
      render: (value) => <StatusBadge status={value} />,
    },
    {
      key: "createdAt",
      label: "Ngày tạo",
      sortable: true,
      render: (value) =>
        value ? (
          <span className="font-[family-name:var(--font-cms-mono)] tabular-nums text-zinc-600">
            {new Date(value).toLocaleDateString("vi-VN")}
          </span>
        ) : (
          "-"
        ),
    },
  ], []);

  React.useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedSearch(searchQuery);
      setPagination((prev) => ({ ...prev, pageNumber: 1 }));
    }, 500);
    return () => clearTimeout(handler);
  }, [searchQuery]);

  const handlePageChange = useCallback((newPage) => {
    setPagination((prev) => ({ ...prev, pageNumber: newPage }));
  }, []);

  const handleSort = useCallback((key) => {
    setSortConfig((prev) => ({
      key,
      direction: prev.key === key && prev.direction === "asc" ? "desc" : "asc",
    }));
  }, []);

  const handleViewUser = useCallback((user) => {
    setSelectedUser(user);
    setSheetOpen(true);
  }, []);

  const confirmBan = useCallback((user) => {
    setUserToBan(user);
    setBanReason("");
    setBanDialogOpen(true);
  }, []);

  // No useCallback — only used inside conditionally-rendered ban Dialog
  const submitBan = async () => {
    if (!userToBan) return;
    try {
      await ban({
        id: userToBan.id,
        reason: banReason || "Vi phạm chính sách hệ thống",
      });
      toast.success("Đã khóa tài khoản người dùng");
      setBanDialogOpen(false);
      setUserToBan(null);
      mutate();
      if (selectedUser?.id === userToBan.id) {
        setSelectedUser((prev) => ({ ...prev, status: "banned" }));
      }
    } catch {
      toast.error("Không thể khóa tài khoản");
    }
  };

  const handleUnban = useCallback(async (user) => {
    try {
      await unban(user.id);
      toast.success("Đã mở khóa tài khoản người dùng");
      mutate();
      // Read from ref to keep this callback stable (used in renderActions → DataTable)
      if (selectedUserRef.current?.id === user.id) {
        setSelectedUser((prev) => ({ ...prev, status: "active" }));
      }
    } catch {
      toast.error("Không thể mở khóa tài khoản");
    }
  }, [unban, mutate]);

  const renderActions = useCallback((row) => (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          size="icon"
          className="group-hover:bg-zinc-200/50 transition-colors"
        >
          <MoreHorizontal className="h-4 w-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent
        align="end"
        className="w-48 shadow-[var(--shadow-cms-card-hover)] border-zinc-100 rounded-xl"
      >
        <DropdownMenuItem
          onClick={() => handleViewUser(row)}
          className="cursor-pointer"
        >
          <Eye className="h-4 w-4 mr-2" />
          Xem chi tiết
        </DropdownMenuItem>

        <DropdownMenuSeparator className="bg-zinc-100" />
        {row.status === "active" ? (
          <DropdownMenuItem
            disabled={isBanning}
            onClick={() => confirmBan(row)}
            className="text-red-600 focus:bg-red-50 focus:text-red-700 cursor-pointer"
          >
            <Ban className="h-4 w-4 mr-2" />
            Khóa tài khoản
          </DropdownMenuItem>
        ) : (
          <DropdownMenuItem
            disabled={isUnbanning}
            onClick={() => handleUnban(row)}
            className="text-emerald-600 focus:bg-emerald-50 focus:text-emerald-700 cursor-pointer"
          >
            <CheckCircle className="h-4 w-4 mr-2" />
            Mở khóa tài khoản
          </DropdownMenuItem>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  ), [handleViewUser, isBanning, confirmBan, isUnbanning, handleUnban]);

  return (
    <div className="cms-animate-page">
      <div className="cms-animate-stagger space-y-6">
        {/* Header */}
        <div className="flex flex-col sm:flex-row items-baseline justify-between gap-4 border-b border-zinc-200 pb-6">
          <div>
            <h1 className="text-2xl font-semibold tracking-tight text-zinc-900">
              Quản lý Người dùng
            </h1>
            <p className="text-sm text-zinc-500 mt-1.5 leading-relaxed">
              Quản lý tài khoản và phân quyền người dùng trong hệ thống
            </p>
          </div>

          <CreateUserDialog mutate={mutate} />
        </div>

        {/* Usage Statistics */}
        <div>
          <div className="flex flex-col sm:flex-row items-center divide-y sm:divide-y-0 sm:divide-x divide-zinc-200 bg-[var(--color-cms-card)] border border-zinc-200 rounded-xl overflow-hidden shadow-[var(--shadow-cms-card)]">
            <div className="flex items-center gap-4 p-5 flex-1 w-full hover:bg-zinc-50/50 transition-colors">
              <div className="w-12 h-12 rounded-xl bg-blue-50 flex items-center justify-center text-blue-600">
                <Users className="h-6 w-6" />
              </div>
              <div>
                <p className="text-xs font-medium text-zinc-500 uppercase tracking-wider">
                  Tổng người dùng
                </p>
                {usageLoading ? (
                  <Skeleton className="h-7 w-16 mt-1.5" />
                ) : (
                  <h3 className="text-2xl font-semibold mt-1 text-zinc-900 font-[family-name:var(--font-cms-mono)] tabular-nums">
                    {usage?.totalUsers?.toLocaleString() || 0}
                  </h3>
                )}
              </div>
            </div>
            <div className="flex items-center gap-4 p-5 flex-1 w-full hover:bg-zinc-50/50 transition-colors">
              <div className="w-12 h-12 rounded-xl bg-emerald-50 flex items-center justify-center text-emerald-600">
                <UserCheck className="h-6 w-6" />
              </div>
              <div>
                <p className="text-xs font-medium text-zinc-500 uppercase tracking-wider">
                  Hoạt động
                </p>
                {usageLoading ? (
                  <Skeleton className="h-7 w-16 mt-1.5" />
                ) : (
                  <h3 className="text-2xl font-semibold mt-1 text-zinc-900 font-[family-name:var(--font-cms-mono)] tabular-nums">
                    {usage?.activeUsers?.toLocaleString() || 0}
                  </h3>
                )}
              </div>
            </div>
            <div className="flex items-center gap-4 p-5 flex-1 w-full hover:bg-zinc-50/50 transition-colors">
              <div className="w-12 h-12 rounded-xl bg-red-50 flex items-center justify-center text-red-600">
                <UserX className="h-6 w-6" />
              </div>
              <div>
                <p className="text-xs font-medium text-zinc-500 uppercase tracking-wider">
                  Bị khóa
                </p>
                {usageLoading ? (
                  <Skeleton className="h-7 w-16 mt-1.5" />
                ) : (
                  <h3 className="text-2xl font-semibold mt-1 text-zinc-900 font-[family-name:var(--font-cms-mono)] tabular-nums">
                    {usage?.bannedUsers?.toLocaleString() || 0}
                  </h3>
                )}
              </div>
            </div>
        </div>
        </div>

        {/* Filters */}
        <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 py-2">
          <Tabs
            value={activeTab}
            onValueChange={setActiveTab}
            className="bg-transparent border-b border-zinc-200 pb-px"
          >
            <TabsList className="bg-transparent p-0 h-auto gap-6 pb-2">
              <TabsTrigger
                value="all"
                className="data-[state=active]:bg-transparent data-[state=active]:shadow-none data-[state=active]:border-b-2 data-[state=active]:border-emerald-500 rounded-none pb-2 pt-0 px-1 text-zinc-500 data-[state=active]:text-zinc-900 font-medium"
              >
                Tất cả
              </TabsTrigger>
              <TabsTrigger
                value="active"
                className="data-[state=active]:bg-transparent data-[state=active]:shadow-none data-[state=active]:border-b-2 data-[state=active]:border-emerald-500 rounded-none pb-2 pt-0 px-1 text-zinc-500 data-[state=active]:text-zinc-900 font-medium"
              >
                Hoạt động
              </TabsTrigger>
              <TabsTrigger
                value="banned"
                className="data-[state=active]:bg-transparent data-[state=active]:shadow-none data-[state=active]:border-b-2 data-[state=active]:border-emerald-500 rounded-none pb-2 pt-0 px-1 text-zinc-500 data-[state=active]:text-zinc-900 font-medium"
              >
                Bị khóa
              </TabsTrigger>
            </TabsList>
          </Tabs>

          <Select value={roleFilter} onValueChange={setRoleFilter}>
            <SelectTrigger className="w-[180px] bg-white border-zinc-200">
              <SelectValue placeholder="Lọc vai trò" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Tất cả vai trò</SelectItem>
              <SelectItem value="User">Người dùng</SelectItem>
              <SelectItem value="MODERATOR">Kiểm duyệt viên</SelectItem>
              <SelectItem value="ADMIN">Quản trị viên</SelectItem>
            </SelectContent>
          </Select>
        </div>

        {/* Data Table */}
        <div>
          <DataTable
            columns={columns}
            data={users}
            loading={isLoading}
            searchPlaceholder="Tìm kiếm người dùng theo tên hoặc email..."
            onSearch={setSearchQuery}
            sortConfig={sortConfig}
            onSort={handleSort}
            actions={renderActions}
            pagination={{
              currentPage: apiPagination.currentPage,
              totalPages: apiPagination.totalPages,
              from: (apiPagination.currentPage - 1) * pagination.pageSize + 1,
              to: Math.min(
                apiPagination.currentPage * pagination.pageSize,
                apiPagination.totalCount,
              ),
              total: apiPagination.totalCount,
            }}
            onPageChange={handlePageChange}
            emptyMessage="Không có người dùng nào khớp với bộ lọc."
          />
        </div>

        {/* Detail Sheet */}
        <DetailSheet
          open={sheetOpen}
          onOpenChange={setSheetOpen}
          title="Hồ sơ tài khoản"
          description={`Chi tiết hoạt động của ${selectedUser?.fullName}`}
        >
          {selectedUser && sheetOpen ? (
            <div className="space-y-8">
              <div className="flex items-center gap-4 bg-zinc-50 p-4 rounded-xl border border-zinc-100">
                <Avatar className="h-16 w-16 border border-zinc-200 shadow-sm">
                  <AvatarImage src={selectedUser.avatarUrl} />
                  <AvatarFallback className="bg-emerald-50 text-emerald-600 text-xl font-medium">
                    {selectedUser.fullName?.charAt(0)}
                  </AvatarFallback>
                </Avatar>
                <div>
                  <p className="text-xl font-semibold text-zinc-900">
                    {selectedUser.fullName}
                  </p>
                  <p className="text-sm text-zinc-500 font-medium">
                    {selectedUser.email}
                  </p>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-x-4 gap-y-6">
                <div>
                  <h4 className="text-xs font-medium text-zinc-400 uppercase tracking-wider mb-2">
                    Vai trò
                  </h4>
                  <Badge
                    variant="outline"
                    className="text-xs font-medium bg-zinc-100 border-zinc-200 text-zinc-700"
                  >
                    {selectedUser.role || "Người dùng"}
                  </Badge>
                </div>
                <div>
                  <h4 className="text-xs font-medium text-zinc-400 uppercase tracking-wider mb-2">
                    Trạng thái
                  </h4>
                  <StatusBadge status={selectedUser.status} />
                </div>
                <div>
                  <h4 className="text-xs font-medium text-zinc-400 uppercase tracking-wider mb-2">
                    Gói đăng ký
                  </h4>
                  <p className="text-sm text-zinc-900 font-medium">
                    {selectedUser.subscriptionType || "Không có (Miễn phí)"}
                  </p>
                </div>
                <div>
                  <h4 className="text-xs font-medium text-zinc-400 uppercase tracking-wider mb-2">
                    Ngày tham gia
                  </h4>
                  <p className="text-sm text-zinc-900 font-medium font-[family-name:var(--font-cms-mono)]">
                    {selectedUser.createdAt
                      ? new Date(selectedUser.createdAt).toLocaleDateString(
                          "vi-VN",
                        )
                      : "-"}
                  </p>
                </div>
              </div>

              <div className="flex gap-3 pt-6 border-t border-zinc-100">
                {selectedUser.status === "active" ? (
                  <Button
                    variant="outline"
                    disabled={isBanning}
                    onClick={() => {
                      confirmBan(selectedUser);
                      setSheetOpen(false); // Close sheet to show dialog properly, or keep it open.
                    }}
                    className="flex-1 cms-btn-interactive border-red-200 hover:bg-red-50 hover:text-red-700 text-red-600"
                  >
                    <Ban className="h-4 w-4 mr-2" />
                    Khóa tài khoản
                  </Button>
                ) : (
                  <Button
                    disabled={isUnbanning}
                    onClick={() => handleUnban(selectedUser)}
                    className="flex-1 cms-btn-interactive bg-emerald-600 hover:bg-emerald-700 text-white"
                  >
                    <CheckCircle className="h-4 w-4 mr-2" />
                    Mở khóa tài khoản
                  </Button>
                )}
              </div>
            </div>
          ) : null}
        </DetailSheet>

        <Dialog open={banDialogOpen} onOpenChange={setBanDialogOpen}>
          <DialogContent className="sm:max-w-[425px]">
            {banDialogOpen ? (
              <>
                <DialogHeader>
                  <DialogTitle className="text-red-600 flex items-center gap-2">
                    <Ban className="h-5 w-5" /> Xác nhận khóa tài khoản
                  </DialogTitle>
                  <DialogDescription>
                    Hành động này sẽ ngăn <strong>{userToBan?.fullName}</strong>{" "}
                    đăng nhập và sử dụng hệ thống.
                  </DialogDescription>
                </DialogHeader>
                <div className="space-y-4 py-4">
                  <div className="space-y-1.5">
                    <label className="text-sm font-medium text-zinc-900">
                      Lý do khóa <span className="text-red-500">*</span>
                    </label>
                    <Input
                      placeholder="e.g. Vi phạm nội dung liên tục..."
                      value={banReason}
                      onChange={(e) => setBanReason(e.target.value)}
                      autoFocus
                    />
                  </div>
                </div>
                <DialogFooter>
                  <Button
                    variant="outline"
                    onClick={() => setBanDialogOpen(false)}
                    className="cms-btn-interactive"
                  >
                    Hủy
                  </Button>
                  <Button
                    variant="destructive"
                    onClick={submitBan}
                    disabled={isBanning}
                    className="cms-btn-interactive"
                  >
                    {isBanning ? "Đang xử lý..." : "Xác nhận khóa"}
                  </Button>
                </DialogFooter>
              </>
            ) : null}
          </DialogContent>
        </Dialog>
      </div>
    </div>
  );
};

const CreateUserDialog = ({ mutate }) => {
  const [dialogOpen, setDialogOpen] = useState(false);
  const [newUser, setNewUser] = useState({
    fullName: "",
    email: "",
    password: "",
    role: "MODERATOR",
  });
  const { createUser, isCreating } = useUserActions();

  const handleCreateUser = async () => {
    if (!newUser.fullName || !newUser.email || !newUser.password) {
      toast.error("Vui lòng điền đầy đủ thông tin bắt buộc");
      return;
    }
    try {
      await createUser(newUser);
      toast.success("Đã tạo tài khoản mới");
      setDialogOpen(false);
      setNewUser({ fullName: "", email: "", password: "", role: "MODERATOR" });
      mutate();
    } catch {
      toast.error("Không thể tạo tài khoản");
    }
  };

  return (
    <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
      <DialogTrigger asChild>
        <Button className="cms-btn-interactive bg-emerald-600 text-white hover:bg-emerald-700 shadow-sm">
          <UserPlus className="h-4 w-4 mr-2" />
          Thêm quản trị viên
        </Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>Tạo tài khoản quản trị</DialogTitle>
          <DialogDescription>
            Thêm tài khoản Admin hoặc Moderator mới vào hệ thống
          </DialogDescription>
        </DialogHeader>
        <div className="space-y-5 py-4">
          <div className="space-y-1.5">
            <label className="text-sm font-medium text-zinc-900">
              Họ và tên <span className="text-red-500">*</span>
            </label>
            <Input
              placeholder="Nhập họ và tên"
              value={newUser.fullName}
              onChange={(e) =>
                setNewUser({ ...newUser, fullName: e.target.value })
              }
            />
          </div>
          <div className="space-y-1.5">
            <label className="text-sm font-medium text-zinc-900">
              Email <span className="text-red-500">*</span>
            </label>
            <Input
              type="email"
              placeholder="Nhập email"
              value={newUser.email}
              onChange={(e) => setNewUser({ ...newUser, email: e.target.value })}
            />
          </div>
          <div className="space-y-1.5">
            <label className="text-sm font-medium text-zinc-900">
              Mật khẩu <span className="text-red-500">*</span>
            </label>
            <Input
              type="password"
              placeholder="Nhập mật khẩu"
              value={newUser.password}
              onChange={(e) =>
                setNewUser({ ...newUser, password: e.target.value })
              }
            />
          </div>
          <div className="space-y-1.5">
            <label className="text-sm font-medium text-zinc-900">Vai trò</label>
            <Select
              value={newUser.role}
              onValueChange={(value) => setNewUser({ ...newUser, role: value })}
            >
              <SelectTrigger>
                <SelectValue placeholder="Chọn vai trò" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="MODERATOR">
                  Kiểm duyệt viên (Moderator)
                </SelectItem>
                <SelectItem value="ADMIN">Quản trị viên (Admin)</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>
        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => setDialogOpen(false)}
            className="cms-btn-interactive"
          >
            Hủy
          </Button>
          <Button
            onClick={handleCreateUser}
            disabled={isCreating}
            className="cms-btn-interactive bg-emerald-600 hover:bg-emerald-700 text-white"
          >
            {isCreating ? "Đang tạo..." : "Tạo tài khoản"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};

export default UserManagementPage;
