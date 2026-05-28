import React, { useState, useImperativeHandle, forwardRef, Suspense, startTransition } from "react";
import DestinationDrawer from "./DestinationDrawer";

/**
 * DrawerPortal — self-contained drawer with internal state.
 * Parent calls ref.current.open(locationData) to open.
 * State lives here → zero parent re-renders.
 * 
 * Vercel Rule: rerender-transitions — uses startTransition for non-urgent drawer open
 */
const DrawerPortal = forwardRef((_, ref) => {
  const [isOpen, setIsOpen] = useState(false);
  const [data, setData] = useState(null);

  useImperativeHandle(ref, () => ({
    open: (locationData) => {
      if (!locationData) return;
      // Set data immediately (urgent) so drawer has content
      setData(locationData);
      // Open animation is non-urgent — use transition to avoid blocking
      startTransition(() => {
        setIsOpen(true);
      });
    },
    close: () => {
      setIsOpen(false);
    },
  }));

  return (
    <DestinationDrawer
      isOpen={isOpen}
      onClose={() => setIsOpen(false)}
      data={data}
    />
  );
});

DrawerPortal.displayName = "DrawerPortal";

export default DrawerPortal;
