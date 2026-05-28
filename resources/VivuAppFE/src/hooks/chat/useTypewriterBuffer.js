import { useState, useRef, useCallback, useEffect } from "react";

const useTypewriterBuffer = (charsPerTick = 15) => {
  const [displayedText, setDisplayedText] = useState("");
  const [isDraining, setIsDraining] = useState(false);

  const targetTextRef = useRef("");
  const currentCursorRef = useRef(0);
  const isRunningRef = useRef(false);
  const rafRef = useRef(null);

  const startDrain = useCallback(() => {
    if (isRunningRef.current) return;

    // Check if we already reached our target
    if (currentCursorRef.current >= targetTextRef.current.length) {
      isRunningRef.current = false;
      setIsDraining(false);
      return;
    }

    isRunningRef.current = true;
    setIsDraining(true);

    const drainTick = () => {
      if (currentCursorRef.current < targetTextRef.current.length) {
        currentCursorRef.current = Math.min(
          currentCursorRef.current + charsPerTick,
          targetTextRef.current.length,
        );

        setDisplayedText(
          targetTextRef.current.substring(0, currentCursorRef.current),
        );
        rafRef.current = requestAnimationFrame(drainTick);
      } else {
        // Reached the end
        if (rafRef.current) {
          cancelAnimationFrame(rafRef.current);
          rafRef.current = null;
        }
        isRunningRef.current = false;
        setIsDraining(false);
      }
    };

    if (rafRef.current) {
      cancelAnimationFrame(rafRef.current);
    }
    rafRef.current = requestAnimationFrame(drainTick);
  }, [charsPerTick]);

  const setTargetText = useCallback(
    (text) => {
      targetTextRef.current = text || "";

      // Edge case: if the new target text is SHORTER than our current cursor
      if (currentCursorRef.current > targetTextRef.current.length) {
        currentCursorRef.current = targetTextRef.current.length;
        setDisplayedText(targetTextRef.current);
      }

      if (
        !isRunningRef.current &&
        currentCursorRef.current < targetTextRef.current.length
      ) {
        startDrain();
      }
    },
    [startDrain],
  );

  const flush = useCallback(() => {
    if (rafRef.current) {
      cancelAnimationFrame(rafRef.current);
      rafRef.current = null;
    }
    isRunningRef.current = false;
    setIsDraining(false);

    currentCursorRef.current = targetTextRef.current.length;
    setDisplayedText(targetTextRef.current);
  }, []);

  const reset = useCallback(() => {
    if (rafRef.current) {
      cancelAnimationFrame(rafRef.current);
      rafRef.current = null;
    }
    isRunningRef.current = false;
    setIsDraining(false);
    setDisplayedText("");
    targetTextRef.current = "";
    currentCursorRef.current = 0;
  }, []);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (rafRef.current) {
        cancelAnimationFrame(rafRef.current);
      }
    };
  }, []);

  return { displayedText, setTargetText, flush, reset, isDraining };
};

export default useTypewriterBuffer;
