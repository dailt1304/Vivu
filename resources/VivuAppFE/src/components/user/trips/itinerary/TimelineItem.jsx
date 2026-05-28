import React from 'react';
import { useDraggable } from '@dnd-kit/core';
import { Clock, MapPin, Trash2, GripVertical } from 'lucide-react';
import { CSS } from '@dnd-kit/utilities';

const TimelineItem = ({ item, height, onDelete }) => {
    const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({
        id: item.id,
        data: {
            type: 'timeline-item',
            item
        }
    });

    const style = {
        transform: CSS.Translate.toString(transform),
        height: `${height}px`,
        opacity: isDragging ? 0.5 : 1,
    };

    return (
        <div 
            ref={setNodeRef} 
            style={style} 
            className={`group absolute left-0 right-0 rounded-lg border border-blue-200 bg-blue-50 shadow-sm hover:shadow-md transition-shadow overflow-hidden flex flex-col ${isDragging ? 'z-50 ring-2 ring-blue-400' : ''}`}
        >
            {/* Drag Handle & Time Header */}
            <div className="flex items-center justify-between px-2 py-1 bg-blue-100/50 border-b border-blue-100 h-7 shrink-0">
                <div 
                    {...attributes} 
                    {...listeners} 
                    className="cursor-move text-blue-400 hover:text-blue-600"
                >
                    <GripVertical size={14} />
                </div>
                <span className="text-[10px] font-bold text-blue-600 font-mono">
                    {item.startTime} - {item.endTime}
                </span>
                <button 
                    onClick={(e) => { e.stopPropagation(); onDelete(); }}
                    className="text-gray-400 hover:text-red-500 transition-colors"
                >
                    <Trash2 size={12} />
                </button>
            </div>

            {/* Content */}
            <div className="flex-1 p-2 flex gap-2 min-h-0">
                 {item.image && height > 50 && (
                    <img 
                        src={item.image} 
                        alt={item.content} 
                        className="w-10 h-10 rounded object-cover shrink-0"
                    />
                )}
                <div className="flex-1 min-w-0">
                    <h5 className="font-bold text-gray-800 text-xs truncate leading-tight mb-0.5">
                        {item.content}
                    </h5>
                    {height > 40 && (
                        <p className="text-[10px] text-gray-500 truncate flex items-center gap-1">
                            <MapPin size={10} /> {item.category || 'Địa điểm'}
                        </p>
                    )}
                </div>
            </div>
        </div>
    );
};

export default TimelineItem;
