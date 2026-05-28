import React, { useMemo, useState } from "react";
import { Map, MapPin, ImageIcon, X } from "lucide-react";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";

import { Dialog, DialogContent } from "@/components/ui/dialog";
import { useLocation } from "react-router-dom";

const CityDetailSheet = ({ open, onOpenChange, city }) => {
  const [previewImage, setPreviewImage] = useState(null);

  const loc = useLocation();
  const isAdminTheme = ['/cms/dashboard', '/cms/users', '/cms/subscriptions'].some(p => loc.pathname.includes(p));
  const iconColor = isAdminTheme ? "text-emerald-500" : "text-blue-500";

  const images = useMemo(() => {
    if (!city?.image) return [];
    try {
      const parsed = JSON.parse(city.image);
      return Array.isArray(parsed) ? parsed : [city.image];
    } catch {
      return [city.image];
    }
  }, [city?.image]);

  if (!city) return null;

  return (
    <>
      <Sheet 
        open={open} 
        onOpenChange={(val) => {
          // Block sheet from closing if the lightbox is currently active
          if (!val && previewImage) return;
          if (onOpenChange) onOpenChange(val);
        }}
      >
        <SheetContent className="w-full sm:max-w-xl bg-white border-zinc-200 shadow-2xl p-0 flex flex-col">
          <SheetHeader className="p-6 border-b border-zinc-100 bg-zinc-50/50">
            <SheetTitle className="text-xl font-semibold flex items-center gap-2 text-zinc-900">
              <Map className={`h-5 w-5 ${iconColor}`} />
              Chi tiết Khu vực
            </SheetTitle>
          </SheetHeader>

          <div className="flex-1 overflow-y-auto p-6 space-y-6 hide-scrollbar">
            <div>
              <h4 className="text-xs font-medium text-zinc-500 mb-1 uppercase tracking-wider">
                Tên Khu vực
              </h4>
              <p className="text-lg font-semibold text-zinc-900">{city.name}</p>
              {city.nameAscii && (
                <p className="text-sm text-zinc-400 mt-0.5">{city.nameAscii}</p>
              )}
            </div>

            {/* Image Gallery */}
            {images.length > 0 && (
              <div className="space-y-2">
                <h4 className="text-xs font-medium text-zinc-500 uppercase tracking-wider flex items-center gap-1.5">
                  <ImageIcon className="h-3.5 w-3.5" />
                  Hình ảnh ({images.length})
                </h4>
                <div className={`grid gap-2 ${images.length === 1 ? "grid-cols-1" : "grid-cols-2"}`}>
                  {images.map((url, idx) => (
                    <button
                      key={idx}
                      type="button"
                      onClick={() => setPreviewImage(url)}
                      className={`relative group overflow-hidden rounded-lg border border-zinc-200 bg-zinc-100 cursor-pointer ${
                        images.length === 1 ? "aspect-video" : "aspect-square"
                      }`}
                    >
                      <img
                        src={url}
                        alt={`${city.name} - ${idx + 1}`}
                        className="w-full h-full object-cover transition-transform duration-300 group-hover:scale-105"
                        onError={(e) => {
                          e.target.style.display = "none";
                          e.target.nextSibling.style.display = "flex";
                        }}
                      />
                      <div className="hidden items-center justify-center w-full h-full text-zinc-400 text-xs">
                        Ảnh lỗi
                      </div>
                      <div className="absolute inset-0 bg-black/0 group-hover:bg-black/10 transition-colors" />
                    </button>
                  ))}
                </div>
              </div>
            )}

            <div className="grid grid-cols-2 gap-4 pb-6 border-b border-zinc-100">
              <div>
                <h4 className="text-xs font-medium text-zinc-500 mb-1">Quốc gia</h4>
                <p className="text-sm text-zinc-800 font-medium">
                  {city.country?.name || "Việt Nam"}
                </p>
              </div>

              <div>
                <h4 className="text-xs font-medium text-zinc-500 mb-1">Số lượng địa điểm</h4>
                <div className="flex items-center gap-1.5 text-sm text-zinc-800 font-medium">
                  <MapPin className={`h-4 w-4 ${iconColor}`} />
                  <span className="font-[family-name:var(--font-cms-mono)]">{city.locationCount ?? 0}</span>
                </div>
              </div>
              
              <div>
                <h4 className="text-xs font-medium text-zinc-500 mb-1">Ngày tạo</h4>
                <p className="text-sm text-zinc-800 font-[family-name:var(--font-cms-mono)]">
                  {city.createdDate ? new Date(city.createdDate).toLocaleDateString("vi-VN") : "N/A"}
                </p>
              </div>

              <div>
                <h4 className="text-xs font-medium text-zinc-500 mb-1">Cập nhật lần cuối</h4>
                <p className="text-sm text-zinc-800 font-[family-name:var(--font-cms-mono)]">
                  {city.modifiedDate ? new Date(city.modifiedDate).toLocaleDateString("vi-VN") : "Chưa chỉnh sửa"}
                </p>
              </div>

              {(city.latitude && city.longitude) && (
                <div className="col-span-2">
                  <h4 className="text-xs font-medium text-zinc-500 mb-1">Tọa độ</h4>
                  <div className="flex items-center gap-1.5 text-sm text-zinc-800 font-medium">
                    <MapPin className={`h-4 w-4 ${iconColor}`} />
                    <span className="font-[family-name:var(--font-cms-mono)]">{city.latitude}, {city.longitude}</span>
                  </div>
                </div>
              )}
            </div>

            <div className="bg-zinc-50 p-4 rounded-xl border border-zinc-100 border-dashed">
               <p className="text-sm text-zinc-500 text-center">
                   ID: <span className="font-[family-name:var(--font-cms-mono)] text-zinc-700">{city.id}</span>
               </p>
            </div>
          </div>
        </SheetContent>
      </Sheet>

      {/* Lightbox Preview mapped to a Radix Dialog so it perfectly stacks over the Sheet */}
      <Dialog open={!!previewImage} onOpenChange={(open) => !open && setPreviewImage(null)}>
        <DialogContent 
          className="max-w-[90vw] sm:max-w-4xl border-none bg-transparent shadow-none p-0 flex items-center justify-center overflow-hidden"
          onOpenAutoFocus={(e) => e.preventDefault()}
        >
          <img
            src={previewImage}
            alt="Preview"
            className="w-auto h-auto max-w-full max-h-[85vh] object-contain rounded-xl shadow-2xl"
          />
        </DialogContent>
      </Dialog>
    </>
  );
};

export default CityDetailSheet;
