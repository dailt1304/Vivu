import React from "react";
import { MessageSquare, Sparkles, ArrowLeft, Send } from "lucide-react";

const DetailsStep = ({ data, setFormData, prevStep, submitForm }) => {
  const { title, notes } = data;

  return (
    <div className="space-y-8 animate-in fade-in slide-in-from-right-4 duration-500">
      <div className="space-y-2">
        <h2 className="text-3xl font-extrabold text-slate-900 tracking-tight">Chi tiết cuối cùng</h2>
        <p className="text-slate-500 font-medium">Thêm tiêu đề và bất kỳ yêu cầu đặc biệt nào cho chuyến đi của bạn.</p>
      </div>

      <div className="space-y-6">
        {/* Title Field */}
        <div className="space-y-2">
          <label className="text-sm font-bold text-slate-700 block">
            Tên chuyến đi <span className="text-xs font-medium text-slate-400 font-normal ml-1">(Không bắt buộc)</span>
          </label>
          <div className="relative group">
            <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400 group-focus-within:text-blue-500 transition-colors">
              <Sparkles size={18} />
            </div>
            <input
              type="text"
              placeholder="VD: Chuyến đi Phú Quốc đáng nhớ..."
              value={title}
              onChange={(e) => setFormData({ title: e.target.value })}
              className="w-full pl-11 pr-4 py-3.5 bg-slate-50 hover:bg-slate-100 focus:bg-white border border-slate-200 focus:border-blue-400 rounded-2xl text-sm font-medium text-slate-800 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-100 transition-all shadow-sm"
            />
          </div>
          <p className="text-xs text-slate-400 font-medium ml-1">Nếu để trống, Vivu AI sẽ tự động tạo tên phù hợp.</p>
        </div>

        {/* Notes Field */}
        <div className="space-y-2">
          <label className="text-sm font-bold text-slate-700 block">
            Ghi chú bổ sung <span className="text-xs font-medium text-slate-400 font-normal ml-1">(Không bắt buộc)</span>
          </label>
          <div className="relative group">
            <div className="absolute top-4 left-0 pl-3.5 flex items-start pointer-events-none text-slate-400 group-focus-within:text-blue-500 transition-colors">
              <MessageSquare size={18} />
            </div>
            <textarea
              placeholder="Bạn có dị ứng thức ăn? Cần không gian yên tĩnh? Hay muốn tham gia các hoạt động mạo hiểm?..."
              value={notes}
              onChange={(e) => setFormData({ notes: e.target.value })}
              rows={4}
              className="w-full pl-11 pr-4 py-3.5 bg-slate-50 hover:bg-slate-100 focus:bg-white border border-slate-200 focus:border-blue-400 rounded-2xl text-sm font-medium text-slate-800 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-100 transition-all shadow-sm resize-none"
            />
          </div>
        </div>
        
        {/* Readiness Info */}
        <div className="bg-gradient-to-r from-blue-50 to-indigo-50 border border-blue-100 p-5 rounded-2xl flex items-start gap-4">
            <div className="w-10 h-10 rounded-full bg-blue-100 text-blue-600 flex items-center justify-center shrink-0">
                <Sparkles size={20} />
            </div>
            <div>
                <h4 className="text-sm font-bold text-slate-800 mb-1">Vivu AI đã sẵn sàng!</h4>
                <p className="text-xs text-slate-600 leading-relaxed">
                    Dựa trên các thông tin bạn cung cấp, AI sẽ thiết kế một lịch trình cá nhân hóa hoàn chỉnh bao gồm địa điểm, thời gian, và các gợi ý ẩm thực phù hợp nhất.
                </p>
            </div>
        </div>
      </div>

      <div className="pt-6 mt-8 border-t border-slate-100 flex items-center justify-between">
        <button
          onClick={prevStep}
          className="px-6 py-3.5 bg-white text-slate-600 font-bold rounded-2xl border border-slate-200 hover:bg-slate-50 hover:text-slate-900 transition-all flex items-center gap-2"
        >
          <ArrowLeft size={18} strokeWidth={2.5} />
          Quay lại
        </button>
        <button
          onClick={submitForm}
          className="px-8 py-3.5 bg-gradient-primary text-white font-bold rounded-2xl shadow-lg shadow-blue-200 hover:shadow-xl hover:scale-[1.02] active:scale-[0.98] transition-all flex items-center gap-2"
        >
          Tạo lịch trình
          <Send size={18} strokeWidth={2.5} />
        </button>
      </div>
    </div>
  );
};

export default DetailsStep;
