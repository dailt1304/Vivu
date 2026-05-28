import { useState, useCallback } from "react";

/**
 * Hook quản lý Browser Geolocation API
 * Vercel: rerender-move-effect-to-event — chỉ gọi khi user click
 * Vercel: js-early-exit — return early nếu browser không hỗ trợ
 */
export default function useGeolocation() {
  const [position, setPosition] = useState(null);
  const [error, setError] = useState(null);
  const [isLoading, setIsLoading] = useState(false);

  const requestPosition = useCallback(() => {
    if (!navigator.geolocation) {
      setError("Trình duyệt không hỗ trợ định vị");
      return;
    }

    setIsLoading(true);
    setError(null);

    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setPosition({
          latitude: pos.coords.latitude,
          longitude: pos.coords.longitude,
          accuracy: pos.coords.accuracy,
        });
        setIsLoading(false);
      },
      (err) => {
        const messages = {
          1: "Bạn đã từ chối quyền truy cập vị trí",
          2: "Không thể xác định vị trí",
          3: "Hết thời gian chờ định vị",
        };
        setError(messages[err.code] || err.message);
        setIsLoading(false);
      },
      { enableHighAccuracy: true, timeout: 10000, maximumAge: 300000 },
    );
  }, []);

  return { position, error, isLoading, requestPosition, setError };
}
