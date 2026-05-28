import { useState, useEffect, useCallback, useRef } from "react";

/**
 * Custom hook for safe localStorage access with auto-sync
 * @param {string} key - localStorage key
 * @param {any} initialValue - Default value if key doesn't exist
 * @returns {[any, Function, Function]} [storedValue, setValue, removeValue]
 */
const useLocalStorage = (key, initialValue) => {
  const initialValueRef = useRef(initialValue);

  // Update ref when initialValue changes (without triggering re-renders)
  useEffect(() => {
    initialValueRef.current = initialValue;
  }, [initialValue]);

  // Get current value from localStorage or use default from Ref
  // We remove initialValue from deps to prevent re-creating this callback
  // every time a parent passes a new object literal as initialValue.
  const readValue = useCallback(() => {
    if (typeof window === "undefined") {
      return initialValueRef.current;
    }

    try {
      const item = window.localStorage.getItem(key);
      const parsed = item ? JSON.parse(item) : initialValueRef.current;
      return parsed;
    } catch (error) {
      console.warn(`Error reading localStorage key "${key}":`, error);
      return initialValueRef.current;
    }
  }, [key]);

  // Use explicit lazy initialization.
  // We use the 'initialValue' prop directly here for the first render
  // to avoid 'reading ref during render' lint warnings.
  const [storedValue, setStoredValue] = useState(() => {
    if (typeof window === "undefined") return initialValue;
    try {
      const item = window.localStorage.getItem(key);
      return item ? JSON.parse(item) : initialValue;
    } catch (error) {
      return initialValue;
    }
  });

  // Return a wrapped version of useState's setter function
  const setValue = useCallback(
    (value) => {
      try {
        // Allow value to be a function (same API as useState)
        const valueToStore =
          value instanceof Function ? value(storedValue) : value;

        setStoredValue(valueToStore);

        if (typeof window !== "undefined") {
          window.localStorage.setItem(key, JSON.stringify(valueToStore));
          // Dispatch event to sync across tabs/components
          window.dispatchEvent(new Event("local-storage"));
        }
      } catch (error) {
        console.warn(`Error setting localStorage key "${key}":`, error);
      }
    },
    [key, storedValue],
  );

  // Remove value from localStorage
  const removeValue = useCallback(() => {
    try {
      setStoredValue(initialValue);
      if (typeof window !== "undefined") {
        window.localStorage.removeItem(key);
        window.dispatchEvent(new Event("local-storage"));
      }
    } catch (error) {
      console.warn(`Error removing localStorage key "${key}":`, error);
    }
  }, [key, initialValue]);

  // Listen for changes in other tabs/windows
  useEffect(() => {
    const handleStorageChange = (event) => {
      if (event.key === key && event.newValue !== null) {
        try {
          setStoredValue(JSON.parse(event.newValue));
        } catch {
          setStoredValue(event.newValue);
        }
      }
    };

    // Named handler for same-tab sync (fixes memory leak - anonymous functions can't be removed)
    const handleLocalStorageSync = () => setStoredValue(readValue());

    // Listen for storage events from other tabs
    window.addEventListener("storage", handleStorageChange);
    // Listen for custom event from same tab
    window.addEventListener("local-storage", handleLocalStorageSync);

    return () => {
      window.removeEventListener("storage", handleStorageChange);
      window.removeEventListener("local-storage", handleLocalStorageSync);
    };
  }, [key, readValue]);

  return [storedValue, setValue, removeValue];
};

export default useLocalStorage;
