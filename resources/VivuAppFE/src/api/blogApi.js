import axiosClient from "./axiosClient";

const blogApi = {
  /**
   * Get all blogs with pagination and filtering
   * @param {Object} params - { sortBy, pageNumber, pageSize }
   */
  getAll: (params) => {
    return axiosClient.get("/blogs", { params });
  },

  /**
   * Full-text search public blogs
   * @param {Object} params - { q, pageNumber, pageSize }
   */
  searchPublic: (params) => {
    return axiosClient.get("/blogs/search", { params });
  },

  /**
   * Get blogs belonging to the current authenticated user
   * @param {Object} params - { pageNumber, pageSize }
   */
  getMyBlogs: (params) => {
    return axiosClient.get("/blogs/me", { params });
  },

  /**
   * Get draft blogs belonging to the current authenticated user
   */
  getMyDrafts: () => {
    return axiosClient.get("/blogs/me/drafts");
  },

  /**
   * Get public blog detail by ID or Slug
   * @param {string} idOrSlug - The blog identifier
   */
  getByIdOrSlug: (idOrSlug) => {
    return axiosClient.get(`/blogs/${idOrSlug}`);
  },

  /**
   * Create a blog from a completed trip
   * @param {FormData} formData - Contains TripId, CoverImage, ShortDescription, TagNames
   */
  createFromTrip: (formData) => {
    return axiosClient.post("/blogs/from-trip", formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });
  },

  /**
   * Update an existing blog
   * @param {string} blogId
   * @param {FormData} formData
   */
  update: (blogId, formData) => {
    return axiosClient.put(`/blogs/${blogId}`, formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });
  },

  /**
   * Delete a blog
   * @param {string} blogId
   */
  delete: (blogId) => {
    return axiosClient.delete(`/blogs/${blogId}`);
  },

  /**
   * Publish a blog
   * @param {string} blogId
   */
  publish: (blogId) => {
    return axiosClient.patch(`/blogs/${blogId}/publish`);
  },

  /**
   * Like or Unlike a blog
   * @param {string} blogId
   */
  like: (blogId) => axiosClient.post(`/blogs/${blogId}/like`),

  /**
   * Bookmark or Unbookmark a blog
   * @param {string} blogId
   */
  bookmark: (blogId) => axiosClient.post(`/blogs/${blogId}/bookmark`),

  /**
   * Get blogs bookmarked by current user
   * @param {Object} params - { pageNumber, pageSize }
   */
  getMyBookmarks: (params) =>
    axiosClient.get("/blogs/me/bookmarks", { params }),

  /**
   * Get comments for a blog
   * @param {string} blogId
   * @param {Object} params - { pageNumber, pageSize }
   */
  getComments: (blogId, params) =>
    axiosClient.get(`/blogs/${blogId}/comments`, { params }),

  /**
   * Create a comment on a blog
   * @param {string} blogId
   * @param {Object} data - { content, parentCommentId }
   */
  createComment: (blogId, data) =>
    axiosClient.post(`/blogs/${blogId}/comments`, data),

  /**
   * Delete a comment from a blog
   * @param {string} blogId
   * @param {string} commentId
   */
  deleteComment: (blogId, commentId) =>
    axiosClient.delete(`/blogs/${blogId}/comments/${commentId}`),

  // ── Moderation ────────────────────────────────────────────────────────

  /**
   * Report a blog for violation (User)
   * @param {string} blogId
   * @param {Object} data - { reportType, reason, description? }
   */
  reportBlog: (blogId, data) =>
    axiosClient.post(`/blogs/${blogId}/report`, data),

  /**
   * Flag a blog for review (Moderator)
   * @param {string} blogId
   * @param {Object} data - { reportType, reason, description? }
   */
  flagBlog: (blogId, data) =>
    axiosClient.post(`/blogs/${blogId}/flag`, data),

  /**
   * Ban/hide a blog (Moderator)
   * @param {string} blogId
   */
  banBlog: (blogId) => axiosClient.patch(`/blogs/${blogId}/ban`),

  /**
   * Unban/unhide a blog (Moderator)
   * @param {string} blogId
   */
  unbanBlog: (blogId) => axiosClient.patch(`/blogs/${blogId}/unban`),
};

export default blogApi;
