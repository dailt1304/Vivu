import useSWR from "swr";
import useSWRMutation from "swr/mutation";
import paymentApi from "../../api/paymentApi";

/**
 * Hook: Tạo link thanh toán PayOS
 * Vercel Rule: client-swr-dedup (automatic request deduplication)
 */
export const useCreatePaymentLink = () => {
  const { trigger, isMutating, error } = useSWRMutation(
    "/payments/create-payment-link",
    (_, { arg: packageId }) => paymentApi.createPaymentLink(packageId)
  );

  return {
    createPaymentLink: trigger,
    isCreating: isMutating,
    error,
  };
};

/**
 * Hook: Lấy danh sách giao dịch của user
 */
export const useMyTransactions = () => {
  const { data, error, isLoading, mutate } = useSWR(
    "/payments/transactions",
    () => paymentApi.getMyTransactions().then((res) => res.data)
  );

  return {
    // Vercel Rule: rerender-derived-state
    transactions: Array.isArray(data) ? data : data?.items || [],
    isLoading,
    isError: error,
    mutate,
  };
};

/**
 * Hook: Lấy chi tiết giao dịch
 */
export const useTransactionDetail = (id) => {
  const { data, error, isLoading } = useSWR(
    id ? `/payments/transactions/${id}` : null,
    () => paymentApi.getTransactionById(id).then((res) => res.data)
  );

  return {
    transaction: data,
    isLoading,
    isError: error,
  };
};

/**
 * Hook: Hủy giao dịch
 */
export const useCancelTransaction = () => {
  const { trigger, isMutating } = useSWRMutation(
    "/payments/cancel",
    (_, { arg }) => paymentApi.cancelTransaction(arg.id, arg.reason)
  );

  return {
    cancelTransaction: trigger,
    isCancelling: isMutating,
  };
};
