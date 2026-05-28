import React, { useState } from "react";
import { Image as ImageIcon, Loader2, X } from "lucide-react";
import { AnimatePresence, motion } from "framer-motion";
import { useTripImages } from "../../../../hooks/trips/useTripImages";

const TripMedia = ({ tripId }) => {
  const { images, isLoading, error } = useTripImages(tripId);
  const [selectedImage, setSelectedImage] = useState(null);

  if (isLoading) {
    return (
      <div className="h-full flex items-center justify-center p-6 bg-slate-50/50">
        <Loader2 className="w-8 h-8 text-blue-500 animate-spin" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="h-full flex items-center justify-center p-6 bg-slate-50/50">
        <p className="text-rose-500 font-medium text-center">
          Không thể tải ảnh. Vui lòng thử lại sau.
        </p>
      </div>
    );
  }

  return (
    <div className="p-6 h-full flex flex-col bg-slate-50/50">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h3 className="font-bold text-slate-800 text-lg flex items-center gap-2">
            <div className="p-2 bg-blue-100 text-blue-600 rounded-xl">
              <ImageIcon size={20} />
            </div>
            Bộ sưu tập chuyến đi
            {images?.length > 0 && (
              <span className="text-sm font-normal text-slate-400 bg-white px-2 py-0.5 rounded-full border border-slate-100">
                {images.length} ảnh
              </span>
            )}
          </h3>
          <p className="text-sm text-slate-500 mt-1">
            Ảnh được các thành viên chia sẻ trong nhóm chat
          </p>
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 overflow-y-auto min-h-0 no-scrollbar pb-20">
        {!images || images.length === 0 ? (
          <div className="h-[60%] flex flex-col items-center justify-center text-center space-y-4">
            <div className="w-20 h-20 bg-slate-100 rounded-full flex items-center justify-center">
              <ImageIcon className="w-10 h-10 text-slate-400" />
            </div>
            <div>
              <p className="text-slate-600 font-medium text-lg">
                Chưa có ảnh nào
              </p>
              <p className="text-slate-400 text-sm mt-1 max-w-sm">
                Hãy bắt đầu chia sẻ ảnh với bạn bè trong phần Nhắn tin của
                chuyến đi.
              </p>
            </div>
          </div>
        ) : (
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
            <AnimatePresence>
              {images.map((img) => (
                <motion.div
                  key={img.id}
                  layout
                  initial={{ opacity: 0, scale: 0.8 }}
                  animate={{ opacity: 1, scale: 1 }}
                  exit={{ opacity: 0, scale: 0.8 }}
                  onClick={() => setSelectedImage(img)}
                  className="group relative aspect-square rounded-2xl overflow-hidden bg-white shadow-sm border border-slate-100 cursor-pointer"
                >
                  <img
                    src={img.url}
                    alt={img.fileName || "Trip image"}
                    className="w-full h-full object-cover transition-transform duration-500 group-hover:scale-110"
                    loading="lazy"
                  />
                  {/* Hover Overlay */}
                  <div className="absolute inset-0 bg-black/0 group-hover:bg-black/20 transition-colors duration-300" />

                  {/* Optional filename overlay */}
                  {img.fileName && (
                    <div className="absolute bottom-0 left-0 right-0 p-3 bg-gradient-to-t from-black/60 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300">
                      <p className="text-white text-xs font-medium truncate drop-shadow-sm">
                        {img.fileName}
                      </p>
                    </div>
                  )}
                </motion.div>
              ))}
            </AnimatePresence>
          </div>
        )}
      </div>

      {/* Lightbox */}
      <AnimatePresence>
        {selectedImage && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 z-50 flex items-center justify-center p-4 md:p-10 bg-black/90 backdrop-blur-sm"
            onClick={() => setSelectedImage(null)}
          >
            <button
              className="absolute top-4 right-4 md:top-8 md:right-8 p-3 bg-white/10 hover:bg-white/20 text-white rounded-full transition-colors backdrop-blur-md"
              onClick={() => setSelectedImage(null)}
            >
              <X size={24} />
            </button>
            <motion.img
              initial={{ scale: 0.9, y: 20 }}
              animate={{ scale: 1, y: 0 }}
              exit={{ scale: 0.9, y: 20 }}
              src={selectedImage.url}
              alt={selectedImage.fileName || "Full screen preview"}
              className="max-w-full max-h-full object-contain rounded-lg shadow-2xl"
              onClick={(e) => e.stopPropagation()}
            />
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
};

export default TripMedia;
