import React from "react";
import { Link, useLocation } from "react-router-dom";
import { XCircle, RefreshCcw, Home, AlertTriangle } from "lucide-react";

const PaymentCancelPage = () => {
  const location = useLocation();
  const queryParams = new URLSearchParams(location.search);
  const orderCode = queryParams.get("orderCode");

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col items-center justify-center p-4">
      <div className="max-w-md w-full bg-white rounded-3xl shadow-xl shadow-slate-200/60 p-8 text-center border border-slate-100 animate-in fade-in slide-in-from-bottom-4 duration-500">
        <div className="flex justify-center mb-6">
          <div className="w-20 h-20 bg-rose-100 rounded-full flex items-center justify-center text-rose-600">
            <XCircle size={48} strokeWidth={2.5} />
          </div>
        </div>
        
        <h1 className="text-2xl font-black text-slate-900 mb-4">
          Thanh toán đã bị hủy
        </h1>
        
        <p className="text-slate-500 mb-8 leading-relaxed">
          Bạn đã chủ động hủy quá trình thanh toán hoặc có lỗi xảy ra. Đừng lo lắng, chúng tôi chưa trừ tiền từ tài khoản của bạn.
        </p>

        {orderCode && (
          <div className="bg-slate-50 border border-slate-100 rounded-xl p-4 mb-8 flex items-center gap-3 text-left">
            <div className="w-10 h-10 bg-slate-200 rounded-full flex items-center justify-center text-slate-500 shrink-0">
              <AlertTriangle size={20} />
            </div>
            <div>
              <p className="text-xs text-slate-400 font-bold uppercase tracking-wider">Mã đơn hàng</p>
              <p className="font-mono text-slate-700 font-bold">{orderCode}</p>
            </div>
          </div>
        )}

        <div className="flex flex-col gap-3">
          <Link
            to="/subscription"
            className="w-full py-4 bg-gradient-primary text-white rounded-xl font-bold flex items-center justify-center gap-2 hover:shadow-lg hover:shadow-blue-500/30 transition-all duration-300"
          >
            <RefreshCcw size={18} /> Thử lại ngay
          </Link>
          <Link
            to="/"
            className="w-full py-4 text-slate-700 bg-slate-100 hover:bg-slate-200 font-bold flex items-center justify-center gap-2 rounded-xl transition-colors"
          >
            <Home size={18} /> Về trang chủ
          </Link>
        </div>
      </div>
      
      <p className="mt-8 text-slate-400 text-sm font-medium">
        Cần hỗ trợ? Liên hệ <a href="/support" className="text-blue-500 hover:underline">Bộ phận Chăm sóc khách hàng</a>
      </p>
    </div>
  );
};

export default PaymentCancelPage;
