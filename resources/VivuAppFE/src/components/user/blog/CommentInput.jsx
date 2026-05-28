import React, { useState, useRef } from "react";
import { Send, Smile } from "lucide-react";
import PropTypes from "prop-types";
import EmojiPicker, { Theme, SuggestionMode } from "emoji-picker-react";
import { Popover, PopoverContent, PopoverTrigger } from "../../ui/popover";

/**
 * Comment input component
 */
const CommentInput = ({
  onSubmit,
  placeholder = "Viết bình luận...",
  userAvatar,
  userName = "Người dùng",
}) => {
  const [text, setText] = useState("");
  const [imgError, setImgError] = useState(false);
  const [showEmojiPicker, setShowEmojiPicker] = useState(false);
  const textareaRef = useRef(null);

  const onEmojiClick = (emojiData) => {
    const textarea = textareaRef.current;
    if (!textarea) return;

    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const emoji = emojiData.emoji;

    const newText = text.substring(0, start) + emoji + text.substring(end);
    setText(newText);

    // Reset height after insertion
    setTimeout(() => {
      textarea.style.height = "48px";
      textarea.style.height = textarea.scrollHeight + "px";
      // Focus back and move cursor after emoji
      textarea.focus();
      textarea.setSelectionRange(start + emoji.length, start + emoji.length);
    }, 0);

    // Closce picker after select (optional, but usually better UX)
    setShowEmojiPicker(false);
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    if (text.trim()) {
      onSubmit(text);
      setText("");
    }
  };

  const getInitials = () => {
    if (userName && userName !== "Người dùng" && userName !== "Khách") {
      return userName.charAt(0).toUpperCase();
    }
    // Fallback: Lấy chữ cái đầu từ email nếu có trong userName
    if (userName && userName.includes("@")) {
      return userName.charAt(0).toUpperCase();
    }
    return null;
  };

  const initials = getInitials();

  return (
    <form onSubmit={handleSubmit} className="flex gap-4 items-start">
      {/* User Avatar Section */}
      <div className="shrink-0 mt-1">
        {userAvatar && !imgError ? (
          <img
            src={userAvatar}
            alt={userName}
            referrerPolicy="no-referrer"
            onError={() => setImgError(true)}
            className="w-10 h-10 rounded-full object-cover ring-2 ring-white shadow-sm"
          />
        ) : initials ? (
          <div className="w-10 h-10 rounded-full bg-gradient-primary flex items-center justify-center text-white font-bold text-sm shadow-sm ring-2 ring-white">
            {initials}
          </div>
        ) : (
          <div className="w-10 h-10 rounded-full bg-gray-100 flex items-center justify-center text-gray-400 shadow-sm ring-2 ring-white">
            <Smile size={20} />
          </div>
        )}
      </div>

      {/* Input Box Section */}
      <div className="flex-1 bg-gray-50 border border-gray-200 rounded-2xl focus-within:ring-2 focus-within:ring-blue-100 focus-within:border-blue-500 transition-all relative">
        <textarea
          ref={textareaRef}
          value={text}
          onChange={(e) => setText(e.target.value)}
          placeholder={placeholder}
          rows={1}
          className="w-full px-4 py-3 bg-transparent text-sm focus:outline-none resize-none transition-all placeholder:text-gray-400 rounded-t-2xl"
          style={{ minHeight: "48px" }}
          onInput={(e) => {
            e.target.style.height = "48px";
            e.target.style.height = e.target.scrollHeight + "px";
          }}
          onKeyDown={(e) => {
            if (e.key === "Enter" && !e.shiftKey) {
              e.preventDefault();
              handleSubmit(e);
            }
          }}
        />

        {/* Desktop & Mobile Actions Row */}
        <div className="px-3 pb-2 flex items-center justify-between border-t border-gray-100/50 bg-gray-50/50 rounded-b-2xl">
          <div className="flex items-center gap-1">
            <div className="relative emoji-picker-container">
              <Popover open={showEmojiPicker} onOpenChange={setShowEmojiPicker}>
                <PopoverTrigger asChild>
                  <button
                    type="button"
                    className={`p-2 transition-colors rounded-xl hover:bg-white active:scale-90 ${
                      showEmojiPicker
                        ? "text-blue-500 bg-white shadow-sm"
                        : "text-gray-400 hover:text-blue-500"
                    }`}
                  >
                    <Smile size={20} />
                  </button>
                </PopoverTrigger>
                <PopoverContent
                  side="top"
                  align="start"
                  sideOffset={12}
                  className="w-auto p-0 border-none bg-transparent shadow-2xl rounded-2xl z-9999"
                >
                  <div className="emoji-picker-wrapper">
                    <EmojiPicker
                      onEmojiClick={onEmojiClick}
                      autoFocusSearch={false}
                      theme={Theme.LIGHT}
                      searchPlaceHolder="Tìm kiếm biểu tượng..."
                      previewConfig={{ showPreview: false }}
                      skinTonesDisabled
                      suggestionMode={SuggestionMode.RECENT}
                      width={320}
                      height={320}
                      categories={[
                        { category: "suggested", name: "Gợi ý" },
                        {
                          category: "smileys_people",
                          name: "Mặt cười & Người",
                        },
                        {
                          category: "animals_nature",
                          name: "Động vật & Thiên nhiên",
                        },
                        { category: "food_drink", name: "Đồ ăn & Thức uống" },
                        {
                          category: "travel_places",
                          name: "Du lịch & Địa điểm",
                        },
                        { category: "activities", name: "Hoạt động" },
                        { category: "objects", name: "Đồ vật" },
                        { category: "symbols", name: "Biểu tượng" },
                        { category: "flags", name: "Cờ" },
                      ]}
                    />
                  </div>
                </PopoverContent>
              </Popover>
            </div>
          </div>

          <style
            dangerouslySetInnerHTML={{
              __html: `
            .emoji-picker-wrapper .epr-main {
              border: none !important;
              border-radius: 1rem !important;
              font-family: inherit !important;
            }
            .emoji-picker-wrapper .epr-search-container input {
              border-radius: 0.75rem !important;
              background-color: #f3f4f6 !important;
              border: 1px solid #e5e7eb !important;
              padding: 0.6rem 1rem 0.6rem 2.5rem !important; /* Tăng padding-left để tránh đè icon */
            }
            .emoji-picker-wrapper .epr-search-container input:focus {
              border-color: #3b82f6 !important;
              box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.1) !important;
            }
            .emoji-picker-wrapper .epr-category-nav {
              padding: 0.5rem !important;
            }
            .emoji-picker-wrapper .epr-cat-name {
              font-weight: 700 !important;
              color: #374151 !important;
              font-size: 0.8rem !important;
              text-transform: none !important;
              padding: 0.5rem 0.5rem 0.25rem !important;
            }
            .emoji-picker-wrapper .epr-body::-webkit-scrollbar {
              width: 5px !important;
            }
            .emoji-picker-wrapper .epr-body::-webkit-scrollbar-thumb {
              background-color: #e5e7eb !important;
              border-radius: 10px !important;
            }
            /* Active state color */
            .emoji-picker-wrapper .epr-category-nav > button.epr-active {
              color: #3b82f6 !important;
            }
          `,
            }}
          />

          <button
            type="submit"
            disabled={!text.trim()}
            className={`flex items-center gap-2 py-1.5 px-4 rounded-xl font-medium transition-all ${
              text.trim()
                ? "bg-gradient-primary text-white shadow-md shadow-blue-200 hover:scale-[1.02] active:scale-95"
                : "text-gray-400 bg-gray-100 cursor-not-allowed"
            }`}
          >
            <span className="text-xs">Gửi</span>
            <Send size={14} className={text.trim() ? "translate-x-0.5" : ""} />
          </button>
        </div>
      </div>
    </form>
  );
};

CommentInput.propTypes = {
  onSubmit: PropTypes.func.isRequired,
  placeholder: PropTypes.string,
  userAvatar: PropTypes.string,
  userName: PropTypes.string,
};

export default CommentInput;
