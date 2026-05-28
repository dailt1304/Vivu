import { useState, useEffect, useCallback, useRef } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { Loader2, CheckCircle2, AlertTriangle, X } from "lucide-react";
import { useAuth } from "../../contexts/auth-context";
import { generationCheckpoint } from "../../utils/generationCheckpoint";
import { useGenerationPolling } from "../../hooks/chat/useGenerationPolling";

// If the checkpoint is older than this, assume generation is done
// (backend cache only lasts 10 minutes with sliding expiry)
const STALE_CHECKPOINT_MS = 10 * 60 * 1000; // 10 minutes

/**
 * Global component mounted in RootLayout.
 * Monitors background AI generation via polling + localStorage checkpoint.
 * Shows persistent floating notification on ALL pages.
 *
 * Vercel Rules applied:
 *  - rerender-memo              : useCallback for all handlers
 *  - rerender-dependencies      : minimal interval deps (only user.id)
 *  - rendering-conditional-render: early return for idle state
 *  - advanced-event-handler-refs : polling hook uses callbacksRef internally
 *
 * Does NOT render when user is on /trips/new?generating=true
 * (TripChatPanel handles that case with its own waiting UI).
 */
export default function GenerationTracker() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const [trackerState, setTrackerState] = useState("idle");
  // idle | processing | completed | failed
  const [completedTripId, setCompletedTripId] = useState(null);
  const [generationId, setGenerationId] = useState(null);
  const [dismissed, setDismissed] = useState(false);
  const [failedError, setFailedError] = useState(null);

  // Vercel Rule: rerender-dependencies — check checkpoint periodically.
  // Uses setInterval(3s) instead of effect on location.pathname because
  // the checkpoint can change from other components (e.g. TripChatPanel).
  useEffect(() => {
    if (!user?.id) return;

    const check = () => {
      const cp = generationCheckpoint.get(user.id);

      // Auto-clear stale checkpoints — backend cache only lasts 10 min
      if (cp?.status === "streaming" && cp?.ts) {
        const age = Date.now() - cp.ts;
        if (age > STALE_CHECKPOINT_MS) {
          generationCheckpoint.clear();
          setTrackerState((prev) => (prev === "idle" ? prev : "idle"));
          return;
        }
      }

      if (cp?.status === "streaming" && cp?.payload?.generationId) {
        setGenerationId(cp.payload.generationId);
        setTrackerState("processing");
        setDismissed(false);
      } else if (cp?.status === "saved" && cp?.tripId) {
        setCompletedTripId(cp.tripId);
        setTrackerState("completed");
        setDismissed(false);
      } else {
        // Vercel Rule: rerender-functional-setstate — avoid unnecessary re-renders
        setTrackerState((prev) => (prev === "idle" ? prev : "idle"));
      }
    };

    check(); // check immediately
    const id = setInterval(check, 3000);
    return () => clearInterval(id);
  }, [user?.id]);

  // Vercel Rule: rendering-conditional-render — ẩn khi ở trang generate
  const isOnGeneratePage =
    location.pathname === "/trips/new" &&
    location.search.includes("generating=true");

  // Vercel Rule: rerender-memo — stable callback refs
  const handleCompleted = useCallback(
    ({ tripId }) => {
      generationCheckpoint.markSaved(user?.id, tripId);
      setCompletedTripId(tripId);
      setTrackerState("completed");
    },
    [user?.id],
  );

  const handleFailed = useCallback(({ error } = {}) => {
    generationCheckpoint.clear();
    setFailedError(error || null);
    setTrackerState("failed");
  }, []);

  // Reuse polling hook — auto-disabled when not processing or on generate page
  useGenerationPolling({
    generationId,
    enabled: trackerState === "processing" && !isOnGeneratePage,
    onCompleted: handleCompleted,
    onFailed: handleFailed,
  });

  // Vercel Rule: rendering-conditional-render — early return
  if (trackerState === "idle" || dismissed || isOnGeneratePage) return null;

  return (
    <div
      className="generation-tracker"
      style={{
        position: "fixed",
        bottom: "1rem",
        right: "1rem",
        zIndex: 50,
        animation: "slideInUp 0.3s ease-out",
      }}
    >
      <div
        style={{
          background: "rgba(255, 255, 255, 0.95)",
          backdropFilter: "blur(16px)",
          WebkitBackdropFilter: "blur(16px)",
          borderRadius: "1rem",
          border: "1px solid rgba(0, 0, 0, 0.06)",
          boxShadow: "0 4px 24px rgba(0, 0, 0, 0.08)",
          padding: "1rem",
          paddingRight: "2.5rem",
          maxWidth: "22rem",
          position: "relative",
        }}
      >
        <button
          onClick={() => {
            generationCheckpoint.clear();
            setDismissed(true);
          }}
          style={{
            position: "absolute",
            top: "0.75rem",
            right: "0.75rem",
            color: "#9ca3af",
            cursor: "pointer",
            background: "none",
            border: "none",
            padding: "2px",
            lineHeight: 1,
          }}
          aria-label="Đóng thông báo"
        >
          <X size={16} />
        </button>

        {/* Vercel Rule: rendering-conditional-render */}
        {trackerState === "processing" && (
          <div style={{ display: "flex", alignItems: "center", gap: "0.75rem" }}>
            <Loader2
              style={{
                width: "1.25rem",
                height: "1.25rem",
                color: "#3b82f6",
                animation: "spin 1s linear infinite",
                flexShrink: 0,
              }}
            />
            <div>
              <p
                style={{
                  fontSize: "0.875rem",
                  fontWeight: 600,
                  color: "#111827",
                  margin: 0,
                }}
              >
                Đang tạo lịch trình...
              </p>
              <p
                style={{
                  fontSize: "0.75rem",
                  color: "#6b7280",
                  margin: "0.125rem 0 0",
                }}
              >
                Hệ thống đang chạy ngầm
              </p>
            </div>
          </div>
        )}

        {trackerState === "completed" && (
          <div style={{ display: "flex", alignItems: "center", gap: "0.75rem" }}>
            <CheckCircle2
              style={{
                width: "1.25rem",
                height: "1.25rem",
                color: "#10b981",
                flexShrink: 0,
              }}
            />
            <div>
              <p
                style={{
                  fontSize: "0.875rem",
                  fontWeight: 600,
                  color: "#111827",
                  margin: 0,
                }}
              >
                Lịch trình đã sẵn sàng!
              </p>
              <button
                onClick={() => {
                  generationCheckpoint.clear();
                  navigate(`/trips/${completedTripId}`);
                }}
                style={{
                  fontSize: "0.75rem",
                  color: "#2563eb",
                  fontWeight: 500,
                  background: "none",
                  border: "none",
                  padding: 0,
                  cursor: "pointer",
                  marginTop: "0.125rem",
                }}
              >
                Xem ngay →
              </button>
            </div>
          </div>
        )}

        {trackerState === "failed" && (
          <div style={{ display: "flex", alignItems: "center", gap: "0.75rem" }}>
            <AlertTriangle
              style={{
                width: "1.25rem",
                height: "1.25rem",
                color: "#f59e0b",
                flexShrink: 0,
              }}
            />
            <div>
              <p
                style={{
                  fontSize: "0.875rem",
                  fontWeight: 600,
                  color: "#111827",
                  margin: 0,
                }}
              >
                Tạo lịch trình thất bại
              </p>
              <p
                style={{
                  fontSize: "0.75rem",
                  color: "#6b7280",
                  margin: "0.125rem 0 0",
                }}
              >
                {failedError || "Vui lòng thử lại"}
              </p>
              <button
                onClick={() => {
                  setDismissed(true);
                  navigate("/trips/new");
                }}
                style={{
                  fontSize: "0.75rem",
                  color: "#2563eb",
                  fontWeight: 500,
                  background: "none",
                  border: "none",
                  padding: 0,
                  cursor: "pointer",
                  marginTop: "0.25rem",
                }}
              >
                Tạo lại →
              </button>
            </div>
          </div>
        )}
      </div>

      <style>{`
        @keyframes slideInUp {
          from {
            opacity: 0;
            transform: translateY(1rem);
          }
          to {
            opacity: 1;
            transform: translateY(0);
          }
        }
      `}</style>
    </div>
  );
}
