import { motion, useScroll, useTransform } from "framer-motion";
import { useState, useEffect, useRef } from "react";
import { Swiper, SwiperSlide } from "swiper/react";
import { Autoplay, EffectFade } from "swiper/modules";
import "swiper/css";
import "swiper/css/effect-fade";
import { Search, MapPin, Calendar, Users } from "lucide-react";

import imgHaLong from "../../../../assets/images/ha_long_bay.jpg";
import imgGoldenBridge from "../../../../assets/images/golden_bridge.jpg";
import imgHoiAn from "../../../../assets/images/hoi_an.jpg";

// Normalize strings to Precomposed Unicode (NFC) which fixes typing/splitting bugs with Vietnamese marks
const words = [
  "Khám phá điểm đến",
  "Trải nghiệm bản nguyên",
  "Ghi dấu thanh xuân",
  "Hành trình vô tận",
].map((word) => word.normalize("NFC"));

const backgroundImages = [imgHaLong, imgGoldenBridge, imgHoiAn];

export default function Hero() {
  const [currentWordIndex, setCurrentWordIndex] = useState(0);
  const [displayText, setDisplayText] = useState("");
  const [isDeleting, setIsDeleting] = useState(false);
  const containerRef = useRef(null);
  const { scrollY } = useScroll();
  const y1 = useTransform(scrollY, [0, 500], [0, 150]);
  const y2 = useTransform(scrollY, [0, 500], [0, -50]);

  useEffect(() => {
    const currentWord = words[currentWordIndex];
    const typingSpeed = isDeleting ? 30 : 80;
    const pauseDuration = 2500;

    const timer = setTimeout(() => {
      // Split using Array.from to correctly handle surrogate pairs and combined characters (NFC ensures Vietnamese marks are precomposed)
      const characters = Array.from(currentWord);

      if (!isDeleting) {
        if (displayText.length < characters.length) {
          setDisplayText(characters.slice(0, displayText.length + 1).join(""));
        } else {
          setTimeout(() => setIsDeleting(true), pauseDuration);
        }
      } else {
        if (displayText.length > 0) {
          setDisplayText(characters.slice(0, displayText.length - 1).join(""));
        } else {
          setIsDeleting(false);
          setCurrentWordIndex((prev) => (prev + 1) % words.length);
        }
      }
    }, typingSpeed);

    return () => clearTimeout(timer);
  }, [displayText, isDeleting, currentWordIndex]);

  return (
    <section
      ref={containerRef}
      className="relative min-h-dvh flex items-center overflow-hidden bg-black"
    >
      {/* Background with Parallax */}
      <motion.div
        style={{ y: y1 }}
        className="absolute inset-0 z-0 pointer-events-none"
      >
        <Swiper
          modules={[Autoplay, EffectFade]}
          effect="fade"
          autoplay={{ delay: 5000, disableOnInteraction: false }}
          loop={true}
          speed={2000}
          className="h-[110%] w-full -mt-[5%]"
        >
          {/* Optimization for LCP: hidden image with high fetch priority */}
          <img
            src={backgroundImages[0]}
            alt=""
            fetchPriority="high"
            className="hidden"
          />
          {backgroundImages.map((img, index) => (
            <SwiperSlide key={index}>
              <div
                className="w-full h-full bg-cover bg-center"
                style={{ backgroundImage: `url(${img})` }}
              >
                {/* Noise overlay and gradient for depth */}
                <div className="absolute inset-0 bg-black/40 bg-[url('https://grainy-gradients.vercel.app/noise.svg')] mix-blend-overlay opacity-80" />
                <div className="absolute inset-0 bg-linear-to-r from-black/80 via-black/40 to-transparent" />
                <div className="absolute inset-0 bg-linear-to-t from-black/80 via-transparent to-transparent" />
              </div>
            </SwiperSlide>
          ))}
        </Swiper>
      </motion.div>

      <div className="relative z-10 w-full max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 mt-20 md:mt-0">
        <div className="flex flex-col md:flex-row items-center lg:items-start justify-between gap-12 lg:gap-8 pt-12 md:pt-0">
          {/* Main Content - Left Aligned */}
          <motion.div
            initial={{ opacity: 0, x: -30 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ duration: 1, type: "spring", stiffness: 50 }}
            className="w-full max-w-2xl xl:max-w-3xl text-left"
          >
            <motion.div
              initial={{ y: 20, opacity: 0 }}
              animate={{ y: 0, opacity: 1 }}
              transition={{ delay: 0.2 }}
              className="inline-flex items-center gap-2 px-3 py-1.5 rounded-sm bg-black/30 backdrop-blur-md border border-white/10 mb-8"
            >
              <span className="relative flex h-2 w-2">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-white opacity-75"></span>
                <span className="relative inline-flex rounded-full h-2 w-2 bg-white"></span>
              </span>
              <span className="text-white/90 text-xs font-semibold tracking-widest uppercase">
                Trợ lý du lịch theo yêu cầu
              </span>
            </motion.div>

            {/* Strictly fixed height container completely decouples text wrapping from right-card positioning */}
            <div className="h-[120px] md:h-[160px] lg:h-[200px] xl:h-[220px] w-full mb-6 flex flex-col justify-start">
              <h1 className="text-4xl md:text-6xl lg:text-[4.5rem] xl:text-7xl font-display font-bold text-white leading-[1.05] tracking-tight drop-shadow-2xl">
                Việt Nam. <br />
                <span className="italic font-light font-serif mt-2 block overflow-visible">
                  <span className="text-gradient-primary drop-shadow-sm">
                    {displayText}
                  </span>
                  <motion.span
                    animate={{ opacity: [1, 0] }}
                    transition={{ duration: 0.5, repeat: Infinity }}
                    className="inline-block w-[3px] h-[0.8em] bg-gradient-primary ml-2 align-middle"
                  />
                </span>
              </h1>
            </div>

            <p className="text-lg md:text-xl text-white/80 max-w-xl font-light leading-relaxed mb-10">
              Lên kế hoạch, đặt vé và trải nghiệm những chuyến đi đáng nhớ nhất.
              Thiết kế riêng cho bạn, vận hành bởi AI cá nhân.
            </p>
          </motion.div>

          {/* Search Bar - True Glassmorphism */}
          <motion.div
            style={{ y: y2 }}
            initial={{ opacity: 0, y: 30 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{
              duration: 0.8,
              delay: 0.4,
              type: "spring",
              stiffness: 60,
            }}
            className="w-full max-w-sm md:max-w-md bg-white/5 backdrop-blur-3xl border border-white/10 p-4 rounded-3xl shadow-[0_30px_60px_-15px_rgba(0,0,0,0.8),inset_0_1px_0_rgba(255,255,255,0.2)] flex flex-col gap-3 group"
          >
            <div className="flex items-center bg-black/20 rounded-2xl px-4 py-3 border border-white/5 hover:bg-white/10 transition-colors cursor-pointer active:scale-[0.98] duration-200">
              <MapPin className="text-white/60 w-5 h-5 mr-3" />
              <div className="flex flex-col items-start w-full">
                <span className="text-[10px] text-white/50 font-medium uppercase tracking-widest">
                  Điểm đến
                </span>
                <input
                  type="text"
                  readOnly
                  placeholder="Bạn muốn đi đâu?"
                  className="bg-transparent text-white placeholder-white/70 w-full outline-none font-medium text-base cursor-pointer"
                />
              </div>
            </div>

            <div className="flex items-center bg-black/20 rounded-2xl px-4 py-3 border border-white/5 hover:bg-white/10 transition-colors cursor-pointer active:scale-[0.98] duration-200">
              <Calendar className="text-white/60 w-5 h-5 mr-3" />
              <div className="flex flex-col items-start w-full">
                <span className="text-[10px] text-white/50 font-medium uppercase tracking-widest">
                  Thời gian
                </span>
                <input
                  type="text"
                  readOnly
                  placeholder="Chọn ngày khởi hành"
                  className="bg-transparent text-white placeholder-white/70 w-full outline-none font-medium text-base cursor-pointer"
                />
              </div>
            </div>

            <div className="flex items-center bg-black/20 rounded-2xl px-4 py-3 border border-white/5 hover:bg-white/10 transition-colors cursor-pointer active:scale-[0.98] duration-200">
              <Users className="text-white/60 w-5 h-5 mr-3" />
              <div className="flex flex-col items-start w-full">
                <span className="text-[10px] text-white/50 font-medium uppercase tracking-widest">
                  Khách
                </span>
                <select
                  disabled
                  className="bg-transparent text-white w-full outline-none font-medium text-base appearance-none cursor-pointer placeholder-white/70"
                >
                  <option>2 người lớn</option>
                  <option>1 người độc hành</option>
                  <option>Gia đình (3-5)</option>
                </select>
              </div>
            </div>

            <button className="bg-white text-black hover:bg-gray-200 p-4 rounded-2xl font-bold text-base shadow-[0_0_20px_rgba(255,255,255,0.3)] transition-all flex items-center justify-center gap-2 mt-2 group/btn active:scale-[0.98] duration-200">
              <Search className="w-5 h-5 group-hover/btn:rotate-90 transition-transform" />
              <span>Tìm hành trình</span>
            </button>
          </motion.div>
        </div>
      </div>

      {/* Scroll indicator */}
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        transition={{ delay: 1.5, duration: 1 }}
        className="absolute bottom-10 left-8 z-20 hidden md:flex"
      >
        <div className="flex items-center gap-4 text-white/50">
          <div className="w-12 h-px bg-white/30" />
          <span className="text-[10px] tracking-[0.3em] uppercase font-medium">
            Cuộn xuống
          </span>
        </div>
      </motion.div>
    </section>
  );
}
