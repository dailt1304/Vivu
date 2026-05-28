import React from 'react';
import { useNavigate } from 'react-router-dom';
import { Zap, AlertTriangle, ArrowRight } from 'lucide-react';
import { motion } from 'framer-motion';
import { format } from 'date-fns';
import { vi } from 'date-fns/locale';

const AIUsageIndicator = ({ usage, compact = false }) => {
  const navigate = useNavigate();

  if (!usage || usage.limit === 0) return null;

  const { used, limit, remaining, resetAt, hasActiveSubscription, packageName } = usage;
  
  const percentage = limit > 0 ? Math.min((used / limit) * 100, 100) : 0;
  const isLow = remaining <= 1 && remaining > 0;
  const isExhausted = remaining === 0;

  const formatResetTime = (dateString) => {
    if (!dateString) return '';
    try {
      return format(new Date(dateString), 'HH:mm', { locale: vi });
    } catch {
      return '';
    }
  };

  const getBarColor = () => {
    if (isExhausted) return 'bg-red-500';
    if (isLow) return 'bg-yellow-400';
    return 'bg-gradient-to-r from-blue-400 to-blue-600';
  };

  const getTextColor = () => {
    if (isExhausted) return 'text-red-600';
    if (isLow) return 'text-yellow-600';
    return 'text-slate-600';
  };

  const getIconColor = () => {
    if (isExhausted) return 'text-red-500 fill-red-500';
    if (isLow) return 'text-yellow-500 fill-yellow-500';
    return 'text-blue-500 fill-blue-500';
  };

  return (
    <div className={`flex flex-col gap-1.5 w-full ${compact ? 'text-[11px]' : 'text-xs'}`}>
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-1.5 font-medium">
          {isExhausted ? (
            <AlertTriangle className={`w-3.5 h-3.5 ${getIconColor()}`} />
          ) : (
            <Zap className={`w-3.5 h-3.5 ${getIconColor()}`} />
          )}
          <span className={getTextColor()}>
            {isExhausted 
              ? 'Đã hết lượt AI hôm nay' 
              : `${used}/${limit} lượt AI hôm nay`}
          </span>
          <span className="text-slate-300 px-1">•</span>
          <span className="text-slate-500 font-semibold border border-slate-200 px-1.5 py-0.5 rounded-md bg-white">
            {hasActiveSubscription ? packageName : 'Free'}
          </span>
        </div>
        
        <div className="flex items-center">
          {isExhausted && !hasActiveSubscription ? (
            <button
              onClick={() => navigate('/subscription')}
              className="text-blue-600 font-bold hover:text-blue-700 flex items-center gap-0.5 transition-colors"
            >
              Nâng cấp <ArrowRight className="w-3 h-3" />
            </button>
          ) : (
            <span className="text-slate-400">
              Reset {formatResetTime(resetAt)}
            </span>
          )}
        </div>
      </div>

      <div className="h-1.5 w-full bg-slate-100 rounded-full overflow-hidden shrink-0">
        <motion.div
          className={`h-full rounded-full ${getBarColor()}`}
          initial={{ width: 0 }}
          animate={{ width: `${percentage}%` }}
          transition={{ duration: 0.5, ease: 'easeOut' }}
        />
      </div>
    </div>
  );
};

export default AIUsageIndicator;
