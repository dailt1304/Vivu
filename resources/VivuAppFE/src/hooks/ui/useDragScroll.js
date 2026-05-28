import { useRef, useCallback } from "react";

export function useDragScroll() {
  const containerRef = useRef(null);
  const isDragging = useRef(false);
  const startX = useRef(0);
  const scrollLeft = useRef(0);

  const onPointerDown = useCallback((e) => {
    if (!containerRef.current) return;
    isDragging.current = true;
    startX.current = e.pageX - containerRef.current.offsetLeft;
    scrollLeft.current = containerRef.current.scrollLeft;
    
    // Disable smooth scrolling while dragging for immediate responsiveness
    containerRef.current.style.scrollBehavior = "auto";
    containerRef.current.style.cursor = "grabbing";
  }, []);

  const onPointerMove = useCallback((e) => {
    if (!isDragging.current || !containerRef.current) return;
    e.preventDefault(); // Prevent text selection/native drag behavior
    
    // Calculate distance moved
    const x = e.pageX - containerRef.current.offsetLeft;
    const walk = (x - startX.current) * 1.5; // Multiplier controls drag speed
    
    containerRef.current.scrollLeft = scrollLeft.current - walk;
  }, []);

  const onPointerUpOrLeave = useCallback(() => {
    isDragging.current = false;
    if (containerRef.current) {
      containerRef.current.style.cursor = "grab";
      // Restore CSS property if using smooth scrolling elsewhere
      containerRef.current.style.scrollBehavior = ""; 
    }
  }, []);

  return {
    ref: containerRef,
    onPointerDown,
    onPointerMove,
    onPointerUp: onPointerUpOrLeave,
    onPointerLeave: onPointerUpOrLeave,
    // Add touch handler so touch devices don't trigger the pointer down event the same way
    // Touch scrolling uses native browser scrolling which is better than JS implementation.
    // Setting touch-action: pan-y prevents browser zoom/pan but allows vertical scrolling.
    style: { cursor: "grab", touchAction: "pan-x pan-y" },
  };
}
