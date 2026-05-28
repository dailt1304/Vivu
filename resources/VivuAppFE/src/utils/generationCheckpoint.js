/**
 * generationCheckpoint.js
 *
 * Manages localStorage persistence for AI trip generation state.
 * Provides a safe, versioned interface so trip generation can survive
 * browser reloads and tab navigations.
 *
 * Vercel Rules applied:
 *  - client-localstorage-schema : Versioned schema, minimal payload, safe JSON parse
 *  - js-cache-storage           : Single access point — no scattered localStorage calls
 *  - advanced-init-once         : Module-level constants, initialized once
 *
 * Schema v1:
 * {
 *   v       : 1,          // Schema version — bump if shape changes
 *   status  : string,     // 'pending' | 'streaming' | 'saved' | 'failed'
 *   payload : object,     // StreamGenerateTripCommand payload (city, dates, etc.)
 *   tripId  : string|null,// Set when trip is successfully saved on the backend
 *   userId  : string,     // Scopes checkpoint to the current user on shared browsers
 *   ts      : number,     // Unix ms — used for expiry check
 * }
 */

const STORAGE_KEY = 'vivu_gen_cp';
const SCHEMA_VERSION = 2;
const EXPIRY_MS = 2 * 60 * 60 * 1000; // 2 hours

/**
 * Safely read and validate checkpoint from localStorage.
 * Returns null if missing, expired, wrong schema version, wrong user, or malformed.
 * @param {string} userId
 * @returns {object|null}
 */
function readRaw(userId) {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;

    const parsed = JSON.parse(raw);

    // Schema version guard — prevents stale structure from breaking the app
    if (parsed.v !== SCHEMA_VERSION) {
      localStorage.removeItem(STORAGE_KEY);
      return null;
    }

    // User scope guard — prevents one user seeing another's in-progress trip
    if (parsed.userId !== userId) return null;

    // Expiry guard — auto-clean stale checkpoints older than 2 hours
    if (Date.now() - parsed.ts > EXPIRY_MS) {
      localStorage.removeItem(STORAGE_KEY);
      return null;
    }

    return parsed;
  } catch {
    // Malformed JSON — clean up and return null
    localStorage.removeItem(STORAGE_KEY);
    return null;
  }
}

/**
 * Write checkpoint to localStorage.
 * Fails silently on storage quota errors (private browsing, full storage).
 * @param {object} data - Full checkpoint object to write
 */
function write(data) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
  } catch (e) {
    // Quota exceeded or private browsing mode — non-fatal
    console.warn('[generationCheckpoint] Could not write to localStorage:', e.message);
  }
}

export const generationCheckpoint = {
  /**
   * Initialize a new generation checkpoint.
   * Call when the user submits the trip creation form (before navigation).
   *
   * @param {string} userId - Current user's ID for scoping
   * @param {object} payload - The StreamGenerateTripCommand payload
   */
  start(userId, payload) {
    write({
      v: SCHEMA_VERSION,
      status: 'pending',
      payload,
      tripId: null,
      userId,
      ts: Date.now(),
    });
  },

  /**
   * Transition status to 'streaming'.
   * Call when the SSE stream starts in TripChatPanel (generation begins).
   *
   * @param {string} userId
   */
  markStreaming(userId) {
    const cp = readRaw(userId);
    if (!cp) return;
    write({ ...cp, status: 'streaming', ts: Date.now() });
  },

  /**
   * Transition status to 'saved' and attach the new tripId.
   * Call in the onSaved callback BEFORE navigating away — ensures the
   * tripId is persisted even if the navigation races with the reload.
   *
   * @param {string} userId
   * @param {string} tripId - The newly created trip's ID
   */
  markSaved(userId, tripId) {
    const cp = readRaw(userId);
    if (!cp) return;
    write({ ...cp, status: 'saved', tripId });
  },

  /**
   * Read current checkpoint for a user.
   * Returns null when no valid checkpoint exists.
   *
   * @param {string} userId
   * @returns {{ status: string, payload: object, tripId: string|null }|null}
   */
  get(userId) {
    return readRaw(userId);
  },

  /**
   * Completely remove the checkpoint.
   * Call after a successful navigation to the trip page.
   */
  clear() {
    localStorage.removeItem(STORAGE_KEY);
  },
};
