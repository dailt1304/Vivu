import { List, Calendar, Users, Image } from "lucide-react";

/**
 * Tab configuration for TripDetailsDrawer
 * Separated from component for Fast Refresh compatibility
 */
export const TRIP_TABS = [
  { id: "itinerary", label: "Lịch trình", icon: List },
  { id: "calendar", label: "Lịch", icon: Calendar },
  { id: "members", label: "Thành viên", icon: Users },
  { id: "media", label: "Media", icon: Image },
];
