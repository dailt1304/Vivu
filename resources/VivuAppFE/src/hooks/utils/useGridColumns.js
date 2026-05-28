import { useState, useEffect } from "react";

/**
 * Hook to detect grid column count based on Tailwind breakpoints
 * sm: 2 cols, lg: 3 cols, xl: 4 cols
 */
export function useGridColumns() {
  const [columns, setColumns] = useState(1);

  useEffect(() => {
    const updateColumns = () => {
      const width = window.innerWidth;
      if (width >= 1280)
        setColumns(4); // xl
      else if (width >= 1024)
        setColumns(3); // lg
      else if (width >= 640)
        setColumns(2); // sm
      else setColumns(1);
    };

    updateColumns();
    window.addEventListener("resize", updateColumns);
    return () => window.removeEventListener("resize", updateColumns);
  }, []);

  return columns;
}
