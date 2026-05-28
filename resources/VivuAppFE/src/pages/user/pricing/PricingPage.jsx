import React, { useState } from "react";
import { Check, Star, Zap, Shield, Crown, Loader2 } from "lucide-react";
import AppNavbar from "@/components/layout/AppNavbar";
import { useActivePlans } from "@/hooks/subscriptions/useSubscriptions";
import { useNavigate } from "react-router-dom";

const PricingPage = () => {
  const [billingCycle, setBillingCycle] = useState("monthly"); // 'monthly' | 'yearly'
  const navigate = useNavigate();
  const { plans, isLoading } = useActivePlans();

  const handleSubscribe = () => {
    navigate("/subscription");
  };

  const formatPrice = (price, cycle) => {
    if (price === 0) return "Miễn phí";
    const displayPrice = cycle === "yearly" ? price * 0.8 : price;
    return `${Math.round(displayPrice).toLocaleString()}đ`;
  };

  return (
    <div className="min-h-screen bg-gray-50 flex flex-col font-sans">
      <AppNavbar />

      <div className="flex-1 py-16 px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="text-center max-w-3xl mx-auto mb-16">
          <span className="inline-block py-1 px-4 rounded-full bg-blue-50 text-blue-600 text-xs font-bold uppercase tracking-wider mb-4 border border-blue-100">
            Nâng cấp hành trình của bạn
          </span>
          <h1 className="text-4xl md:text-5xl font-black text-slate-900 mb-6 tracking-tight leading-tight">
            Chọn gói trải nghiệm <br />
            <span className="text-transparent bg-clip-text bg-linear-to-r from-blue-600 to-indigo-500">
              xứng đáng nhất với bạn.
            </span>
          </h1>
          <p className="text-lg text-slate-500 max-w-2xl mx-auto">
            Mở khóa những tính năng AI thông minh nhất và tận hưởng những chuyến
            đi trọn vẹn không giới hạn.
          </p>

          {/* Toggle */}
          <div className="flex justify-center items-center mt-10 gap-5 bg-white p-2 rounded-2xl w-fit mx-auto shadow-sm border border-slate-100">
            <button
              onClick={() => setBillingCycle("monthly")}
              className={`px-6 py-2 rounded-xl text-sm font-bold transition-all ${billingCycle === "monthly" ? "bg-slate-900 text-white shadow-md" : "text-slate-500 hover:bg-slate-50"}`}
            >
              Hàng tháng
            </button>
            <button
              onClick={() => setBillingCycle("yearly")}
              className={`px-6 py-2 rounded-xl text-sm font-bold transition-all flex items-center gap-2 ${billingCycle === "yearly" ? "bg-slate-900 text-white shadow-md" : "text-slate-500 hover:bg-slate-50"}`}
            >
              Hàng năm
              <span className="bg-green-100 text-green-600 text-[10px] px-2 py-0.5 rounded-full font-black">
                -20%
              </span>
            </button>
          </div>
        </div>

        {/* Plans Grid */}
        <div className="max-w-7xl mx-auto">
          {isLoading ? (
            <div className="flex justify-center items-center py-20">
              <Loader2 className="w-10 h-10 animate-spin text-blue-600" />
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8 items-stretch">
              {plans.map((plan) => (
                <div
                  key={plan.id}
                  className={`relative rounded-[2.5rem] p-10 transition-all duration-500 flex flex-col group
                                        ${
                                          plan.isRecommended
                                            ? "bg-white shadow-[0_20px_50px_rgba(0,0,0,0.1)] ring-2 ring-blue-500 scale-105 z-10"
                                            : "bg-white/80 backdrop-blur-sm border border-slate-200 hover:border-blue-300 hover:shadow-xl"
                                        }
                                    `}
                >
                  {plan.isRecommended && (
                    <div className="absolute top-0 left-1/2 -translate-x-1/2 -translate-y-1/2 bg-linear-to-r from-blue-600 to-indigo-600 text-white px-6 py-1.5 rounded-full text-xs font-black shadow-lg flex items-center gap-2 uppercase tracking-widest">
                      <Star size={14} fill="currentColor" /> Được đề xuất
                    </div>
                  )}

                  <div className="mb-8">
                    <h3 className="text-2xl font-black text-slate-800 mb-3 flex items-center gap-3">
                      {plan.type === "PRO" ? (
                        <Crown
                          size={24}
                          className="text-amber-500 fill-amber-500"
                        />
                      ) : plan.type === "PREMIUM" ? (
                        <Zap
                          size={24}
                          className="text-blue-500 fill-blue-500"
                        />
                      ) : (
                        <Star
                          size={24}
                          className="text-slate-400 fill-slate-400"
                        />
                      )}
                      {plan.name}
                    </h3>
                    <div className="flex items-baseline gap-2">
                      <span className="text-5xl font-black text-slate-900 tracking-tighter">
                        {formatPrice(plan.price, billingCycle)}
                      </span>
                      {plan.price > 0 && (
                        <span className="text-slate-400 font-medium text-sm">
                          /{billingCycle === "monthly" ? "tháng" : "năm"}
                        </span>
                      )}
                    </div>
                    <p className="text-slate-500 mt-6 text-sm leading-relaxed font-medium">
                      {plan.description}
                    </p>
                  </div>

                  <div className="h-px bg-slate-100 w-full mb-8"></div>

                  <ul className="space-y-4 mb-10 flex-1">
                    {/* AI Quota feature */}
                    <li className="flex items-start gap-4 text-sm text-blue-600 font-bold group-hover:text-blue-700 transition-colors">
                      <div className="mt-0.5 p-1 rounded-full bg-blue-100">
                        <Zap size={14} fill="currentColor" />
                      </div>
                      {plan.maxAiRequestPerDay} lượt yêu cầu AI mỗi ngày
                    </li>

                    {(plan.features || []).map((feature, idx) => (
                      <li
                        key={idx}
                        className="flex items-start gap-4 text-sm text-slate-600 font-semibold group-hover:text-slate-900 transition-colors"
                      >
                        <div
                          className={`mt-0.5 p-1 rounded-full ${plan.isRecommended ? "bg-blue-100 text-blue-600" : "bg-slate-100 text-slate-400"}`}
                        >
                          <Check size={14} strokeWidth={4} />
                        </div>
                        {feature}
                      </li>
                    ))}
                  </ul>

                  <button
                    onClick={handleSubscribe}
                    disabled={plan.price === 0}
                    className={`w-full py-5 rounded-2xl font-black text-base transition-all duration-300 transform active:scale-[0.98] flex items-center justify-center gap-2
                                            ${
                                              plan.isRecommended
                                                ? "bg-linear-to-r from-blue-600 to-indigo-600 text-white shadow-[0_10px_20px_rgba(37,99,235,0.3)] hover:shadow-[0_15px_30px_rgba(37,99,235,0.4)] hover:-translate-y-1"
                                                : plan.price === 0
                                                  ? "bg-slate-100 text-slate-400 cursor-default"
                                                  : "bg-slate-900 text-white hover:bg-slate-800 shadow-md hover:-translate-y-1"
                                            }
                                        `}
                  >
                    {plan.price === 0 ? "Gói mặc định" : "Nâng cấp ngay"}
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Trust Section */}
        <div className="mt-32 text-center max-w-4xl mx-auto bg-white/50 backdrop-blur-md rounded-[3rem] p-12 border border-slate-100 shadow-sm">
          <p className="flex items-center justify-center gap-3 text-slate-400 font-bold mb-10 uppercase tracking-widest text-xs">
            <Shield size={20} className="text-green-500" /> Thanh toán được bảo
            mật 256-bit
          </p>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-8 grayscale opacity-50 contrast-125">
            {/* Payment Logos Placeholder */}
            <div className="flex flex-col items-center">
              <div className="h-10 w-24 bg-slate-200 rounded-lg mb-2"></div>
              <span className="text-[10px] font-black tracking-tighter">
                MOMO
              </span>
            </div>
            <div className="flex flex-col items-center">
              <div className="h-10 w-24 bg-slate-200 rounded-lg mb-2"></div>
              <span className="text-[10px] font-black tracking-tighter">
                ZALOPAY
              </span>
            </div>
            <div className="flex flex-col items-center">
              <div className="h-10 w-24 bg-slate-200 rounded-lg mb-2"></div>
              <span className="text-[10px] font-black tracking-tighter">
                VISA / MASTER
              </span>
            </div>
            <div className="flex flex-col items-center">
              <div className="h-10 w-24 bg-slate-200 rounded-lg mb-2"></div>
              <span className="text-[10px] font-black tracking-tighter">
                ATM BANKING
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default PricingPage;
