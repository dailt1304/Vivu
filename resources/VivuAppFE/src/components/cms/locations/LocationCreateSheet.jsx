import React, { useState, useRef } from "react";
import {
  Sheet, SheetContent, SheetHeader, SheetTitle, SheetDescription,
} from "@/components/ui/sheet";
import { Button } from "@/components/ui/button";
import {
  Save, Plus, ImagePlus, MapPin, Search, Trash2, ImageOff, Crown,
  Check, ChevronsUpDown,
} from "lucide-react";
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from "@/components/ui/select";
import {
  Popover, PopoverContent, PopoverTrigger,
} from "@/components/ui/popover";
import {
  Command, CommandEmpty, CommandGroup, CommandItem, CommandList, CommandInput,
} from "@/components/ui/command";
import { cn } from "@/lib/utils";
import toast from "@/utils/toast";
import { useCreateLocation, useLocationCategories } from "@/hooks/locations";
import { useGoongAutocomplete } from "@/hooks/maps/useGoongAutocomplete";
import goongApi from "@/api/goongApi";
import WeeklyScheduleInput from "@/components/common/inputs/WeeklyScheduleInput";
import useSWR from "swr";
import cityApi from "@/api/cityApi";

const MAX_IMAGES = 10;

const focusRing = "focus:ring-blue-500/20 focus:border-blue-500 focus-within:ring-blue-500/20 focus-within:border-blue-500";
const focusText = "focus-within:text-blue-600 focus:text-blue-600";

const INITIAL_FORM = {
  name: "", description: "", address: "",
  latitude: "", longitude: "",
  cityId: "", categoryId: "none",
  openingHours: "", phone: "", website: "", tags: "",
};

