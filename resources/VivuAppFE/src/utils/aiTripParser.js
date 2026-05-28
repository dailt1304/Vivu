export const convertTripPlanToTripData = (tripPlan) => {
  if (!tripPlan) return null;

  return {
    id: "new",
    title: tripPlan.Title || "Chuyến đi mới",
    description: tripPlan.Description || "",
    startDate: tripPlan.Start || new Date().toISOString().split("T")[0],
    endDate: tripPlan.End || new Date().toISOString().split("T")[0],
    groupSize: tripPlan.Size || 1,
    coverUrl:
      "https://images.unsplash.com/photo-1469854523086-cc02fe5d8800?w=1200", // Default cover
    tripDays: (tripPlan.Days || []).map((day) => ({
      id: `day-${day.DayIndex}`,
      date: day.Date || "",
      dayIndex: day.DayIndex,
      title: day.Title || `Ngày ${day.DayIndex}`,
      locations: (day.Locations || []).map((loc) => ({
        id: loc.LocationId || `loc-${Math.random().toString(36).substr(2, 9)}`,
        locationId: loc.LocationId,
        customName: loc.Name || "",
        description: loc.Description || "",
        startTime: loc.StartTime || "08:00:00",
        endTime: loc.EndTime || "10:00:00",
        orderIndex: loc.OrderIndex || 0,
        // Since chunks don't have lat/lng, we assign dummy coordinates for the initial render
        // to avoid map crashes until real data is fetched.
        latitude: 21.028511, // Hanoi center fallback
        longitude: 105.804817,
        location: {
          id: loc.LocationId,
          name: loc.Name,
          address: "Đang cập nhật...",
        },
      })),
    })),
  };
};
