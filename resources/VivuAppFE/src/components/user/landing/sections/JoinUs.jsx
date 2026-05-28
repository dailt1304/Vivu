import { motion } from "framer-motion";
import { ArrowRight, Star, ShieldCheck, MapPin, Sparkles } from "lucide-react";
import { Link } from "react-router-dom";

export default function JoinUs() {
  return (
    <section className="py-32 bg-slate-50 dark:bg-black overflow-hidden relative transition-colors duration-300">
      {/* Immersive mesh background */}
      <div className="absolute inset-0 z-0">
        <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[80vw] h-[80vh] bg-blue-900/20 blur-[150px] rounded-full mix-blend-screen pointer-events-none" />
        <div className="absolute top-0 right-0 w-[500px] h-[500px] bg-cyan-900/20 blur-[120px] rounded-full mix-blend-screen pointer-events-none" />
        <div className="absolute bottom-0 left-0 w-[600px] h-[600px] bg-indigo-900/20 blur-[130px] rounded-full mix-blend-screen pointer-events-none" />
      </div>

      <div className="max-w-[1440px] mx-auto px-4 sm:px-6 lg:px-8 relative z-10">
        <motion.div
          initial={{ opacity: 0, scale: 0.95, y: 40 }}
          whileInView={{ opacity: 1, scale: 1, y: 0 }}
          viewport={{ once: true, margin: "-10%" }}
          transition={{ duration: 1, type: "spring", bounce: 0 }}
          className="relative rounded-[3rem] overflow-hidden bg-white/80 dark:bg-zinc-950/80 border border-slate-200 dark:border-white/10 shadow-[0_0_100px_-20px_rgba(59,130,246,0.1)] dark:shadow-[0_0_100px_-20px_rgba(59,130,246,0.2)] backdrop-blur-3xl transition-colors duration-300"
        >
          {/* Subtle noise over the card */}
          <div className="absolute inset-0 bg-[url('https://grainy-gradients.vercel.app/noise.svg')] opacity-30 mix-blend-overlay pointer-events-none" />
          <div className="absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-white/30 to-transparent" />

          <div className="relative z-10 grid lg:grid-cols-2 gap-16 p-12 md:p-16 lg:p-24 items-center">
            {/* Left Content Column */}
            <div className="flex flex-col items-start text-left">
              <motion.div
                initial={{ opacity: 0, y: 20 }}
                whileInView={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.2, duration: 0.8 }}
                className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-slate-100 dark:bg-white/5 border border-slate-200 dark:border-white/10 text-slate-700 dark:text-white/80 text-xs font-bold uppercase tracking-widest mb-8"
              >
                <Sparkles className="w-4 h-4 text-cyan-500 dark:text-cyan-400" />
                <span>Thế hệ du lịch mới</span>
              </motion.div>

              <motion.h2
                initial={{ opacity: 0, y: 20 }}
                whileInView={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.3, duration: 0.8 }}
                className="text-5xl lg:text-7xl font-display font-black text-slate-900 dark:text-white mb-6 leading-[1.05] tracking-tight transition-colors duration-300"
              >
                Sẵn sàng cho <br />
                <span className="text-transparent bg-clip-text bg-gradient-to-r from-blue-600 to-cyan-500 dark:from-blue-400 dark:to-cyan-300">
                  chuyến đi trong mơ?
                </span>
              </motion.h2>

              <motion.p
                initial={{ opacity: 0, y: 20 }}
                whileInView={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.4, duration: 0.8 }}
                className="text-lg text-slate-600 dark:text-white/50 mb-12 leading-relaxed max-w-lg font-light transition-colors duration-300"
              >
                Đừng để việc lên kế hoạch làm bạn đau đầu. Vivu AI Intelligence
                sẽ cá nhân hóa mọi trải nghiệm dựa trên sở thích của chính bạn.
              </motion.p>

              <motion.div
                initial={{ opacity: 0, y: 20 }}
                whileInView={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.5, duration: 0.8 }}
                className="flex flex-col sm:flex-row gap-6 w-full sm:w-auto"
              >
                <Link
                  to="/register"
                  className="relative overflow-hidden group bg-gradient-primary text-white dark:bg-white dark:text-black px-10 py-5 rounded-full font-bold text-sm tracking-wide transition-all duration-500 hover:scale-105 hover:shadow-[0_0_40px_-10px_rgba(0,0,0,0.2)] dark:hover:shadow-[0_0_40px_-10px_rgba(255,255,255,0.5)] flex items-center justify-center gap-3"
                >
                  <span className="relative z-10">Bắt đầu miễn phí</span>
                  <ArrowRight className="w-4 h-4 relative z-10 group-hover:translate-x-1.5 transition-transform duration-300" />
                  <div className="absolute inset-0 bg-gradient-to-r from-blue-600 to-blue-500 dark:from-blue-100 dark:to-white opacity-0 group-hover:opacity-100 transition-opacity duration-500 z-0" />
                </Link>
                <button className="px-10 py-5 rounded-full font-bold text-sm tracking-wide text-slate-700 dark:text-white border border-slate-300 dark:border-white/20 hover:bg-slate-100 dark:hover:bg-white/10 hover:border-slate-400 dark:hover:border-white/40 transition-all duration-500 flex items-center justify-center gap-3 active:scale-95">
                  <ShieldCheck className="w-4 h-4 text-slate-500 dark:text-white/60" />
                  Tìm hiểu thêm
                </button>
              </motion.div>

              {/* <motion.div
                initial={{ opacity: 0 }}
                whileInView={{ opacity: 1 }}
                transition={{ delay: 0.8, duration: 1 }}
                className="mt-12 flex items-center gap-5 text-sm text-slate-600 dark:text-white/40 font-medium transition-colors"
              >
                <div className="flex -space-x-3">
                  {[1, 2, 3, 4].map((i) => (
                    <div
                      key={i}
                      className="w-10 h-10 rounded-full border-2 border-black bg-zinc-800 overflow-hidden relative"
                    >
                      <img
                        src={`https://i.pravatar.cc/100?img=${i + 15}`}
                        alt="user"
                        className="w-full h-full object-cover"
                        loading="lazy"
                      />
                    </div>
                  ))}
                </div>
                <div className="flex flex-col">
                  <div className="flex text-yellow-500 gap-0.5 mb-0.5">
                    {[1, 2, 3, 4, 5].map((i) => (
                      <Star key={i} className="w-3.5 h-3.5 fill-current" />
                    ))}
                  </div>
                  <p>Được yêu thích bởi 50,000+ du khách</p>
                </div>
              </motion.div> */}
            </div>

            {/* Right Mockup Column (Vision Pro Style) */}
            <div className="hidden lg:flex justify-end relative h-full items-center">
              {/* Main Card */}
              <motion.div
                initial={{ y: 40, opacity: 0, rotateY: 10 }}
                whileInView={{ y: 0, opacity: 1, rotateY: 0 }}
                transition={{ delay: 0.4, duration: 1, type: "spring" }}
                className="relative z-10 bg-white/50 dark:bg-white/[0.02] backdrop-blur-[40px] border border-slate-200 dark:border-white/10 p-8 rounded-[2.5rem] shadow-[0_30px_60px_-15px_rgba(0,0,0,0.1)] dark:shadow-[0_30px_60px_-15px_rgba(0,0,0,0.8)] w-[400px] transform hover:-translate-y-2 hover:border-slate-300 dark:hover:border-white/20 transition-all duration-500"
              >
                {/* Inner glow */}
                <div className="absolute inset-0 bg-gradient-to-b from-white/50 dark:from-white/5 to-transparent opacity-50 pointer-events-none rounded-[2.5rem]" />

                <div className="flex items-center gap-5 mb-8">
                  <div className="w-16 h-16 bg-gradient-to-br from-blue-500 to-cyan-400 rounded-2xl flex items-center justify-center text-white shadow-[0_0_20px_rgba(59,130,246,0.3)]">
                    <MapPin className="w-7 h-7" />
                  </div>
                  <div>
                    <h4 className="text-slate-900 dark:text-white font-display font-bold text-2xl tracking-tight">
                      Hà Giang Loop
                    </h4>
                    <p className="text-cyan-600 dark:text-cyan-300 font-medium text-sm mt-1">
                      Chuyến đi 3 ngày 2 đêm
                    </p>
                  </div>
                </div>

                {/* Skeleton lines representing schedule/AI text */}
                <div className="space-y-4 mb-8">
                  <div className="h-2.5 bg-slate-200 dark:bg-white/10 rounded-full w-full overflow-hidden relative">
                    <div className="absolute inset-0 bg-gradient-to-r from-transparent via-slate-300 dark:via-white/20 to-transparent translate-x-[-100%] animate-[shimmer_2s_infinite]" />
                  </div>
                  <div className="h-2.5 bg-slate-200 dark:bg-white/10 rounded-full w-4/5 overflow-hidden relative">
                    <div className="absolute inset-0 bg-gradient-to-r from-transparent via-slate-300 dark:via-white/20 to-transparent translate-x-[-100%] animate-[shimmer_2s_infinite_0.2s]" />
                  </div>
                  <div className="h-2.5 bg-slate-200 dark:bg-white/10 rounded-full w-5/6 overflow-hidden relative">
                    <div className="absolute inset-0 bg-gradient-to-r from-transparent via-slate-300 dark:via-white/20 to-transparent translate-x-[-100%] animate-[shimmer_2s_infinite_0.4s]" />
                  </div>
                </div>

                <div className="p-5 rounded-2xl bg-slate-100 dark:bg-white/5 border border-slate-200 dark:border-white/5 flex justify-between items-center group/btn cursor-pointer">
                  <div>
                    <p className="text-slate-500 dark:text-white/40 text-xs uppercase tracking-widest font-bold mb-1">
                      Dự kiến
                    </p>
                    <span className="text-slate-900 dark:text-white font-display font-bold text-2xl tracking-tight">
                      2.500.000đ
                    </span>
                  </div>
                  <div className="w-10 h-10 rounded-full bg-blue-500 flex items-center justify-center group-hover/btn:bg-cyan-500 dark:group-hover/btn:bg-cyan-400 transition-colors shadow-lg">
                    <ArrowRight className="w-5 h-5 text-white" />
                  </div>
                </div>
              </motion.div>

              {/* Floating Notification Snippet */}
              <motion.div
                initial={{ y: -30, opacity: 0, x: -40, scale: 0.9 }}
                whileInView={{ y: 0, opacity: 1, x: -30, scale: 1 }}
                transition={{ delay: 0.7, duration: 1, type: "spring" }}
                className="absolute top-10 left-[-40px] z-20 bg-white/90 dark:bg-black/60 shadow-[0_20px_40px_-10px_rgba(0,0,0,0.1)] dark:shadow-[0_20px_40px_-10px_rgba(0,0,0,0.5)] backdrop-blur-3xl border border-slate-200 dark:border-white/10 p-5 rounded-[1.5rem] w-64 flex items-center gap-4 hover:border-slate-300 dark:hover:border-white/20 transition-colors cursor-default"
              >
                <div className="w-12 h-12 rounded-full p-[1px] bg-gradient-to-br from-purple-500 to-blue-500 shrink-0">
                  <div className="w-full h-full rounded-full bg-slate-50 dark:bg-black flex items-center justify-center">
                    <Sparkles className="w-5 h-5 text-purple-500 dark:text-purple-400" />
                  </div>
                </div>
                <div>
                  <p className="text-slate-900 dark:text-white text-sm font-bold leading-tight mb-1">
                    Lịch trình đã xong!
                  </p>
                  <p className="text-slate-500 dark:text-white/40 text-xs">
                    AI vừa tối ưu hóa lộ trình cho bạn.
                  </p>
                </div>
              </motion.div>
            </div>
          </div>
        </motion.div>
      </div>

    </section>
  );
}
