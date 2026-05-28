import React, { useState, useCallback } from "react";
import { Upload, X, Image as ImageIcon } from "lucide-react";
import PropTypes from "prop-types";

/**
 * Cover image upload component with drag-and-drop
 */
const CoverImageUpload = ({ value, onChange }) => {
  const [isDragging, setIsDragging] = useState(false);

  const handleDragOver = useCallback((e) => {
    e.preventDefault();
    setIsDragging(true);
  }, []);

  const handleDragLeave = useCallback((e) => {
    e.preventDefault();
    setIsDragging(false);
  }, []);

  const handleDrop = useCallback(
    (e) => {
      e.preventDefault();
      setIsDragging(false);

      const file = e.dataTransfer.files[0];
      if (file && file.type.startsWith("image/")) {
        const reader = new FileReader();
        reader.onloadend = () => {
          onChange(reader.result);
        };
        reader.readAsDataURL(file);
      }
    },
    [onChange],
  );

  const handleFileSelect = useCallback(
    (e) => {
      const file = e.target.files[0];
      if (file && file.type.startsWith("image/")) {
        const reader = new FileReader();
        reader.onloadend = () => {
          onChange(reader.result);
        };
        reader.readAsDataURL(file);
      }
    },
    [onChange],
  );

  const handleRemove = () => {
    onChange(null);
  };

  if (value) {
    return (
      <div className="relative group rounded-2xl overflow-hidden aspect-[21/9]">
        <img src={value} alt="Cover" className="w-full h-full object-cover" />
        <div className="absolute inset-0 bg-black/40 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center gap-3">
          <label className="px-4 py-2 bg-white/90 text-gray-800 font-medium rounded-full cursor-pointer hover:bg-white transition-colors text-sm">
            Thay đổi
            <input
              type="file"
              accept="image/*"
              onChange={handleFileSelect}
              className="hidden"
            />
          </label>
          <button
            onClick={handleRemove}
            className="p-2 bg-red-500 text-white rounded-full hover:bg-red-600 transition-colors"
          >
            <X size={18} />
          </button>
        </div>
      </div>
    );
  }

  return (
    <label
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
      className={`
        flex flex-col items-center justify-center aspect-[21/9] rounded-2xl border-2 border-dashed cursor-pointer transition-all
        ${
          isDragging
            ? "border-blue-500 bg-blue-50"
            : "border-gray-300 bg-gray-50 hover:border-blue-400 hover:bg-blue-50/50"
        }
      `}
    >
      <input
        type="file"
        accept="image/*"
        onChange={handleFileSelect}
        className="hidden"
      />
      <div className="text-center p-8">
        <div
          className={`mx-auto w-16 h-16 rounded-full flex items-center justify-center mb-4 transition-colors ${
            isDragging ? "bg-blue-100" : "bg-gray-100"
          }`}
        >
          {isDragging ? (
            <Upload size={28} className="text-blue-500" />
          ) : (
            <ImageIcon size={28} className="text-gray-400" />
          )}
        </div>
        <p className="text-gray-600 font-medium mb-1">
          {isDragging ? "Thả ảnh tại đây" : "Kéo thả hoặc nhấn để chọn ảnh bìa"}
        </p>
        <p className="text-gray-400 text-sm">Khuyến nghị: 1200 x 630 pixels</p>
      </div>
    </label>
  );
};

CoverImageUpload.propTypes = {
  value: PropTypes.string,
  onChange: PropTypes.func.isRequired,
};

export default CoverImageUpload;
