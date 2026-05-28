import React from "react";
import { motion } from "framer-motion";
import { MapPin, Sliders, FileText, Check } from "lucide-react";

const steps = [
  { id: 1, label: "Hành trình", icon: <MapPin size={16} /> },
  { id: 2, label: "Sở thích", icon: <Sliders size={16} /> },
  { id: 3, label: "Chi tiết", icon: <FileText size={16} /> },
];

const StepIndicator = ({ currentStep }) => {
  return (
    <div className="flex items-center justify-between relative w-full mb-8">
      {/* Background Line */}
      <div className="absolute top-1/2 left-0 w-full h-[2px] bg-slate-100 -z-10 translate-y-[-50%]"></div>
      
      {/* Active Line Progress */}
      <motion.div 
        className="absolute top-1/2 left-0 h-[2px] bg-blue-500 -z-10 translate-y-[-50%]"
        initial={{ width: "0%" }}
        animate={{ width: `${((currentStep - 1) / (steps.length - 1)) * 100}%` }}
        transition={{ type: "spring", stiffness: 100, damping: 20 }}
      />

      {steps.map((step) => {
        const isActive = step.id === currentStep;
        const isCompleted = step.id < currentStep;

        return (
          <div key={step.id} className="flex flex-col items-center gap-2 relative bg-white px-2">
            <div 
              className={`w-10 h-10 rounded-full flex items-center justify-center font-bold text-sm z-10 transition-colors duration-300 relative ${
                isActive 
                  ? "text-white" 
                  : isCompleted 
                    ? "text-blue-500 bg-blue-50 border border-blue-200" 
                    : "text-slate-400 bg-slate-50 border border-slate-200"
              }`}
            >
              {isActive && (
                <motion.div
                  layoutId="activeStepIndicator"
                  className="absolute inset-0 bg-gradient-primary rounded-full shadow-md shadow-blue-200/50"
                  transition={{ type: "spring", stiffness: 300, damping: 25 }}
                />
              )}
              <span className="relative z-10">
                  {isCompleted ? <Check size={16} strokeWidth={3} /> : step.icon}
              </span>
            </div>
            
            <span className={`text-xs font-bold transition-colors ${
              isActive ? "text-blue-600" : isCompleted ? "text-slate-700" : "text-slate-400"
            }`}>
              {step.label}
            </span>
          </div>
        );
      })}
    </div>
  );
};

export default StepIndicator;
