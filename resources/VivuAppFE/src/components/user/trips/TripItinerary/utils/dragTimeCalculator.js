import { format } from "date-fns";

// Time constants
export const START_HOUR = 6;
export const END_HOUR = 23;
export const TOTAL_DAY_MINUTES = (END_HOUR - START_HOUR) * 60;
export const DEFAULT_DURATION_MINUTES = 60; // 1 hour

/**
 * Calculate time based on vertical percentage within a day container
 * @param {number} percentage - 0 to 1
 * @returns {string} HH:mm
 */
export const calculateTimeFromPercentage = (percentage) => {
  const dayStart = new Date();
  dayStart.setHours(START_HOUR, 0, 0, 0);
  const minutesToAdd = percentage * TOTAL_DAY_MINUTES;
  const proposedDate = new Date(dayStart.getTime() + minutesToAdd * 60 * 1000);
  const remainder = proposedDate.getMinutes() % 15;
  const roundedDate = new Date(proposedDate.getTime() - remainder * 60 * 1000);
  return format(roundedDate, "HH:mm");
};

/**
 * Calculate drop time from cursor position relative to container
 * @param {number} cursorY - Absolute Y position of cursor
 * @param {Element} containerEl - Container DOM element
 * @returns {string|null} HH:mm or null if invalid
 */
export const calculateDropTime = (cursorY, containerEl) => {
  if (!containerEl) return "09:00"; // Default fallback

  const containerRect = containerEl.getBoundingClientRect();
  const containerTop = containerRect.top;
  const containerHeight = containerRect.height;

  const relativeY = cursorY - containerTop;
  // Clamp percentage between 0 and 1
  const percentage = Math.max(0, Math.min(1, relativeY / containerHeight));

  return calculateTimeFromPercentage(percentage);
};

/**
 * Calculate end time based on start time and duration.
 * Capped at 23:59 to prevent wrapping past midnight.
 * @param {string} startTime - HH:mm
 * @param {number} durationMinutes - default 60
 * @returns {string} HH:mm
 */
export const calculateEndTime = (
  startTime,
  durationMinutes = DEFAULT_DURATION_MINUTES,
) => {
  if (!startTime) return "02:00";

  const [startHour, startMinute] = startTime.split(":").map(Number);
  const startTotalMinutes = startHour * 60 + startMinute;
  const endTotalMinutes = startTotalMinutes + durationMinutes;

  // Cap at 23:59 to prevent wrapping past midnight
  const clampedMinutes = Math.min(endTotalMinutes, 23 * 60 + 59);

  // If clamped end time is not greater than start, force at least 30 min gap
  const finalMinutes =
    clampedMinutes <= startTotalMinutes
      ? Math.min(startTotalMinutes + 30, 23 * 60 + 59)
      : clampedMinutes;

  const h = Math.floor(finalMinutes / 60)
    .toString()
    .padStart(2, "0");
  const m = (finalMinutes % 60).toString().padStart(2, "0");
  return `${h}:${m}`;
};

/**
 * Determine the best start time for a drop
 * Priority: 1. Drag indicator time, 2. After previous item, 3. Default (01:00)
 */
export const getSmartDropTime = (dragOverInfo, dayId, index, itemsInDay) => {
  // 1. If we have a specific time from the drag indicator
  if (dragOverInfo?.time && dragOverInfo?.dayId === dayId) {
    return dragOverInfo.time;
  }

  // 2. If dropping after an existing item
  if (index > 0 && itemsInDay && itemsInDay[index - 1]) {
    const prevItem = itemsInDay[index - 1];
    if (prevItem && prevItem.endTime) {
      const [prevEndHour, prevEndMinute] = prevItem.endTime
        .split(":")
        .map(Number);
      const prevEndTotalMinutes = prevEndHour * 60 + prevEndMinute;

      // Add 30 mins buffer
      const dropStartMinutes = prevEndTotalMinutes + 30;

      // If exceeds day limit, fallback to default start time
      if (dropStartMinutes >= END_HOUR * 60) {
        return "01:00";
      }

      const h = Math.floor(dropStartMinutes / 60)
        .toString()
        .padStart(2, "0");
      const m = (dropStartMinutes % 60).toString().padStart(2, "0");
      return `${h}:${m}`;
    }
  }

  // 3. Fallback — beginning of day
  return "01:00";
};