const LocationCreateSheet = ({ open, onOpenChange, onSuccess }) => {
  const [formData, setFormData] = useState({ ...INITIAL_FORM });
  const [imageFiles, setImageFiles] = useState([]);
  const [imagePreviews, setImagePreviews] = useState([]);
  const [failedImages, setFailedImages] = useState({});
  const [newImageUrl, setNewImageUrl] = useState("");
  const [imageUrls, setImageUrls] = useState([]);
  const fileInputRef = useRef(null);

  // Address autocomplete
  const [addressSearch, setAddressSearch] = useState("");
  const [addressOpen, setAddressOpen] = useState(false);
  const [sessionToken] = useState(() => crypto.randomUUID());
  const { data: autocompleteData } = useGoongAutocomplete(addressSearch, sessionToken);

  // City selector
  const [cityOpen, setCityOpen] = useState(false);
  const { data: citiesResponse } = useSWR(
    ["all-cities-create", { pageNumber: 1, pageSize: 999 }],
    ([, params]) => cityApi.getAll(params),
    { revalidateOnFocus: false },
  );
  const cities = citiesResponse?.data?.items || [];

  // Categories
  const { data: categoriesPayload } = useLocationCategories();
  const categories = categoriesPayload?.items || [];

  // Create mutation
  const { trigger: createLocation, isMutating: isCreating } = useCreateLocation();

  const resetForm = () => {
    setFormData({ ...INITIAL_FORM });
    setImageFiles([]);
    setImagePreviews([]);
    setImageUrls([]);
    setNewImageUrl("");
    setAddressSearch("");
    setFailedImages({});
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSelectAddress = async (placeId, description) => {
    setAddressSearch(description);
    setAddressOpen(false);
    setFormData((prev) => ({ ...prev, address: description }));
    try {
      const detail = await goongApi.placeDetail(placeId, sessionToken);
      if (detail?.result?.geometry?.location) {
        setFormData((prev) => ({
          ...prev,
          latitude: detail.result.geometry.location.lat,
          longitude: detail.result.geometry.location.lng,
        }));
      }
    } catch {
      toast.error("Không thể lấy tọa độ của địa chỉ này.");
    }
  };

  const handleFileUpload = (e) => {
    const files = Array.from(e.target.files);
    if (!files.length) return;
    const totalCurrent = imagePreviews.length + imageUrls.length;
    const remaining = MAX_IMAGES - totalCurrent;
    const filesToAdd = files.slice(0, remaining);
    const previews = filesToAdd.map((f) => URL.createObjectURL(f));
    setImageFiles((prev) => [...prev, ...filesToAdd]);
    setImagePreviews((prev) => [...prev, ...previews]);
    if (fileInputRef.current) fileInputRef.current.value = "";
    if (files.length > remaining) toast.error(`Chỉ thêm được ${remaining} ảnh nữa`);
  };

  const handleAddImageUrl = () => {
    const url = newImageUrl.trim();
    if (!url) return;
    if (imagePreviews.length + imageUrls.length >= MAX_IMAGES) {
      toast.error(`Tối đa ${MAX_IMAGES} ảnh`);
      return;
    }
    setImageUrls((prev) => [...prev, url]);
    setNewImageUrl("");
  };

  const allImages = [...imagePreviews, ...imageUrls];

  const handleRemoveImage = (idx) => {
    if (idx < imagePreviews.length) {
      URL.revokeObjectURL(imagePreviews[idx]);
      setImagePreviews((prev) => prev.filter((_, i) => i !== idx));
      setImageFiles((prev) => prev.filter((_, i) => i !== idx));
    } else {
      const urlIdx = idx - imagePreviews.length;
      setImageUrls((prev) => prev.filter((_, i) => i !== urlIdx));
    }
    setFailedImages((prev) => { const n = { ...prev }; delete n[idx]; return n; });
  };

  const handleSubmit = async () => {
    if (!formData.name.trim()) { toast.error("Vui lòng nhập tên địa điểm"); return; }
    if (!formData.address.trim()) { toast.error("Vui lòng nhập địa chỉ"); return; }
    if (!formData.cityId) { toast.error("Vui lòng chọn Tỉnh/Thành phố"); return; }

    try {
      const fd = new FormData();
      fd.append("Name", formData.name.trim());
      if (formData.description) fd.append("Description", formData.description);
      fd.append("Address", formData.address.trim());
      if (formData.latitude) fd.append("Latitude", formData.latitude);
      if (formData.longitude) fd.append("Longitude", formData.longitude);
      fd.append("CityId", formData.cityId);
      if (formData.categoryId && formData.categoryId !== "none")
        fd.append("CategoryId", formData.categoryId);
      if (formData.openingHours) fd.append("OpeningHours", formData.openingHours);
      if (formData.phone) fd.append("Phone", formData.phone);
      if (formData.website) fd.append("Website", formData.website);
      if (formData.tags) fd.append("Tags", formData.tags);
      imageFiles.forEach((file) => fd.append("Images", file));

      await createLocation(fd);
      toast.success("Tạo địa điểm thành công!");
      onSuccess?.();
      onOpenChange(false);
      resetForm();
    } catch (e) {
      console.error(e);
      const msg = e?.response?.data?.error?.message || "Lỗi khi tạo địa điểm";
      toast.error(msg);
    }
  };

  return (
    <Sheet open={open} onOpenChange={(v) => { onOpenChange(v); if (!v) resetForm(); }}>
      <SheetContent className="sm:max-w-md md:max-w-lg lg:max-w-2xl p-0 flex flex-col h-full">
        {/* Header */}
        <div className="px-6 pt-6 pb-4 border-b bg-white shrink-0">
          <SheetHeader>
            <SheetTitle>Thêm Địa Điểm Mới</SheetTitle>
            <SheetDescription>Tạo địa điểm mới trực tiếp (tự động xác thực)</SheetDescription>
          </SheetHeader>
        </div>

        {/* Scrollable content */}
        <div className="flex-1 overflow-y-auto px-6 py-6 hide-scrollbar">
          <div className="space-y-8">
            {/* 1. Thông tin cơ bản */}
            <div className="space-y-4">
              <h3 className="text-lg font-semibold border-b pb-2">1. Thông tin cơ bản</h3>
              <div className="space-y-4">
                <div>
                  <label className={`text-sm font-semibold text-gray-700 block mb-1.5 transition-colors ${focusText}`}>
                    Tên địa điểm <span className="text-red-500">*</span>
                  </label>
                  <input type="text" name="name"
                    className={`w-full text-sm p-3 bg-gray-50 border border-gray-200 rounded-xl focus:bg-white focus:ring-2 outline-none transition-all ${focusRing}`}
                    value={formData.name} onChange={handleChange} placeholder="VD: Cầu Rồng Đà Nẵng" />
                </div>
                <div>
                  <label className="text-sm font-medium text-gray-700 block mb-1">Mô tả</label>
                  <textarea name="description"
                    className={`w-full text-sm p-3 border border-gray-200 rounded-xl min-h-[100px] focus:ring-2 outline-none hide-scrollbar transition-all leading-relaxed ${focusRing}`}
                    value={formData.description} onChange={handleChange} />
                </div>

                {/* Address */}
                <div>
                  <label className={`text-sm font-semibold text-gray-700 block mb-1.5 transition-colors ${focusText}`}>
                    Địa chỉ <span className="text-red-500">*</span>
                  </label>
                  <Popover open={addressOpen} onOpenChange={setAddressOpen}>
                    <PopoverTrigger asChild>
                      <div className="relative group cursor-text">
                        <div className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 transition-colors pointer-events-none group-focus-within:text-blue-500">
                          <Search size={16} />
                        </div>
                        <input type="text" value={addressSearch}
                          onChange={(e) => { setAddressSearch(e.target.value); setFormData((p) => ({ ...p, address: e.target.value })); if (!addressOpen) setAddressOpen(true); }}
                          onFocus={() => { if (addressSearch.length > 0) setAddressOpen(true); }}
                          placeholder="Nhập địa chỉ..."
                          className={`w-full text-sm p-3 pl-10 bg-gray-50 border border-gray-200 rounded-xl focus:bg-white focus:ring-2 outline-none transition-all ${focusRing}`} />
                      </div>
                    </PopoverTrigger>
                    {autocompleteData?.predictions?.length > 0 && (
                      <PopoverContent className="w-[calc(100vw-48px)] sm:w-[480px] p-0 border border-gray-100 shadow-xl rounded-xl z-[100]" align="start" onOpenAutoFocus={(e) => e.preventDefault()}>
                        <Command>
                          <CommandList>
                            <CommandGroup heading="Gợi ý địa chỉ từ Goong Maps">
                              {autocompleteData.predictions.map((p) => (
                                <CommandItem key={p.place_id} value={p.description}
                                  onSelect={() => handleSelectAddress(p.place_id, p.description)}
                                  className="flex items-start gap-2 py-3 cursor-pointer hover:bg-blue-50">
                                  <MapPin className="h-4 w-4 text-gray-400 mt-0.5 shrink-0" />
                                  <span className="text-sm text-gray-700 leading-snug">{p.description}</span>
                                </CommandItem>
                              ))}
                            </CommandGroup>
                          </CommandList>
                        </Command>
                      </PopoverContent>
                    )}
                  </Popover>
                </div>

                {/* City */}
                <div>
                  <label className={`text-sm font-semibold text-gray-700 block mb-1.5 transition-colors ${focusText}`}>
                    Tỉnh/Thành phố <span className="text-red-500">*</span>
                  </label>
                  <Popover open={cityOpen} onOpenChange={setCityOpen}>
                    <PopoverTrigger asChild>
                      <Button variant="outline" role="combobox" aria-expanded={cityOpen}
                        className={`w-full justify-between font-normal hover:bg-white bg-gray-50 hover:text-zinc-900 transition-colors h-[46px] rounded-xl border-gray-200 ${focusRing}`}>
                        <span className="truncate">
                          {formData.cityId ? cities.find((c) => c.id === formData.cityId)?.name : "Chọn Tỉnh/TP..."}
                        </span>
                        <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
                      </Button>
                    </PopoverTrigger>
                    <PopoverContent className="w-[320px] p-0" align="start">
                      <Command>
                        <CommandInput placeholder="Tìm nhanh Tỉnh/TP..." />
                        <CommandList>
                          <CommandEmpty>Không tìm thấy.</CommandEmpty>
                          <CommandGroup>
                            {cities.map((city) => (
                              <CommandItem key={city.id} value={city.name}
                                onSelect={() => { setFormData((p) => ({ ...p, cityId: city.id })); setCityOpen(false); }}>
                                <Check className={cn("mr-2 h-4 w-4", formData.cityId === city.id ? "opacity-100" : "opacity-0")} />
                                {city.name}
                              </CommandItem>
                            ))}
                          </CommandGroup>
                        </CommandList>
                      </Command>
                    </PopoverContent>
                  </Popover>
                </div>

                {/* Category */}
                <div>
                  <label className="text-sm font-semibold text-gray-700 block mb-1.5">Danh mục</label>
                  <Select value={formData.categoryId} onValueChange={(val) => setFormData((p) => ({ ...p, categoryId: val }))}>
                    <SelectTrigger className={`w-full h-[46px] rounded-xl border-gray-200 bg-gray-50 focus:bg-white transition-all ${focusRing}`}>
                      <SelectValue placeholder="Chọn danh mục" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="none">-- Không chọn --</SelectItem>
                      {categories.map((cat) => (
                        <SelectItem key={cat.id} value={cat.id.toString()}>{cat.name}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>
            </div>

            {/* 2. Tọa độ */}
            <div className="space-y-4">
              <h3 className="text-lg font-semibold border-b pb-2">2. Tọa độ bản đồ</h3>
              <p className="text-xs text-gray-500">Tọa độ tự động điền khi chọn gợi ý địa chỉ. Có thể sửa tay.</p>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className={`text-sm font-semibold text-gray-700 block mb-1.5 transition-colors ${focusText}`}>Vĩ độ</label>
                  <input type="number" name="latitude"
                    className={`w-full text-sm p-3 bg-gray-50 border border-gray-200 rounded-xl focus:bg-white focus:ring-2 outline-none font-mono transition-all ${focusRing}`}
                    value={formData.latitude} onChange={handleChange} />
                </div>
                <div>
                  <label className={`text-sm font-semibold text-gray-700 block mb-1.5 transition-colors ${focusText}`}>Kinh độ</label>
                  <input type="number" name="longitude"
                    className={`w-full text-sm p-3 bg-gray-50 border border-gray-200 rounded-xl focus:bg-white focus:ring-2 outline-none font-mono transition-all ${focusRing}`}
                    value={formData.longitude} onChange={handleChange} />
                </div>
              </div>
            </div>

            {/* 3. Chi tiết */}
            <div className="space-y-4">
              <h3 className="text-lg font-semibold border-b pb-2">3. Chi tiết vận hành</h3>
              <div className="space-y-6">
                <div>
                  <label className="text-sm font-semibold text-gray-700 block mb-3">Giờ mở cửa theo ngày</label>
                  <WeeklyScheduleInput value={formData.openingHours}
                    onChange={(json) => setFormData((p) => ({ ...p, openingHours: json }))}
                    compact={false} variant="cms" themeColor="blue" />
                </div>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <label className={`text-sm font-semibold text-gray-700 block mb-1.5 transition-colors ${focusText}`}>Số điện thoại</label>
                    <input type="text" name="phone"
                      className={`w-full text-sm p-3 bg-gray-50 border border-gray-200 rounded-xl focus:bg-white focus:ring-2 outline-none transition-all ${focusRing}`}
                      value={formData.phone} onChange={handleChange} />
                  </div>
                  <div>
                    <label className={`text-sm font-semibold text-gray-700 block mb-1.5 transition-colors ${focusText}`}>Website</label>
                    <input type="text" name="website"
                      className={`w-full text-sm p-3 bg-gray-50 border border-gray-200 rounded-xl focus:bg-white focus:ring-2 outline-none transition-all ${focusRing}`}
                      value={formData.website} onChange={handleChange} />
                  </div>
                </div>
                <div>
                  <label className={`text-sm font-semibold text-gray-700 block mb-1.5 transition-colors ${focusText}`}>Tags</label>
                  <input type="text" name="tags" placeholder="VD: cafe, view đẹp, yên tĩnh"
                    className={`w-full text-sm p-3 bg-gray-50 border border-gray-200 rounded-xl focus:bg-white focus:ring-2 outline-none transition-all ${focusRing}`}
                    value={formData.tags} onChange={handleChange} />
                </div>
              </div>
            </div>

            {/* 4. Hình ảnh */}
            <div className="space-y-4">
              <h3 className="text-lg font-semibold border-b pb-2">4. Hình ảnh</h3>
              <label className="text-sm font-semibold text-gray-700 block mb-3">
                Hình ảnh ({allImages.length}/{MAX_IMAGES})
              </label>
              <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
                {allImages.map((url, idx) => (
                  <div key={idx}
                    className={`relative aspect-square rounded-xl border ${idx === 0 ? "border-blue-500 ring-2 ring-blue-500/20" : "border-gray-200"} overflow-hidden bg-gray-50 group`}>
                    {failedImages[idx] ? (
                      <div className="w-full h-full flex flex-col items-center justify-center text-gray-400 bg-gray-100 p-2">
                        <ImageOff size={24} className="mb-1 opacity-50" />
                        <span className="text-[10px] text-center font-medium">Lỗi ảnh</span>
                      </div>
                    ) : (
                      <img src={url} alt={`Ảnh ${idx + 1}`} className="w-full h-full object-cover"
                        onError={() => setFailedImages((p) => ({ ...p, [idx]: true }))} />
                    )}
                    {idx === 0 && (
                      <div className="absolute top-2 left-2 bg-blue-500 text-white text-[10px] font-bold px-2 py-1 rounded shadow-sm flex items-center gap-1 z-10">
                        <Crown size={10} /> Ảnh Bìa
                      </div>
                    )}
                    <div className="absolute inset-0 bg-black/50 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center">
                      <button type="button" onClick={() => handleRemoveImage(idx)}
                        className="p-2 bg-red-500 text-white rounded-full hover:bg-red-600 transition-colors shadow-lg">
                        <Trash2 size={12} />
                      </button>
                    </div>
                  </div>
                ))}
                {allImages.length < MAX_IMAGES && (
                  <button type="button" onClick={() => fileInputRef.current?.click()}
                    className="aspect-square rounded-xl border-2 border-dashed border-gray-300 bg-gray-50 flex flex-col items-center justify-center gap-2 transition-all cursor-pointer group hover:border-blue-500 hover:bg-blue-50/50">
                    <ImagePlus size={24} className="text-gray-400 transition-colors group-hover:text-blue-500" />
                    <span className="text-xs font-medium text-gray-400 transition-colors group-hover:text-blue-500">Tải ảnh</span>
                  </button>
                )}
              </div>
              <input type="file" ref={fileInputRef} onChange={handleFileUpload} accept="image/*" multiple className="hidden" />
              <div className="flex gap-2 mt-4">
                <input type="text" placeholder="Dán URL ảnh trực tiếp..."
                  className={`flex-1 text-sm p-2.5 bg-gray-50 border border-gray-200 rounded-xl focus:bg-white focus:ring-2 outline-none transition-all ${focusRing}`}
                  value={newImageUrl} onChange={(e) => setNewImageUrl(e.target.value)}
                  onKeyDown={(e) => e.key === "Enter" && handleAddImageUrl()} />
                <Button type="button" variant="outline" className="rounded-xl border-gray-200 bg-white"
                  onClick={handleAddImageUrl} disabled={!newImageUrl.trim() || allImages.length >= MAX_IMAGES}>
                  <Plus className="h-4 w-4 mr-1" /> Thêm URL
                </Button>
              </div>
            </div>

            <div className="h-4" />
          </div>
        </div>

        {/* Footer */}
        <div className="flex justify-end gap-3 px-6 py-4 border-t bg-white shrink-0">
          <Button variant="outline" onClick={() => { onOpenChange(false); resetForm(); }}>Hủy</Button>
          <Button onClick={handleSubmit} disabled={isCreating}
            className="bg-blue-400 hover:bg-blue-500 text-white shadow-md shadow-blue-400/20">
            {isCreating ? (
              <span className="w-4 h-4 rounded-full border-2 border-white/30 border-t-white animate-spin mr-2" />
            ) : (
              <Save className="h-4 w-4 mr-2" />
            )}
            Tạo địa điểm
          </Button>
        </div>
      </SheetContent>
    </Sheet>
  );
};

export default LocationCreateSheet;
