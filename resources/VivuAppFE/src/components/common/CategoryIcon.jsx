import React from "react";
import {
  Sparkles,
  TreePine,
  Umbrella,
  Building2,
  UtensilsCrossed,
  Coffee,
  Ticket,
  Landmark,
  ShoppingBag,
  MapPin,
  Tag,
  Waves,
  Mountain,
  Palmtree,
  Tent,
  Wine,
  Mic2,
  Gem,
  Camera,
  Heart,
} from "lucide-react";

const CATEGORY_ICONS = {
  All: <Sparkles size={16} />,
  "Nature & Park": <TreePine size={16} />,
  Beach: <Umbrella size={16} />,
  Hotel: <Building2 size={16} />,
  Restaurant: <UtensilsCrossed size={16} />,
  "Coffee Shop": <Coffee size={16} />,
  Entertainment: <Ticket size={16} />,
  "Historical Site": <Landmark size={16} />,
  "Shopping Mall": <ShoppingBag size={16} />,
  "Ẩm thực": <UtensilsCrossed size={16} />,
  "Cà phê": <Coffee size={16} />,
  "Giải trí": <Ticket size={16} />,
  "Mua sắm": <ShoppingBag size={16} />,
  "Lưu trú": <Building2 size={16} />,
  "Thiên nhiên": <TreePine size={16} />,
  "Văn hóa": <Landmark size={16} />,
  "Tâm linh": <Gem size={16} />,
  "Hải sản": <Waves size={16} />,
  "Đặc sản": <MapPin size={16} />,
  "Đường phố": <Camera size={16} />,
  Chay: <Heart size={16} />,
};

const CategoryIcon = ({ name, iconUrl, size = 16, className = "" }) => {
  // If iconUrl is a functional image URL (starts with http)
  if (iconUrl && (iconUrl.startsWith("http") || iconUrl.startsWith("/"))) {
    return (
      <img
        src={iconUrl}
        alt={name}
        style={{ width: size, height: size }}
        className={`object-contain shrink-0 ${className}`}
      />
    );
  }

  // If iconUrl is a string (emoji/unicode) - Vercel Rule: js-early-exit
  if (iconUrl && iconUrl.length > 0 && iconUrl.length <= 4) {
    return (
      <span
        style={{ fontSize: size }}
        className={`flex items-center justify-center leading-none shrink-0 ${className}`}
        role="img"
        aria-label={name}
      >
        {iconUrl}
      </span>
    );
  }

  // Fallback to Lucide icons based on name (look for partial match)
  const iconKey = Object.keys(CATEGORY_ICONS).find((key) =>
    name?.toLowerCase().includes(key.toLowerCase()),
  );

  if (iconKey) {
    const Icon = CATEGORY_ICONS[iconKey];
    return React.cloneElement(Icon, {
      size,
      className: `shrink-0 ${className}`,
    });
  }

  // Final fallback
  return <MapPin size={size} className={`shrink-0 ${className}`} />;
};

export default React.memo(CategoryIcon);
