import React from "react";
import { AlertCircle, RefreshCw } from "lucide-react";
import { motion } from "framer-motion";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

const ErrorBanner = ({
  title = "Đã xảy ra lỗi",
  description = "Hệ thống gặp sự cố trong quá trình tải dữ liệu. Vui lòng thử lại sau.",
  onRetry,
  className,
}) => {
  return (
    <motion.div
      initial={{ opacity: 0, y: -10 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3 }}
      className={cn("bg-red-50 border border-red-100 rounded-xl p-5 w-full", className)}
    >
      <div className="flex items-start sm:items-center gap-4 flex-col sm:flex-row">
        <div className="w-10 h-10 rounded-full bg-red-100 flex items-center justify-center shrink-0">
          <AlertCircle className="h-5 w-5 text-red-600" />
        </div>
        <div className="flex-1">
          <h4 className="text-sm font-semibold text-red-900">{title}</h4>
          <p className="text-sm text-red-700 mt-1">{description}</p>
        </div>
        {onRetry && (
          <Button
            variant="outline"
            size="sm"
            onClick={onRetry}
            className="cms-btn-interactive shrink-0 bg-white hover:bg-red-50 border-red-200 text-red-700 w-full sm:w-auto"
          >
            <RefreshCw className="h-4 w-4 mr-2" />
            Thử lại
          </Button>
        )}
      </div>
    </motion.div>
  );
};

export default ErrorBanner;
