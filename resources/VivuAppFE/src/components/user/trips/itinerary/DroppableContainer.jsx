import React from 'react';
import { useDroppable } from '@dnd-kit/core';

const DroppableContainer = ({ id, children, className }) => {
    const { setNodeRef, isOver } = useDroppable({ id });
    
    // Visual feedback: Dashed box + Light Blue bg + Scale effect
    const activeStyle = isOver 
        ? "bg-blue-50 ring-2 ring-blue-200 ring-offset-2 border-2 border-dashed border-blue-400 rounded-xl scale-[1.01] z-10 !border-l-transparent" 
        : "";

    return (
        <div 
            id={id}
            ref={setNodeRef} 
            className={`${className} ${activeStyle} transition-all duration-200 ease-out`}
        >
            {children}
        </div>
    );
};

export default DroppableContainer;
