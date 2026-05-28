import { describe, it, expect, vi, beforeEach } from "vitest";
import paymentApi from "../paymentApi";
import axiosClient from "../axiosClient";

vi.mock("../axiosClient", () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
  },
}));

describe("paymentApi", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("createPaymentLink", () => {
    it("should POST to /payments/create-payment-link with packageId", async () => {
      const mockResponse = { data: { checkoutUrl: "https://pay.payos.vn/..." } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const result = await paymentApi.createPaymentLink("pkg-123");

      expect(axiosClient.post).toHaveBeenCalledWith(
        "/payments/create-payment-link",
        { packageId: "pkg-123" }
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe("getMyTransactions", () => {
    it("should GET /payments/transactions", async () => {
      const mockResponse = { data: [] };
      axiosClient.get.mockResolvedValueOnce(mockResponse);

      const result = await paymentApi.getMyTransactions();

      expect(axiosClient.get).toHaveBeenCalledWith("/payments/transactions");
      expect(result).toEqual(mockResponse);
    });
  });

  describe("getTransactionById", () => {
    it("should GET /payments/transactions/:id", async () => {
      const mockResponse = { data: { id: "tx-1" } };
      axiosClient.get.mockResolvedValueOnce(mockResponse);

      const result = await paymentApi.getTransactionById("tx-1");

      expect(axiosClient.get).toHaveBeenCalledWith("/payments/transactions/tx-1");
      expect(result).toEqual(mockResponse);
    });
  });

  describe("cancelTransaction", () => {
    it("should POST to /payments/transactions/:id/cancel", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const result = await paymentApi.cancelTransaction("tx-1", "Changed mind");

      expect(axiosClient.post).toHaveBeenCalledWith(
        "/payments/transactions/tx-1/cancel",
        { cancellationReason: "Changed mind" }
      );
      expect(result).toEqual(mockResponse);
    });
  });
});
