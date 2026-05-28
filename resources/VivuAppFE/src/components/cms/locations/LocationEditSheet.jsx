import React, { useState, useEffect, useRef } from "react";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
} from "@/components/ui/sheet";
import { Button } from "@/components/ui/button";
import {
  Trash2,
  Save,
  X,
  Plus,
  ImagePlus,
  Clock,
  ChevronDown,
  ArrowRight,
  MapPin,
  Crown,
  Search,
  Check,
  ImageOff,
} from "lucide-react";
import { useLocation } from "react-router-dom";
import { motion, AnimatePresence } from "framer-motion";
import toast from "@/utils/toast";
import {
  useUpdateLocation,
  useDeleteLocation,
  useLocationCategories,
} from "@/hooks/locations";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Switch } from "@/components/ui/switch";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandItem,
  CommandList,
} from "@/components/ui/command";
import useDebounce from "@/hooks/utils/useDebounce";
import { useGoongAutocomplete } from "@/hooks/maps/useGoongAutocomplete";
import goongApi from "@/api/goongApi";
import WeeklyScheduleInput from "@/components/common/inputs/WeeklyScheduleInput";

const MAX_IMAGES = 10;


const LocationEditSheet = ({ open, onOpenChange, location, onMutate }) => {
  const [formData, setFormData] = useState({
    name: "",
    description: "",
    address: "",
    latitude: "",
    longitude: "",
    categoryId: "none",
    openingHours: "",
    phone: "",
    website: "",
    tags: "",
    images: "",
    isVerified: false,
  });

  const routerLocation = useLocation();
  const isAdminTheme = ['/cms/dashboard', '/cms/users', '/cms/subscriptions'].some(p => routerLocation.pathname.includes(p));
  const focusRing = isAdminTheme 
    ? "focus:ring-emerald-500/20 focus:border-emerald-500 focus-within:ring-emerald-500/20 focus-within:border-emerald-500" 
    : "focus:ring-blue-500/20 focus:border-blue-500 focus-within:ring-blue-500/20 focus-within:border-blue-500";
  const focusText = isAdminTheme 
    ? "focus-within:text-emerald-600 focus:text-emerald-600" 
    : "focus-within:text-blue-600 focus:text-blue-600";

  const imageRingClass = isAdminTheme 
    ? "border-emerald-500 ring-2 ring-emerald-500/20" 
    : "border-blue-500 ring-2 ring-blue-500/20";
  const badgeBgClass = isAdminTheme ? "bg-emerald-500" : "bg-blue-500";
  const uploadBoxClass = isAdminTheme ? "hover:border-emerald-500 hover:bg-emerald-50/50" : "hover:border-blue-500 hover:bg-blue-50/50";
  const iconColorClass = isAdminTheme ? "text-emerald-500" : "text-blue-500";
  const btnColorClass = isAdminTheme ? "bg-emerald-400 hover:bg-emerald-500 text-white shadow-md shadow-emerald-400/20" : "bg-blue-400 hover:bg-blue-500 text-white shadow-md shadow-blue-400/20";

  const [newImageUrl, setNewImageUrl] = useState("");
  const fileInputRef = useRef(null);

  // Address Autocomplete
  const [failedImages, setFailedImages] = useState({});
  const [addressSearch, setAddressSearch] = useState("");
  const [addressOpen, setAddressOpen] = useState(false);
  const [sessionToken] = useState(() => crypto.randomUUID());

  const { data: autocompleteData, isLoading: isSearchingAddress } =
    useGoongAutocomplete(addressSearch, sessionToken);



  const { data: categoriesPayload } = useLocationCategories();
  const categories = categoriesPayload?.items || [];

  const { trigger: updateLocation, isMutating: isUpdating } =
    useUpdateLocation();
  const { trigger: deleteLocation, isMutating: isDeleting } =
    useDeleteLocation();

  useEffect(() => {
    if (location && open) {
      const detail = location.locationDetail || {};
      setFormData({
        name: location.name || "",
        description: location.description || "",
        address: location.address || "",
        latitude: location.latitude || "",
        longitude: location.longitude || "",
        categoryId: location.categoryId
          ? location.categoryId.toString()
          : "none",
        openingHours: detail.openingHours || "",
        phone: detail.phone || "",
        website: detail.website || "",
        tags: detail.tags || "",
        images: detail.images || location.images || "",
        isVerified: location.isVerified || false,
      });

      setAddressSearch(location.address || "");


    }
  }, [location, open]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));
  };

  const handleSelectChange = (name, value) => {
    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));
  };

  const handleSwitchChange = (checked) => {
    setFormData((prev) => ({
      ...prev,
      isVerified: checked,
    }));
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
    } catch (err) {
      toast.error("Không thể lấy tọa độ của địa chỉ này.");
    }
  };

  const handleSubmit = async () => {
    if (!formData.name || !formData.address) {
      toast.error("Vui lòng nhập tên và địa chỉ địa điểm");
      return;
    }

    try {

      // Format images back to expected DB object structure
      const formattedImages = imageList.map((url, i) => ({
        url,
        order: i,
        isPrimary: i === 0,
      }));

      const updateData = {
        ...formData,
        categoryId:
          formData.categoryId && formData.categoryId !== "none"
            ? formData.categoryId
            : null,
        latitude: formData.latitude ? Number(formData.latitude) : null,
        longitude: formData.longitude ? Number(formData.longitude) : null,
        openingHours: formData.openingHours,
        images: JSON.stringify(formattedImages),
      };

      await updateLocation({ id: location.id, data: updateData });
      toast.success("Cập nhật địa điểm thành công");
      onMutate?.();
      onOpenChange(false);
    } catch (e) {
      console.error(e);
      toast.error("Lỗi khi cập nhật địa điểm");
    }
  };

  const handleDelete = async () => {
    if (
      !confirm(
        "Bạn có chắc chắn muốn xóa địa điểm này không? Hành động này không thể hoàn tác.",
      )
    ) {
      return;
    }

    try {
      await deleteLocation(location.id);
      toast.success("Đã xóa địa điểm");
      onMutate?.();
      onOpenChange(false);
    } catch (e) {
      console.error(e);
      toast.error("Lỗi khi xóa địa điểm");
    }
  };

  const TimeInput = ({ value, onChange, placeholder = "--:--" }) => {
    const [isOpen, setIsOpen] = useState(false);
    const dropdownRef = useRef(null);
    const [hour, minute] = value ? value.split(":") : ["", ""];

    useEffect(() => {
      const handleClickOutside = (event) => {
        if (
          dropdownRef.current &&
          !dropdownRef.current.contains(event.target)
        ) {
          setIsOpen(false);
        }
      };
      document.addEventListener("mousedown", handleClickOutside);
      return () =>
        document.removeEventListener("mousedown", handleClickOutside);
    }, []);

    const hours = Array.from({ length: 24 }, (_, i) =>
      i.toString().padStart(2, "0"),
    );
    const minutes = Array.from({ length: 12 }, (_, i) =>
      (i * 5).toString().padStart(2, "0"),
    );

    const handleSelect = (newHour, newMinute) => {
      const h = newHour || hour || "00";
      const m = newMinute || minute || "00";
      onChange(`${h}:${m}`);
    };

    return (
      <div className="relative w-full" ref={dropdownRef}>
        <div
          onClick={() => setIsOpen(!isOpen)}
          className={`w-full bg-white border border-gray-200 rounded-lg px-3 py-2 text-sm font-semibold text-gray-900 focus-within:ring-2 transition-all cursor-pointer flex items-center justify-between hover:bg-gray-50 h-10 shadow-sm ${focusRing}`}
        >
          <span className={!value ? "text-gray-400 font-normal" : "tabular-nums"}>
            {value || placeholder}
          </span>
          <Clock size={14} className="text-gray-400" />
        </div>

        <AnimatePresence>
          {isOpen && (
            <motion.div
              initial={{ opacity: 0, scale: 0.95, y: 10 }}
              animate={{ opacity: 1, scale: 1, y: 0 }}
              exit={{ opacity: 0, scale: 0.95, y: 10 }}
              className="absolute top-full right-0 mt-1 bg-white rounded-xl shadow-xl border border-gray-100 overflow-hidden z-[100] p-2 flex gap-2 min-w-[160px]"
            >
              <div className="flex-1">
                <div className="text-[10px] font-black text-gray-400 uppercase text-center mb-2">
                  Giờ
                </div>
                <div className="grid grid-cols-1 gap-1 max-h-[150px] overflow-y-auto pr-1 hide-scrollbar scroll-smooth">
                  {hours.map((h) => (
                    <button
                      key={h}
                      onClick={(e) => {
                        e.stopPropagation();
                        handleSelect(h, minute);
                      }}
                      className={`px-2 py-1.5 text-xs font-semibold tabular-nums rounded-md transition-all ${
                        hour === h
                          ? "bg-emerald-50 text-emerald-600"
                          : "text-gray-600 hover:bg-gray-100"
                      }`}
                    >
                      {h}
                    </button>
                  ))}
                </div>
              </div>
              <div className="flex-1">
                <div className="text-[10px] font-black text-gray-400 uppercase text-center mb-2">
                  Phút
                </div>
                <div className="grid grid-cols-1 gap-1 max-h-[150px] overflow-y-auto pr-1 hide-scrollbar scroll-smooth">
                  {minutes.map((m) => (
                    <button
                      key={m}
                      onClick={(e) => {
                        e.stopPropagation();
                        handleSelect(hour, m);
                      }}
                      className={`px-2 py-1.5 text-xs font-semibold tabular-nums rounded-md transition-all ${
                        minute === m
                          ? "bg-emerald-50 text-emerald-600"
                          : "text-gray-600 hover:bg-gray-100"
                      }`}
                    >
                      {m}
                    </button>
                  ))}
                </div>
              </div>
            </motion.div>
          )}
        </AnimatePresence>
      </div>
    );
  };

  if (!location) return null;

  // Helper to safely parse images from various formats
  const parseImages = (imagesData) => {
    if (!imagesData) return [];
    if (Array.isArray(imagesData)) {
      return imagesData.map((img) => {
        if (!img) return null;
        if (typeof img === "string") return img;
        return img?.url || img?.imageUrl || img?.src || null;
      }).filter(Boolean);
    }

    const StringData = String(imagesData).trim();
    if (StringData.startsWith("[")) {
      try {
        const parsed = JSON.parse(StringData);
        if (Array.isArray(parsed)) {
          return parsed
            .map((img) => {
              if (!img) return null;
              if (typeof img === "string") return img.trim();
              return img?.url || img?.imageUrl || img?.src || null;
            })
            .filter(Boolean);
        }
      } catch (e) {
        console.error("Failed to parse images JSON:", e);
      }
    }

    return StringData.split(",")
      .map((url) => url.trim().replace(/^['"]|['"]$/g, ""))
      .filter(Boolean);
  };

  const imageList = parseImages(formData.images);

  const handleRemoveImage = (indexToRemove) => {
    const updated = imageList.filter((_, i) => i !== indexToRemove);
    // Keep internal representation as array of strings for simplicity during editing
    setFormData((prev) => ({ ...prev, images: JSON.stringify(updated) }));
  };

  const handleSetCoverImage = (indexToCover) => {
    if (indexToCover === 0) return;
    const updated = [...imageList];
    const temp = updated[0];
    updated[0] = updated[indexToCover];
    updated[indexToCover] = temp;
    setFormData((prev) => ({ ...prev, images: JSON.stringify(updated) }));
    toast.success("Đã đổi ảnh bìa");
  };

  const handleAddImageUrl = () => {
    const url = newImageUrl.trim();
    if (!url) return;
    if (imageList.length >= MAX_IMAGES) {
      toast.error(`Tối đa ${MAX_IMAGES} ảnh`);
      return;
    }
    const updated = [...imageList, url];
    setFormData((prev) => ({ ...prev, images: JSON.stringify(updated) }));
    setNewImageUrl("");
  };

  const handleFileUpload = (e) => {
    const files = Array.from(e.target.files);
    if (!files.length) return;

    const remaining = MAX_IMAGES - imageList.length;
    const filesToAdd = files.slice(0, remaining);
    const newUrls = filesToAdd.map((file) => URL.createObjectURL(file));
    const updated = [...imageList, ...newUrls];
    setFormData((prev) => ({ ...prev, images: JSON.stringify(updated) }));

    if (fileInputRef.current) fileInputRef.current.value = "";
    if (files.length > remaining) {
      toast.error(`Chỉ thêm được ${remaining} ảnh nữa`);
    }
  };

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="sm:max-w-md md:max-w-lg lg:max-w-2xl p-0 flex flex-col h-full">
        {/* Header */}
        <div className="px-6 pt-6 pb-4 border-b bg-white shrink-0">
          <SheetHeader>
            <SheetTitle>Chỉnh sửa Địa Điểm</SheetTitle>
            <SheetDescription>ID: {location.id}</SheetDescription>
          </SheetHeader>
        </div>

        {/* Scrollable content */}
        <div className="flex-1 overflow-y-auto px-6 py-6 hide-scrollbar">
          <div className="space-y-8">
            {/* 1. Thông tin cơ bản */}
            <div className="space-y-4">
              <h3 className="text-lg font-semibold border-b pb-2">
                1. Thông tin cơ bản
              </h3>

              <div className="space-y-4">
                <div>
                  <label className={`text-sm font-semibold text-gray-700 block mb-1.5 transition-colors ${focusText}`}>
                    Tên địa điểm <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="text"
                    name="name"
                    className={`w-full text-sm p-3 bg-gray-50 border border-gray-200 rounded-xl focus:bg-white focus:ring-2 outline-none transition-all ${focusRing}`}
                    value={formData.name}
                    onChange={handleChange}
                  />
                </div>

                <div>
                  <label className="text-sm font-medium text-gray-700 block mb-1">
                    Mô tả
                  </label>
                  <textarea
                    name="description"
                    className={`w-full text-sm p-3 border border-gray-200 rounded-xl min-h-[100px] focus:ring-2 outline-none hide-scrollbar transition-all leading-relaxed ${focusRing}`}
                    value={formData.description}
                    onChange={handleChange}
                  />
                </div>

                <div>
                  <label className={`text-sm font-semibold text-gray-700 block mb-1.5 transition-colors ${focusText}`}>
                    Địa chỉ <span className="text-red-500">*</span>
                  </label>
                  <Popover open={addressOpen} onOpenChange={setAddressOpen}>
                    <PopoverTrigger asChild>
                      <div className="relative group cursor-text">
                        <div className={`absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 transition-colors pointer-events-none ${isAdminTheme ? "group-focus-within:text-emerald-500" : "group-focus-within:text-blue-500"}`}>
                          <Search size={16} />
                        </div>
                        <input
                          type="text"
                          value={addressSearch}
                          onChange={(e) => {
                            setAddressSearch(e.target.value);
                            setFormData((prev) => ({
                              ...prev,
                              address: e.target.value,
                            }));
                            if (!addressOpen) setAddressOpen(true);
                          }}
                          onFocus={() => {
                            if (addressSearch.length > 0) setAddressOpen(true);
                          }}
                          placeholder="Nhập địa chỉ..."
                          className={`w-full text-sm p-3 pl-10 bg-gray-50 border border-gray-200 rounded-xl focus:bg-white focus:ring-2 outline-none transition-all ${focusRing}`}
                        />
                      </div>
                    </PopoverTrigger>
                    {autocompleteData?.predictions?.length > 0 && (
                      <PopoverContent
                        className="w-[calc(100vw-48px)] sm:w-[480px] p-0 border border-gray-100 shadow-xl rounded-xl z-[100]"
                        align="start"
                        onOpenAutoFocus={(e) => e.preventDefault()}
                      >
                        <Command>
                          <CommandList>
                            <CommandGroup heading="Gợi ý địa chỉ từ Goong Maps">
                              {autocompleteData.predictions.map((p) => (
                                <CommandItem
                                  key={p.place_id}
                                  value={p.description}
                                  onSelect={() =>
                                    handleSelectAddress(
                                      p.place_id,
                                      p.description,
                                    )
                                  }
                                  className="flex items-start gap-2 py-3 cursor-pointer hover:bg-emerald-50"
                                >
                                  <MapPin className="h-4 w-4 text-gray-400 mt-0.5 shrink-0" />
                                  <span className="text-sm text-gray-700 leading-snug">
                                    {p.description}
                                  </span>
                                </CommandItem>
                              ))}
                            </CommandGroup>
                          </CommandList>
                        </Command>
                      </PopoverContent>
                    )}
                  </Popover>
                </div>

                <div>
                  <label className="text-sm font-semibold text-gray-700 block mb-1.5">
                    Danh mục <span className="text-red-500">*</span>
                  </label>
                  <Select
                    value={formData.categoryId}
                    onValueChange={(val) =>
                      handleSelectChange("categoryId", val)
                    }
                  >
                    <SelectTrigger className={`w-full h-[46px] rounded-xl border-gray-200 bg-gray-50 focus:bg-white transition-all ${focusRing}`}>
                      <SelectValue placeholder="Chọn danh mục" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="none">
                        -- Không chọn danh mục --
                      </SelectItem>
                      {categories.map((cat) => (
                        <SelectItem key={cat.id} value={cat.id.toString()}>
                          {cat.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>
            </div>

            {/* 2. Tọa độ bản đồ */}
            <div className="space-y-4">
              <h3 className="text-lg font-semibold border-b pb-2">
                2. Tọa độ bản đồ
              </h3>
              <p className="text-xs text-gray-500">
                Tọa độ tự động điền khi chọn gợi ý địa chỉ ở phần trên. Bạn có
                thể tự chỉnh sửa tay nếu cần độ chính xác cao hơn.
              </p>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className={`text-sm font-semibold text-gray-700 block mb-1.5 transition-colors ${focusText}`}>
                    Vĩ độ (Latitude)
                  </label>
                  <input
                    type="number"
                    name="latitude"
                    className={`w-full text-sm p-3 bg-gray-50 border border-gray-200 rounded-xl focus:bg-white focus:ring-2 outline-none font-mono transition-all ${focusRing}`}
                    value={formData.latitude}
                    onChange={handleChange}
                  />
                </div>
                <div>
                  <label className={`text-sm font-semibold text-gray-700 block mb-1.5 transition-colors ${focusText}`}>
                    Kinh độ (Longitude)
                  </label>
                  <input
                    type="number"
                    name="longitude"
                    className={`w-full text-sm p-3 bg-gray-50 border border-gray-200 rounded-xl focus:bg-white focus:ring-2 outline-none font-mono transition-all ${focusRing}`}
                    value={formData.longitude}
                    onChange={handleChange}
                  />
                </div>
              </div>
            </div>

            {/* 3. Chi tiết vận hành */}
            <div className="space-y-4">
              <h3 className="text-lg font-semibold border-b pb-2">
                3. Chi tiết vận hành
              </h3>

              <div className="space-y-6">
                <div>
                  <label className="text-sm font-semibold text-gray-700 block mb-3">
                    Giờ mở cửa theo ngày
                  </label>
                  <WeeklyScheduleInput
                    value={formData.openingHours}
                    onChange={(json) => setFormData(prev => ({ ...prev, openingHours: json }))}
                    compact={false}
                    variant="cms"
                    themeColor={isAdminTheme ? "emerald" : "blue"}
                  />
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <label className={`text-sm font-semibold text-gray-700 block mb-1.5 transition-colors ${focusText}`}>
                      Số điện thoại
                    </label>
                    <input
                      type="text"
                      name="phone"
                      className={`w-full text-sm p-3 bg-gray-50 border border-gray-200 rounded-xl focus:bg-white focus:ring-2 outline-none transition-all ${focusRing}`}
                      value={formData.phone}
                      onChange={handleChange}
                    />
                  </div>
                  <div>
                    <label className={`text-sm font-semibold text-gray-700 block mb-1.5 transition-colors ${focusText}`}>
                      Website
                    </label>
                    <input
                      type="text"
                      name="website"
                      className={`w-full text-sm p-3 bg-gray-50 border border-gray-200 rounded-xl focus:bg-white focus:ring-2 outline-none transition-all ${focusRing}`}
                      value={formData.website}
                      onChange={handleChange}
                    />
                  </div>
                </div>

                <div>
                  <label className={`text-sm font-semibold text-gray-700 block mb-1.5 transition-colors ${focusText}`}>
                    Tags (Phân cách bởi dấu phẩy)
                  </label>
                  <input
                    type="text"
                    name="tags"
                    placeholder="VD: cafe, view đẹp, yên tĩnh"
                    className={`w-full text-sm p-3 bg-gray-50 border border-gray-200 rounded-xl focus:bg-white focus:ring-2 outline-none transition-all ${focusRing}`}
                    value={formData.tags}
                    onChange={handleChange}
                  />
                </div>
              </div>
            </div>

            {/* 4. Hình ảnh & Xác thực */}
            <div className="space-y-4">
              <h3 className="text-lg font-semibold border-b pb-2">
                4. Hình ảnh & Xác thực
              </h3>

              <div>
                <label className="text-sm font-semibold text-gray-700 block mb-3">
                  Hình ảnh ({imageList.length}/{MAX_IMAGES})
                </label>

                {/* Thumbnail grid */}
                <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
                  {imageList.map((url, idx) => (
                    <div
                      key={idx}
                      className={`relative aspect-square rounded-xl border ${idx === 0 ? imageRingClass : "border-gray-200"} overflow-hidden bg-gray-50 group flex-shrink-0`}
                    >
                      {failedImages[idx] ? (
                        <div className="w-full h-full flex flex-col items-center justify-center text-gray-400 bg-gray-100 p-2">
                          <ImageOff size={24} className="mb-1 opacity-50" />
                          <span
                            className="text-[10px] text-center font-medium line-clamp-2 break-all"
                            title={url}
                          >
                            Lỗi ảnh
                          </span>
                        </div>
                      ) : (
                        <img
                          src={url}
                          alt={`Ảnh ${idx + 1}`}
                          className="w-full h-full object-cover"
                          onError={() =>
                            setFailedImages((prev) => ({
                              ...prev,
                              [idx]: true,
                            }))
                          }
                        />
                      )}

                      {idx === 0 && (
                        <div className={`absolute top-2 left-2 ${badgeBgClass} text-white text-[10px] font-bold px-2 py-1 rounded shadow-sm flex items-center gap-1 z-10`}>
                          <Crown size={10} /> Ảnh Bìa
                        </div>
                      )}

                      <div className="absolute inset-0 bg-black/50 opacity-0 group-hover:opacity-100 transition-opacity flex flex-col items-center justify-center gap-2">
                        {idx !== 0 && (
                          <button
                            type="button"
                            onClick={() => handleSetCoverImage(idx)}
                            className={`text-xs font-semibold px-3 py-1.5 bg-white text-gray-900 rounded-full transition-colors shadow-lg ${isAdminTheme ? 'hover:bg-emerald-50' : 'hover:bg-blue-50'}`}
                          >
                            Chọn làm ảnh bìa
                          </button>
                        )}
                        <button
                          type="button"
                          onClick={() => handleRemoveImage(idx)}
                          className="p-2 bg-red-500 text-white rounded-full hover:bg-red-600 transition-colors shadow-lg absolute bottom-2 right-2"
                        >
                          <Trash2 size={12} />
                        </button>
                      </div>
                    </div>
                  ))}

                  {/* Upload button tile */}
                  {imageList.length < MAX_IMAGES && (
                    <button
                      type="button"
                      onClick={() => fileInputRef.current?.click()}
                      className={`aspect-square rounded-xl border-2 border-dashed border-gray-300 bg-gray-50 flex flex-col items-center justify-center gap-2 transition-all cursor-pointer group ${uploadBoxClass}`}
                    >
                      <ImagePlus
                        size={24}
                        className={`text-gray-400 transition-colors group-hover:${iconColorClass}`}
                      />
                      <span className={`text-xs font-medium text-gray-400 transition-colors group-hover:${iconColorClass}`}>
                        Tải ảnh
                      </span>
                    </button>
                  )}
                </div>

                <input
                  type="file"
                  ref={fileInputRef}
                  onChange={handleFileUpload}
                  accept="image/*"
                  multiple
                  className="hidden"
                />

                {/* Add URL input */}
                <div className="flex gap-2 mt-4">
                  <input
                    type="text"
                    placeholder="Dán URL ảnh trực tiếp..."
                    className={`flex-1 text-sm p-2.5 bg-gray-50 border border-gray-200 rounded-xl focus:bg-white focus:ring-2 outline-none transition-all ${focusRing}`}
                    value={newImageUrl}
                    onChange={(e) => setNewImageUrl(e.target.value)}
                    onKeyDown={(e) => e.key === "Enter" && handleAddImageUrl()}
                  />
                  <Button
                    type="button"
                    variant="outline"
                    className="rounded-xl border-gray-200 bg-white"
                    onClick={handleAddImageUrl}
                    disabled={
                      !newImageUrl.trim() || imageList.length >= MAX_IMAGES
                    }
                  >
                    <Plus className="h-4 w-4 mr-1" />
                    Thêm URL
                  </Button>
                </div>
              </div>

              <div className="flex items-center justify-between p-4 border rounded-xl bg-gray-50">
                <div>
                  <p className="font-semibold text-gray-900 text-sm">
                    Xác thực địa điểm
                  </p>
                  <p className="text-gray-500 text-xs mt-1">
                    Chỉ dành cho các đối tác hoặc địa danh đã được xác minh
                    chuẩn xác.
                  </p>
                </div>
                <Switch
                  checked={formData.isVerified}
                  onCheckedChange={handleSwitchChange}
                  themeColor={isAdminTheme ? "emerald" : "blue"}
                />
              </div>
            </div>

            {/* Danger zone */}
            <div className="border-t pt-6 space-y-4">
              <h4 className="font-semibold text-red-600 border-l-4 border-red-600 pl-2">
                Khu Vực Nguy Hiểm
              </h4>
              <div className="bg-red-50 p-4 rounded-xl border border-red-100 flex items-center justify-between">
                <div>
                  <p className="font-medium text-red-900 text-sm">
                    Xóa địa điểm
                  </p>
                  <p className="text-red-700 text-xs mt-1">
                    Hành động này không thể hoàn tác.
                  </p>
                </div>
                <Button
                  variant="destructive"
                  size="sm"
                  onClick={handleDelete}
                  disabled={isDeleting}
                >
                  <Trash2 className="h-4 w-4 mr-2" />
                  Xóa
                </Button>
              </div>
            </div>

            {/* Pad bottom for scroll */}
            <div className="h-4"></div>
          </div>
        </div>

        {/* Sticky bottom bar */}
        <div className="flex justify-end gap-3 px-6 py-4 border-t bg-white shrink-0">
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Hủy
          </Button>
          <Button onClick={handleSubmit} disabled={isUpdating} className={btnColorClass}>
            {isUpdating ? (
              <span className="w-4 h-4 rounded-full border-2 border-white/30 border-t-white animate-spin mr-2"></span>
            ) : (
              <Save className="h-4 w-4 mr-2" />
            )}
            Lưu thay đổi
          </Button>
        </div>
      </SheetContent>
    </Sheet>
  );
};

export default LocationEditSheet;
