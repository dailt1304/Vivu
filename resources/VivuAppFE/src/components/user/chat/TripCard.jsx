import React from 'react';

const TripCard = ({ image, title, initials }) => (
    <div className="group relative rounded-2xl overflow-hidden cursor-pointer h-32">
        <img src={image} alt={title} className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500" />
        <div className="absolute inset-0 bg-black/20 group-hover:bg-black/10 transition-colors" />
        {initials && (
            <div className="absolute top-2 left-2 flex -space-x-1">
                {initials.map((init, i) => (
                    <div key={i} className={`w-5 h-5 rounded-full flex items-center justify-center text-[8px] font-bold text-white ring-1 ring-white ${
                        i === 0 ? 'bg-purple-500' : i === 1 ? 'bg-orange-500' : 'bg-blue-500'
                    }`}>
                        {init}
                    </div>
                ))}
            </div>
        )}
        <div className="absolute bottom-2 left-3">
             <h3 className="text-white font-bold text-sm shadow-black drop-shadow-md">{title}</h3>
        </div>
    </div>
);

export default TripCard;
