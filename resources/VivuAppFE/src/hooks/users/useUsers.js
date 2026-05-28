import useSWR from "swr";
import useSWRMutation from "swr/mutation";
import userApi from "@/api/userApi";
import React from "react";

/**
 * Hook to fetch all users with pagination and filters
 * @param {Object} params - { pageNumber, pageSize, status, role, searchQuery, sortColumn, sortDescending }
 */
export function useAllUsers(params) {
  const { data, error, isLoading, mutate } = useSWR(["/users", params], () =>
    userApi.getAll(params).then((res) => {
      // Backend returns Result<List<UserDto>> which is { success: true, data: [...] }
      // axiosClient.js returns response.data, so 'res' here is the Result object.
      return res.data;
    }),
    { revalidateOnFocus: false }
  );

  // Handle both paginated (items) and non-paginated (flat list) responses
  const isPaginatedOnBackend =
    data && !Array.isArray(data) && Array.isArray(data.items);
  const fullUserList = isPaginatedOnBackend
    ? data.items
    : Array.isArray(data)
      ? data
      : [];

  // Client-side pagination logic if not paginated on backend
  const pageNumber = params?.pageNumber || 1;
  const pageSize = params?.pageSize || 10;

  let displayedUsers = fullUserList;
  let totalCount = fullUserList.length;
  let totalPages = 1;

  if (!isPaginatedOnBackend) {
    totalCount = fullUserList.length;
    totalPages = Math.ceil(totalCount / pageSize) || 1;
    const startIndex = (pageNumber - 1) * pageSize;
    displayedUsers = fullUserList.slice(startIndex, startIndex + pageSize);
  } else {
    displayedUsers = fullUserList;
    totalCount = data.totalCount;
    totalPages = data.totalPages;
  }

  return {
    users: displayedUsers,
    pagination: {
      currentPage: isPaginatedOnBackend ? data.pageNumber : pageNumber,
      totalPages: totalPages,
      totalCount: totalCount,
      hasPreviousPage: isPaginatedOnBackend
        ? data.hasPreviousPage
        : pageNumber > 1,
      hasNextPage: isPaginatedOnBackend
        ? data.hasNextPage
        : pageNumber < totalPages,
    },
    isLoading,
    isError: error,
    mutate,
  };
}

/**
 * Hook to search users with filters (Admin only)
 * @param {Object} params - { searchTerm, status, role, isEmailVerified, createdFrom, createdTo, pageNumber, pageSize }
 */
export function useSearchUsers(params) {
  const { data, error, isLoading, mutate } = useSWR(
    params ? ["/users/search", params] : null,
    () =>
      userApi.search(params).then((res) => res.data),
  );

  return {
    users: data?.items || [],
    pagination: {
      currentPage: data?.pageNumber || 1,
      totalPages: data?.totalPages || 1,
      totalCount: data?.totalCount || 0,
      hasPreviousPage: data?.hasPreviousPage || false,
      hasNextPage: data?.hasNextPage || false,
    },
    isLoading,
    isError: error,
    mutate,
  };
}

/**
 * Hook to fetch AI usage stats from the backend API
 * Returns AI usage limits, subscription info, etc.
 */
export function useUserUsage() {
  const { data, error, isLoading, mutate } = useSWR(
    "/users/usage",
    () => userApi.getUsageStats().then((res) => res.data),
    {
      refreshInterval: 60000, // Refresh every 1 minute
      revalidateOnFocus: true,
    }
  );

  return {
    usage: data || {
      used: 0,
      limit: 0,
      remaining: 0,
      resetAt: null,
      hasActiveSubscription: false,
      packageName: null,
      subscriptionEndDate: null,
    },
    isLoading,
    isError: error,
    mutate,
  };
}

/**
 * Hook for user management statistics (CMS admin page)
 * Calculates totalUsers, activeUsers, bannedUsers from the backend
 */
export function useUserManagementStats() {
  const { data: allData, isLoading: loadingAll } = useSWR(
    ["/users/search", { pageSize: 1 }], 
    () => userApi.search({ pageSize: 1 }).then((res) => res.data)
  );

  const { data: activeData, isLoading: loadingActive } = useSWR(
    ["/users/search", { pageSize: 1, status: "active" }], 
    () => userApi.search({ pageSize: 1, status: "active" }).then((res) => res.data)
  );

  const { data: bannedData, isLoading: loadingBanned } = useSWR(
    ["/users/search", { pageSize: 1, status: "banned" }], 
    () => userApi.search({ pageSize: 1, status: "banned" }).then((res) => res.data)
  );

  return {
    usage: {
      totalUsers: allData?.totalCount || 0,
      activeUsers: activeData?.totalCount || 0,
      bannedUsers: bannedData?.totalCount || 0,
    },
    isLoading: loadingAll || loadingActive || loadingBanned,
  };
}

/**
 * Hook for updating user profile
 */
export function useUpdateProfile() {
  const { trigger, isMutating, error } = useSWRMutation(
    "/users/me/profile",
    (_, { arg: data }) => userApi.updateMyProfile(data),
  );

  return {
    updateProfile: trigger,
    isUpdating: isMutating,
    error,
  };
}

/**
 * Hook for uploading user avatar to Cloudinary
 * Returns a Cloudinary URL string on success
 */
export function useUploadAvatar() {
  const { trigger, isMutating, error } = useSWRMutation(
    "/users/me/avatar",
    (_, { arg: file }) => userApi.uploadAvatar(file),
  );

  return {
    uploadAvatar: trigger,
    isUploading: isMutating,
    error,
  };
}

/**
 * Hook for user management actions (ban, unban, create)
 */
export function useUserActions() {
  const { trigger: banTrigger, isMutating: isBanning } = useSWRMutation(
    "/users/ban",
    (_, { arg: { id, reason } }) => userApi.ban(id, reason),
  );

  const { trigger: unbanTrigger, isMutating: isUnbanning } = useSWRMutation(
    "/users/unban",
    (_, { arg: id }) => userApi.unban(id),
  );

  const { trigger: createTrigger, isMutating: isCreating } = useSWRMutation(
    "/users/create",
    (_, { arg: data }) => userApi.create(data),
  );

  return {
    ban: banTrigger,
    isBanning,
    unban: unbanTrigger,
    isUnbanning,
    createUser: createTrigger,
    isCreating,
  };
}
