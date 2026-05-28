import React, { useState } from "react";
import {
  X,
  Wallet,
  Users,
  Heart,
  Utensils,
  Camera,
  Moon,
  Sun,
  MapPin,
} from "lucide-react";

const TripPreferencesModal = ({ isOpen, onClose, onSubmit }) => {
  const [preferences, setPreferences] = useState({
    budget: "balanced", // saving, balanced, luxury
    companions: "couple", // solo, couple, family, friends
    interests: [],
    pace: "relaxed", // relaxed, moderate, fast
    memberCount: "",
  });

  if (!isOpen) return null;

  const toggleInterest = (id) => {
    setPreferences((prev) => ({
      ...prev,
      interests: prev.interests.includes(id)
        ? prev.interests.filter((i) => i !== id)
        : [...prev.interests, id],
    }));
  };

  const handleSubmit = () => {
    // Validate if member count is needed
    if (
      (preferences.companions === "family" ||
        preferences.companions === "friends") &&
      !preferences.memberCount
    ) {
      // You might want to show an error or just submit anyway
    }
    onSubmit(preferences);
    onClose();
  };

  return (
    <div className="fixed inset-0 z-70 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="bg-white rounded-3xl shadow-2xl w-full max-w-lg overflow-hidden animate-in zoom-in-95 duration-200">
        {/* Header */}
        <div className="p-6 border-b border-gray-100 flex justify-between items-center bg-gray-50/50">
          <div>
            <h2 className="text-xl font-bold text-gray-900">
              Sở thích chuyến đi
            </h2>
            <p className="text-sm text-gray-500">
              Giúp Vivu hiểu rõ nhu cầu của bạn
            </p>
          </div>
          <button
            onClick={onClose}
            className="p-2 hover:bg-gray-200 rounded-full transition-colors text-gray-500"
          >
            <X size={20} />
          </button>
        </div>

        {/* Body */}
        <div className="p-6 space-y-8 max-h-[70vh] overflow-y-auto custom-scrollbar">
          {/* Section 1: Companions */}
          <div className="space-y-3">
            <label className="flex items-center gap-2 text-sm font-semibold text-gray-700">
              <Users size={18} className="text-blue-500" /> Bạn đi cùng ai?
            </label>
            <div className="flex flex-wrap gap-2">
              {[
                { id: "solo", label: "Một mình" },
                { id: "couple", label: "Cặp đôi" },
                { id: "family", label: "Gia đình" },
                { id: "friends", label: "Nhóm bạn" },
              ].map((opt) => (
                <button
                  key={opt.id}
                  onClick={() =>
                    setPreferences((p) => ({ ...p, companions: opt.id }))
                  }
                  className={`px-4 py-2 rounded-full text-sm font-medium border transition-all ${
                    preferences.companions === opt.id
                      ? "bg-gradient-primary text-white border-transparent shadow-md shadow-blue-200"
                      : "bg-white text-gray-600 border-gray-200 hover:border-blue-300 hover:bg-blue-50"
                  }`}
                >
                  {opt.label}
                </button>
              ))}
            </div>

            {/* Member Count Input for Family/Friends */}
            {(preferences.companions === "family" ||
              preferences.companions === "friends") && (
              <div className="animate-in slide-in-from-top-2 duration-200 pt-2">
                <div className="flex items-center gap-3">
                  <input
                    type="number"
                    min="1"
                    placeholder="Số lượng thành viên"
                    value={preferences.memberCount}
                    onChange={(e) =>
                      setPreferences((p) => ({
                        ...p,
                        memberCount: e.target.value,
                      }))
                    }
                    className="w-full p-3 bg-gray-50 border border-gray-200 rounded-xl text-sm focus:outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100 transition-all placeholder:text-gray-400"
                  />
                </div>
              </div>
            )}
          </div>

          {/* Section 2: Budget */}
          <div className="space-y-3">
            <label className="flex items-center gap-2 text-sm font-semibold text-gray-700">
              <Wallet size={18} className="text-green-500" /> Ngân sách dự kiến
            </label>
            <div className="grid grid-cols-3 gap-2 bg-gray-50 p-1 rounded-xl border border-gray-200">
              {[
                { id: "saving", label: "Tiết kiệm" },
                { id: "balanced", label: "Cân đối" },
                { id: "luxury", label: "Sang chảnh" },
              ].map((opt) => (
                <button
                  key={opt.id}
                  onClick={() =>
                    setPreferences((p) => ({ ...p, budget: opt.id }))
                  }
                  className={`py-2 rounded-lg text-sm font-medium transition-all ${
                    preferences.budget === opt.id
                      ? "bg-white text-gray-900 shadow-sm border border-gray-100"
                      : "text-gray-500 hover:text-gray-700"
                  }`}
                >
                  {opt.label}
                </button>
              ))}
            </div>
          </div>

          {/* Section 3: Interests */}
          <div className="space-y-3">
            <label className="flex items-center gap-2 text-sm font-semibold text-gray-700">
              <Heart size={18} className="text-red-500" /> Bạn thích trải nghiệm
              gì?
            </label>
            <div className="flex flex-wrap gap-2">
              {[
                { id: "food", label: "Ẩm thực", icon: <Utensils size={14} /> },
                { id: "culture", label: "Văn hóa", icon: <MapPin size={14} /> },
                { id: "nature", label: "Thiên nhiên", icon: <Sun size={14} /> },
                {
                  id: "shopping",
                  label: "Mua sắm",
                  icon: <Wallet size={14} />,
                },
                { id: "relax", label: "Nghỉ dưỡng", icon: <Moon size={14} /> },
                { id: "photos", label: "Sống ảo", icon: <Camera size={14} /> },
              ].map((opt) => (
                <button
                  key={opt.id}
                  onClick={() => toggleInterest(opt.id)}
                  className={`flex items-center gap-1.5 px-3 py-2 rounded-xl text-sm font-medium border transition-all ${
                    preferences.interests.includes(opt.id)
                      ? "bg-indigo-50 text-indigo-700 border-indigo-200"
                      : "bg-white text-gray-600 border-gray-200 hover:border-indigo-200"
                  }`}
                >
                  {opt.icon}
                  {opt.label}
                </button>
              ))}
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="p-6 border-t border-gray-100 bg-gray-50/50 flex justify-end gap-3">
          <button
            onClick={onClose}
            className="px-5 py-2.5 text-gray-600 font-medium hover:bg-gray-200 rounded-xl transition-colors"
          >
            Bỏ qua
          </button>
          <button
            onClick={handleSubmit}
            className="px-6 py-2.5 bg-gradient-primary text-white font-bold rounded-xl shadow-lg shadow-blue-200 hover:shadow-xl hover:scale-105 active:scale-95 transition-all"
          >
            Áp dụng
          </button>
        </div>
      </div>
    </div>
  );
};

export default TripPreferencesModal;
