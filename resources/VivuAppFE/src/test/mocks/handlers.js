import { http, HttpResponse } from "msw";

const API_URL = "https://localhost:7294/api"; // Matches the proxy target in vite.config.js

// Mock data
export const mockUser = {
  id: "user-1",
  fullName: "Test User",
  email: "test@vivu.app",
  avatarUrl: null,
};

export const mockTrip = {
  id: "trip-1",
  title: "Test Trip Đà Nẵng",
  description: "Chuyến đi test",
  startDate: "2026-03-01T00:00:00",
  endDate: "2026-03-03T00:00:00",
  visibility: "Private",
  isFavorited: false,
  tripDays: [],
};

export const handlers = [
  // Auth
  http.post(`${API_URL}/auth/login`, () =>
    HttpResponse.json({
      success: true,
      data: { accessToken: "token", refreshToken: "refresh", user: mockUser },
    }),
  ),

  // Trips
  http.get(`${API_URL}/trips/:id`, () =>
    HttpResponse.json({ success: true, data: mockTrip }),
  ),
  http.put(`${API_URL}/trips/:id`, () =>
    HttpResponse.json({ success: true, data: {} }),
  ),
  http.get(`${API_URL}/trips/:id/messages`, () =>
    HttpResponse.json({ success: true, data: { items: [], totalCount: 0 } }),
  ),

  // Trip Days
  http.post(`${API_URL}/trips/:id/days`, () =>
    HttpResponse.json({ success: true, data: {} }),
  ),
  http.delete(`${API_URL}/trip-days/:dayId`, () =>
    HttpResponse.json({ success: true, data: {} }),
  ),

  // Trip Members
  http.get(`${API_URL}/trips/:id/members`, () =>
    HttpResponse.json({
      success: true,
      data: { items: [{ userId: mockUser.id, role: "Owner" }] },
    }),
  ),

  // Favorites
  http.post(`${API_URL}/trips/:id/favorite`, () =>
    HttpResponse.json({ success: true }),
  ),
  http.delete(`${API_URL}/trips/:id/favorite`, () =>
    HttpResponse.json({ success: true }),
  ),

  // User
  http.get(`${API_URL}/users/me`, () =>
    HttpResponse.json({ success: true, data: mockUser }),
  ),
];
