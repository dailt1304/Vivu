import React, { useState, useEffect } from "react";
import { Layers, Save, ImagePlus, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useCreateCategory, useUpdateCategory } from "@/hooks/locations/useCategoryManagement";
import toast from "@/utils/toast";
import { useLocation } from "react-router-dom";

// Mapping with backend Enum
const CATEGORY_TYPES = [
  { value: "0", label: "Điểm tham quan (Attraction)" },
  { value: "1", label: "Ăn uống (Food & Beverage)" },
  { value: "2", label: "Lưu trú (Accommodation)" },
  { value: "3", label: "Giải trí (Entertainment)" },
  { value: "4", label: "Khác (Other)" },
  { value: "5", label: "Thiên nhiên (Nature)" }
];

const CategoryFormSheet = ({ open, onOpenChange, category, onMutate }) => {
  const isEditing = !!category;
  const { trigger: createCategory, isMutating: isCreating } = useCreateCategory();
  const { trigger: updateCategory, isMutating: isUpdating } = useUpdateCategory();

  // Dynamic Theme (Moderator)
  const loc = useLocation();
  const isAdminTheme = ['/cms/dashboard', '/cms/users', '/cms/subscriptions'].some(p => loc.pathname.includes(p));
  const focusRing = isAdminTheme ? "focus:border-emerald-400 focus:ring-emerald-400/20" : "focus:border-blue-400 focus:ring-blue-400/20";
  const iconColor = isAdminTheme ? "text-emerald-400" : "text-blue-400";
  const btnColor = isAdminTheme ? "bg-emerald-400 hover:bg-emerald-500 text-white font-medium shadow-md shadow-emerald-400/20" : "bg-blue-400 hover:bg-blue-500 text-white font-medium shadow-md shadow-blue-400/20";
  const boxInfoBg = isAdminTheme ? "bg-emerald-50 border-emerald-100 text-emerald-800" : "bg-blue-50 border-blue-100 text-blue-800";
  const boxInfoIconBg = isAdminTheme ? "bg-emerald-200 text-emerald-700" : "bg-blue-200 text-blue-700";

  // Form data
  const [formData, setFormData] = useState({
    name: "",
    iconUrl: "",
    categoryType: "4" // Default to Other
  });

  useEffect(() => {
    if (open) {
      if (category) {
        setFormData({
          name: category.name || "",
          iconUrl: category.iconUrl || "",
          categoryType: category.categoryType?.toString() || "4"
        });
      } else {
        setFormData({
          name: "",
          iconUrl: "",
          categoryType: "4"
        });
      }
    }
  }, [open, category]);

  const loading = isCreating || isUpdating;

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.name.trim()) {
      toast.error("Vui lòng nhập tên danh mục!");
      return;
    }

    const payload = {
      name: formData.name.trim(),
      iconUrl: formData.iconUrl.trim() || null,
      categoryType: parseInt(formData.categoryType)
    };

    try {
      if (isEditing) {
        await updateCategory({ id: category.id, data: payload });
        toast.success("Cập nhật danh mục thành công");
      } else {
        await createCategory(payload);
        toast.success("Đã thêm danh mục mới");
      }
      onMutate();
      onOpenChange(false);
    } catch (error) {
      console.error(error);
      toast.error(error?.response?.data?.message || "Có lỗi xảy ra khi lưu dữ liệu!");
    }
  };

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-full sm:max-w-md bg-white border-zinc-200 shadow-2xl p-0 flex flex-col">
        <SheetHeader className="p-6 border-b border-zinc-100 bg-zinc-50/50">
          <SheetTitle className="text-xl font-semibold flex items-center gap-2 text-zinc-900">
            <Layers className={`h-5 w-5 ${iconColor}`} />
            {isEditing ? "Chỉnh sửa Danh mục" : "Thêm Danh mục mới"}
          </SheetTitle>
        </SheetHeader>

        <form onSubmit={handleSubmit} className="flex-1 overflow-y-auto p-6 space-y-5 hide-scrollbar flex flex-col">
          <div className="space-y-5 flex-1">

            {/* Name field */}
            <div className="space-y-2">
              <label htmlFor="name" className="block text-sm font-medium text-zinc-700">
                Tên Danh mục <span className="text-red-500">*</span>
              </label>
              <Input
                id="name"
                placeholder="VD: Quán ăn, Chùa, Bãi biển..."
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                className={`bg-white border-zinc-200 rounded-lg h-11 ${focusRing}`}
              />
            </div>

            {/* Category Type */}
            <div className="space-y-2">
              <label className="block text-sm font-medium text-zinc-700">
                Loại Danh mục (Phân loại chính)
              </label>
              <Select 
                 value={formData.categoryType} 
                 onValueChange={(val) => setFormData({ ...formData, categoryType: val })}
              >
                <SelectTrigger className={`bg-white h-11 border-zinc-200 rounded-lg ${focusRing}`}>
                  <SelectValue placeholder="Chọn loại phân loại" />
                </SelectTrigger>
                <SelectContent>
                  {CATEGORY_TYPES.map(type => (
                     <SelectItem key={type.value} value={type.value}>
                        {type.label}
                     </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

             {/* Icon URL / Emoji */}
            <div className="space-y-2">
              <label htmlFor="iconUrl" className="block text-sm font-medium text-zinc-700">
                 <ImagePlus className="inline h-3.5 w-3.5 mr-1 text-zinc-400" />
                 Icon (Emoji / Text hoặc Image URL)
              </label>
              <Input
                id="iconUrl"
                placeholder="VD: 🍔, 🏞️ hoặc https://example.com/icon.png"
                value={formData.iconUrl}
                onChange={(e) => setFormData({ ...formData, iconUrl: e.target.value })}
                className={`bg-white border-zinc-200 rounded-lg h-11 ${focusRing}`}
              />
            </div>

            {/* Icon Preview */}
            {formData.iconUrl && (
              <div className="flex justify-center items-center p-4 bg-zinc-50 border border-zinc-200 rounded-lg h-24">
                 {formData.iconUrl.startsWith('http') || formData.iconUrl.startsWith('/') ? (
                   <img 
                     src={formData.iconUrl} 
                     alt="Icon Preview" 
                     className="w-12 h-12 object-contain"
                     onError={(e) => { e.target.src = "https://via.placeholder.com/48?text=Error" }}
                   />
                 ) : (
                   <span className="text-4xl leading-none">{formData.iconUrl}</span>
                 )}
              </div>
            )}

            <div className={`p-3 rounded-lg border flex gap-3 text-sm mt-4 ${boxInfoBg}`}>
                <div className={`w-5 h-5 shrink-0 rounded-full flex items-center justify-center font-bold text-xs mt-0.5 ${boxInfoIconBg}`}>i</div>
                <p>
                  Việc sửa đổi Danh mục chỉ ảnh hưởng đến tên và Icon. Không làm thay đổi thiết lập của các địa điểm đang sử dụng danh mục này.
                </p>
            </div>

          </div>

          <div className="pt-5 border-t border-zinc-100 shrink-0">
            <Button
              type="submit"
              disabled={loading}
              className={`w-full h-11 text-white rounded-lg shadow-sm ${btnColor}`}
            >
              {loading ? (
                <>
                  <span className="h-4 w-4 mr-2 border-2 border-white/20 border-t-white rounded-full animate-spin"></span>
                  Đang lưu...
                </>
              ) : (
                <>
                  <Save className="h-4 w-4 mr-2" />
                  {isEditing ? "Lưu thay đổi" : "Tạo Danh mục"}
                </>
              )}
            </Button>
          </div>
        </form>
      </SheetContent>
    </Sheet>
  );
};

export default CategoryFormSheet;
