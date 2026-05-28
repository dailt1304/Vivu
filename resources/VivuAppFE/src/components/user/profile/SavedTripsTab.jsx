import React from "react";
import { MapPin, Heart, Plane } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { useFavoriteTrips } from "../../../hooks/trips/useTrips";
import { format, parseISO } from "date-fns";
import { EmptyState } from "../../common";
import { TripGridSkeleton } from "../../ui/TripCardSkeleton";

const SavedTripsTab = () => {
  const navigate = useNavigate();
  const { trips: tripsArray, isLoading, isError } = useFavoriteTrips();

  if (isLoading) {
    return (
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
        <TripGridSkeleton count={6} />
      </div>
    );
  }

  if (isError) {
    return (
      <div className="text-center text-red-500 p-8 bg-red-50 rounded-2xl border border-red-100">
        Không thể tải danh sách chuyến đi đã lưu. Vui lòng thử lại sau.
      </div>
    );
  }

  if (tripsArray.length === 0) {
    return (
      <EmptyState
        icon={Heart}
        title="Chưa có chuyến đi nào được lưu"
        subtitle="Hãy khám phá và bấm ❤️ để lưu lại những chuyến đi thú vị nhé!"
        actionLabel="Khám phá ngay"
        onAction={() => navigate("/explore")}
      />
    );
  }

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
      {tripsArray.map((trip) => {
        const formattedDate = trip.startDate
          ? format(parseISO(trip.startDate), "MMM yyyy")
          : "Đang lên kế hoạch";

        // Count locations from trip days if available
        const locationsCount =
          trip.tripDays?.reduce(
            (acc, day) => acc + (day.locations?.length || 0),
            0,
          ) || 0;

        return (
          <div
            key={trip.id}
            onClick={() => navigate(`/trips/${trip.id}`)}
            className="group relative rounded-2xl overflow-hidden shadow-lg hover:-translate-y-1 hover:shadow-xl transition-all cursor-pointer"
          >
            <div className="h-48 w-full bg-gray-200 relative">
              <img
                src={
                  trip.coverUrl ||
                  "https://images.unsplash.com/photo-1544885935-98dd03b09034?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80"
                }
                alt={trip.title || "Chuyến đi không tên"}
                className="w-full h-full object-cover transition-transform duration-500 group-hover:scale-110"
              />
              <div className="absolute inset-0 bg-linear-to-t from-black/80 via-black/20 to-transparent transition-colors" />

              {/* Heart Badge */}
              <div className="absolute top-3 right-3 p-2 bg-white/20 backdrop-blur-md rounded-full text-white">
                <Heart size={16} className="fill-white text-white" />
              </div>
            </div>
            <div className="absolute bottom-0 left-0 right-0 p-4">
              <h3 className="text-white font-bold text-lg mb-1 truncate">
                {trip.title || "Chuyến đi không tên"}
              </h3>
              <div className="flex items-center justify-between text-white/80 text-xs font-medium">
                <span>{formattedDate}</span>
                <span className="flex items-center gap-1">
                  <MapPin size={12} /> {locationsCount} Places
                </span>
              </div>
            </div>
          </div>
        );
      })}
    </div>
  );
};

export default SavedTripsTab;
