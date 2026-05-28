import React, { useState } from "react";
import { X, MapPin, Clock } from "lucide-react";

const TripDetailsModal = ({ isOpen, onClose, onSubmit }) => {
  const [destination, setDestination] = useState("");
  const [duration, setDuration] = useState("");

  if (!isOpen) return null;

  const handleSubmit = (e) => {
    e.preventDefault();
    if (destination && duration) {
      onSubmit(destination, duration);
      onClose();
    }
  };

  return (
    <div className="fixed inset-0 z-70 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="bg-white rounded-3xl shadow-2xl w-full max-w-md overflow-hidden animate-in zoom-in-95 duration-200">
        {/* Header */}
        <div className="p-6 border-b border-gray-100 flex justify-between items-center bg-gray-50/50">
          <div>
            <h2 className="text-xl font-bold text-gray-900">
              Chi tiết chuyến đi
            </h2>
            <p className="text-sm text-gray-500">
              Hãy cho Vivu biết dự định của bạn
            </p>
          </div>
          <button
            onClick={onClose}
            className="p-2 hover:bg-gray-200 rounded-full transition-colors text-gray-500"
          >
            <X size={20} />
          </button>
        </div>

        {/* Form Body */}
        <form onSubmit={handleSubmit} className="p-6 space-y-6">
          <div className="space-y-3">
            <label className="flex items-center gap-2 text-sm font-semibold text-gray-700">
              <MapPin size={18} className="text-red-500" /> Bạn muốn đi đâu?
            </label>
            <input
              type="text"
              value={destination}
              onChange={(e) => setDestination(e.target.value)}
              placeholder="Ví dụ: Đà Nẵng, Phú Quốc..."
              className="w-full p-3 bg-gray-50 border border-gray-200 rounded-xl text-sm focus:outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100 transition-all placeholder:text-gray-400"
              autoFocus
              required
            />
          </div>

          <div className="space-y-3">
            <label className="flex items-center gap-2 text-sm font-semibold text-gray-700">
              <Clock size={18} className="text-orange-500" /> Đi trong bao lâu?
            </label>
            <input
              type="text"
              value={duration}
              onChange={(e) => setDuration(e.target.value)}
              placeholder="Ví dụ: 3 ngày 2 đêm, cuối tuần này..."
              className="w-full p-3 bg-gray-50 border border-gray-200 rounded-xl text-sm focus:outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100 transition-all placeholder:text-gray-400"
              required
            />
          </div>

          <div className="pt-2 flex justify-end gap-3">
            <button
              type="button"
              onClick={onClose}
              className="px-5 py-2.5 text-gray-600 font-medium hover:bg-gray-200 rounded-xl transition-colors"
            >
              Hủy
            </button>
            <button
              type="submit"
              disabled={!destination || !duration}
              className="px-6 py-2.5 bg-gradient-primary text-white font-bold rounded-xl shadow-lg shadow-blue-200 hover:shadow-xl hover:scale-105 active:scale-95 transition-all disabled:opacity-50 disabled:shadow-none"
            >
              Tiếp tục
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default TripDetailsModal;
