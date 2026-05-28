import { useMemo } from "react";
import useSWR from "swr";
import useSWRMutation from "swr/mutation";
import subscriptionApi from "../../api/subscriptionApi";

/**
 * Hook: Lấy trạng thái subscription hiện tại của user.
 * Vercel Rule: client-swr-dedup (automatic request deduplication)
 */
export const useMySubscription = () => {
  const { data, error, isLoading, mutate } = useSWR(
    "/users/usage",
    () => subscriptionApi.getMySubscription().then((res) => res.data),
    { revalidateOnFocus: true }
  );

  // Vercel Rule: rerender-derived-state (computed from fetched data)
  const daysRemaining = useMemo(() => {
    if (!data?.subscriptionEndDate) return null;
    const days = Math.ceil(
      (new Date(data.subscriptionEndDate) - new Date()) / (1000 * 60 * 60 * 24)
    );
    return Math.max(0, days);
  }, [data?.subscriptionEndDate]);

  const isExpired = useMemo(() => {
    if (!data?.hasActiveSubscription) return false;
    return daysRemaining !== null && daysRemaining <= 0;
  }, [data?.hasActiveSubscription, daysRemaining]);

  const isNearExpiry = useMemo(() => {
    if (!data?.hasActiveSubscription) return false;
    return daysRemaining !== null && daysRemaining <= 3 && daysRemaining > 0;
  }, [data?.hasActiveSubscription, daysRemaining]);

  return {
    subscription: data,
    daysRemaining,
    isExpired,
    isNearExpiry,
    hasSubscription: data?.hasActiveSubscription || false,
    isLoading,
    isError: error,
    mutate,
  };
};

/**
 * Hook for managing subscription plans (Admin).
 */
export const useSubscriptionPlans = (params) => {
  const { data, error, isLoading, mutate } = useSWR(
    ["/subscription-packages", params],
    () => subscriptionApi.getAll(params).then((res) => res.data),
  );

  // Handle both paginated (items) and non-paginated (flat list) responses
  const plansData =
    data && !Array.isArray(data) && Array.isArray(data.items)
      ? data.items
      : Array.isArray(data)
        ? data
        : [];

  return {
    plans: plansData,
    pagination:
      data && data.items
        ? {
            totalCount: data.totalCount,
            totalPages: data.totalPages,
            pageNumber: data.pageNumber,
          }
        : null,
    isLoading,
    isError: error,
    mutate,
  };
};

/**
 * Hook for fetching active subscription plans (User).
 */
export const useActivePlans = () => {
  const { data, error, isLoading } = useSWR(
    "/subscription-packages/active",
    () => subscriptionApi.getActivePackages().then((res) => res.data),
  );

  // Handle both paginated (items) and non-paginated (flat list) responses
  const plansData =
    data && !Array.isArray(data) && Array.isArray(data.items)
      ? data.items
      : Array.isArray(data)
        ? data
        : [];

  return {
    plans: plansData,
    isLoading,
    isError: error,
  };
};

/**
 * Hook for subscription-related mutations (CRUD & Subscribe).
 */
export const useSubscriptionMutation = () => {
  const { trigger: createPlan, isMutating: isCreating } = useSWRMutation(
    "/subscription-packages",
    (_, { arg }) => subscriptionApi.create(arg),
  );

  const { trigger: updatePlan, isMutating: isUpdating } = useSWRMutation(
    "/subscription-packages/update",
    (_, { arg }) => subscriptionApi.update(arg.id, arg.data),
  );

  const { trigger: deletePlan, isMutating: isDeleting } = useSWRMutation(
    "/subscription-packages/delete",
    (_, { arg }) => subscriptionApi.delete(arg),
  );

  const { trigger: subscribe, isMutating: isSubscribing } = useSWRMutation(
    "/subscription-packages/subscribe",
    (_, { arg }) => subscriptionApi.subscribe(arg),
  );

  return {
    createPlan,
    isCreating,
    updatePlan,
    isUpdating,
    deletePlan,
    isDeleting,
    subscribe,
    isSubscribing,
  };
};
