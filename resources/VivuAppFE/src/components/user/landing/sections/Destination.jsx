import { motion } from "framer-motion";
import { MapPin, Star, ArrowRight } from "lucide-react";

// Mock data
const mockDestinations = [
  {
    id: "hoian",
    name: "Cầu Vàng",
    province: "Đà Nẵng",
    description:
      "Cầu Vàng là một trong những điểm check-in nổi tiếng nhất Việt Nam với kiến trúc độc đáo và view tuyệt đẹp.",
    rating: 4.9,
    reviews: 3200,
    imageUrl:
      "https://images.unsplash.com/photo-1559592413-7cec4d0cae2b?w=800&q=80",
  },
  {
    id: "halong",
    name: "Vịnh Hạ Long",
    province: "Quảng Ninh",
    description:
      "Tuyệt tác thiên nhiên thế giới với hàng ngàn hòn đảo đá vôi kỳ vĩ nổi bật trên mặt nước xanh ngọc bích.",
    rating: 4.8,
    reviews: 5120,
    imageUrl:
      "https://images.unsplash.com/photo-1528127269322-539801943592?w=800&q=80",
  },
  {
    id: "sapa",
    name: "Núi Bà Đen",
    province: "Tây Ninh",
    description:
      "Núi Bà Đen là một trong những điểm du lịch tâm linh nổi tiếng nhất Việt Nam với kiến trúc độc đáo và view tuyệt đẹp.",
    rating: 4.7,
    reviews: 2840,
    imageUrl:
      "https://dulich.tayninh.gov.vn/uploads/images/mang-huong-sac-tay-ninh-den-voi-thu-do-ha-noi-6b07b.jpg",
  },
  {
    id: "dalat",
    name: "Thành phố Đà Lạt",
    province: "Lâm Đồng",
    description:
      "Thành phố ngàn hoa với khí hậu se lạnh quanh năm, rừng thông bạt ngàn và những góc nhìn lãng mạn.",
    rating: 4.9,
    reviews: 4150,
    imageUrl:
      "https://images.unsplash.com/photo-1518118014316-ea2db65c6f66?w=800&q=80",
  },
];

export default function Destinations() {
  const items = mockDestinations;

  return (
    <section
      id="destinations"
      className="py-32 bg-slate-50 dark:bg-black overflow-hidden relative transition-colors duration-300"
    >
      <div className="max-w-[1440px] mx-auto px-4 sm:px-6 lg:px-8">
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, margin: "-10%" }}
          transition={{ duration: 0.8, type: "spring" }}
          className="flex flex-col md:flex-row md:items-end justify-between gap-6 mb-16"
        >
          <div>
            <span className="text-slate-500 dark:text-white/40 font-medium text-[10px] tracking-[0.4em] uppercase mb-4 flex items-center gap-4">
              <div className="w-8 h-px bg-slate-300 dark:bg-white/20" />
              Thị hiếu du lịch
            </span>
            <h2 className="text-4xl md:text-5xl lg:text-7xl font-display font-bold text-slate-900 dark:text-white leading-[1.05] tracking-tight transition-colors duration-300">
              Điểm đến <br />
              <span className="text-transparent bg-clip-text bg-gradient-to-r from-blue-600 to-cyan-500 dark:from-blue-400 dark:to-cyan-300 italic font-serif font-light">
                đang thịnh hành
              </span>
            </h2>
          </div>
          <button className="text-slate-700 dark:text-white/70 hover:text-slate-900 dark:hover:text-white flex items-center gap-2 group transition-all text-[10px] font-bold uppercase tracking-[0.2em] bg-slate-100 dark:bg-white/5 hover:bg-slate-200 dark:hover:bg-white/10 px-8 py-4 rounded-full border border-slate-200 dark:border-white/5 hover:border-slate-300 dark:hover:border-white/20 active:scale-95 duration-300">
            Khám phá thêm
            <ArrowRight className="w-4 h-4 group-hover:translate-x-1.5 transition-transform duration-300" />
          </button>
        </motion.div>

        {/* Horizontal Scroll Snap Container */}
        <div className="flex overflow-x-auto snap-x snap-mandatory hide-scrollbar gap-8 pb-12 -mx-4 px-4 sm:mx-0 sm:px-0">
          {items.map((destination, index) => (
            <DestinationCard
              key={destination.id}
              destination={destination}
              index={index}
            />
          ))}
          <div className="min-w-[40px] sm:hidden" />
        </div>
      </div>
    </section>
  );
}

