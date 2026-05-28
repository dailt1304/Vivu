import React from 'react';
import { motion } from "framer-motion";
import PropTypes from 'prop-types';

const DestinationCard = ({ data, variants, className = "", onClick, compact }) => {
  const { name, img, province, description } = data;

  return (
    <motion.div
      variants={variants}
      onClick={onClick}
      whileHover={{ y: -10 }}
      className={`group relative rounded-3xl overflow-hidden cursor-pointer shadow-lg hover:shadow-2xl transition-all duration-500 ${compact ? 'h-64' : 'h-80'} ${className}`}
    >
      {/* Full Background Image */}
      <img
        alt={name}
        className="absolute inset-0 w-full h-full object-cover group-hover:scale-110 transition-transform duration-700"
        src={img}
      />
      
      {/* Gradient Overlay */}
      <div className="absolute inset-0 bg-linear-to-t from-black/90 via-black/40 to-transparent opacity-80 group-hover:opacity-90 transition-opacity duration-300" />
      
      {/* Content */}
      <div className="absolute inset-0 p-5 flex flex-col justify-end">
        <div className="transform translate-y-2 group-hover:translate-y-0 transition-transform duration-300">
            <span className="inline-block px-2 py-0.5 rounded-md bg-white/20 backdrop-blur-md text-[10px] font-bold text-white mb-2 border border-white/20">
                {province}
            </span>
            <h3 className="text-xl font-bold text-white mb-1 leading-tight drop-shadow-md">
                {name}
            </h3>
            <p className="text-gray-200 text-xs line-clamp-2 opacity-0 group-hover:opacity-100 transition-opacity duration-300 delay-75">
                {description}
            </p>
        </div>
      </div>
    </motion.div>
  );
};

DestinationCard.propTypes = {
  data: PropTypes.shape({
    name: PropTypes.string.isRequired,
    img: PropTypes.string.isRequired,
    province: PropTypes.string.isRequired,
    description: PropTypes.string,
  }).isRequired,
  variants: PropTypes.object,
  className: PropTypes.string,
  onClick: PropTypes.func,
};

export default DestinationCard;
