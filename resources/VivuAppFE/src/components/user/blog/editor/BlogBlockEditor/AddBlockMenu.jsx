import React, { useEffect, useRef } from "react";
import { Quote, Type, ImageIcon, X, CalendarDays } from "lucide-react";

/**
 * AddBlockMenu
 * A floating menu to select the type of block to add.
 * Redesigned to a vertical popup matching Notion-style add button.
 */
const AddBlockMenu = ({ onSelect, onClose }) => {
  const menuRef = useRef(null);

  // Close when clicking outside
  useEffect(() => {
    const handleClickOutside = (e) => {
      if (menuRef.current && !menuRef.current.contains(e.target)) {
        onClose();
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [onClose]);

  const options = [
    {
      type: "quote",
      label: "Quote (Trích dẫn)",
      icon: Quote,
    },
    {
      type: "text",
      label: "Body text (Đoạn văn)",
      icon: Type,
    },
    {
      type: "photo",
      label: "Image/Video (Hình / Video)",
      icon: ImageIcon,
    },
    {
      type: "day_header",
      label: "Day (Thêm ngày mới)",
      icon: CalendarDays,
    },
  ];

  return (
    <div
      ref={menuRef}
      className="absolute top-4 left-1/2 -translate-x-1/2 z-50 w-64 bg-white border border-slate-100 rounded-xl shadow-2xl overflow-hidden animate-in fade-in slide-in-from-top-2 duration-200"
    >
      <div className="flex items-center justify-between px-4 py-3 bg-slate-50 border-b border-slate-100/60">
        <span className="text-sm font-bold text-slate-800">Add item</span>
        <button
          onClick={onClose}
          className="text-slate-400 hover:text-red-500 transition-colors bg-white rounded-full p-1 shadow-sm border border-slate-200"
        >
          <X size={14} strokeWidth={3} />
        </button>
      </div>
      <div className="flex flex-col py-2 max-h-72 overflow-y-auto hide-scrollbar">
        {options.map((opt) => (
          <button
            key={opt.type}
            onClick={() => onSelect(opt.type)}
            className="flex items-center gap-3 px-4 py-2.5 hover:bg-slate-50 transition-colors w-full text-left"
          >
            <opt.icon size={18} className="text-slate-500" />
            <span className="text-sm font-medium text-slate-700">
              {opt.label}
            </span>
          </button>
        ))}
      </div>
    </div>
  );
};

export default AddBlockMenu;
