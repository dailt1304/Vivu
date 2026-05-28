import React from "react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";

const ConfirmDialog = ({
  open,
  onOpenChange,
  title,
  description,
  onConfirm,
  loading = false,
  isDestructive = false,
  confirmText = "Xác nhận",
  cancelText = "Hủy bỏ"
}) => {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="bg-white rounded-xl sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle className="text-zinc-900">{title}</DialogTitle>
          <DialogDescription className="text-zinc-500">
            {description}
          </DialogDescription>
        </DialogHeader>
        <DialogFooter className="mt-4 gap-2 sm:gap-0">
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={loading} className="rounded-lg shadow-sm">
            {cancelText}
          </Button>
          <Button
            disabled={loading}
            onClick={(e) => {
              e.preventDefault();
              onConfirm();
            }}
            className={`rounded-lg shadow-sm ${
              isDestructive 
                ? "bg-red-600 text-white hover:bg-red-700 border-transparent focus:ring-red-600" 
                : "bg-emerald-600 text-white hover:bg-emerald-700 border-transparent"
            }`}
          >
            {loading ? (
               <span className="flex items-center gap-2">
                 <span className="h-4 w-4 border-2 border-white/20 border-t-white rounded-full animate-spin"></span>
                 Đang xử lý...
               </span>
            ) : confirmText}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};

export default ConfirmDialog;
