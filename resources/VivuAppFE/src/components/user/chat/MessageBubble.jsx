import React, { useState } from "react";
import { format } from "date-fns";
import { Sparkles, Loader2, AlertTriangle, Trash2 } from "lucide-react";
import { Avatar, AvatarImage, AvatarFallback } from "../../ui/avatar";
import MessageContent from "./MessageContent";
import tripApi from "../../../api/tripApi";
import toast from "../../../utils/toast";
import ConfirmationModal from "../../common/modals/ConfirmationModal";

const parseTripPlanContent = (jsonStr) => {
  try {
    const plan = JSON.parse(jsonStr);
    let content = `✨ **${plan.Title}**\n\n`;
    content += `${plan.Description || ""}\n\n`;
    content += `📅 ${plan.Start || ""} - ${plan.End || ""}\n\n`;

    plan.Days?.forEach((day) => {
      content += `---\n### ${day.Title || `Ngày ${day.DayIndex}`}\n\n`;
      day.Locations?.forEach((loc, idx) => {
        const name = loc.Name || `Địa điểm ${idx + 1}`;
        const time = loc.StartTime ? loc.StartTime.slice(0, 5) : "";
        const desc = loc.Description || "";
        content += `**${idx + 1}. ${name}** ${time ? `(${time})` : ""}\n`;
        if (desc) content += `   ${desc}\n`;
        content += "\n";
      });
    });

    content +=
      "\n✅ Lịch trình đã sẵn sàng! Bạn có thể chỉnh sửa trong phần Chi tiết.";
    return content;
  } catch {
    return jsonStr;
  }
};

