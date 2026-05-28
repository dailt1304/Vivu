import React, { useState } from "react";
import { PlusCircle, Mic, Send, Map, Calendar, Sparkles } from "lucide-react";
import { useNavigate } from "react-router-dom";

import TripPreferencesModal from "./TripPreferencesModal";
import TripDetailsModal from "./TripDetailsModal";
import { useUserUsage } from "../../../hooks/users/useUsers";
import AIUsageIndicator from "./AIUsageIndicator";
import toast from "../../../utils/toast";

const ChatWindow = () => {
  const navigate = useNavigate();
  const [inputText, setInputText] = useState("");

  // Preferences State
  const [showPreferences, setShowPreferences] = useState(false);
  const [showTripDetails, setShowTripDetails] = useState(false); // New state for Trip Details
  const [userPreferences, setUserPreferences] = useState(null);

  const { usage } = useUserUsage();
  const isExhausted = usage?.hasActiveSubscription === false && usage?.remaining === 0;

  const handleSend = async () => {
    if (!inputText.trim() || isExhausted) return;

    const userPrompt = inputText.trim(); // Store for redirect

    let finalPrompt = userPrompt;
    if (userPreferences) {
      const prefText = `
        [Thông tin bổ sung từ người dùng]:
        - Ngân sách: ${userPreferences.budget}
        - Đồng hành: ${userPreferences.companions} ${userPreferences.memberCount ? `(${userPreferences.memberCount} người)` : ""}
        - Sở thích: ${userPreferences.interests.join(", ")}
        - Nhịp độ: ${userPreferences.pace}
        `;
      finalPrompt += prefText;
    }

    // Save prompt for TripDetailPage to consume
    sessionStorage.setItem("vivu_generating_prompt", finalPrompt);
    navigate("/trips/new?generating=true");
  };

  return (
    <div className="flex flex-col relative h-full bg-transparent">
      {/* Messages Area - Removed Header, Expanded Content */}
      <div className="flex-1 overflow-y-auto p-6 scrollbar-hide">
        <div className="h-full flex flex-col items-center justify-center text-center space-y-10 pb-40">
          <div className="space-y-2 max-w-lg">
            <h1 className="text-2xl md:text-4xl font-extrabold text-transparent bg-clip-text bg-gradient-to-r from-gray-900 to-gray-700 tracking-tight leading-tight">
              Bạn muốn lên kế hoạch
              <br /> đi đâu hôm nay?
            </h1>
            <p className="text-gray-500 text-sm md:text-base font-medium">
              Vivu AI sẵn sàng hỗ trợ bạn tìm địa điểm, lên lịch trình và nhiều
              hơn nữa.
            </p>
          </div>

          <div className="grid grid-cols-2 gap-3 max-w-md w-full px-4">
            {[
              {
                icon: <Calendar size={16} />,
                label: "Lên kế hoạch",
                action: () => setShowPreferences(true),
              },
              {
                icon: <Map size={16} />,
                label: "Gợi ý lịch trình",
                action: () => setShowTripDetails(true),
              },
            ].map((item, i) => (
              <button
                key={i}
                onClick={item.action}
                className="flex items-center gap-2 p-3 bg-white border border-gray-100 rounded-xl hover:border-blue-200 hover:shadow-md hover:bg-blue-50/30 transition-all text-sm font-medium text-gray-600 hover:text-blue-600 text-left group"
              >
                <span className="p-1.5 rounded-lg bg-gray-50 group-hover:bg-blue-100 transition-colors text-gray-500 group-hover:text-blue-600">
                  {item.icon}
                </span>
                {item.label}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Input Area - Floating & Clean */}
      {/* Floating Input Area */}
      <div className="p-4 md:p-6 w-full z-20">
        <div className="max-w-3xl mx-auto">
          <div className="relative group">
            <div className="absolute -inset-1 bg-gradient-to-r from-blue-400/20 to-indigo-400/20 rounded-2xl blur opacity-0 group-hover:opacity-100 transition duration-700"></div>
            <form
              onSubmit={(e) => {
                e.preventDefault();
                handleSend();
              }}
              className="relative flex items-center gap-2 bg-white/90 backdrop-blur-sm rounded-2xl border border-gray-200/80 shadow-lg shadow-gray-200/50 p-2 pl-4 hover:shadow-xl hover:border-blue-300/50 transition-all"
            >
              <button
                type="button"
                className="p-2.5 text-gray-400 hover:text-blue-600 hover:bg-blue-50 rounded-xl transition-colors"
              >
                <PlusCircle size={20} />
              </button>

              <input
                type="text"
                value={inputText}
                onChange={(e) => setInputText(e.target.value)}
                placeholder={isExhausted ? "Đã hết lượt AI hôm nay" : "Hỏi Vivu bất cứ điều gì..."}
                disabled={isExhausted}
                className="flex-1 bg-transparent border-none outline-none text-gray-700 placeholder-gray-400/80 font-medium py-3 disabled:opacity-60 disabled:cursor-not-allowed"
              />

              <div className="flex items-center gap-1 pr-1">
                <button
                  type="button"
                  className="p-2.5 text-gray-400 hover:text-gray-700 hover:bg-gray-100 rounded-xl transition-colors"
                >
                  <Mic size={20} />
                </button>
                <button
                  type="submit"
                  disabled={!inputText.trim() || isExhausted}
                  className="p-3 bg-gradient-primary text-white rounded-xl shadow-md shadow-blue-200 disabled:opacity-50 disabled:shadow-none hover:shadow-lg hover:scale-105 active:scale-95 transition-all duration-200"
                >
                  <Send size={18} fill="currentColor" strokeWidth={2.5} />
                </button>
              </div>
            </form>
          </div>
          <div className="mt-4 w-full">
            <AIUsageIndicator usage={usage} />
          </div>
        </div>
      </div>

      <TripPreferencesModal
        isOpen={showPreferences}
        onClose={() => setShowPreferences(false)}
        onSubmit={(prefs) => {
          setUserPreferences(prefs);
          toast.success("Đã ghi nhận sở thích cho chuyến đi sắp tới!");
        }}
      />

      <TripDetailsModal
        isOpen={showTripDetails}
        onClose={() => setShowTripDetails(false)}
        onSubmit={(dest, duration) => {
          // Draft Mode: Set input text but don't send
          const prompt = `Lên lịch trình đi ${dest} trong ${duration}`;
          setInputText(prompt);
          // Optional: Focus the input field if possible (ref needed) or just let user click
        }}
      />
    </div>
  );
};

export default ChatWindow;
