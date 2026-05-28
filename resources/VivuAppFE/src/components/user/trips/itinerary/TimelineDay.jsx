import React from 'react';
import { useDroppable } from '@dnd-kit/core';
import { format, parse, differenceInMinutes, startOfDay, addMinutes } from 'date-fns';
import TimelineItem from './TimelineItem';

const PIXELS_PER_MINUTE = 1; // 60px per hour
const TOTAL_MINUTES = 24 * 60;

const TimelineDay = ({ 
    dayId, 
    dateLabel, 
    items, 
    onTimeChange,
    onViewDetails,
    onDelete 
}) => {
    const { setNodeRef, isOver } = useDroppable({
        id: `timeline-${dayId}`,
        data: { type: 'timeline', dayId }
    });

    // Generate time slots (every 30 mins)
    const timeSlots = Array.from({ length: 48 }, (_, i) => {
        const minutes = i * 30;
        const timeLabel = format(addMinutes(startOfDay(new Date()), minutes), 'HH:mm');
        return { minutes, label: timeLabel };
    });

    return (
        <div className="flex flex-col h-full select-none">
            {/* Header */}
            <div className="flex items-center justify-between p-4 border-b border-gray-100 bg-white sticky top-0 z-20">
                <h4 className="font-bold text-gray-800 text-lg">{dateLabel}</h4>
                <span className="text-xs font-medium text-gray-400 bg-gray-50 px-2 py-1 rounded-full">
                    {items.length} địa điểm
                </span>
            </div>

            {/* Timeline Container */}
            <div className="flex-1 overflow-y-auto relative bg-white">
                <div 
                    ref={setNodeRef}
                    className={`relative min-h-[1440px] w-full transition-colors ${
                        isOver ? 'bg-blue-50/50' : 'bg-white'
                    }`}
                >
                    {/* Grid Lines */}
                    {timeSlots.map((slot) => (
                        <div 
                            key={slot.minutes} 
                            className="absolute w-full border-t border-gray-100 flex items-start"
                            style={{ top: `${slot.minutes * PIXELS_PER_MINUTE}px`, height: '30px' }}
                        >
                            <span className="text-[10px] text-gray-400 font-medium w-12 text-right pr-2 -mt-2 bg-white/80">
                                {slot.label}
                            </span>
                        </div>
                    ))}

                    {/* Timeline Items */}
                    {items.map((item) => {
                        if (!item.startTime || !item.endTime) return null;
                        
                        const start = parse(item.startTime, 'HH:mm', new Date());
                        const end = parse(item.endTime, 'HH:mm', new Date());
                        
                        // Calculate position
                        const startMinutes = differenceInMinutes(start, startOfDay(start));
                        const duration = differenceInMinutes(end, start);
                        
                        const top = startMinutes * PIXELS_PER_MINUTE;
                        const height = Math.max(duration * PIXELS_PER_MINUTE, 30); // Min height 30px

                        return (
                            <div
                                key={item.id}
                                className="absolute left-14 right-2 z-10"
                                style={{ 
                                    top: `${top}px`, 
                                    height: `${height}px` 
                                }}
                            >
                                <TimelineItem 
                                    item={item} 
                                    height={height}
                                    onDelete={() => onDelete(item.id)}
                                    // Pass other handlers
                                />
                            </div>
                        );
                    })}
                </div>
            </div>
        </div>
    );
};

export default TimelineDay;
