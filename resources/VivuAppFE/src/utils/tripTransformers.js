import { format } from "date-fns";

/**
 * Transforms PublicTripDto into a format suitable for Card components
 * Vercel: js-combine-iterations — combine transform and filter in one pass
 * Vercel: js-early-exit — return early on invalid input
 */
export const transformAndFilterTrips = (trips, filterFn = null) => {
  if (!trips || !trips.length) return [];

  const result = [];
  for (const trip of trips) {
    const transformed = {
      id: trip.id,
      title: trip.title || "Chuyến đi chưa có tên",
      coverUrl:
        trip.coverUrl ||
        "https://images.unsplash.com/photo-1469854523086-cc02fe5d8800?w=1200", // Fallback image
      description:
        trip.description ||
        `Chuyến đi ${trip.durationDays || "?"} ngày tại ${trip.cityName || "Việt Nam"}`,
      ownerName: trip.owner?.fullName || "Người dùng Vivu",
      ownerAvatar: trip.owner?.avatarUrl || null,
      date: trip.createdAt
        ? format(new Date(trip.createdAt), "dd/MM/yyyy")
        : "",
      durationDays: trip.durationDays,
      favoritesCount: trip.favoritesCount || 0,
      memberCount: trip.memberCount || 0,
      cityName: trip.cityName,
    };

    if (!filterFn || filterFn(transformed)) {
      result.push(transformed);
    }
  }
  return result;
};
