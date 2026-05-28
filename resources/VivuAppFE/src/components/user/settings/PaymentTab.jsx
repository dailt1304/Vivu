import { useState } from "react";
import {
  CreditCard,
  Star,
  Crown,
  Check,
  ArrowRight,
  Clock,
  XCircle,
  Zap,
  ExternalLink,
  Copy,
  Receipt,
  X,
} from "lucide-react";
import { format } from "date-fns";
import { vi } from "date-fns/locale";
import { useNavigate } from "react-router-dom";
import SettingsSection from "./SettingsSection";
import { useMyTransactions } from "../../../hooks/payments/usePayments";
import { useUserUsage } from "../../../hooks/users/useUsers";
import { useMySubscription } from "../../../hooks/subscriptions/useSubscriptions";
import { EmptyState } from "../../common";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import toast from "@/utils/toast";

/**
 * Payment settings tab
 */
const PaymentTab = () => {
  const navigate = useNavigate();
  const { transactions, isLoading: isTxLoading } = useMyTransactions();
  const { usage, isLoading: isUsageLoading } = useUserUsage();
  const [selectedTransaction, setSelectedTransaction] = useState(null);
  const [detailOpen, setDetailOpen] = useState(false);

  const aiPercentage = usage?.limit > 0 ? Math.min((usage.used / usage.limit) * 100, 100) : 0;

  const formatCurrency = (amount) => {
    return new Intl.NumberFormat("vi-VN", {
      style: "currency",
      currency: "VND",
    }).format(Math.abs(amount));
  };

  const getStatusInfo = (status) => {
    const s = String(status).toLowerCase();
    if (s === "paid" || s === "1" || s === "success") {
      return {
        label: "Thành công",
        color: "text-green-600",
        bgColor: "bg-green-50",
        borderColor: "border-green-200",
        icon: <Check size={12} />,
      };
    }
    if (s === "cancelled" || s === "2" || s === "failed") {
      return {
        label: "Đã hủy",
        color: "text-red-500",
        bgColor: "bg-red-50",
        borderColor: "border-red-200",
        icon: <XCircle size={12} />,
      };
    }
    return {
      label: "Đang chờ",
      color: "text-amber-500",
      bgColor: "bg-amber-50",
      borderColor: "border-amber-200",
      icon: <Clock size={12} />,
    };
  };

  const handleViewTransaction = (tx) => {
    setSelectedTransaction(tx);
    setDetailOpen(true);
  };

  const handleCopyOrderCode = (code) => {
    navigator.clipboard.writeText(String(code));
    toast.success("Đã sao chép mã đơn hàng");
  };

  // Thêm useMySubscription vào import và hook
  const { isNearExpiry, isExpired, hasSubscription, daysRemaining } = useMySubscription();

  return (
    <div className="space-y-6">
      {/* AI Usage Section - Bỏ useUserUsage vì đã dùng chung thông tin */}
      <SettingsSection
        title="Sử dụng AI hôm nay"
        description="Theo dõi số lượt sử dụng AI và thông tin gói cước của bạn"
      >
        {isUsageLoading ? (
          <div className="flex justify-center py-4">
            <div className="w-5 h-5 border-2 border-blue-600 border-t-transparent rounded-full animate-spin" />
          </div>
        ) : usage && usage.limit > 0 ? (
          <div className="flex flex-col gap-4">
            <div className="flex items-center justify-between font-medium text-sm">
               <div className="flex items-center gap-2 text-slate-700">
                  <Zap className={`w-4 h-4 ${usage.remaining === 0 ? 'text-red-500 fill-red-500' : 'text-blue-500 fill-blue-500'}`} />
                  <span>{usage.remaining === 0 ? "Đã hết lượt AI" : `${usage.used} / ${usage.limit} lượt`}</span>
               </div>
               <span className="text-slate-500">
                  Reset lúc: {usage.resetAt ? format(new Date(usage.resetAt), "HH:mm, dd/MM", { locale: vi }) : ""}
               </span>
            </div>
            
            <div className="h-2 w-full bg-slate-100 rounded-full overflow-hidden shrink-0">
               <div 
                 className={`h-full rounded-full transition-all duration-500 ease-out ${usage.remaining === 0 ? 'bg-red-500' : usage.remaining <= 1 ? 'bg-yellow-400' : 'bg-gradient-to-r from-blue-400 to-blue-600'}`}
                 style={{ width: `${aiPercentage}%` }}
               />
            </div>
            
            <div className="flex items-center justify-between mt-2 flex-wrap gap-4 border border-slate-200 bg-slate-50 rounded-xl p-4">
               <div>
                 <p className="text-sm font-semibold text-slate-800">
                    Gói cước: {usage.hasActiveSubscription && usage.packageName ? usage.packageName : "Free (5 lượt/ngày)"}
                 </p>
                 {usage.hasActiveSubscription && usage.subscriptionEndDate && (
                   <p className="text-xs text-slate-500 mt-1">
                      Hết hạn: {format(new Date(usage.subscriptionEndDate), "dd/MM/yyyy", { locale: vi })}
                   </p>
                 )}
               </div>
               
               {(!usage.hasActiveSubscription || isExpired) && (
                 <button
                   onClick={() => navigate("/subscription")}
                   className="text-sm font-bold text-blue-600 hover:text-blue-700 flex items-center gap-1 bg-blue-100/50 hover:bg-blue-100 px-3 py-1.5 rounded-lg transition-colors"
                 >
                    Nâng cấp Premium <ArrowRight size={14} />
                 </button>
               )}
            </div>
          </div>
        ) : (
          <p className="text-sm text-slate-500">Chưa có thông tin sử dụng AI.</p>
        )}
      </SettingsSection>

      {/* Transaction History */}
      <SettingsSection
        title="Lịch sử giao dịch"
        description="Các giao dịch gần đây của bạn qua PayOS"
      >
        {isTxLoading ? (
          <div className="flex justify-center py-8">
            <div className="w-6 h-6 border-2 border-blue-600 border-t-transparent rounded-full animate-spin" />
          </div>
        ) : transactions && transactions.length > 0 ? (
          <div className="space-y-1">
            {transactions.map((tx) => {
              const statusInfo = getStatusInfo(tx.status);
              return (
                <div
                  key={tx.id}
                  onClick={() => handleViewTransaction(tx)}
                  className="flex items-center justify-between py-4 px-3 -mx-3 border-b border-slate-100 last:border-0 rounded-xl cursor-pointer hover:bg-slate-50 transition-colors group"
                >
                  <div className="flex flex-col">
                    <p className="font-semibold text-slate-900 group-hover:text-blue-600 transition-colors">
                      {tx.packageName || tx.description || "Thanh toán gói Vivu"}
                    </p>
                    <div className="flex items-center gap-2 mt-1">
                      <p className="text-[10px] text-slate-400 font-mono uppercase bg-slate-100 px-1.5 py-0.5 rounded-sm">
                        #{tx.orderCode}
                      </p>
                      <span className="text-slate-300">•</span>
                      <p className="text-xs text-slate-500 font-medium">
                        {tx.paymentDate
                          ? new Date(tx.paymentDate).toLocaleDateString("vi-VN")
                          : new Date(tx.createdAt).toLocaleDateString("vi-VN")}
                      </p>
                    </div>
                  </div>
                  <div className="text-right flex items-center gap-3">
                    <div>
                      <p className="font-bold text-slate-900">
                        {formatCurrency(tx.amount)}
                      </p>
                      <p
                        className={`text-xs ${statusInfo.color} font-medium flex items-center gap-1 justify-end mt-1`}
                      >
                        {statusInfo.icon}
                        {statusInfo.label}
                      </p>
                    </div>
                    <ArrowRight size={14} className="text-slate-300 group-hover:text-blue-400 transition-colors" />
                  </div>
                </div>
              );
            })}
          </div>
        ) : (
          <EmptyState
            icon={CreditCard}
            title="Chưa có giao dịch"
            description="Lịch sử thanh toán của bạn sẽ xuất hiện tại đây."
          />
        )}

        {/* Tạm ẩn nút Xem tất cả giao dịch vì chưa có trang chi tiết
        {!isTxLoading && transactions?.length > 5 && (
          <button className="mt-4 w-full py-3 text-blue-600 font-semibold bg-blue-50/50 hover:bg-blue-100 rounded-xl transition-all active:scale-[0.98] flex items-center justify-center gap-2">
            Xem tất cả giao dịch
            <ArrowRight size={16} />
          </button>
        )}
        */}
      </SettingsSection>

      {/* Subscription Information */}
      <SettingsSection
        title="Gói đăng ký"
        description="Thông tự về gói dịch vụ bạn đang sử dụng"
      >
        <div className={`flex flex-col md:flex-row items-start md:items-center justify-between p-5 border rounded-2xl gap-4 ${
          hasSubscription && !isExpired 
            ? isNearExpiry 
              ? "bg-amber-50/50 border-amber-200" 
              : "bg-blue-50/50 border-blue-200"
            : "bg-slate-50 border-slate-200"
        }`}>
          <div className="flex items-center gap-4">
            <div className={`p-3 rounded-xl ${
              hasSubscription && !isExpired
                ? isNearExpiry 
                  ? "bg-amber-100 text-amber-600" 
                  : "bg-blue-100 text-blue-600"
                : "bg-slate-200 text-slate-500"
            }`}>
              <Crown size={24} />
            </div>
            <div>
              <div className="flex items-center gap-2 mb-1">
                <p className="font-bold text-slate-900 text-lg">
                  {hasSubscription && usage?.packageName ? usage.packageName : "Gói Tiêu chuẩn"}
                </p>
                {hasSubscription ? (
                  isExpired ? (
                    <span className="px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide bg-red-100 text-red-700 rounded-full">
                      Đã hết hạn
                    </span>
                  ) : isNearExpiry ? (
                    <span className="px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide bg-amber-100 text-amber-700 rounded-full flex items-center gap-1">
                      <Clock size={10} /> Còn {daysRemaining} ngày
                    </span>
                  ) : (
                    <span className="px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide bg-emerald-100 text-emerald-700 rounded-full flex items-center gap-1">
                      <Check size={10} /> Đang hoạt động
                    </span>
                  )
                ) : (
                  <span className="px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide bg-slate-200 text-slate-600 rounded-full">
                    Gói Miễn phí
                  </span>
                )}
              </div>
              <p className="text-sm text-slate-500">
                {hasSubscription && usage?.subscriptionEndDate 
                  ? `Hạn dùng: ${format(new Date(usage.subscriptionEndDate), "dd/MM/yyyy", { locale: vi })}` 
                  : "Khám phá các gói Premium để tận hưởng ưu đãi"}
              </p>
            </div>
          </div>
          <button
            onClick={() => navigate("/subscription")}
            className={`px-5 py-2.5 text-sm font-bold rounded-xl transition-all shadow-sm shrink-0 w-full md:w-auto ${
              (!hasSubscription || isExpired)
                ? "bg-blue-600 text-white hover:bg-blue-700 shadow-blue-500/25"
                : "bg-white border border-slate-200 text-slate-700 hover:bg-slate-50"
            }`}
          >
            {(!hasSubscription || isExpired) ? "Nâng cấp Premium" : "Quản lý gói"}
          </button>
        </div>

        {/* Premium CTA */}
        {(!hasSubscription || isExpired) && (
          <div className="mt-4 p-6 bg-linear-to-br from-indigo-50 via-blue-50 to-cyan-50 rounded-2xl border border-blue-100 relative overflow-hidden">
            <div className="absolute top-0 right-0 p-4 opacity-10 rotate-12">
              <Star size={80} className="fill-blue-400 text-blue-400" />
            </div>
            
            <div className="relative z-10">
              <div className="flex items-center gap-2 mb-3">
                <Crown className="text-blue-600" size={24} />
                <h4 className="font-bold text-slate-900">Nâng tầm trải nghiệm với Premium</h4>
              </div>
              
              <ul className="space-y-2 mb-6">
                {[
                  "Không giới hạn chuyến đi & địa điểm",
                  "AI lập kế hoạch hành trình chuyên sâu",
                  "Trải nghiệm không quảng cáo",
                  "Hỗ trợ ưu tiên từ cộng đồng Vivu",
                ].map((feature, i) => (
                  <li
                    key={i}
                    className="flex items-center gap-2 text-sm text-slate-600 font-medium"
                  >
                    <div className="bg-blue-100 rounded-full p-0.5">
                      <Check size={12} className="text-blue-600" />
                    </div>
                    {feature}
                  </li>
                ))}
              </ul>
              
              <button
                onClick={() => navigate("/subscription")}
                className="w-full py-4 bg-gradient-primary text-white font-bold rounded-xl shadow-lg shadow-blue-500/25 hover:shadow-blue-500/40 hover:-translate-y-0.5 active:scale-[0.98] transition-all flex items-center justify-center gap-2"
              >
                Xem các gói nâng cấp
                <ArrowRight size={18} />
              </button>
            </div>
          </div>
        )}
      </SettingsSection>

      {/* Transaction Detail Dialog */}
      <Dialog open={detailOpen} onOpenChange={setDetailOpen}>
        <DialogContent className="bg-white rounded-2xl sm:max-w-md p-0 overflow-hidden">
          {selectedTransaction && (() => {
            const statusInfo = getStatusInfo(selectedTransaction.status);
            return (
              <>
                {/* Header */}
                <div className="relative bg-gradient-to-br from-slate-50 to-blue-50/50 px-6 pt-6 pb-5 border-b border-slate-100">
                  <DialogHeader>
                    <div className="flex items-center gap-3 mb-3">
                      <div className="p-2.5 bg-white rounded-xl shadow-sm border border-slate-200">
                        <Receipt size={20} className="text-blue-600" />
                      </div>
                      <div>
                        <DialogTitle className="text-lg font-bold text-slate-900">
                          Chi tiết giao dịch
                        </DialogTitle>
                        <p className="text-xs text-slate-500 mt-0.5 font-medium">Mã đơn #{selectedTransaction.orderCode}</p>
                      </div>
                    </div>
                  </DialogHeader>

                  {/* Amount highlight */}
                  <div className="mt-2 flex items-center justify-between">
                    <span className="text-3xl font-black text-slate-900 tracking-tight">
                      {formatCurrency(selectedTransaction.amount)}
                    </span>
                    <div className={`flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-bold border ${statusInfo.bgColor || 'bg-slate-50'} ${statusInfo.color} ${statusInfo.borderColor || 'border-slate-200'}`}>
                      {statusInfo.icon}
                      {statusInfo.label}
                    </div>
                  </div>
                </div>

                {/* Detail rows */}
                <div className="px-6 py-5 space-y-4">
                  <DetailRow
                    label="Gói đăng ký"
                    value={selectedTransaction.packageName || "Gói Vivu"}
                  />
                  <DetailRow
                    label="Mã đơn hàng"
                    value={
                      <button
                        onClick={() => handleCopyOrderCode(selectedTransaction.orderCode)}
                        className="flex items-center gap-1.5 font-mono text-sm text-blue-600 hover:text-blue-700 bg-blue-50 hover:bg-blue-100 px-2 py-1 rounded-lg transition-colors"
                      >
                        #{selectedTransaction.orderCode}
                        <Copy size={12} />
                      </button>
                    }
                  />
                  <DetailRow
                    label="Phương thức"
                    value={selectedTransaction.paymentMethod || "PayOS"}
                  />
                  <DetailRow
                    label="Ngày tạo"
                    value={
                      selectedTransaction.createdAt
                        ? format(new Date(selectedTransaction.createdAt), "HH:mm, dd/MM/yyyy", { locale: vi })
                        : "—"
                    }
                  />
                  {selectedTransaction.paymentDate && (
                    <DetailRow
                      label="Ngày thanh toán"
                      value={format(new Date(selectedTransaction.paymentDate), "HH:mm, dd/MM/yyyy", { locale: vi })}
                    />
                  )}
                  {selectedTransaction.description && (
                    <DetailRow
                      label="Mô tả"
                      value={selectedTransaction.description}
                    />
                  )}
                  {selectedTransaction.checkoutUrl && String(selectedTransaction.status).toLowerCase() === "pending" && (
                    <div className="pt-3 border-t border-slate-100">
                      <a
                        href={selectedTransaction.checkoutUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="w-full flex items-center justify-center gap-2 py-3 bg-blue-600 hover:bg-blue-700 text-white font-bold text-sm rounded-xl shadow-sm shadow-blue-500/20 hover:shadow-blue-500/30 transition-all active:scale-[0.98]"
                      >
                        Tiếp tục thanh toán
                        <ExternalLink size={14} />
                      </a>
                    </div>
                  )}
                </div>
              </>
            );
          })()}
        </DialogContent>
      </Dialog>
    </div>
  );
};

/** Reusable detail row for the transaction modal */
const DetailRow = ({ label, value }) => (
  <div className="flex items-start justify-between gap-4">
    <span className="text-sm text-slate-500 font-medium shrink-0">{label}</span>
    <span className="text-sm font-semibold text-slate-900 text-right">{value}</span>
  </div>
);

export default PaymentTab;
