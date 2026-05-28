import React from 'react';

const Card = ({ image, title, subtitle, icon, large }) => (
    <div className="group relative rounded-2xl overflow-hidden cursor-pointer h-40">
        <img src={image} alt={title} className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500" />
        <div className="absolute inset-0 bg-linear-to-t from-black/80 via-transparent to-transparent opacity-90" />
        <div className="absolute bottom-3 left-3 right-3 text-white">
            <h3 className={`font-bold leading-tight ${large ? 'text-sm' : 'text-xs mb-1'}`}>{title}</h3>
            {!large && <div className="flex items-center gap-1 text-[10px] text-white/80">
                {icon}
                <span>{subtitle}</span>
            </div>}
        </div>
    </div>
);

export default Card;
