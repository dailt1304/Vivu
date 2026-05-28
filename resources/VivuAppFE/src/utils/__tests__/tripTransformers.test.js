import { describe, it, expect } from "vitest";
import { transformAndFilterTrips } from "../tripTransformers";

// Note: Date formatting assumes specific timezone. For tests, we mock format or ensure deterministic tests.
// Here we mock date-fns format just to make tests stable.
import { vi } from "vitest";

vi.mock("date-fns", async (importOriginal) => {
  const actual = await importOriginal();
  return {
    ...actual,
    format: vi.fn(() => "01/01/2024"),
  };
});

describe("transformAndFilterTrips", () => {
  const mockTrip = {
    id: "123",
    title: "Trip to Paris",
    description: "A wonderful trip",
    coverUrl: "http://example.com/image.jpg",
    createdAt: "2024-01-01T00:00:00Z",
    owner: {
      fullName: "John Doe",
      avatarUrl: "http://example.com/avatar.jpg",
    },
    durationDays: 5,
    favoritesCount: 10,
    memberCount: 2,
    cityName: "Paris",
  };

  it("should return empty array for null or empty input", () => {
    expect(transformAndFilterTrips(null)).toEqual([]);
    expect(transformAndFilterTrips([])).toEqual([]);
  });

  it("should map all fields correctly", () => {
    const result = transformAndFilterTrips([mockTrip]);

    expect(result).toHaveLength(1);
    expect(result[0]).toEqual({
      id: "123",
      title: "Trip to Paris",
      description: "A wonderful trip",
      coverUrl: "http://example.com/image.jpg",
      ownerName: "John Doe",
      ownerAvatar: "http://example.com/avatar.jpg",
      date: "01/01/2024",
      durationDays: 5,
      favoritesCount: 10,
      memberCount: 2,
      cityName: "Paris",
    });
  });

  it("should provide fallbacks for missing fields", () => {
    const minimalTrip = { id: "456", owner: null };
    const result = transformAndFilterTrips([minimalTrip]);

    expect(result).toHaveLength(1);
    expect(result[0].title).toBe("Chuyến đi chưa có tên");
    expect(result[0].coverUrl).toBe(
      "https://images.unsplash.com/photo-1469854523086-cc02fe5d8800?w=1200",
    );
    expect(result[0].description).toBe("Chuyến đi ? ngày tại Việt Nam");
    expect(result[0].ownerName).toBe("Người dùng Vivu");
    expect(result[0].ownerAvatar).toBeNull();
    expect(result[0].favoritesCount).toBe(0);
    expect(result[0].memberCount).toBe(0);
  });

  it("should apply filter function if provided", () => {
    const trips = [
      { id: "1", title: "A" },
      { id: "2", title: "B" },
    ];

    const filterFn = (t) => t.title === "A";
    const result = transformAndFilterTrips(trips, filterFn);

    expect(result).toHaveLength(1);
    expect(result[0].id).toBe("1");
  });
});
