import React, { useEffect, useState } from "react";
import { useNavigate, useLocation, Link } from "react-router-dom";
import { CheckCircle2, Loader2, ArrowRight, Home } from "lucide-react";
import { mutate } from "swr";
import paymentApi from "../../../api/paymentApi";
import toast from "@/utils/toast";

const PaymentSuccessPage = () => {
  const location = useLocation();
  const [status, setStatus] = useState("checking"); // checking | success | timeout
  const queryParams = new URLSearchParams(location.search);
  const orderCode = queryParams.get("orderCode");

  useEffect(() => {
    if (!orderCode) {
      setStatus("timeout");
      return;
    }

    let attempts = 0;
    const maxAttempts = 10; // 10 times
    const interval = 3000; // every 3 seconds

    const checkStatus = async () => {
      try {
        const res = await paymentApi.getMyTransactions();
        // Backend returns items array or direct items
        const rawItems = Array.isArray(res.data) ? res.data : res.data?.items || [];
        
        const tx = rawItems.find((t) => String(t.orderCode) === String(orderCode));

        if (tx?.status === "Paid" || tx?.status === 1) { // 1 might be enum for Paid
          setStatus("success");
          toast.success("Thanh toán đã được xác nhận!");
          
          // Vercel Rule: rerender-move-effect-to-event
          // Revalidate SWR cache to update subscription status and transaction history
          mutate("/users/usage");
          mutate("/payments/transactions");
          return;
        }
      } catch (err) {
        console.error("Polling error:", err);
      }

      attempts++;
      if (attempts >= maxAttempts) {
        setStatus("timeout");
      } else {
        setTimeout(checkStatus, interval);
      }
    };

    const timer = setTimeout(checkStatus, 1000); // Start after 1s
    return () => clearTimeout(timer);
  }, [orderCode]);

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col items-center justify-center p-4">
      <div className="max-w-md w-full bg-white rounded-3xl shadow-xl shadow-slate-200/60 p-8 text-center border border-slate-100">
        {status === "checking" && (
          <div className="space-y-6">
            <div className="flex justify-center">
              <div className="relative">
                <div className="w-20 h-20 border-4 border-blue-100 rounded-full" />
                <Loader2 className="w-20 h-20 text-blue-600 animate-spin absolute top-0 left-0" />
              </div>
            </div>
            <h1 className="text-2xl font-black text-slate-900">
              Đang xác nhận thanh toán...
            </h1>
            <p className="text-slate-500">
              Vui lòng đợi trong giây lát khi hệ thống xử lý giao dịch của bạn.
            </p>
          </div>
        )}

        {status === "success" && (
          <div className="space-y-6 animate-in fade-in zoom-in duration-500">
            <div className="flex justify-center">
              <div className="w-20 h-20 bg-emerald-100 rounded-full flex items-center justify-center text-emerald-600">
                <CheckCircle2 size={48} strokeWidth={2.5} />
              </div>
            </div>
            <h1 className="text-2xl font-black text-slate-900">
              Thanh toán thành công!
            </h1>
            <p className="text-slate-500">
              Chúc mừng! Gói dịch vụ của bạn đã được kích hoạt. Hãy bắt đầu hành trình của mình ngay nhé.
            </p>
            <div className="pt-4 flex flex-col gap-3">
              <Link
                to="/profile"
                className="w-full py-4 bg-gradient-primary text-white rounded-xl font-bold flex items-center justify-center gap-2 hover:shadow-lg hover:shadow-blue-500/30 transition-all duration-300"
              >
                Xem gói của tôi <ArrowRight size={18} />
              </Link>
              <Link
                to="/"
                className="w-full py-4 text-slate-700 bg-slate-100 hover:bg-slate-200 font-bold flex items-center justify-center gap-2 rounded-xl transition-colors"
              >
                <Home size={18} /> Về trang chủ
              </Link>
            </div>
          </div>
        )}

        {status === "timeout" && (
          <div className="space-y-6">
            <div className="flex justify-center">
              <div className="w-20 h-20 bg-amber-100 rounded-full flex items-center justify-center text-amber-600">
                <Loader2 size={48} className="animate-pulse" />
              </div>
            </div>
            <h1 className="text-2xl font-black text-slate-900">
              Đang xử lý giao dịch
            </h1>
            <p className="text-slate-500">
              Hệ thống đang hoàn tất việc kích hoạt gói của bạn. Quá trình này có thể mất thêm vài phút.
            </p>
            <div className="pt-4 flex flex-col gap-3">
              <Link
                to="/profile"
                className="w-full py-4 bg-gradient-primary text-white rounded-xl font-bold flex items-center justify-center gap-2 hover:shadow-lg hover:shadow-blue-500/30 transition-all duration-300"
              >
                Kiểm tra trong Cài đặt
              </Link>
              <Link
                to="/"
                className="w-full py-4 text-slate-700 bg-slate-100 hover:bg-slate-200 font-bold flex items-center justify-center gap-2 rounded-xl transition-colors"
              >
                Về trang chủ
              </Link>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default PaymentSuccessPage;
