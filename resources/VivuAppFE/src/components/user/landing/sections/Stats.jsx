import {
  motion,
  useInView,
  useSpring,
  useMotionValue,
  useTransform,
} from "framer-motion";
import { useEffect, useRef } from "react";
import { Users, Map, CheckCircle, Star } from "lucide-react";

function Counter({ value }) {
  const ref = useRef(null);
  const isInView = useInView(ref, { once: true, margin: "-50px" });
  const motionValue = useMotionValue(0);

  // Using a slower, more cinematic spring
  const springValue = useSpring(motionValue, {
    damping: 40,
    stiffness: 70,
    mass: 1,
  });

  const displayValue = useTransform(springValue, (latest) =>
    Math.round(latest).toLocaleString("vi-VN"),
  );

  useEffect(() => {
    if (isInView) {
      motionValue.set(value);
    }
  }, [isInView, value, motionValue]);

  return (
    <motion.span ref={ref} className="tabular-nums tracking-tighter">
      {displayValue}
    </motion.span>
  );
}

function Stats() {
  const stats = [
    {
      icon: <Users className="w-6 h-6 text-blue-400" />,
      rawValue: 50000,
      suffix: "+",
      label: "Du khách hài lòng",
    },
    {
      icon: <Map className="w-6 h-6 text-cyan-400" />,
      rawValue: 850,
      suffix: "+",
      label: "Điểm đến",
    },
    {
      icon: <CheckCircle className="w-6 h-6 text-emerald-400" />,
      rawValue: 12500,
      suffix: "+",
      label: "Chuyến đi tuyệt vời",
    },
    {
      icon: <Star className="w-6 h-6 text-amber-400" />,
      rawValue: 4.9,
      suffix: "/5",
      isFloat: true,
      label: "Đánh giá trung bình",
    },
  ];

  return (
    <section className="py-32 bg-white dark:bg-black relative flex items-center justify-center min-h-[60vh] transition-colors duration-300">
      {/* Ambient Gradient Core - The "Star" of this section */}
      <div className="absolute inset-0 flex items-center justify-center overflow-hidden pointer-events-none">
        <div className="w-[80vw] max-w-[1000px] h-[300px] bg-gradient-to-r from-blue-600 via-cyan-500 to-indigo-600 rounded-full blur-[120px] opacity-15 dark:opacity-30 mix-blend-multiply dark:mix-blend-screen animate-pulse duration-[8s]" />
      </div>

      {/* Grid texture overlay */}
      <div className="absolute inset-0 bg-[url('https://grainy-gradients.vercel.app/noise.svg')] opacity-10 dark:opacity-20 pointer-events-none mix-blend-overlay" />
      <div className="absolute inset-0 bg-grid bg-[size:4rem_4rem] mask-radial-gradient pointer-events-none" />

      <div className="max-w-[1440px] mx-auto px-4 sm:px-6 lg:px-8 relative z-10 w-full">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-12 lg:gap-8 text-center divide-y sm:divide-y-0 sm:divide-x divide-slate-200 dark:divide-white/5 border-y border-slate-200 dark:border-white/5 py-16">
          {stats.map((stat, index) => (
            <motion.div
              key={index}
              initial={{ opacity: 0, scale: 0.9, y: 20 }}
              whileInView={{ opacity: 1, scale: 1, y: 0 }}
              viewport={{ once: true, margin: "-10%" }}
              transition={{
                duration: 0.8,
                delay: index * 0.15,
                type: "spring",
                bounce: 0,
              }}
              className="flex flex-col items-center pt-8 sm:pt-0 group cursor-default"
            >
              <div className="mb-6 w-14 h-14 rounded-2xl bg-slate-50 dark:bg-white/[0.03] border border-slate-200 dark:border-white/10 flex items-center justify-center shadow-lg group-hover:scale-110 group-hover:bg-slate-100 dark:group-hover:bg-white/10 group-hover:border-slate-300 dark:group-hover:border-white/20 transition-all duration-500 relative">
                <div className="absolute inset-0 bg-blue-500/10 dark:bg-blue-500/20 blur-xl opacity-0 group-hover:opacity-100 transition-opacity duration-500 rounded-full" />
                <div className="relative z-10">{stat.icon}</div>
              </div>

              <div className="flex items-baseline justify-center font-display font-black text-slate-900 dark:text-white mb-2 group-hover:-translate-y-1 transition-transform duration-500">
                <span className="text-6xl md:text-7xl lg:text-8xl tracking-tighter bg-clip-text text-transparent bg-gradient-to-b from-slate-900 dark:from-white to-slate-500 dark:to-white/50">
                  {stat.isFloat ? (
                    <span>{stat.rawValue}</span>
                  ) : (
                    <Counter value={stat.rawValue} />
                  )}
                </span>
                <span className="text-2xl md:text-3xl text-slate-500 dark:text-white/50 font-bold ml-1">
                  {stat.suffix}
                </span>
              </div>

              <p className="text-slate-500 dark:text-white/60 text-sm md:text-base font-medium uppercase tracking-[0.2em] group-hover:text-blue-600 dark:group-hover:text-blue-200 transition-colors duration-500">
                {stat.label}
              </p>
            </motion.div>
          ))}
        </div>
      </div>
    </section>
  );
}

export default Stats;