const Card = ({ children, className }) => (
  <div
    className={`relative rounded-[2.5rem] overflow-hidden group snap-center shrink-0 w-[85vw] sm:w-[420px] h-[560px] cursor-pointer bg-slate-100 dark:bg-zinc-950 border border-slate-200 dark:border-white/10 hover:border-slate-300 dark:hover:border-white/20 transition-colors duration-500 ${className}`}
  >
    {children}
  </div>
);

Card.Image = ({ src, alt, tintedGradient }) => (
  <>
    <img
      src={src}
      alt={alt}
      loading="lazy"
      className="absolute inset-0 w-full h-full object-cover transition-transform duration-[2s] ease-[cubic-bezier(0.2,1,0.3,1)] group-hover:scale-[1.08] brightness-[0.95] dark:brightness-[0.85] group-hover:brightness-[0.8] dark:group-hover:brightness-[0.7]"
    />
    <div className="absolute inset-0 bg-black/10 bg-[url('https://grainy-gradients.vercel.app/noise.svg')] mix-blend-overlay opacity-20 dark:opacity-40 z-10 pointer-events-none" />
    <div
      className={`absolute inset-0 bg-gradient-to-t ${tintedGradient || "from-black/90"} via-black/40 dark:via-black/20 to-transparent opacity-80 group-hover:opacity-100 transition-opacity duration-700 z-10`}
    />
  </>
);

Card.Badge = ({ province }) => (
  <div className="absolute top-8 left-8 bg-white/40 dark:bg-black/20 backdrop-blur-2xl px-5 py-2 rounded-2xl border border-white/40 dark:border-white/10 flex items-center gap-2 z-20 group-hover:bg-white/60 dark:group-hover:bg-white/10 transition-colors duration-500 shadow-sm">
    <MapPin className="w-3.5 h-3.5 text-slate-900 dark:text-white/60" />
    <span className="text-slate-900 dark:text-white/80 text-[10px] font-bold uppercase tracking-[0.2em]">
      {province}
    </span>
  </div>
);

Card.Content = ({ name, description, rating, reviews }) => (
  <div className="absolute inset-x-0 bottom-0 p-10 flex flex-col justify-end z-20">
    <div className="flex items-center gap-3 mb-4 translate-y-8 opacity-0 group-hover:translate-y-0 group-hover:opacity-100 transition-all duration-700 delay-100 ease-[cubic-bezier(0.2,1,0.3,1)]">
      <div className="flex items-center gap-1 bg-white/20 dark:bg-white/10 backdrop-blur-md px-2.5 py-1 rounded-lg border border-white/20 dark:border-white/10 shadow-sm">
        <Star className="w-3 h-3 fill-yellow-500 text-yellow-500" />
        <span className="text-white text-[11px] font-bold">
          {rating ? rating.toFixed(1) : "0.0"}
        </span>
      </div>
      <span className="text-white/80 dark:text-white/40 text-[10px] font-medium tracking-wider uppercase">
        ({reviews || 0} đánh giá)
      </span>
    </div>

    <h3 className="text-3xl sm:text-4xl font-display font-bold text-white mb-2 leading-[1.1] tracking-tight transition-transform duration-700 ease-[cubic-bezier(0.2,1,0.3,1)] group-hover:-translate-y-2 drop-shadow-md">
      {name}
    </h3>

    <div className="overflow-hidden max-h-0 group-hover:max-h-[200px] opacity-0 group-hover:opacity-100 transition-all duration-700 delay-150 ease-[cubic-bezier(0.2,1,0.3,1)]">
      <p className="text-white/90 dark:text-white/60 text-sm leading-relaxed mt-4 pt-4 border-t border-white/20 dark:border-white/5 font-light">
        {description}
      </p>
    </div>
  </div>
);

const cardGradients = [
  "from-teal-950/95",
  "from-blue-950/95",
  "from-indigo-950/95",
  "from-rose-950/95",
];

function DestinationCard({ destination, index }) {
  const tintedGradient = cardGradients[index % cardGradients.length];

  return (
    <motion.div
      initial={{ opacity: 0, x: 60 }}
      whileInView={{ opacity: 1, x: 0 }}
      viewport={{ once: true, margin: "-5%" }}
      transition={{
        duration: 1,
        type: "spring",
        bounce: 0,
        delay: index * 0.1,
      }}
      className="will-change-[transform,opacity]"
    >
      <Card className="shadow-[0_20px_50px_-12px_rgba(0,0,0,0.2)] dark:shadow-[0_20px_50px_-12px_rgba(0,0,0,0.5)]">
        <Card.Image
          src={destination.imageUrl}
          alt={destination.name}
          tintedGradient={tintedGradient}
        />
        <Card.Badge province={destination.province} />
        <Card.Content
          name={destination.name}
          description={destination.description}
          rating={destination.rating}
          reviews={destination.reviews}
        />
      </Card>
    </motion.div>
  );
}
