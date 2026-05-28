import React, { useState } from "react";
import { Heart, Reply, Trash2 } from "lucide-react";
import PropTypes from "prop-types";

/**
 * Single comment item with reply support
 */
const CommentItem = ({
  comment,
  onLike,
  onReply,
  onDelete,
  isReply = false,
  currentUserId,
}) => {
  const [liked, setLiked] = useState(false);
  const [showReplyInput, setShowReplyInput] = useState(false);
  const [replyText, setReplyText] = useState("");

  const handleLike = () => {
    setLiked(!liked);
    onLike?.(comment.id, !liked);
  };

  const handleReply = () => {
    if (replyText.trim()) {
      onReply?.(comment.id, replyText);
      setReplyText("");
      setShowReplyInput(false);
    }
  };

  const formatDate = (dateString) => {
    if (!dateString) return "";
    const date = new Date(dateString);
    return date.toLocaleString("vi-VN", {
      hour: "2-digit",
      minute: "2-digit",
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
    });
  };

  const [imgError, setImgError] = useState(false);

  const authorName =
    comment.author?.name ||
    comment.authorName ||
    comment.userName ||
    "Người dùng";
  const authorAvatar =
    comment.author?.avatar || comment.authorAvatarUrl || comment.userAvatarUrl;

  const getInitials = () => {
    if (authorName && authorName !== "Người dùng" && authorName !== "Khách") {
      return authorName.charAt(0).toUpperCase();
    }
    // Thử lấy từ email nếu tên trống
    const email = comment.authorEmail || comment.userEmail;
    if (email) return email.charAt(0).toUpperCase();
    return "N";
  };

  const initials = getInitials();

  return (
    <div className={`${isReply ? "ml-12 mt-3" : ""}`}>
      <div className="flex gap-3">
        {authorAvatar && !imgError ? (
          <img
            src={authorAvatar}
            alt={authorName}
            referrerPolicy="no-referrer"
            onError={() => setImgError(true)}
            className={`${isReply ? "w-8 h-8" : "w-10 h-10"} rounded-full object-cover ring-2 ring-white shadow-sm`}
          />
        ) : (
          <div
            className={`${isReply ? "w-8 h-8 text-[10px]" : "w-10 h-10 text-xs"} rounded-full bg-gradient-primary flex items-center justify-center text-white font-bold shadow-sm ring-2 ring-white`}
          >
            {initials}
          </div>
        )}

        {/* Content */}
        <div className="flex-1">
          <div className="bg-gray-50 rounded-2xl rounded-tl-sm px-4 py-3">
            <div className="flex items-center justify-between mb-1">
              <span className="font-semibold text-gray-900 text-sm">
                {authorName}
              </span>
              <span className="text-xs text-gray-400">
                {formatDate(comment.createdAt)}
              </span>
            </div>
            <p className="text-gray-700 text-sm leading-relaxed">
              {comment.content}
            </p>
          </div>

          {/* Actions */}
          <div className="flex items-center gap-4 mt-2 ml-2">
            {currentUserId &&
              (currentUserId === comment.userId ||
                currentUserId === comment.authorId ||
                currentUserId === comment.author?.id ||
                currentUserId === comment.author?.userId) && (
                <button
                  onClick={() => onDelete?.(comment.id)}
                  className="flex items-center gap-1 text-xs font-medium text-gray-500 hover:text-red-500 transition-colors ml-auto"
                >
                  <Trash2 size={14} />
                  <span>Xóa</span>
                </button>
              )}
          </div>
        </div>
      </div>

      {/* Nested Replies */}
      {comment.replies && comment.replies.length > 0 && (
        <div className="mt-3">
          {comment.replies.map((reply) => (
            <CommentItem
              key={reply.id}
              comment={reply}
              onLike={onLike}
              onDelete={onDelete}
              isReply={true}
              currentUserId={currentUserId}
            />
          ))}
        </div>
      )}
    </div>
  );
};

CommentItem.propTypes = {
  comment: PropTypes.shape({
    id: PropTypes.string.isRequired,
    author: PropTypes.shape({
      name: PropTypes.string,
      avatar: PropTypes.string,
    }),
    authorName: PropTypes.string,
    authorAvatarUrl: PropTypes.string,
    content: PropTypes.string.isRequired,
    createdAt: PropTypes.string.isRequired,
    likes: PropTypes.number,
    replies: PropTypes.array,
  }).isRequired,
  onLike: PropTypes.func,
  onReply: PropTypes.func,
  onDelete: PropTypes.func,
  isReply: PropTypes.bool,
  currentUserId: PropTypes.string,
};

export default CommentItem;
