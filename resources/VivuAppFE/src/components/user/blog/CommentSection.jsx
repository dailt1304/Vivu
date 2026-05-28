import React, { useState } from "react";
import { MessageCircle, ChevronDown } from "lucide-react";
import PropTypes from "prop-types";
import CommentItem from "./CommentItem";
import CommentInput from "./CommentInput";

/**
 * Comment section container
 */
const CommentSection = ({
  comments = [],
  blogId,
  onAddComment,
  onLikeComment,
  onReplyComment,
  onDeleteComment,
  userAvatar,
  userName,
  currentUserId,
}) => {
  const [visibleCount, setVisibleCount] = useState(3);

  const handleAddComment = (text) => {
    const newComment = {
      id: `new-${Date.now()}`,
      blogId,
      author: {
        name: "Bạn",
        avatar: userAvatar || "https://i.pravatar.cc/150?img=99",
      },
      content: text,
      createdAt: new Date().toISOString(),
      likes: 0,
      replies: [],
    };
    onAddComment?.(newComment);
  };

  const handleShowMore = () => {
    setVisibleCount((prev) => prev + 5);
  };

  const visibleComments = comments.slice(0, visibleCount);
  const hasMore = visibleCount < comments.length;

  return (
    <div className="bg-white rounded-2xl border border-gray-100 shadow-sm overflow-hidden">
      {/* Header */}
      <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <MessageCircle size={20} className="text-blue-500" />
          <h3 className="font-bold text-gray-900">Bình luận</h3>
          <span className="text-sm text-gray-500">({comments.length})</span>
        </div>
      </div>

      {/* Comment Input */}
      <div className="p-6 border-b border-gray-100">
        <CommentInput
          onSubmit={handleAddComment}
          userAvatar={userAvatar}
          userName={userName}
          placeholder="Chia sẻ suy nghĩ của bạn..."
        />
      </div>

      {/* Comments List */}
      <div className="p-6 space-y-6">
        {visibleComments.length > 0 ? (
          <>
            {visibleComments.map((comment) => (
              <CommentItem
                key={comment.id}
                comment={comment}
                onLike={onLikeComment}
                onReply={onReplyComment}
                onDelete={onDeleteComment}
                currentUserId={currentUserId}
              />
            ))}

            {/* Show More Button */}
            {hasMore && (
              <button
                onClick={handleShowMore}
                className="w-full py-3 text-sm font-medium text-blue-600 hover:text-blue-700 hover:bg-blue-50 rounded-xl transition-colors flex items-center justify-center gap-2"
              >
                <ChevronDown size={18} />
                Xem thêm {comments.length - visibleCount} bình luận
              </button>
            )}
          </>
        ) : (
          <div className="text-center py-8">
            <MessageCircle size={40} className="mx-auto text-gray-300 mb-3" />
            <p className="text-gray-500 text-sm">
              Chưa có bình luận nào. Hãy là người đầu tiên!
            </p>
          </div>
        )}
      </div>
    </div>
  );
};

CommentSection.propTypes = {
  comments: PropTypes.array,
  blogId: PropTypes.oneOfType([PropTypes.string, PropTypes.number]),
  onAddComment: PropTypes.func,
  onLikeComment: PropTypes.func,
  onReplyComment: PropTypes.func,
  onDeleteComment: PropTypes.func,
  userAvatar: PropTypes.string,
  userName: PropTypes.string,
  currentUserId: PropTypes.string,
};

export default CommentSection;
