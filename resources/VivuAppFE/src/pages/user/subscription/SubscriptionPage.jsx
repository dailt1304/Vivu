import React, { useState } from "react";
import { Check, Star, Zap, Shield, Crown } from "lucide-react";
import AppNavbar from "../../../components/layout/AppNavbar";
import {
  useActivePlans,
  useSubscriptionMutation,
  useMySubscription
} from "../../../hooks/subscriptions/useSubscriptions";
import { useCreatePaymentLink } from "../../../hooks/payments/usePayments";
import toast from "@/utils/toast";

const SubscriptionPage = () => {
  const { plans: plansData, isLoading, isError: error } = useActivePlans();
  const { subscribe, isSubscribing } = useSubscriptionMutation();
  const { createPaymentLink, isCreating } = useCreatePaymentLink();
  const { hasSubscription, isExpired, subscription } = useMySubscription();

  const [selectPlanId, setSelectPlanId] = useState(null);

  // Static Free plan — always shown, not fetched from API
  const FREE_PLAN = {
    id: "free-tier",
    name: "Cơ bản",
    type: "FREE",
    price: 0,
    durationDays: 0,
    maxAiRequestPerDay: 5,
    description:
      "Khám phá Vivu hoàn toàn miễn phí. Lên kế hoạch chuyến đi đầu tiên cùng AI — không cần thẻ, không cam kết.",
    isRecommended: false,
    isFree: true,
  };

  // Map Backend Packages to UI structure
  const rawPlans = Array.isArray(plansData)
    ? plansData
    : plansData?.items || [];

  // Sort plans: Recommended (middle of the 3 items), then by duration
  const sortedPlans = [...rawPlans].sort((a, b) => {
    if (a.isRecommended && !b.isRecommended) return -1;
    if (!a.isRecommended && b.isRecommended) return 1;
    return (a.durationDays || 0) - (b.durationDays || 0);
  });
  
  const displayPlans = [FREE_PLAN, ...sortedPlans];

  const getIcon = (type) => {
    const t = (type || "").toString().toUpperCase();
    if (t === "PRO" || t === "3")
      return <Crown size={20} className="text-yellow-500 fill-yellow-500" />;
    if (t === "PREMIUM" || t === "2")
      return <Zap size={20} className="text-blue-500 fill-blue-500" />;
    return <Star size={20} className="text-gray-400 fill-gray-400" />;
  };

  const getFeatures = (plan) => {
    if (plan.isFree) {
      return [
        "5 lượt hỏi AI mỗi ngày",
        "Tối đa 5 chuyến đi",
        "Lên lịch trình tự động bằng AI",
        "Chia sẻ & khám phá trải nghiệm",
      ];
    }

    const features = [
      `${plan.maxAiRequestPerDay || 5} lượt AI / ngày`,
    ];

    features.push(
      "Tạo chuyến đi không giới hạn",
      "Trải nghiệm không quảng cáo",
      "Hỗ trợ ưu tiên từ cộng đồng Vivu"
    );

    return features;
  };

  const handleSubscribe = async (plan) => {
    // Hardcoded free tier — no API call needed
    if (plan.isFree) {
      toast.success("Bạn đang sử dụng gói Cơ bản miễn phí!");
      return;
    }
    setSelectPlanId(plan.id);
    try {
      if (plan.price === 0) {
        // Gói miễn phí -> subscribe trực tiếp
        const response = await subscribe(plan.id);
        if (response.success) {
          toast.success(
            `Chúc mừng! Bạn đã đăng ký gói ${response.data?.packageName || "thành công"}.`,
          );
        } else {
          toast.error(
            response.message || "Đăng ký không thành công. Vui lòng thử lại.",
          );
        }
      } else {
        // Gói trả phí -> tạo payment link -> redirect PayOS
        const response = await createPaymentLink(plan.id);
        if (response.success && response.data?.checkoutUrl) {
          // Vercel Rule: rerender-move-effect-to-event
          window.location.href = response.data.checkoutUrl;
        } else {
          toast.error(
            response.message || "Không thể tạo link thanh toán. Vui lòng thử lại.",
          );
        }
      }
    } catch (err) {
      console.error("Subscription/Payment failed:", err);
      const errorMsg =
        err.response?.data?.message ||
        err.response?.data?.error?.message ||
        "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.";
      toast.error(errorMsg);
    } finally {
      setSelectPlanId(null);
    }
  };

  const isPending = isSubscribing || isCreating;

  return (
    <div className="min-h-screen bg-gray-50 flex flex-col">
      <AppNavbar />

      <div className="flex-1 py-16 px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="text-center max-w-3xl mx-auto mb-16">
          <span className={`inline-block py-1 px-3 rounded-full text-xs font-bold uppercase tracking-wider mb-4 ${hasSubscription && !isExpired ? "bg-emerald-50 text-emerald-600" : "bg-blue-50 text-blue-600"}`}>
            {hasSubscription && !isExpired ? "Quản lý gói cước" : "Nâng cấp tài khoản"}
          </span>
          <h1 className="text-4xl md:text-5xl font-black text-gray-900 mb-6 tracking-tight">
            {hasSubscription && !isExpired ? (
              <>
                Thông tin gói hành trình <br />
                <span className="text-transparent bg-clip-text bg-gradient-to-r from-emerald-600 to-teal-500">
                  hiện tại của bạn.
                </span>
              </>
            ) : (
              <>
                Chọn gói hành trình <br />
                <span className="text-transparent bg-clip-text bg-linear-to-r from-blue-600 to-cyan-500">
                  phù hợp với bạn.
                </span>
              </>
            )}
          </h1>
          <p className="text-xl text-gray-500 max-w-2xl mx-auto">
            {hasSubscription && !isExpired
              ? "Xem lại chi tiết các quyền lợi và thời hạn sử dụng gói cước bạn đang đăng ký."
              : "Mở khóa toàn bộ tính năng và tận hưởng những chuyến đi trọn vẹn nhất cùng Vivu."}
          </p>
        </div>

        {/* Subscription Status Banner */}
        {hasSubscription && (
           <div className={`max-w-4xl mx-auto mb-12 p-4 rounded-2xl border flex items-center justify-center gap-3 text-center shadow-sm
              ${isExpired 
                ? "bg-red-50 border-red-200 text-red-800" 
                : "bg-blue-50 border-blue-200 text-blue-900"
              }
           `}>
             {isExpired ? (
               <>
                 <div className="bg-red-100 p-2 rounded-full"><Zap size={20} className="text-red-600" /></div>
                 <div>
                   <p className="font-bold">Gói {subscription.packageName || "Premium"} của bạn đã hết hạn.</p>
                   <p className="text-sm text-red-600/80 mt-0.5">Vui lòng chọn mua một gói mới bên dưới để tiếp tục trải nghiệm!</p>
                 </div>
               </>
             ) : (
               <>
                 <div className="bg-blue-100 p-2 rounded-full"><Crown size={20} className="text-blue-600" /></div>
                 <div>
                   <p className="font-bold text-lg">Bạn đang sử dụng gói {subscription.packageName || "Premium"}</p>
                   <p className="text-sm opacity-80 mt-1">Hết hạn vào: <span className="font-bold">{new Date(subscription.subscriptionEndDate).toLocaleDateString('vi-VN')}</span>. Bạn cần chờ gói hiện tại hết hạn để có thể mua gói mới.</p>
                 </div>
               </>
             )}
           </div>
        )}

        {/* Plans Grid */}
        <div
          className="max-w-7xl mx-auto flex flex-wrap justify-center items-stretch gap-8"
        >
          {isLoading ? (
            <div className="w-full py-20 text-center">
              Đang tải danh sách gói...
            </div>
          ) : error ? (
            <div className="w-full py-20 text-center text-red-500">
              Lỗi khi tải dữ liệu. Vui lòng thử lại.
            </div>
          ) : (
            displayPlans.map((plan) => {
              const isPremium = plan.isRecommended;
              const isCurrentPlan = hasSubscription && !isExpired && (subscription?.packageId === plan.id || subscription?.packageName === plan.name);
              return (
                <div
                  key={plan.id}
                  className={`w-full md:w-[380px] relative rounded-3xl p-8 transition-all duration-500 flex flex-col group
                                  ${
                                    isPremium
                                      ? "bg-white border-2 border-blue-500 shadow-2xl shadow-blue-500/20 md:scale-110 z-10 text-slate-900"
                                      : "bg-white/80 backdrop-blur-xl border border-slate-200 hover:shadow-xl hover:bg-white text-slate-800 mt-2 md:mt-0"
                                  }
                              `}
                >
                  {isPremium && (
                    <div className="absolute top-0 left-1/2 -translate-x-1/2 -translate-y-1/2 bg-linear-to-r from-blue-500 to-cyan-400 text-white px-4 py-1.5 rounded-full text-xs font-black uppercase tracking-wider shadow-lg shadow-blue-500/30 flex items-center gap-1.5">
                      <Star size={14} fill="currentColor" /> Phổ biến nhất
                    </div>
                  )}

                  <div className="mb-8">
                    <h3
                      className={`text-xl font-bold mb-2 flex items-center gap-2 ${isPremium ? "text-blue-600" : "text-slate-900"}`}
                    >
                      {plan.name}
                      {getIcon(plan.type)}
                    </h3>
                    <div className="flex items-baseline gap-1">
                      <span
                        className="text-4xl md:text-5xl font-black tracking-tight text-slate-900"
                      >
                        {plan.price === 0
                          ? "Miễn phí"
                          : `${plan.price.toLocaleString()}đ`}
                      </span>
                      {plan.price > 0 && (
                        <span
                          className="text-sm font-semibold text-slate-500"
                        >
                          /
                          {plan.durationDays === 30
                            ? "tháng"
                            : plan.durationDays === 365
                              ? "năm"
                              : `${plan.durationDays} ngày`}
                        </span>
                      )}
                    </div>
                    <p
                      className="mt-4 text-sm leading-relaxed min-h-[40px] font-medium text-slate-500"
                    >
                      {plan.description}
                    </p>
                  </div>

                  <ul className="space-y-4 mb-8 flex-1">
                    {getFeatures(plan).map((feature, idx) => (
                      <li
                        key={idx}
                        className={`flex items-start gap-3 text-sm font-medium ${isPremium ? "text-slate-700" : "text-slate-600"}`}
                      >
                        <div
                          className={`mt-0.5 p-0.5 rounded-full ${isPremium ? "bg-blue-100 text-blue-600" : "bg-slate-100 text-slate-500"}`}
                        >
                          <Check size={12} strokeWidth={3} />
                        </div>
                        {feature}
                      </li>
                    ))}
                  </ul>

                  <button
                    onClick={() => handleSubscribe(plan)}
                    disabled={
                      plan.isFree || (isPending && selectPlanId === plan.id) || (hasSubscription && !isExpired)
                    }
                    className={`w-full py-4 rounded-xl font-bold text-sm transition-all duration-300 transform active:scale-95 flex items-center justify-center
                                      ${
                                        isCurrentPlan
                                          ? "bg-slate-200 text-slate-500 cursor-not-allowed opacity-80"
                                          : (hasSubscription && !isExpired)
                                            ? "bg-slate-100 text-slate-400 cursor-not-allowed opacity-60"
                                            : plan.isFree
                                              ? "bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200 cursor-default"
                                              : isPremium
                                                ? "bg-gradient-primary text-white shadow-xl shadow-blue-500/40 hover:shadow-2xl hover:shadow-blue-500/60 hover:-translate-y-1 cursor-pointer"
                                                : "bg-slate-100 text-slate-900 hover:bg-slate-200 cursor-pointer"
                                      }
                                      ${
                                        isPending &&
                                        selectPlanId === plan.id
                                          ? "opacity-70 cursor-not-allowed"
                                          : ""
                                      }
                                  `}
                  >
                    {isCurrentPlan ? (
                      <div className="flex items-center gap-2"><Check size={16} /> Đang sử dụng</div>
                    ) : (hasSubscription && !isExpired) ? (
                      <div className="flex items-center gap-2"> Đã có gói kích hoạt</div>
                    ) : plan.isFree ? (
                      <div className="flex items-center gap-2"><Check size={16} /> Gói hiện tại</div>
                    ) : isPending && selectPlanId === plan.id ? (
                      <div className="flex items-center gap-2">
                        <div
                          className={`w-4 h-4 border-2 border-t-transparent rounded-full animate-spin ${isPremium ? "border-white/30" : "border-slate-400"}`}
                        />
                        Đang xử lý...
                      </div>
                    ) : (
                      "Nâng cấp ngay"
                    )}
                  </button>
                </div>
              );
            })
          )}
        </div>

        {/* FAQ / Trust */}
        <div className="mt-24 text-center border-t border-gray-200 pt-16 max-w-4xl mx-auto">
          <p className="flex items-center justify-center gap-2 text-gray-400 font-medium mb-8">
            <Shield size={18} /> Thanh toán an toàn & bảo mật
          </p>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-6 opacity-60 grayscale hover:grayscale-0 transition-all duration-500">
            {/* Mock Payment Logos */}
            <div className="h-10 bg-gray-200 rounded flex items-center justify-center text-xs font-bold text-gray-400">
              MOMO
            </div>
            <div className="h-10 bg-gray-200 rounded flex items-center justify-center text-xs font-bold text-gray-400">
              ZALOPAY
            </div>
            <div className="h-10 bg-gray-200 rounded flex items-center justify-center text-xs font-bold text-gray-400">
              VISA/MASTER
            </div>
            <div className="h-10 bg-gray-200 rounded flex items-center justify-center text-xs font-bold text-gray-400">
              ATM
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default SubscriptionPage;
