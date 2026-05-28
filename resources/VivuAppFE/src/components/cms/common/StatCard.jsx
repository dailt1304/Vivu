import React from "react";
import { cn } from "@/lib/utils";
import { Card, CardContent } from "@/components/ui/card";
import { TrendingUp, TrendingDown } from "lucide-react";

const StatCard = ({
  title,
  value,
  icon: Icon,
  trend,
  trendValue,
  trendLabel,
  className,
}) => {
  const isPositive = trend === "up";

  return (
    <Card className={cn(
      "relative overflow-hidden transition-all duration-200 ease-out hover:-translate-y-0.5 hover:shadow-[var(--shadow-cms-card-hover)] active:translate-y-0 active:scale-[0.98] border-0 border-t-2 border-emerald-400 rounded-lg bg-[var(--color-cms-card)]",
      className
    )}>
      <CardContent className="p-6 focus-visible:outline-none">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-xs font-medium tracking-wider uppercase text-zinc-400">{title}</p>
            <p className="text-2xl font-semibold text-zinc-900 mt-1.5 font-[family-name:var(--font-cms-mono)] tabular-nums tracking-tight">{value}</p>
            {trendValue && (
              <div className="flex items-center gap-1.5 mt-2">
                {isPositive ? (
                  <TrendingUp className="h-3.5 w-3.5 text-emerald-500" />
                ) : (
                  <TrendingDown className="h-3.5 w-3.5 text-red-500" />
                )}
                <span
                  className={cn(
                    "text-sm font-medium",
                    isPositive ? "text-emerald-500" : "text-red-500",
                  )}
                >
                  {trendValue}
                </span>
                {trendLabel && (
                  <span className="text-xs text-zinc-400">{trendLabel}</span>
                )}
              </div>
            )}
          </div>
          {Icon && (
            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-emerald-50 border border-emerald-100/50">
              <Icon className="h-5 w-5 text-emerald-600 animate-pulse" style={{ animationDuration: '3s' }} />
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  );
};

export default StatCard;
