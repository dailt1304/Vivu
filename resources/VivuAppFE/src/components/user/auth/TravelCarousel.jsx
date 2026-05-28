import React, { useState, useEffect } from "react";

import imghaLong from "../../../assets/images/ha_long_bay.jpg";
import imgcaVang from "../../../assets/images/golden_bridge.jpg";
import imghoiAn from "../../../assets/images/hoi_an.jpg";

const locations = [
  {
    id: 1,
    name: "Cầu Vàng, Đà Nẵng",
    image: imgcaVang,
    quote:
      "Cuộc hành trình ngàn dặm bắt đầu từ một bước chân. Hãy để Vivu đồng hành cùng bạn.",
  },
  {
    id: 2,
    name: "Vịnh Hạ Long, Quảng Ninh",
    image: imghaLong,
    quote:
      "Kỳ quan thiên nhiên giữa đất trời hùng vĩ. Khám phá vẻ đẹp bất tận.",
  },
  {
    id: 3,
    name: "Cầu Vàng, Đà Nẵng",
    image: imghoiAn,
    quote: "Nét đẹp cổ kính vượt thời gian. Nơi lưu giữ ký ức vàng son.",
  },
];

const TravelCarousel = () => {
  const [currentLocationIndex, setCurrentLocationIndex] = useState(0);

  useEffect(() => {
    // Preload images
    locations.forEach((loc) => {
      const img = new Image();
      img.src = loc.image;
    });

    const interval = setInterval(() => {
      setCurrentLocationIndex(
        (prevIndex) => (prevIndex + 1) % locations.length,
      );
    }, 6000);

    return () => clearInterval(interval);
  }, []);

  const currentLocation = locations[currentLocationIndex];

  return (
    <div className="hidden lg:block relative w-0 flex-1 overflow-hidden bg-gray-900">
      {locations.map((location, index) => (
        <div
          key={location.id}
          className={`absolute inset-0 w-full h-full transition-opacity duration-1000 ease-in-out ${
            index === currentLocationIndex ? "opacity-100" : "opacity-0"
          }`}
        >
          <img
            className="absolute inset-0 w-full h-full object-cover animate-pan-image"
            src={location.image}
            alt={location.name}
            style={{ animationDuration: "20s" }}
          />
          <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/30 to-transparent"></div>
        </div>
      ))}

      {/* Decorative Elements */}
      <div className="absolute top-0 right-0 p-12 pointer-events-none">
        <div className="w-24 h-24 border-r-2 border-t-2 border-white/30 rounded-tr-3xl"></div>
      </div>
      <div className="absolute bottom-0 left-0 p-12 pointer-events-none">
        <div className="w-24 h-24 border-l-2 border-b-2 border-white/30 rounded-bl-3xl"></div>
      </div>

      {/* Content Overlay */}
      <div className="absolute bottom-0 left-0 right-0 p-16 z-20">
        <blockquote className="space-y-4 animate-fade-in-up">
          <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-white/20 backdrop-blur-md text-white text-xs font-semibold border border-white/30 mb-2">
            <span className="w-2 h-2 rounded-full bg-accent animate-pulse"></span>
            Khám phá Việt Nam
          </div>
          {/* Dynamic Quote */}
          <p className="text-4xl font-bold text-white leading-tight drop-shadow-lg min-h-[120px]">
            "{currentLocation.quote}"
          </p>
          <footer className="mt-4">
            {/* Dynamic Location Name */}
            <p className="text-lg font-medium text-white/90">
              {currentLocation.name}
            </p>
          </footer>
        </blockquote>

        {/* Carousel Indicators */}
        <div className="flex gap-2 mt-8">
          {locations.map((_, index) => (
            <button
              key={index}
              onClick={() => setCurrentLocationIndex(index)}
              className={`h-1.5 rounded-full transition-all duration-300 ${
                index === currentLocationIndex
                  ? "w-8 bg-primary"
                  : "w-2 bg-white/30 hover:bg-white/50"
              }`}
              aria-label={`Go to slide ${index + 1}`}
            />
          ))}
        </div>
      </div>
    </div>
  );
};

export default TravelCarousel;
