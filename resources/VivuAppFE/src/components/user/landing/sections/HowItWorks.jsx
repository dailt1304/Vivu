import { useRef } from "react";
import { motion } from "framer-motion";
import { Search, PenTool, Sliders, Map } from "lucide-react";

export default function HowItWorks() {
  const steps = [
    {
      id: "01",
      title: "Khám phá",
      desc: "Duyệt qua hàng trăm điểm đến hấp dẫn, đọc đánh giá thực tế và tìm nguồn cảm hứng cho chuyến đi của bạn.",
      icon: <Search className="w-6 h-6 text-blue-500" />,
      blob: "bg-blue-400/20 dark:bg-blue-600/20",
      image:
        "https://images.unsplash.com/photo-1488646953014-85cb44e25828?q=80&w=2074&auto=format&fit=crop",
    },
    {
      id: "02",
      title: "Lập kế hoạch",
      desc: "Nhập sở thích, ngân sách và thời gian. AI của chúng tôi sẽ thiết kế một lịch trình tối ưu nhất dành riêng cho bạn.",
      icon: <PenTool className="w-6 h-6 text-indigo-500" />,
      blob: "bg-indigo-400/20 dark:bg-indigo-600/20",
      image:
        "https://images.unsplash.com/photo-1436491865332-7a61a109cc05?q=80&w=2074&auto=format&fit=crop",
    },
    {
      id: "03",
      title: "Cá nhân hóa",
      desc: "Dễ dàng chỉnh sửa, thêm hoặc bớt các hoạt động để hoàn thiện lịch trình theo ý thích ngay trên giao diện trực quan.",
      icon: <Sliders className="w-6 h-6 text-purple-500" />,
      blob: "bg-purple-400/20 dark:bg-purple-600/20",
      image:
        "https://images.unsplash.com/photo-1539635278303-d4002c07eae3?q=80&w=2070&auto=format&fit=crop",
    },
    {
      id: "04",
      title: "Tận hưởng",
      desc: "Lên đường với sự tự tin tuyệt đối. Truy cập lịch trình chi tiết mọi lúc, mọi nơi ngay cả khi không có mạng.",
      icon: <Map className="w-6 h-6 text-cyan-500" />,
      blob: "bg-cyan-400/20 dark:bg-cyan-600/20",
      image:
        "https://images.unsplash.com/photo-1476514525535-07fb3b4ae5f1?q=80&w=2073&auto=format&fit=crop",
    },
  ];

  return (
    <section
      id="how-it-works"
      className="py-32 bg-slate-50 dark:bg-black relative transition-colors duration-300"
    >
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 relative z-10">
        <div className="text-center max-w-3xl mx-auto mb-24">
          <motion.div
            initial={{ opacity: 0, y: 30 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            transition={{ duration: 0.8 }}
          >
            <span className="text-slate-500 dark:text-white/40 font-medium text-[10px] tracking-[0.4em] uppercase mb-4 flex items-center justify-center gap-4">
              <div className="w-8 h-px bg-slate-300 dark:bg-white/20" />
              Quy trình đơn giản
              <div className="w-8 h-px bg-slate-300 dark:bg-white/20" />
            </span>
            <h2 className="text-4xl md:text-5xl lg:text-7xl font-display font-bold text-slate-900 dark:text-white mb-6 leading-[1.05] tracking-tight transition-colors duration-300">
              Lên lịch trình <br />
              <span className="text-transparent bg-clip-text bg-gradient-to-r from-blue-600 to-cyan-500 dark:from-blue-400 dark:to-cyan-300 italic font-serif font-light">
                chỉ với 4 bước
              </span>
            </h2>
            <p className="text-lg text-slate-600 dark:text-slate-400 leading-relaxed font-light max-w-2xl mx-auto">
              Từ ý tưởng ban đầu đến khi khởi hành, quy trình tinh gọn của chúng
              tôi giúp việc chuẩn bị du lịch trở nên dễ dàng và thú vị.
            </p>
          </motion.div>
        </div>

        {/* Parallax Stacking Cards Container */}
        {/* We add pb-64 so the last card has room to stick before the section ends */}
        <div className="relative w-full max-w-5xl mx-auto pb-[20vh]">
          {steps.map((step, index) => (
            <StickyCard
              key={index}
              step={step}
              index={index}
              total={steps.length}
            />
          ))}
        </div>
      </div>
    </section>
  );
}

function StickyCard({ step, index, total }) {
  // We use CSS sticky. The top offset increases slightly for each card to create the stacked look.
  const topOffset = `calc(15vh + ${index * 30}px)`;

  return (
    <motion.div
      className="sticky w-full mb-12 sm:mb-24 last:mb-0"
      style={{ top: topOffset }}
      initial={{ opacity: 0, y: 50 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-10%" }}
      transition={{ duration: 0.6, ease: [0.22, 1, 0.36, 1] }}
    >
      <div className="w-full bg-white dark:bg-zinc-900 border border-slate-200/60 dark:border-white/5 rounded-[2.5rem] p-8 md:p-12 lg:p-16 shadow-[0_20px_40px_-15px_rgba(0,0,0,0.05)] dark:shadow-[0_20px_40px_-15px_rgba(0,0,0,0.5)] overflow-hidden relative flex flex-col md:flex-row items-center gap-12 group transition-colors duration-500">
        {/* Massive Watermark Number */}
        <div className="absolute -right-8 -top-12 text-[12rem] md:text-[20rem] font-display font-black text-slate-900 dark:text-white opacity-[0.02] dark:opacity-[0.03] select-none pointer-events-none group-hover:scale-105 group-hover:opacity-[0.04] transition-all duration-700 ease-out">
          {step.id}
        </div>

        {/* Content */}
        <div className="w-full md:w-1/2 relative z-10 flex flex-col items-start text-left">
          <div className="w-16 h-16 rounded-[1.25rem] bg-slate-50 dark:bg-black border border-slate-100 dark:border-white/5 shadow-sm flex items-center justify-center mb-8 group-hover:scale-110 transition-transform duration-500 shadow-inner">
            {step.icon}
          </div>
          <span className="text-blue-600 dark:text-blue-400 font-bold text-xs tracking-[0.2em] uppercase mb-4 block">
            Bước {step.id}
          </span>
          <h3 className="text-3xl md:text-4xl font-display font-bold text-slate-900 dark:text-white mb-6 tracking-tight">
            {step.title}
          </h3>
          <p className="text-lg text-slate-500 dark:text-slate-400 leading-relaxed font-light">
            {step.desc}
          </p>
        </div>

        {/* Image Display inside Card */}
        <div className="w-full md:w-1/2 h-[300px] md:h-[400px] rounded-3xl bg-slate-100 dark:bg-black/50 border border-slate-200/50 dark:border-white/5 relative overflow-hidden group-hover:border-slate-300 dark:group-hover:border-white/10 transition-colors duration-500 shadow-inner flex-shrink-0">
          {/* Beautiful background image mapped for each step */}
          <img
            src={step.image}
            alt={step.title}
            className="absolute inset-0 w-full h-full object-cover transition-transform duration-[2s] group-hover:scale-105"
          />

          {/* Bottom gradient overlay so the image isn't too overpowering */}
          <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/20 to-transparent opacity-60 dark:opacity-80 transition-opacity duration-500" />

          {/* Animated blob tint overlay mixing with the image */}
          <div
            className={`absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-3/4 h-3/4 ${step.blob} rounded-full blur-[80px] pointer-events-none transition-transform duration-[2s] group-hover:scale-150 mix-blend-overlay opacity-60`}
          />

          <div className="absolute inset-0 bg-[url('https://grainy-gradients.vercel.app/noise.svg')] opacity-20 dark:opacity-30 mix-blend-overlay pointer-events-none" />

          {/* A small elegant badge on the image itself */}
          <div className="absolute bottom-6 left-6 right-6 border border-white/20 rounded-2xl bg-black/30 backdrop-blur-md p-4 flex gap-4 shadow-xl transform translate-y-8 opacity-0 group-hover:translate-y-0 group-hover:opacity-100 transition-all duration-700 ease-[cubic-bezier(0.2,1,0.3,1)]">
            <div className="w-10 h-10 rounded-[1.25rem] bg-white/20 flex items-center justify-center shrink-0">
              {step.icon}
            </div>
            <div className="flex flex-col justify-center">
              <span className="text-white font-medium text-sm">
                Hình ảnh minh họa
              </span>
              <span className="text-white/60 text-xs">
                Vivu mang đến trải nghiệm trực quan
              </span>
            </div>
          </div>
        </div>
      </div>
    </motion.div>
  );
}
