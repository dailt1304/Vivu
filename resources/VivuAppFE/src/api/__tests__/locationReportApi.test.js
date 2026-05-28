import { describe, it, expect, vi, beforeEach } from "vitest";
import locationReportApi from "../locationReportApi";
import axiosClient from "../axiosClient";

vi.mock("../axiosClient", () => ({
  default: {
    get: vi.fn(),
    put: vi.fn(),
  },
}));

describe("locationReportApi", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("getAll", () => {
    it("should call get with /location-reports", async () => {
      const mockParams = { pageNumber: 1, pageSize: 6 };
      const expectedResponse = { data: [{ id: "1", title: "Test" }] };

      axiosClient.get.mockResolvedValueOnce(expectedResponse);

      const result = await locationReportApi.getAll(mockParams);

      expect(axiosClient.get).toHaveBeenCalledWith("/location-reports", {
        params: mockParams,
      });
      expect(result).toEqual(expectedResponse);
    });
  });

  describe("review", () => {
    it("should call put with /location-reports/review", async () => {
      const mockPayload = {
        reportId: "r1",
        decision: "APPROVED",
        adminNote: "Ok",
      };
      const expectedResponse = { data: { success: true } };

      axiosClient.put.mockResolvedValueOnce(expectedResponse);

      const result = await locationReportApi.review(mockPayload);

      expect(axiosClient.put).toHaveBeenCalledWith(
        "/location-reports/review",
        mockPayload,
      );
      expect(result).toEqual(expectedResponse);
    });
  });
});
