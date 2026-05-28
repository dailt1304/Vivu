import axiosClient from "./axiosClient";

const paymentApi = {
  // Tạo link thanh toán PayOS
  createPaymentLink: (packageId) => {
    return axiosClient.post("/payments/create-payment-link", { packageId });
  },

  // Lấy danh sách giao dịch của user
  getMyTransactions: () => {
    return axiosClient.get("/payments/transactions");
  },

  // Lấy chi tiết 1 giao dịch
  getTransactionById: (id) => {
    return axiosClient.get(`/payments/transactions/${id}`);
  },

  // Hủy giao dịch
  cancelTransaction: (id, cancellationReason) => {
    return axiosClient.post(`/payments/transactions/${id}/cancel`, {
      cancellationReason,
    });
  },
};

export default paymentApi;
