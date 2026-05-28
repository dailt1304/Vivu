import { describe, it, expect } from "vitest";
import { convertTripPlanToTripData } from "../aiTripParser";

describe("convertTripPlanToTripData", () => {
  it("returns null when tripPlan is falsy", () => {
    expect(convertTripPlanToTripData(null)).toBeNull();
    expect(convertTripPlanToTripData(undefined)).toBeNull();
  });

  it("converts basic TripPlanResponse correctly", () => {
    const tripPlan = {
      Title: "Chuyến đi test",
      Description: "Mô tả test",
      Start: "2024-01-01",
      End: "2024-01-03",
      Size: 4,
      Days: [
        {
          DayIndex: 1,
          Date: "2024-01-01",
          Title: "Ngày đầu",
          Locations: [],
        },
      ],
    };

    const result = convertTripPlanToTripData(tripPlan);
    expect(result.id).toBe("new");
    expect(result.title).toBe("Chuyến đi test");
    expect(result.description).toBe("Mô tả test");
    expect(result.startDate).toBe("2024-01-01");
    expect(result.endDate).toBe("2024-01-03");
    expect(result.groupSize).toBe(4);
    expect(result.tripDays).toHaveLength(1);
    expect(result.tripDays[0].title).toBe("Ngày đầu");
  });

  it("handles empty Days array and missing optional fields gracefully", () => {
    const tripPlan = {};
    const result = convertTripPlanToTripData(tripPlan);

    expect(result.id).toBe("new");
    expect(result.title).toBe("Chuyến đi mới");
    expect(result.description).toBe("");
    expect(result.startDate).toBeDefined();
    expect(result.groupSize).toBe(1);
    expect(result.tripDays).toEqual([]);
  });

  it("maps Locations data properly with fallback coordinates", () => {
    const tripPlan = {
      Days: [
        {
          DayIndex: 1,
          Locations: [
            {
              LocationId: "loc-123",
              Name: "Hồ Gươm",
              Description: "Đi dạo",
              StartTime: "08:00:00",
              EndTime: "10:00:00",
              OrderIndex: 1,
            },
          ],
        },
      ],
    };

    const result = convertTripPlanToTripData(tripPlan);
    const firstLocation = result.tripDays[0].locations[0];

    expect(firstLocation.locationId).toBe("loc-123");
    expect(firstLocation.customName).toBe("Hồ Gươm");
    expect(firstLocation.latitude).toBeDefined(); // Fallback should exist
    expect(firstLocation.longitude).toBeDefined();
    expect(firstLocation.location.name).toBe("Hồ Gươm");
  });
});
