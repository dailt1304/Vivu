import React, { useState, useEffect } from "react";
import { Search, MapPin, X, Bookmark, Plus, Star, Loader2 } from "lucide-react";
import { motion, AnimatePresence } from "framer-motion";
import locationApi from "../../../../api/locationApi";
import CollectionPickerTab from "./CollectionPickerTab";

const parseImages = (imagesData) => {
  try {
    let parsed = [];
    if (typeof imagesData === "string" && imagesData.startsWith("[")) {
      parsed = JSON.parse(imagesData);
    } else if (Array.isArray(imagesData)) {
      parsed = imagesData;
    }
    return parsed.map((img) => {
      if (!img) return null;
      if (typeof img === "string") return img;
      return img?.url || img?.imageUrl || img?.src || null;
    }).filter(Boolean);
  } catch (e) {
    console.error("Failed to parse images:", e);
    return [];
  }
};

const AddLocationModal = ({ isOpen, onClose, onAddLocation, existingLocationIds = [], cityId }) => {
  const [activeTab, setActiveTab] = useState("search");
  const [searchTerm, setSearchTerm] = useState("");
  const [locations, setLocations] = useState([]);
  const [isLoading, setIsLoading] = useState(false);

  // Debounced Search Effect
  useEffect(() => {
    const timer = setTimeout(async () => {
      const trimmedTerm = searchTerm.trim();
      if (trimmedTerm) {
        setIsLoading(true);
        try {
          // ✅ Gọi đúng endpoint có hỗ trợ SearchText server-side
          const response = await locationApi.getByFilter({
            SearchText: trimmedTerm,
            pageNumber: 1,
            pageSize: 50,
            SortBy: "rating",
            IsDescending: true,
            ...(cityId ? { cityId } : {}),
          });

          if (response.success && response.data?.items) {
            // ✅ Không cần filter client-side — server đã xử lý dựa trên SearchText
            const mappedItems = response.data.items.map((item) => {
              const parsedImgs = parseImages(
                item.images || item.locationDetail?.images,
              );
              return {
                id: item.id,
                name: item.name,
                address: item.address || item.city?.name || "Vietnam",
                image:
                  parsedImgs[0]?.url ||
                  parsedImgs[0] ||
                  item.imageUrl ||
                  "https://images.unsplash.com/photo-1596394516093-501ba68a0ba6?q=80&w=200&auto=format&fit=crop",
                rating: item.ratingAverage || 0,
                reviews: item.ratingCount || 0,
                latitude: item.latitude,
                longitude: item.longitude,
                category: item.category?.name,
              };
            });

            // ✅ Deduplicate bằng id để đảm bảo chính xác
            const uniqueItems = Array.from(
              new Map(mappedItems.map((item) => [item.id, item])).values(),
            );

            setLocations(uniqueItems);
          } else {
            setLocations([]);
          }
        } catch (error) {
          console.error("Search failed:", error);
          setLocations([]);
        } finally {
          setIsLoading(false);
        }
      } else {
        setLocations([]); // Clear if empty
      }
    }, 500); // 500ms debounce

    return () => clearTimeout(timer);
  }, [searchTerm, cityId]);

  if (!isOpen) return null;

  return (
    <AnimatePresence>
      <div className="fixed inset-0 z-80 flex items-center justify-center p-4">
        {/* Backdrop */}
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          onClick={onClose}
          className="absolute inset-0 bg-black/40 backdrop-blur-sm"
        />

        {/* Modal Content */}
        <motion.div
          initial={{ opacity: 0, scale: 0.95, y: 20 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          exit={{ opacity: 0, scale: 0.95, y: 20 }}
          className="bg-white rounded-2xl shadow-2xl w-full max-w-md overflow-hidden relative z-10 flex flex-col h-[70vh] md:h-[80vh]"
        >
          {/* Header */}
          <div className="p-4 border-b border-gray-100 flex items-center justify-between bg-white sticky top-0 z-20">
            <h3 className="text-lg font-bold text-gray-800">Thêm địa điểm</h3>
            <button
              onClick={onClose}
              className="p-2 hover:bg-gray-100 rounded-full transition-colors"
            >
              <X size={20} className="text-gray-500" />
            </button>
          </div>

          {/* Tab Navigation */}
          <div className="flex border-b border-gray-100 px-4 shrink-0 transition-colors">
            <button
              onClick={() => setActiveTab("search")}
              className={`relative flex-1 py-3 text-sm font-bold transition-colors duration-200 ${
                activeTab === "search" ? "text-blue-600" : "text-gray-500 hover:text-gray-700"
              }`}
            >
              Tất cả địa điểm
              {activeTab === "search" && (
                <motion.div
                  layoutId="activeTabIndicatorAddLocation"
                  className="absolute bottom-0 left-0 right-0 h-0.5 bg-blue-600 rounded-t-full"
                />
              )}
            </button>
            <button
              onClick={() => setActiveTab("collection")}
              className={`relative flex-1 py-3 text-sm font-bold transition-colors duration-200 ${
                activeTab === "collection" ? "text-blue-600" : "text-gray-500 hover:text-gray-700"
              }`}
            >
              Bộ sưu tập của tôi
              {activeTab === "collection" && (
                <motion.div
                  layoutId="activeTabIndicatorAddLocation"
                  className="absolute bottom-0 left-0 right-0 h-0.5 bg-blue-600 rounded-t-full"
                />
              )}
            </button>
          </div>

          {activeTab === "search" ? (
            <>
              {/* Search Input */}
              <div className="p-4 pb-2 shrink-0">
                <div className="relative">
                  <Search
                    size={16}
                    className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
                  />
                  <input
                    type="text"
                    placeholder="Tìm kiếm địa điểm (ví dụ: Chùa Cầu)..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="w-full pl-10 pr-10 py-3 bg-gray-50 border border-gray-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-blue-100 focus:border-blue-400 transition-all font-medium"
                    autoFocus
                  />
                  {isLoading && (
                    <div className="absolute right-3 top-1/2 -translate-y-1/2">
                      <Loader2 size={16} className="animate-spin text-blue-500" />
                    </div>
                  )}
                </div>
              </div>

              {/* List Content */}
              <div className="flex-1 overflow-y-auto p-4 space-y-3">
            {!searchTerm.trim() && locations.length === 0 && (
              <div className="text-center text-gray-400 py-10 text-sm">
                Nhập tên địa điểm để tìm kiếm
              </div>
            )}

            {searchTerm.trim() && locations.length === 0 && !isLoading && (
              <div className="text-center text-gray-400 py-10 text-sm">
                Không tìm thấy kết quả
              </div>
            )}

            {locations.map((item) => (
              <div
                key={item.id}
                className="flex gap-3 p-3 rounded-xl hover:bg-gray-50 border border-transparent hover:border-gray-100 transition-all group cursor-pointer"
                onClick={() => {
                  onAddLocation({ ...item });
                  onClose();
                }}
              >
                <img
                  src={
                    item.image ||
                    "https://images.unsplash.com/photo-1596394516093-501ba68a0ba6?q=80&w=200&auto=format&fit=crop"
                  }
                  alt={item.name}
                  onError={(e) => {
                    e.target.onerror = null;
                    e.target.src =
                      "https://images.unsplash.com/photo-1596394516093-501ba68a0ba6?q=80&w=200&auto=format&fit=crop";
                  }}
                  className="w-16 h-16 rounded-lg object-cover shrink-0 bg-gray-200"
                />
                <div className="flex-1 min-w-0">
                  <div className="flex justify-between items-start">
                    <h4 className="font-bold text-gray-900 text-sm truncate">
                      {item.name}
                    </h4>
                    {item.rating > 0 && (
                      <div className="flex items-center gap-0.5 text-orange-500 text-xs font-bold shrink-0">
                        <Star size={10} fill="currentColor" /> {item.rating}
                      </div>
                    )}
                  </div>
                  <p className="text-gray-500 text-xs truncate flex items-center gap-1 mt-1">
                    <MapPin size={10} /> {item.address}
                  </p>
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      onAddLocation({ ...item });
                      onClose();
                    }}
                    className="mt-2 w-full py-1.5 bg-blue-50 hover:bg-blue-100 text-blue-600 text-xs font-bold rounded-lg transition-colors flex items-center justify-center gap-1 opacity-100 md:opacity-0 group-hover:opacity-100"
                  >
                    <Plus size={12} /> Thêm vào lịch trình
                  </button>
                </div>
              </div>
            ))}
              </div>
            </>
          ) : (
            <CollectionPickerTab 
              onAddLocation={onAddLocation}
              existingLocationIds={existingLocationIds}
            />
          )}
        </motion.div>
      </div>
    </AnimatePresence>
  );
};

export default AddLocationModal;
