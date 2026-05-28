import { useRef, useState } from "react";
import { motion } from "framer-motion";
import { Zap, Users, Map, Compass } from "lucide-react";

const features = [
  {
    icon: <Zap className="w-6 h-6 text-blue-500" />,
    title: "AI Lên Lịch Trình",
    description:
      "Công nghệ AI tự động tối ưu hóa lộ trình dựa trên sở thích cá nhân chỉ trong tích tắc. Một kỷ nguyên cá nhân hóa hoàn toàn mới dành cho trải nghiệm du lịch của bạn.",
    className: "md:col-span-2 md:row-span-1",
    gradient: "from-blue-500/10 to-transparent",
  },
  {
    icon: <Users className="w-6 h-6 text-purple-500" />,
    title: "Du Lịch Cùng Nhau",
    description:
      "Mời bạn bè, bỏ phiếu điểm đến và chia sẻ chi phí dễ dàng ngay trên ứng dụng.",
    className: "md:col-span-1 md:row-span-1",
    gradient: "from-purple-500/10 to-transparent",
  },
  {
    icon: <Map className="w-6 h-6 text-emerald-500" />,
    title: "Bản Đồ Thông Minh",
    description:
      "Tích hợp bản đồ trực quan, gợi ý địa điểm ăn uống, vui chơi xung quanh bạn.",
    className: "md:col-span-1 md:row-span-1",
    gradient: "from-emerald-500/10 to-transparent",
  },
  {
    icon: <Compass className="w-6 h-6 text-amber-500" />,
    title: "Khám Phá Độc Đáo",
    description:
      "Tìm kiếm viên ngọc ẩn giấu và trải nghiệm văn hóa địa phương chân thực nhất với những gợi ý được chọn lọc kỹ lưỡng dành riêng cho bạn.",
    className: "md:col-span-2 md:row-span-1",
    gradient: "from-amber-500/10 to-transparent",
  },
];

export default function WhyChooseUs() {
  return (
    <section className="py-32 bg-slate-50 dark:bg-black relative overflow-hidden transition-colors duration-300">
      {/* Background Elements */}
      <div className="absolute top-0 left-0 w-full h-full overflow-hidden pointer-events-none">
        <div className="absolute top-[20%] right-[-10%] w-[600px] h-[600px] bg-blue-400/10 dark:bg-blue-600/10 rounded-full blur-[120px]" />
        <div className="absolute bottom-[10%] left-[-10%] w-[500px] h-[500px] bg-cyan-400/10 dark:bg-cyan-600/10 rounded-full blur-[120px]" />
      </div>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 relative z-10">
        <div className="text-center max-w-3xl mx-auto mb-20">
          <motion.div
            initial={{ opacity: 0, y: 30 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            transition={{ duration: 0.8 }}
          >
            <span className="text-slate-500 dark:text-white/40 font-medium text-[10px] tracking-[0.4em] uppercase mb-4 flex items-center justify-center gap-4">
              <div className="w-8 h-px bg-slate-300 dark:bg-white/20" />
              Tại sao chọn Vivu?
              <div className="w-8 h-px bg-slate-300 dark:bg-white/20" />
            </span>
            <h2 className="text-4xl md:text-5xl lg:text-7xl font-display font-bold text-slate-900 dark:text-white mb-6 leading-[1.05] tracking-tight transition-colors duration-300">
              Công nghệ tiên phong <br />
              <span className="text-transparent bg-clip-text bg-gradient-to-r from-blue-600 to-cyan-500 dark:from-blue-400 dark:to-cyan-300 italic font-serif font-light">
                nâng tầm trải nghiệm
              </span>
            </h2>
            <p className="text-lg text-slate-600 dark:text-slate-400 leading-relaxed max-w-2xl mx-auto">
              Không chỉ là một ứng dụng đặt vé, Vivu là người bạn đồng hành
              thông minh giúp mọi chuyến đi của bạn trở nên hoàn hảo.
            </p>
          </motion.div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 auto-rows-auto md:auto-rows-[250px]">
          {features.map((feature, index) => (
            <BentoCard key={index} feature={feature} index={index} />
          ))}
        </div>
      </div>
    </section>
  );
}

function BentoCard({ feature, index }) {
  const [isHovered, setIsHovered] = useState(false);
  const cardRef = useRef(null);

  const handleMouseMove = (e) => {
    if (!cardRef.current) return;
    const rect = cardRef.current.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;
    cardRef.current.style.setProperty("--mouse-x", `${x}px`);
    cardRef.current.style.setProperty("--mouse-y", `${y}px`);
  };

  return (
    <motion.div
      initial={{ opacity: 0, y: 40 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-10%" }}
      transition={{
        duration: 0.7,
        delay: index * 0.1,
        ease: [0.22, 1, 0.36, 1],
      }}
      className={`relative rounded-[2rem] overflow-hidden group ${feature.className} bg-white/60 dark:bg-zinc-900/40 backdrop-blur-xl border border-slate-200/50 dark:border-white/5 transition-colors shadow-sm cursor-crosshair`}
      ref={cardRef}
      onMouseMove={handleMouseMove}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
    >
      {/* Spotlight Hover Effect */}
      <div
        className="pointer-events-none absolute -inset-px rounded-[2rem] opacity-0 transition duration-500 group-hover:opacity-100 z-20"
        style={{
          background: isHovered
            ? `radial-gradient(600px circle at var(--mouse-x) var(--mouse-y), rgba(255,255,255,0.1), transparent 40%)`
            : "transparent",
        }}
      />
      <div
        className="pointer-events-none absolute inset-0 opacity-0 transition duration-500 group-hover:opacity-100 dark:hidden z-20"
        style={{
          background: isHovered
            ? `radial-gradient(600px circle at var(--mouse-x) var(--mouse-y), rgba(59,130,246,0.05), transparent 40%)`
            : "transparent",
        }}
      />

      {/* Subtle background gradient based on feature */}
      <div
        className={`absolute inset-0 bg-gradient-to-br ${feature.gradient} opacity-50 dark:opacity-20 z-0`}
      />

      <div className="relative z-10 p-8 flex flex-col h-full justify-between items-start">
        <div className="p-4 rounded-2xl bg-white dark:bg-zinc-800 shadow-sm border border-slate-100 dark:border-white/5 mb-8 group-hover:scale-110 transition-transform duration-500 ease-out">
          {feature.icon}
        </div>

        <div>
          <h3 className="text-2xl font-bold text-slate-900 dark:text-white mb-3 tracking-tight">
            {feature.title}
          </h3>
          <p className="text-slate-500 dark:text-slate-400 leading-relaxed font-light">
            {feature.description}
          </p>
        </div>
      </div>
    </motion.div>
  );
}
