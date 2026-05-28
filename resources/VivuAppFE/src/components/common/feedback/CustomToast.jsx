import React from 'react';
import { CheckCircle, AlertCircle, Info, AlertTriangle, X } from 'lucide-react';
import { motion } from 'framer-motion';

const variants = {
  initial: { opacity: 0, y: 50, scale: 0.9 },
  animate: { opacity: 1, y: 0, scale: 1 },
  exit: { opacity: 0, scale: 0.9, transition: { duration: 0.2 } }
};

const toastConfig = {
  success: {
    icon: CheckCircle,
    bg: 'bg-emerald-500/10',
    border: 'border-emerald-500/20',
    text: 'text-emerald-600',
    iconColor: 'text-emerald-500',
    blob: 'bg-emerald-500'
  },
  error: {
    icon: AlertCircle,
    bg: 'bg-red-500/10',
    border: 'border-red-500/20',
    text: 'text-red-600',
    iconColor: 'text-red-500',
    blob: 'bg-red-500'
  },
  warning: {
    icon: AlertTriangle,
    bg: 'bg-orange-500/10',
    border: 'border-orange-500/20',
    text: 'text-orange-600',
    iconColor: 'text-orange-500',
    blob: 'bg-orange-500'
  },
  info: {
    icon: Info,
    bg: 'bg-blue-500/10',
    border: 'border-blue-500/20',
    text: 'text-blue-600',
    iconColor: 'text-blue-500',
    blob: 'bg-blue-500'
  }
};

const CustomToast = ({ type, message, closeToast }) => {
  const config = toastConfig[type] || toastConfig.info;
  const Icon = config.icon;

  return (
    <div className="relative w-full max-w-md rounded-2xl pointer-events-auto overflow-hidden">
      {/* Dynamic Background Blob for Glow Effect */}
      <div 
        className={`absolute top-0 left-0 w-full h-full opacity-5 pointer-events-none ${config.blob}`} 
        style={{ filter: 'blur(20px)' }}
      />
      
      <div 
        className={`relative flex items-center gap-3 w-full p-4 rounded-2xl bg-white/80 backdrop-blur-xl border ${config.border} shadow-lg shadow-gray-200/50 hover:shadow-xl transition-shadow duration-300`}
      >
        <div className={`p-2 rounded-xl ${config.bg} ${config.iconColor}`}>
            <Icon size={20} className="stroke-[2.5px]" />
        </div>

        <div className="flex-1 min-w-0">
            <p className={`text-sm font-semibold ${config.text} leading-tight`}>
                {message}
            </p>
        </div>

        <button 
            onClick={closeToast}
            className="p-1.5 rounded-full text-gray-400 hover:bg-gray-100 hover:text-gray-600 transition-colors"
        >
            <X size={16} />
        </button>
      </div>
    </div>
  );
};

export default CustomToast;
