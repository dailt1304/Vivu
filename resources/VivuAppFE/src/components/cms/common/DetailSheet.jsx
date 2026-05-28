import React from "react";
import { cn } from "@/lib/utils";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";

const DetailSheet = ({
  open,
  onOpenChange,
  title,
  description,
  children,
  side = "right",
  className = "",
}) => {
  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side={side} className={cn("sm:max-w-lg shadow-[var(--shadow-cms-card-hover)] border-l border-zinc-200 transition-transform duration-300 ease-out", className)}>
        <SheetHeader className="pb-4 border-b border-zinc-100">
          <SheetTitle className="text-xl font-semibold tracking-tight text-zinc-900">{title}</SheetTitle>
          {description && <SheetDescription className="text-sm text-zinc-500">{description}</SheetDescription>}
        </SheetHeader>
        <div className="mt-6 overflow-y-auto max-h-[calc(100vh-120px)] hide-scrollbar">
          {children}
        </div>
      </SheetContent>
    </Sheet>
  );
};

export default DetailSheet;
