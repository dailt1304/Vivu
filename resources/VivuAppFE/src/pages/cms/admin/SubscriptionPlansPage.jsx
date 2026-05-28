import React, { useState, useMemo, useCallback } from "react";
import {
  Plus,
  Edit,
  Check,
  Crown,
  Star,
  Zap,
  MoreVertical,
  Activity,
  Archive
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardHeader, CardTitle, CardFooter, CardDescription } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Switch } from "@/components/ui/switch";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { cn } from "@/lib/utils";
import { useSubscriptionPlans, useSubscriptionMutation } from "@/hooks/subscriptions/useSubscriptions";
import { toast } from "sonner";

// ✅ Vercel rule: rerender-memo — Hoist non-primitive constants ra module-level
const DURATION_DAYS_MAP = {
  "Tháng": 30,
  "Năm": 365,
  "Vĩnh viễn": 9999,
};

const TYPE_MAP = {
  FREE: 1,
  PREMIUM: 2,
  PRO: 3,
};

// ✅ Fix BUG 4: Reverse map để normalize type từ API (number) → string cho form
const REVERSE_TYPE_MAP = { 1: "FREE", 2: "PREMIUM", 3: "PRO" };


const SubscriptionPlansPage = () => {
  const { plans, isLoading, mutate } = useSubscriptionPlans();
  const { createPlan, updatePlan, isCreating, isUpdating } =
    useSubscriptionMutation();

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editMode, setEditMode] = useState(false);

  // Switch confirmation state
  const [switchConfirmOpen, setSwitchConfirmOpen] = useState(false);
  const [selectedSwitchPlan, setSelectedSwitchPlan] = useState(null);

  const [formData, setFormData] = useState({
    id: null,
    name: "",
    code: "",
    price: "",
    duration: "Tháng",
    description: "",
    isActive: true,
    isRecommended: false,
    maxAiRequestPerDay: 10,
    type: "FREE",
    displayOrder: 0,
    color: "gray",
  });

  const handleFieldChange = useCallback((field, value) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
  }, []);

  const formatCurrency = useCallback((value) => {
    if (value === 0) return "Miễn phí";
    return new Intl.NumberFormat("vi-VN", {
      style: "currency",
      currency: "VND",
      maximumFractionDigits: 0,
    }).format(value);
  }, []);

  const handleEditPlan = useCallback((plan) => {
    setFormData({
      id: plan.id,
      name: plan.name,
      code: plan.code || "",
      price: plan.price.toString(),
      duration: plan.durationDays === 365 ? "Năm" : "Tháng",
      description: plan.description,
      isActive: plan.isActive,
      isRecommended: plan.isRecommended || false,
      maxAiRequestPerDay: plan.maxAiRequestPerDay || 0,
      type: REVERSE_TYPE_MAP[plan.type] || plan.type || "FREE",
      displayOrder: plan.displayOrder || 0,
      color: plan.color || "gray",
    });
    setEditMode(true);
    setDialogOpen(true);
  }, []);

  const handleAddPlan = useCallback(() => {
    const activePlansCount = plans?.filter(p => p.isActive).length || 0;
    setFormData({
      id: null,
      name: "",
      code: "",
      price: "",
      duration: "Tháng",
      description: "",
      isActive: activePlansCount < 2,
      isRecommended: false,
      maxAiRequestPerDay: 10,
      type: "FREE",
      displayOrder: 0,
      color: "gray",
    });
    setEditMode(false);
    setDialogOpen(true);
  }, [plans]);

  const handleSavePlan = useCallback(async () => {
    try {
      const payload = {
        id: editMode ? formData.id : undefined,
        name: formData.name,
        code: formData.code || null,
        description: formData.description,
        price: Number(formData.price) || 0,
        durationDays: DURATION_DAYS_MAP[formData.duration] || 30,
        maxAiRequestPerDay: Number(formData.maxAiRequestPerDay) || 0,
        isActive: formData.isActive,
        isRecommended: formData.isRecommended,
        type: TYPE_MAP[formData.type] || 1,
        displayOrder: Number(formData.displayOrder) || 0,
      };

      if (editMode) {
        await updatePlan({ id: formData.id, data: payload });
        toast.success("Cập nhật gói thành công");
      } else {
        await createPlan(payload);
        toast.success("Thêm gói thành công");
      }
      mutate();
      setDialogOpen(false);
    } catch (err) {
      toast.error("Có lỗi xảy ra khi lưu gói");
    }
  }, [formData, editMode, updatePlan, createPlan, mutate]);

  const confirmToggleStatus = useCallback((plan) => {
    if (!plan.isActive) {
      const activePlansCount = plans?.filter(p => p.isActive).length || 0;
      if (activePlansCount >= 2) {
        toast.error("Chỉ cho phép tối đa 2 gói được Active cùng lúc!");
        return;
      }
    }
    setSelectedSwitchPlan(plan);
    setSwitchConfirmOpen(true);
  }, [plans]);

  const handleExecuteToggle = useCallback(async () => {
    if (!selectedSwitchPlan) return;
    try {
      const typeMap = { FREE: 1, PREMIUM: 2, PRO: 3 };
      const payload = {
        id: selectedSwitchPlan.id,
        name: selectedSwitchPlan.name,
        code: selectedSwitchPlan.code,
        description: selectedSwitchPlan.description,
        price: selectedSwitchPlan.price,
        durationDays: selectedSwitchPlan.durationDays,
        maxAiRequestPerDay: selectedSwitchPlan.maxAiRequestPerDay,
        isActive: !selectedSwitchPlan.isActive,
        isRecommended: selectedSwitchPlan.isRecommended,
        type: typeMap[selectedSwitchPlan.type] || 1,
        displayOrder: selectedSwitchPlan.displayOrder,
      };
      await updatePlan({ id: selectedSwitchPlan.id, data: payload });
      toast.success(`Đã ${!selectedSwitchPlan.isActive ? "kích hoạt" : "ẩn"} gói ${selectedSwitchPlan.name}`);
      mutate();
      setSwitchConfirmOpen(false);
    } catch (error) {
      toast.error("Thay đổi trạng thái thất bại");
    }
  }, [selectedSwitchPlan, updatePlan, mutate]);

  const getTypeIcon = (type) => {
    switch (type) {
      case "PRO":
         return <Crown className="h-6 w-6 text-yellow-500" />;
      case "PREMIUM":
         return <Star className="h-6 w-6 text-emerald-500" />;
      default:
         return <Zap className="h-6 w-6 text-zinc-400" />;
    }
  };

  const sortedPlans = useMemo(() => {
    return plans ? [...plans].sort((a,b) => (a.displayOrder || 0) - (b.displayOrder || 0)) : [];
  }, [plans]);

  return (
    <div className="cms-animate-page space-y-8">
      <div className="flex flex-col sm:flex-row items-baseline justify-between gap-4 border-b border-zinc-200 pb-6">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-zinc-900">
            Gói Đăng ký (Subscriptions)
          </h1>
          <p className="text-sm text-zinc-500 mt-1.5 leading-relaxed">
            Cấu hình quyền lợi, giá cả và giới hạn API cho từng gói
          </p>
        </div>
        <Button onClick={handleAddPlan} className="cms-btn-interactive bg-emerald-600 hover:bg-emerald-700 text-white">
          <Plus className="h-4 w-4 mr-2" />
          Tạo gói mới
        </Button>
      </div>

      <div className="cms-animate-stagger flex flex-wrap items-stretch justify-center gap-8 pt-4">
        {isLoading ? (
           <p className="text-zinc-500">Đang tải gói dịch vụ...</p>
        ) : sortedPlans.length === 0 ? (
           <p className="text-zinc-500">Chưa có gói cước nào.</p>
        ) : (
           sortedPlans.map((plan) => {
              const isFeatured = plan.type === 'PREMIUM' || plan.isRecommended;
              
              return (
                 <Card 
                   key={plan.id}
                   className={cn(
                      "relative flex flex-col w-full sm:w-[380px] transition-all duration-300",
                      isFeatured 
                        ? "lg:scale-105 border-emerald-400 shadow-[var(--shadow-cms-card-hover)] z-10 bg-white" 
                        : "border-zinc-200 shadow-sm bg-zinc-50/50 hover:bg-white",
                      !plan.isActive && "opacity-60 grayscale-[0.5]"
                   )}
                 >
                   {isFeatured && (
                      <div className="absolute top-0 inset-x-0 -translate-y-1/2 flex justify-center">
                         <span className="bg-emerald-500 text-white text-[10px] font-bold uppercase tracking-widest px-3 py-1 rounded-full shadow-sm">
                            Được đề xuất
                         </span>
                      </div>
                   )}
                   
                   <CardHeader className="text-center pb-2">
                      <div className="mx-auto mb-4 w-12 h-12 bg-white rounded-2xl border border-zinc-100 flex items-center justify-center shadow-sm">
                         {getTypeIcon(plan.type)}
                      </div>
                      <CardTitle className="text-xl font-semibold text-zinc-900">{plan.name}</CardTitle>
                      <CardDescription className="text-sm text-zinc-500 mt-2 min-h-[40px] px-4">
                         {plan.description || "Chưa có mô tả"}
                      </CardDescription>
                   </CardHeader>

                   <CardContent className="text-center flex-1">
                      <div className="my-6">
                         <span className="text-4xl font-bold tracking-tight text-zinc-900 font-[family-name:var(--font-cms-mono)] tabular-nums">
                            {formatCurrency(plan.price)}
                         </span>
                         <span className="text-sm text-zinc-500 font-medium ml-1">
                            / {plan.durationDays === 365 ? "Năm" : "Tháng"}
                         </span>
                      </div>

                      <div className="space-y-3 ms-2 text-left mt-6 border-t border-zinc-100 pt-6">
                         <div className="flex items-center gap-3">
                           <Check className="h-5 w-5 text-emerald-500 shrink-0" />
                           <span className="text-sm text-zinc-700">Tối đa <strong className="font-[family-name:var(--font-cms-mono)] text-emerald-600">{plan.maxAiRequestPerDay}</strong> Yêu cầu AI / ngày</span>
                         </div>
                         <div className="flex items-center gap-3">
                           <Check className="h-5 w-5 text-emerald-500 shrink-0" />
                           <span className="text-sm text-zinc-700">Quyền truy cập toàn hệ thống</span>
                         </div>
                         {plan.type === 'PRO' && (
                           <div className="flex items-center gap-3">
                             <Check className="h-5 w-5 text-emerald-500 shrink-0" />
                             <span className="text-sm text-zinc-700">Hỗ trợ khách hàng VIP 24/7</span>
                           </div>
                         )}
                      </div>
                   </CardContent>

                   <CardFooter className="flex items-center justify-between bg-zinc-50/80 border-t border-zinc-100 p-4 rounded-b-xl gap-2">
                       <div className="flex items-center gap-2">
                         <Switch 
                           checked={plan.isActive} 
                           onCheckedChange={() => confirmToggleStatus(plan)}
                           themeColor="emerald"
                         />
                         <span className="text-xs font-medium uppercase tracking-wider text-zinc-500">{plan.isActive ? 'Active' : 'Hidden'}</span>
                       </div>
                       
                       <div className="flex items-center gap-1">
                          <Button variant="ghost" size="sm" onClick={() => handleEditPlan(plan)} className="h-8 w-8 p-0 text-zinc-500 hover:text-emerald-600">
                             <Edit className="h-4 w-4" />
                          </Button>
                       </div>
                   </CardFooter>
                 </Card>
              );
           })
        )}
      </div>

      {/* Plan Form Dialog */}
      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="sm:max-w-[600px]">
          {dialogOpen ? (
            <>
              <DialogHeader>
                <DialogTitle>{editMode ? "Sửa Gói Cước" : "Tạo Gói Cước Mới"}</DialogTitle>
                <DialogDescription>
                  {editMode ? "Chỉnh sửa cấu hình và giá trị của gói." : "Thiết lập thuộc tính cho hạng đăng ký mới."}
                </DialogDescription>
              </DialogHeader>
              <div className="grid grid-cols-2 gap-x-6 gap-y-4 py-4">
                <div className="col-span-2 space-y-1.5">
                  <label className="text-sm font-medium text-zinc-900">Tên gói <span className="text-red-500">*</span></label>
                  <Input
                    placeholder="VD: Premium Plus"
                    value={formData.name}
                    onChange={(e) => handleFieldChange("name", e.target.value)}
                  />
                </div>
                
                <div className="space-y-1.5">
                  <label className="text-sm font-medium text-zinc-900">Loại (Type)</label>
                  <Select
                    value={formData.type}
                    onValueChange={(value) => handleFieldChange("type", value)}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Chọn loại" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="FREE">FREE</SelectItem>
                      <SelectItem value="PREMIUM">PREMIUM</SelectItem>
                      <SelectItem value="PRO">PRO</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-1.5">
                  <label className="text-sm font-medium text-zinc-900">Giá trị (VND) <span className="text-red-500">*</span></label>
                  <Input
                    type="number"
                    min="0"
                    step="1000"
                    placeholder="VD: 59000"
                    value={formData.price}
                    onChange={(e) => handleFieldChange("price", e.target.value)}
                  />
                </div>
                
                <div className="col-span-2 space-y-1.5">
                  <label className="text-sm font-medium text-zinc-900">Mô tả gói</label>
                  <Input
                    placeholder="Hiển thị trên trang đăng ký..."
                    value={formData.description}
                    onChange={(e) => handleFieldChange("description", e.target.value)}
                  />
                </div>

                <div className="space-y-1.5">
                  <label className="text-sm font-medium text-zinc-900">Giới hạn AI / ngày</label>
                  <Input
                    type="number"
                    min="0"
                    placeholder="VD: 10"
                    value={formData.maxAiRequestPerDay}
                    onChange={(e) => handleFieldChange("maxAiRequestPerDay", e.target.value)}
                  />
                </div>
                
                <div className="space-y-1.5">
                  <label className="text-sm font-medium text-zinc-900">Thời hạn</label>
                  <Select
                    value={formData.duration}
                    onValueChange={(value) => handleFieldChange("duration", value)}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Chọn thời hạn" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="Tháng">Hàng tháng</SelectItem>
                      <SelectItem value="Năm">Hàng năm</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <div className="col-span-2 flex items-center justify-between p-4 bg-zinc-50 border border-zinc-100 rounded-xl mt-2">
                  <div>
                     <h4 className="text-sm font-medium text-zinc-900">Gói được đề xuất</h4>
                     <p className="text-xs text-zinc-500">Hiển thị nổi bật gói này trên trang người dùng</p>
                  </div>
                  <Switch 
                    checked={formData.isRecommended}
                    onCheckedChange={(val) => handleFieldChange("isRecommended", val)}
                    themeColor="emerald"
                  />
                </div>
              </div>
              <DialogFooter>
                <Button variant="outline" onClick={() => setDialogOpen(false)} className="cms-btn-interactive">
                  Đóng
                </Button>
                <Button onClick={handleSavePlan} disabled={isCreating || isUpdating} className="cms-btn-interactive bg-emerald-600 hover:bg-emerald-700 text-white">
                  {isCreating || isUpdating ? "Đang lưu..." : "Lưu gói cước"}
                </Button>
              </DialogFooter>
            </>
          ) : null}
        </DialogContent>
      </Dialog>

      {/* Toggle Status Confirm Dialog */}
      <Dialog open={switchConfirmOpen} onOpenChange={setSwitchConfirmOpen}>
         <DialogContent className="sm:max-w-[400px]">
           {switchConfirmOpen ? (
             <>
                <DialogHeader>
                   <DialogTitle className="flex items-center gap-2">
                     {selectedSwitchPlan?.isActive ? <Archive className="h-5 w-5 text-amber-500" /> : <Activity className="h-5 w-5 text-emerald-500" />}
                     Xác nhận thay đổi
                   </DialogTitle>
                   <DialogDescription>
                     Bạn có chắc muốn {selectedSwitchPlan?.isActive ? <strong>Tắt (Ẩn)</strong> : <strong>Bật (Kích hoạt)</strong>} gói <strong>{selectedSwitchPlan?.name}</strong> không? Các đăng ký tự động của người dùng sẽ {selectedSwitchPlan?.isActive ? "bị ảnh hưởng." : "có thể được tiếp tục."}
                   </DialogDescription>
                </DialogHeader>
                <DialogFooter className="mt-4">
                   <Button variant="outline" onClick={() => setSwitchConfirmOpen(false)} className="cms-btn-interactive">Hủy bỏ</Button>
                   <Button 
                      onClick={handleExecuteToggle} 
                      disabled={isUpdating}
                      className={cn("cms-btn-interactive", selectedSwitchPlan?.isActive ? "bg-amber-600 hover:bg-amber-700 text-white" : "bg-emerald-600 hover:bg-emerald-700 text-white")}
                   >
                      {isUpdating ? "Đang xử lý..." : "Xác nhận"}
                   </Button>
                </DialogFooter>
             </>
           ) : null}
         </DialogContent>
      </Dialog>

    </div>
  );
};

export default SubscriptionPlansPage;
