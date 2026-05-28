import React from "react";
import { Users, Wallet, Heart, Compass, ArrowRight, ArrowLeft, Minus, Plus, Check, Loader2, UserRound, UsersRound, Home } from "lucide-react";
import { useLocationCategories } from "../../../../hooks/locations/useLocations";
import { motion, AnimatePresence } from "framer-motion";
import CategoryIcon from "../../../common/CategoryIcon";


const containerVariants = {
  hidden: { opacity: 0 },
  show: {
    opacity: 1,
    transition: {
      staggerChildren: 0.04
    }
  }
};

const itemVariants = {
  hidden: { opacity: 0, y: 10, scale: 0.95 },
  show: { opacity: 1, y: 0, scale: 1, transition: { type: "spring", stiffness: 300, damping: 24 } }
};

const tripTypeOptions = [
  { key: "solo", label: "Một mình", icon: UserRound, description: "Tự do khám phá" },
  { key: "couple", label: "Cặp đôi", icon: Heart, description: "Lãng mạn & riêng tư" },
  { key: "family", label: "Gia đình", icon: Home, description: "Gắn kết yêu thương" },
  { key: "friends", label: "Bạn bè", icon: UsersRound, description: "Vui vẻ cùng nhau" },
  { key: "senior", label: "Người lớn tuổi", icon: Users, description: "Nhẹ nhàng & thư giãn" },
];

