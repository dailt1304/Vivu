import * as React from "react";

import { cn } from "@/lib/utils";

import { useLocation } from "react-router-dom";

const Input = React.forwardRef(({ className, type, ...props }, ref) => {
  const loc = useLocation();
  const isAdminPath = ['/cms/dashboard', '/cms/users', '/cms/subscriptions'].some(p => loc.pathname.includes(p));
  const ringClass = isAdminPath ? "focus-visible:ring-emerald-500/20 focus-visible:border-emerald-500" : "focus-visible:ring-blue-500/20 focus-visible:border-blue-500";

  return (
    <input
      type={type}
      className={cn(
        "flex h-10 w-full rounded-md border border-gray-200 bg-white px-3 py-2 text-sm file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-gray-500 focus-visible:outline-none focus-visible:ring-2 disabled:cursor-not-allowed disabled:opacity-50 transition-colors",
        ringClass,
        className,
      )}
      ref={ref}
      {...props}
    />
  );
});
Input.displayName = "Input";

export { Input };