const MessageBubble = ({ msg, currentUser, tripData, actualTripId }) => {
  const [isDeleting, setIsDeleting] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);

  const currentUserId = currentUser?.id || currentUser?.Id;
  const ownerId = tripData?.userId || tripData?.UserId;
  const senderId = msg.sender?.id || msg.senderId;
  
  const isTripOwner = ownerId && currentUserId === ownerId;
  const isSender = senderId && currentUserId === senderId;
  const canDelete = isTripOwner || isSender;

  const handleDelete = async () => {
    try {
      setIsDeleting(true);
      await tripApi.deleteChatMessage(actualTripId, msg.id);
      setIsModalOpen(false);
      toast.success("Tin nhắn đã được thu hồi");
      // On success, SignalR will handle the UI removal
    } catch (e) {
      setIsDeleting(false);
      setIsModalOpen(false);
      toast.error("Không thể xóa tin nhắn");
    }
  };

  const renderDeleteTrigger = (marginClass) => (
    <>
      <button
        onClick={() => setIsModalOpen(true)}
        disabled={isDeleting || isModalOpen}
        className={`p-1.5 text-slate-400 hover:text-rose-500 hover:bg-rose-50 opacity-0 group-hover:opacity-100 transition-all rounded-full shrink-0 ${marginClass}`}
        title="Thu hồi tin nhắn"
      >
        <Trash2 size={15} className={isDeleting ? "animate-pulse delay-75" : ""} />
      </button>
      <ConfirmationModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onConfirm={handleDelete}
        title="Thu hồi tin nhắn"
        message="Bạn có chắc chắn muốn thu hồi tin nhắn này không? Tin nhắn sẽ bị xóa vĩnh viễn với tất cả thành viên trong chuyến đi."
        confirmText={isDeleting ? "Đang xóa..." : "Xóa tin nhắn"}
        cancelText="Hủy"
        isDanger={true}
      />
    </>
  );

  if (msg.role === "user") {
    return (
      <div className="space-y-1 group">
        <div className="flex justify-end items-start gap-3 animate-in fade-in slide-in-from-right-2 duration-300">
          {canDelete && !msg.isStreaming && renderDeleteTrigger("mt-1")}
          <div className="flex flex-col items-end gap-1 max-w-[85%]">
            {msg.imageUrl && (
              <div className="mb-1 rounded-xl overflow-hidden bg-white/5 shadow-sm border border-black/5 max-w-[240px] sm:max-w-[320px]">
                <img src={msg.imageUrl} alt={msg.imageFileName || "Uploaded image"} className="w-full h-auto object-cover" loading="lazy" />
              </div>
            )}
            {msg.content && (
              <div className="inline-flex items-center gap-2 px-4 py-2 bg-gradient-primary text-white text-sm font-medium rounded-2xl rounded-tr-sm shadow-md shadow-blue-200/50 wrap-break-word">
                <span>{msg.content}</span>
              </div>
            )}
          </div>
          <Avatar className="w-8 h-8 border-2 border-white shadow-sm shrink-0">
            <AvatarImage src={currentUser?.avatarUrl} />
            <AvatarFallback className="bg-gradient-primary text-white text-[10px]">
              {currentUser?.fullName?.charAt(0) || "U"}
            </AvatarFallback>
          </Avatar>
        </div>
      </div>
    );
  }

  if (msg.role === "member") {
    return (
      <div className="space-y-1 group">
        <div className="flex gap-4 animate-in fade-in slide-in-from-left-2 duration-300 pr-2">
          <div className="shrink-0 flex flex-col items-center">
            <Avatar className="w-8 h-8 border-2 border-white shadow-sm hover:scale-110 transition-transform">
              <AvatarImage src={msg.sender?.avatarUrl} />
              <AvatarFallback className="bg-slate-200 text-slate-600 text-[10px]">
                {msg.sender?.fullName?.charAt(0) || "U"}
              </AvatarFallback>
            </Avatar>
          </div>

          <div className="space-y-1 max-w-[85%]">
            <div className="flex items-baseline gap-2 pl-1">
              <span className="text-xs font-semibold text-gray-600">
                {msg.sender?.fullName || "Thành viên"}
              </span>
              {msg.timestamp && (
                <span className="text-[10px] text-gray-400">
                  {format(new Date(msg.timestamp), "HH:mm")}
                </span>
              )}
            </div>

            {msg.imageUrl && (
              <div className="mb-1 rounded-2xl rounded-tl-sm overflow-hidden bg-white shadow-sm border border-black/5 max-w-[240px] sm:max-w-[320px]">
                <img src={msg.imageUrl} alt={msg.imageFileName || "Uploaded image"} className="w-full h-auto object-cover" loading="lazy" />
              </div>
            )}
            {msg.content && (
              <div className="bg-white border border-gray-100/50 rounded-2xl rounded-tl-sm px-4 py-2.5 text-gray-700 shadow-sm text-sm leading-relaxed">
                {msg.content}
              </div>
            )}
          </div>

          {canDelete && !msg.isStreaming && renderDeleteTrigger("mt-6")}
        </div>
      </div>
    );
  }

  if (msg.role === "assistant") {
    const isErrorMsg = msg.isError || (msg.content && msg.content.startsWith("❌"));
    return (
      <div className="space-y-1">
        <div className="flex gap-4 group animate-in fade-in slide-in-from-left-2 duration-300 pr-12">
          <div className="shrink-0">
            {isErrorMsg ? (
              <div className="w-8 h-8 rounded-full bg-rose-100 flex items-center justify-center text-rose-500 shadow-md ring-2 ring-white">
                <AlertTriangle size={16} />
              </div>
            ) : (
              <div className="w-8 h-8 rounded-full bg-gradient-primary flex items-center justify-center text-white shadow-md ring-2 ring-white">
                <Sparkles size={16} />
              </div>
            )}
          </div>

          <div className="space-y-1 flex-1">
            <div className="flex items-baseline gap-2 pl-1">
              <span className={`text-xs font-bold ${isErrorMsg ? "text-rose-500" : "text-transparent bg-clip-text bg-gradient-primary"}`}>
                {isErrorMsg ? "Lỗi" : "Vivu AI"}
              </span>
            </div>

            <div className={`relative pl-4 border-l-2 ml-1 ${isErrorMsg ? "border-rose-300/50" : "border-primary/30"}`}>
              {msg.isStreaming ? (
                <div className="leading-relaxed text-gray-800">
                  <MessageContent content={msg.content} />
                  <span className="inline-block w-1.5 h-4 bg-primary ml-1 animate-pulse rounded-full"></span>
                  {msg.status && (
                    <div className="flex items-center gap-2 text-sm text-primary font-medium animate-pulse mt-2">
                      <Loader2 size={16} className="animate-spin" />
                      <span>{msg.status}</span>
                    </div>
                  )}
                </div>
              ) : (
                <div className={isErrorMsg ? "text-gray-700 bg-rose-50/50 rounded-lg p-2 -ml-2" : "text-gray-800"}>
                  <MessageContent
                    content={
                      msg.messageType === "trip_plan"
                        ? parseTripPlanContent(msg.content)
                        : msg.content
                    }
                  />
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    );
  }

  return null;
};

// Compare correctly for React.memo
const areEqual = (prevProps, nextProps) => {
  return (
    prevProps.msg.id === nextProps.msg.id &&
    prevProps.msg.content === nextProps.msg.content &&
    prevProps.msg.isStreaming === nextProps.msg.isStreaming &&
    prevProps.msg.status === nextProps.msg.status &&
    prevProps.msg.isError === nextProps.msg.isError
  );
};

export default React.memo(MessageBubble, areEqual);
