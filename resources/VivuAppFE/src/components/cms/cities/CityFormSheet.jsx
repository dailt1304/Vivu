import React, { useState, useEffect, useRef, useCallback } from "react";
import { Map, Save, Search, MapPin, X, Plus, ImagePlus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { useCreateCity, useUpdateCity } from "@/hooks/cities/useCities";
import { useGoongAutocomplete } from "@/hooks/maps/useGoongAutocomplete";
import goongApi from "@/api/goongApi";
import toast from "@/utils/toast";
import { motion, AnimatePresence } from "framer-motion";
import { useLocation } from "react-router-dom";

const CityFormSheet = ({ open, onOpenChange, city, onMutate }) => {
  const isEditing = !!city;
  const { trigger: createCity, isMutating: isCreating } = useCreateCity();
  const { trigger: updateCity, isMutating: isUpdating } = useUpdateCity();

  // Dynamic Theme
  const loc = useLocation();
  const isAdminTheme = ['/cms/dashboard', '/cms/users', '/cms/subscriptions'].some(p => loc.pathname.includes(p));
  const focusRing = isAdminTheme ? "focus:border-emerald-400 focus:ring-emerald-400/20" : "focus:border-blue-400 focus:ring-blue-400/20";
  const iconColor = isAdminTheme ? "text-emerald-400" : "text-blue-400";
  const hoverBg = isAdminTheme ? "hover:bg-emerald-50" : "hover:bg-blue-50";
  const boxBg = isAdminTheme ? "bg-emerald-50 border-emerald-100" : "bg-blue-50 border-blue-100";
  const boxIconColor = isAdminTheme ? "text-emerald-500" : "text-blue-500";
  const boxTextColor = isAdminTheme ? "text-emerald-700" : "text-blue-700";
  const boxInfoBg = isAdminTheme ? "bg-emerald-50 border-emerald-100 text-emerald-800" : "bg-blue-50 border-blue-100 text-blue-800";
  const boxInfoIconBg = isAdminTheme ? "bg-emerald-200 text-emerald-700" : "bg-blue-200 text-blue-700";
  const btnColor = isAdminTheme ? "bg-emerald-400 hover:bg-emerald-500 text-white font-medium shadow-md shadow-emerald-400/20" : "bg-blue-400 hover:bg-blue-500 text-white font-medium shadow-md shadow-blue-400/20";

  // Goong Autocomplete
  const [searchInput, setSearchInput] = useState("");
  const [showSuggestions, setShowSuggestions] = useState(false);
  const [sessionToken] = useState(() => crypto.randomUUID());
  const suggestionsRef = useRef(null);

  const { data: autocompleteData } = useGoongAutocomplete(searchInput, sessionToken);
  const predictions = autocompleteData?.predictions || [];

  // Form data
  const [formData, setFormData] = useState({
    name: "",
    countryId: "0f8194b3-3ca1-46b1-b9d8-1f67fe0a9220",
    latitude: "",
    longitude: "",
  });

  // Images (array of URL strings)
  const [images, setImages] = useState([]);
  const [newImageUrl, setNewImageUrl] = useState("");

  useEffect(() => {
    if (open) {
      if (city) {
        setFormData({
          name: city.name || "",
          countryId: city.countryId || "0f8194b3-3ca1-46b1-b9d8-1f67fe0a9220",
          latitude: city.latitude ?? "",
          longitude: city.longitude ?? "",
        });
        setSearchInput(city.name || "");
        // Parse existing image — could be a JSON array or single URL
        try {
          const parsed = city.image ? JSON.parse(city.image) : [];
          setImages(Array.isArray(parsed) ? parsed : city.image ? [city.image] : []);
        } catch {
          setImages(city.image ? [city.image] : []);
        }
      } else {
        setFormData({
          name: "",
          countryId: "0f8194b3-3ca1-46b1-b9d8-1f67fe0a9220",
          latitude: "",
          longitude: "",
        });
        setSearchInput("");
        setImages([]);
      }
      setNewImageUrl("");
      setShowSuggestions(false);
    }
  }, [open, city]);

  // Close suggestions when clicking outside
  useEffect(() => {
    const handleClickOutside = (e) => {
      if (suggestionsRef.current && !suggestionsRef.current.contains(e.target)) {
        setShowSuggestions(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const handleSelectPrediction = useCallback(async (prediction) => {
    setSearchInput(prediction.description);
    setShowSuggestions(false);

    // Extract city name from structured_formatting
    const mainText = prediction.structured_formatting?.main_text || prediction.description;
    setFormData((prev) => ({ ...prev, name: mainText }));

    // Fetch Place Detail for lat/lng
    try {
      const detail = await goongApi.placeDetail(prediction.place_id, sessionToken);
      const location = detail?.result?.geometry?.location;
      if (location) {
        setFormData((prev) => ({
          ...prev,
          latitude: location.lat,
          longitude: location.lng,
        }));
      }
    } catch (error) {
      console.error("Failed to fetch place detail:", error);
    }
  }, [sessionToken]);

  const handleAddImage = () => {
    if (!newImageUrl.trim()) return;
    try {
      new URL(newImageUrl); // Validate URL
      setImages((prev) => [...prev, newImageUrl.trim()]);
      setNewImageUrl("");
    } catch {
      toast.error("URL ảnh không hợp lệ!");
    }
  };

  const handleRemoveImage = (index) => {
    setImages((prev) => prev.filter((_, i) => i !== index));
  };

  const loading = isCreating || isUpdating;

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.name.trim()) {
      toast.error("Vui lòng nhập tên khu vực!");
      return;
    }

    const payload = {
      name: formData.name,
      countryId: formData.countryId,
      latitude: formData.latitude !== "" ? Number(formData.latitude) : null,
      longitude: formData.longitude !== "" ? Number(formData.longitude) : null,
      image: images.length > 0 ? JSON.stringify(images) : null,
    };

    try {
      if (isEditing) {
        await updateCity({ id: city.id, data: payload });
        toast.success("Cập nhật thông tin thành công");
      } else {
        await createCity(payload);
        toast.success("Đã thêm khu vực mới");
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
      <SheetContent className="w-full sm:max-w-lg bg-white border-zinc-200 shadow-2xl p-0 flex flex-col">
        <SheetHeader className="p-6 border-b border-zinc-100 bg-zinc-50/50">
          <SheetTitle className="text-xl font-semibold flex items-center gap-2 text-zinc-900">
            <Map className={`h-5 w-5 ${iconColor}`} />
            {isEditing ? "Chỉnh sửa Khu vực" : "Thêm Khu vực mới"}
          </SheetTitle>
        </SheetHeader>

        <form onSubmit={handleSubmit} className="flex-1 overflow-y-auto p-6 space-y-5 hide-scrollbar flex flex-col">
          <div className="space-y-5 flex-1">

            {/* Goong Autocomplete Search */}
            <div className="space-y-2 relative" ref={suggestionsRef}>
              <label className="block text-sm font-medium text-zinc-700">
                <Search className="inline h-3.5 w-3.5 mr-1 text-zinc-400" />
                Tìm kiếm địa điểm (Goong Maps)
              </label>
              <Input
                placeholder="Nhập tên tỉnh/thành để tìm kiếm..."
                value={searchInput}
                onChange={(e) => {
                  setSearchInput(e.target.value);
                  setShowSuggestions(true);
                }}
                onFocus={() => predictions.length > 0 && setShowSuggestions(true)}
                className={`bg-white border-zinc-200 rounded-lg h-11 ${focusRing}`}
              />
              <AnimatePresence>
                {showSuggestions && predictions.length > 0 && (
                  <motion.div
                    initial={{ opacity: 0, y: -4 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, y: -4 }}
                    transition={{ duration: 0.15 }}
                    className="absolute z-50 top-full left-0 right-0 mt-1 bg-white border border-zinc-200 rounded-xl shadow-lg overflow-hidden"
                  >
                    {predictions.map((p) => (
                      <button
                        key={p.place_id}
                        type="button"
                        onClick={() => handleSelectPrediction(p)}
                        className={`w-full text-left px-4 py-3 transition-colors border-b border-zinc-50 last:border-b-0 flex items-start gap-3 ${hoverBg}`}
                      >
                        <MapPin className={`h-4 w-4 mt-0.5 shrink-0 ${iconColor}`} />
                        <div>
                          <p className="text-sm font-medium text-zinc-800">
                            {p.structured_formatting?.main_text || p.description}
                          </p>
                          <p className="text-xs text-zinc-500 mt-0.5">
                            {p.structured_formatting?.secondary_text || ""}
                          </p>
                        </div>
                      </button>
                    ))}
                  </motion.div>
                )}
              </AnimatePresence>
            </div>

            {/* Name field */}
            <div className="space-y-2">
              <label htmlFor="name" className="block text-sm font-medium text-zinc-700">
                Tên Tỉnh / Thành phố <span className="text-red-500">*</span>
              </label>
              <Input
                id="name"
                placeholder="Tên sẽ tự động điền khi chọn từ gợi ý"
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                className={`bg-white border-zinc-200 rounded-lg h-11 ${focusRing}`}
              />
            </div>

            {isEditing && city?.nameAscii && (
              <div className="space-y-1">
                <label className="block text-sm font-medium text-zinc-500">Tên không dấu (tự động)</label>
                <p className="text-sm text-zinc-700 bg-zinc-50 border border-zinc-200 rounded-lg px-3 py-2.5 font-[family-name:var(--font-cms-mono)]">
                  {city.nameAscii}
                </p>
              </div>
            )}

            {/* Lat/Lng */}
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-2">
                <label htmlFor="latitude" className="block text-sm font-medium text-zinc-700">
                  Latitude
                </label>
                <Input
                  id="latitude"
                  type="number"
                  step="any"
                  placeholder="Tự động điền"
                  value={formData.latitude}
                  onChange={(e) => setFormData({ ...formData, latitude: e.target.value })}
                  className={`bg-white border-zinc-200 rounded-lg h-11 font-[family-name:var(--font-cms-mono)] ${focusRing}`}
                />
              </div>
              <div className="space-y-2">
                <label htmlFor="longitude" className="block text-sm font-medium text-zinc-700">
                  Longitude
                </label>
                <Input
                  id="longitude"
                  type="number"
                  step="any"
                  placeholder="Tự động điền"
                  value={formData.longitude}
                  onChange={(e) => setFormData({ ...formData, longitude: e.target.value })}
                  className={`bg-white border-zinc-200 rounded-lg h-11 font-[family-name:var(--font-cms-mono)] ${focusRing}`}
                />
              </div>
            </div>
             
            {(formData.latitude && formData.longitude) && (
              <div className={`flex items-center gap-2 py-2 px-3 rounded-lg border ${boxBg}`}>
                <MapPin className={`h-4 w-4 shrink-0 ${boxIconColor}`} />
                <span className={`text-sm font-[family-name:var(--font-cms-mono)] ${boxTextColor}`}>
                  {formData.latitude}, {formData.longitude}
                </span>
              </div>
            )}

            {/* Images */}
            <div className="space-y-3">
              <label className="block text-sm font-medium text-zinc-700">
                <ImagePlus className="inline h-3.5 w-3.5 mr-1 text-zinc-400" />
                Hình ảnh Khu vực
              </label>

              {/* Existing images */}
              {images.length > 0 && (
                <div className="grid grid-cols-3 gap-2">
                  {images.map((url, idx) => (
                    <div key={idx} className="relative group aspect-video rounded-lg overflow-hidden bg-zinc-100 border border-zinc-200">
                      <img
                        src={url}
                        alt={`City image ${idx + 1}`}
                        className="w-full h-full object-cover"
                        onError={(e) => { e.target.style.display = "none"; }}
                      />
                      <button
                        type="button"
                        onClick={() => handleRemoveImage(idx)}
                        className="absolute top-1 right-1 bg-red-500 text-white rounded-full p-0.5 opacity-0 group-hover:opacity-100 transition-opacity shadow-sm"
                      >
                        <X className="h-3 w-3" />
                      </button>
                    </div>
                  ))}
                </div>
              )}

              {/* Add new image URL */}
              <div className="flex gap-2">
                <Input
                  placeholder="Dán URL ảnh tại đây..."
                  value={newImageUrl}
                  onChange={(e) => setNewImageUrl(e.target.value)}
                  onKeyDown={(e) => { if (e.key === "Enter") { e.preventDefault(); handleAddImage(); } }}
                  className={`bg-white border-zinc-200 rounded-lg h-10 flex-1 text-sm ${focusRing}`}
                />
                <Button
                  type="button"
                  variant="outline"
                  onClick={handleAddImage}
                  className="h-10 px-3 shrink-0 border-zinc-200 hover:bg-zinc-50 rounded-lg"
                >
                  <Plus className="h-4 w-4" />
                </Button>
              </div>
              <p className="text-xs text-zinc-400">Hỗ trợ dán trực tiếp URL ảnh. Nhấn Enter hoặc nút + để thêm.</p>
            </div>

            <div className={`p-3 rounded-lg border flex gap-3 text-sm ${boxInfoBg}`}>
                <div className={`w-5 h-5 shrink-0 rounded-full flex items-center justify-center font-bold text-xs mt-0.5 ${boxInfoIconBg}`}>i</div>
                <p>
                  Tên không dấu (ASCII) và tọa độ sẽ tự động tạo khi bạn chọn gợi ý từ Goong Maps.
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
                  {isEditing ? "Lưu thay đổi" : "Tạo Khu vực"}
                </>
              )}
            </Button>
          </div>
        </form>
      </SheetContent>
    </Sheet>
  );
};

export default CityFormSheet;