const PreferencesStep = ({ data, errors, setFormData, nextStep, prevStep }) => {
  const { tripType, tripSize, budget, interests } = data;
  const { data: categoriesData, isLoading, error } = useLocationCategories();
  
  // Handle pagination list or direct array
  const categories = categoriesData?.items || (Array.isArray(categoriesData) ? categoriesData : []);

  const toggleInterest = (category) => {
    const isSelected = interests.some(i => i.id === category.id);
    if (isSelected) {
      setFormData({ interests: interests.filter(i => i.id !== category.id) });
    } else {
      setFormData({ interests: [...interests, category] });
    }
  };

  const handleTripTypeChange = (type) => {
    if (type === "solo") {
      setFormData({ tripType: type, tripSize: 1 });
    } else if (type === "couple") {
      setFormData({ tripType: type, tripSize: 2 });
    } else {
      setFormData({ tripType: type, tripSize: tripSize < 2 ? 2 : tripSize });
    }
  };

  return (
    <div className="space-y-8 animate-in fade-in slide-in-from-right-4 duration-500">
      <div className="space-y-2">
        <h2 className="text-3xl font-extrabold text-slate-900 tracking-tight">Sở thích của bạn</h2>
        <p className="text-slate-500 font-medium">Giúp Vivu hiểu rõ hơn để thiết kế lịch trình hoàn hảo nhất.</p>
      </div>

      <div className="space-y-8">
        {/* Trip Type */}
        <div className="space-y-3">
          <label className="flex items-center gap-2 text-sm font-bold text-slate-700">
            <Users size={18} className="text-blue-500" /> Bạn đi cùng ai?
          </label>
          <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
            {tripTypeOptions.map(({ key, label, icon: Icon, description }) => {
              const isSelected = tripType === key;
              return (
                <button
                  key={key}
                  type="button"
                  onClick={() => handleTripTypeChange(key)}
                  className={`group relative flex flex-col items-center gap-1.5 p-4 rounded-2xl border-2 transition-all duration-200 active:scale-[0.97] ${
                    isSelected
                      ? "bg-blue-50 border-blue-400 text-blue-700 shadow-sm shadow-blue-100"
                      : "bg-white border-slate-200 text-slate-500 hover:border-slate-300 hover:bg-slate-50"
                  }`}
                >
                  <Icon size={22} strokeWidth={isSelected ? 2.5 : 2} className={isSelected ? "text-blue-600" : "text-slate-400 group-hover:text-slate-500"} />
                  <span className={`text-sm font-bold ${isSelected ? "text-blue-700" : "text-slate-700"}`}>{label}</span>
                  <span className={`text-[11px] font-medium ${isSelected ? "text-blue-500" : "text-slate-400"}`}>{description}</span>
                </button>
              );
            })}
          </div>
        </div>

        {/* Trip Size — only shown for groups, animated */}
        <AnimatePresence>
          {tripType && !["solo", "couple"].includes(tripType) && (
            <motion.div
              initial={{ opacity: 0, height: 0 }}
              animate={{ opacity: 1, height: "auto" }}
              exit={{ opacity: 0, height: 0 }}
              transition={{ duration: 0.25, ease: "easeInOut" }}
              className="overflow-hidden"
            >
              <div className="space-y-3">
                <label className="flex items-center gap-2 text-sm font-bold text-slate-700">
                  <UsersRound size={18} className="text-indigo-500" /> Số lượng thành viên
                </label>
                <div className="flex items-center gap-4 bg-slate-50 p-2 border border-slate-200 rounded-2xl w-fit">
                  <button
                    type="button"
                    onClick={() => setFormData({ tripSize: Math.max(2, tripSize - 1) })}
                    className="w-10 h-10 flex items-center justify-center rounded-xl bg-white border border-slate-200 text-slate-600 hover:text-blue-600 hover:border-blue-300 hover:bg-blue-50 transition-all shadow-sm active:scale-[0.97]"
                    disabled={tripSize <= 2}
                  >
                    <Minus size={18} />
                  </button>
                  <span className="w-8 text-center font-bold text-lg text-slate-800">{tripSize}</span>
                  <button
                    type="button"
                    onClick={() => setFormData({ tripSize: tripSize + 1 })}
                    className="w-10 h-10 flex items-center justify-center rounded-xl bg-white border border-slate-200 text-slate-600 hover:text-blue-600 hover:border-blue-300 hover:bg-blue-50 transition-all shadow-sm active:scale-[0.97]"
                  >
                    <Plus size={18} />
                  </button>
                </div>
                {errors.tripSize && <p className="text-xs font-semibold text-red-500 mt-1">{errors.tripSize}</p>}
              </div>
            </motion.div>
          )}
        </AnimatePresence>

        {/* Budget */}
        <div className="space-y-3">
          <label className="flex items-center gap-2 text-sm font-bold text-slate-700">
            <Wallet size={18} className="text-emerald-500" /> Ngân sách dự kiến
          </label>
          <div className="grid grid-cols-3 gap-2 bg-slate-50 p-1.5 rounded-2xl border border-slate-200">
            {Object.entries({
              saving: "Tiết kiệm",
              balanced: "Cân đối",
              luxury: "Cao cấp"
            }).map(([key, label]) => (
              <button
                key={key}
                type="button"
                onClick={() => setFormData({ budget: key })}
                className={`py-3 rounded-xl text-sm font-bold transition-all active:scale-[0.98] ${
                  budget === key
                    ? "bg-white text-slate-900 shadow-sm border border-slate-200/50"
                    : "text-slate-500 hover:text-slate-700 hover:bg-slate-100/50 border border-transparent"
                }`}
              >
                {label}
              </button>
            ))}
          </div>
        </div>

        {/* Travel Style (Tạm thời ẩn)
        <div className="space-y-3">
          <label className="flex items-center gap-2 text-sm font-bold text-slate-700">
            <Compass size={18} className="text-purple-500" /> Nhịp độ chuyến đi
          </label>
          <div className="grid grid-cols-3 gap-2 bg-slate-50 p-1.5 rounded-2xl border border-slate-200">
            {Object.entries({
              relaxed: "Thong thả",
              moderate: "Vừa phải",
              packed: "Sát sao"
            }).map(([key, label]) => (
              <button
                key={key}
                type="button"
                onClick={() => setFormData({ travelStyle: key })}
                className={`py-3 rounded-xl text-sm font-bold transition-all active:scale-[0.98] ${
                  travelStyle === key
                    ? "bg-white text-slate-900 shadow-sm border border-slate-200/50"
                    : "text-slate-500 hover:text-slate-700 hover:bg-slate-100/50 border border-transparent"
                }`}
              >
                {label}
              </button>
            ))}
          </div>
        </div>
        */}

        {/* Interests */}
        <div className="space-y-3">
          <div className="flex justify-between items-end">
            <label className="flex items-center gap-2 text-sm font-bold text-slate-700">
              <Heart size={18} className="text-red-500" /> Trải nghiệm yêu thích <span className="text-xs text-slate-400 font-medium ml-1">(Không bắt buộc)</span>
            </label>
            <AnimatePresence>
              {interests.length > 0 && (
                <motion.span 
                  initial={{ opacity: 0, scale: 0.8 }}
                  animate={{ opacity: 1, scale: 1 }}
                  exit={{ opacity: 0, scale: 0.8 }}
                  className="text-xs font-bold text-blue-600 bg-blue-50 px-2 py-1 rounded-md"
                >
                  {interests.length} đã chọn
                </motion.span>
              )}
            </AnimatePresence>
          </div>

          <div className="min-h-[100px]">
            {isLoading ? (
              <div className="flex flex-wrap gap-2.5">
                {[1, 2, 3, 4, 5, 6].map(i => (
                  <div key={i} className="h-9 w-24 bg-slate-100 rounded-xl animate-pulse" />
                ))}
              </div>
            ) : error ? (
              <div className="p-4 rounded-xl bg-red-50 border border-red-100 text-sm text-red-600 font-medium">
                Đã có lỗi xảy ra khi tải danh mục. Vui lòng thử lại sau.
              </div>
            ) : categories.length === 0 ? (
              <div className="p-4 rounded-xl bg-slate-50 border border-slate-100 text-sm text-slate-500 font-medium text-center">
                Chưa có danh mục nào để hiển thị.
              </div>
            ) : (
              <motion.div 
                className="flex flex-wrap gap-2.5"
                variants={containerVariants}
                initial="hidden"
                animate="show"
              >
                {categories.map((category) => {
                  const isSelected = interests.some(i => i.id === category.id);
                  return (
                    <motion.button
                      layout
                      variants={itemVariants}
                      key={category.id}
                      type="button"
                      whileTap={{ scale: 0.95 }}
                      onClick={() => toggleInterest(category)}
                      className={`relative flex items-center gap-1.5 px-4 py-2 rounded-xl border transition-colors duration-200 ${
                        isSelected
                          ? "bg-blue-50 text-blue-700 border-blue-200 shadow-sm shadow-blue-100/50 font-bold hover:bg-blue-100/60"
                          : "bg-slate-50/80 text-slate-600 border-slate-200/80 font-semibold hover:border-slate-300 hover:bg-slate-100"
                      }`}
                    >
                      {isSelected && (
                        <motion.div 
                          initial={{ scale: 0 }} 
                          animate={{ scale: 1 }} 
                          transition={{ type: "spring", stiffness: 400, damping: 25 }}
                        >
                          <Check size={14} strokeWidth={3} className="text-blue-600" />
                        </motion.div>
                      )}
                      <CategoryIcon 
                        name={category.name || category.title} 
                        iconUrl={category.iconUrl} 
                        size={16} 
                        className={isSelected ? "text-blue-600" : "text-slate-400"}
                      />
                      <span className="text-sm">{category.name || category.title}</span>

                    </motion.button>
                  );
                })}
              </motion.div>
            )}
          </div>
        </div>
      </div>

      <div className="pt-6 mt-8 border-t border-slate-100 flex items-center justify-between">
        <button
          onClick={prevStep}
          className="px-6 py-3.5 bg-white text-slate-600 font-bold rounded-2xl border border-slate-200 hover:bg-slate-50 hover:text-slate-900 transition-all flex items-center gap-2 active:scale-[0.98]"
        >
          <ArrowLeft size={18} strokeWidth={2.5} />
          Quay lại
        </button>
        <button
          onClick={nextStep}
          className="px-8 py-3.5 bg-gradient-primary text-white font-bold rounded-2xl shadow-lg shadow-blue-200 hover:shadow-xl hover:-translate-y-0.5 active:translate-y-0 transition-all flex items-center gap-2 active:scale-[0.98]"
        >
          Tiếp tục
          <ArrowRight size={18} strokeWidth={2.5} />
        </button>
      </div>
    </div>
  );
};

export default PreferencesStep;
