import { useEffect, useRef } from "react";
import aiApi from "../../api/aiApi";

/**
 * Polls GET /generation-status/{generationId} as fallback for SignalR.
 * Designed to complement (not replace) SignalR — whoever delivers the
 * result first wins, the other is automatically stopped via cleanup.
 *
 * Vercel Rules applied:
 *  - advanced-event-handler-refs : callbacksRef for stable effect deps
 *  - rerender-dependencies       : effect only re-runs on [generationId, enabled, intervalMs]
 *  - async-error-handling         : tolerates network failures silently (keeps polling)
 *
 * @param {Object} options
 * @param {string|null} options.generationId - Tracking ID to poll
 * @param {boolean} options.enabled - Whether polling is active
 * @param {Function} options.onCompleted - Called with { tripId } when generation succeeds
 * @param {Function} options.onFailed - Called with { error } when generation fails
 * @param {number} [options.intervalMs=5000] - Poll interval in ms
 */
export function useGenerationPolling({
  generationId,
  enabled,
  onCompleted,
  onFailed,
  intervalMs = 5000,
}) {
  // Vercel Rule: advanced-event-handler-refs
  // Store latest callbacks in a ref so the polling effect doesn't need
  // to re-run when callback identities change between renders.
  const callbacksRef = useRef({ onCompleted, onFailed });
  useEffect(() => {
    callbacksRef.current = { onCompleted, onFailed };
  }, [onCompleted, onFailed]);

  // Vercel Rule: rerender-dependencies — minimal deps
  useEffect(() => {
    if (!enabled || !generationId) return;

    let active = true;
    let unknownCount = 0;
    const MAX_UNKNOWN = 6; // 6 × 5s = 30s of consecutive "unknown"

    const poll = async () => {
      try {
        console.log("[useGenerationPolling] Polling for:", generationId);
        const res = await aiApi.getGenerationStatus(generationId);
        console.log("[useGenerationPolling] Response:", res);
        if (!active) return;

        if (res.status === "completed" && res.tripId) {
          console.log(
            "[useGenerationPolling] Trip completed! tripId:",
            res.tripId,
          );
          callbacksRef.current.onCompleted?.({ tripId: res.tripId });
        } else if (res.status === "failed") {
          console.log("[useGenerationPolling] Trip failed:", res.error);
          callbacksRef.current.onFailed?.({ error: res.error });
        } else if (res.status === "unknown") {
          unknownCount++;
          console.log(
            `[useGenerationPolling] Unknown (${unknownCount}/${MAX_UNKNOWN})`,
          );
          if (unknownCount >= MAX_UNKNOWN) {
            console.log("[useGenerationPolling] Max unknown — treating as stale");
            callbacksRef.current.onFailed?.({ error: "Generation status expired" });
          }
        } else {
          // 'processing' → backend has it, reset counter
          unknownCount = 0;
        }
      } catch (err) {
        console.warn(
          "[useGenerationPolling] Poll error:",
          err?.response?.status,
          err?.message,
        );
      }
    };

    // Poll immediately on mount, then repeat every intervalMs
    poll();
    const id = setInterval(poll, intervalMs);

    return () => {
      active = false;
      clearInterval(id);
    };
  }, [generationId, enabled, intervalMs]);
}
