import { motion } from "framer-motion";
import { Star, Quote } from "lucide-react";

const testimonials = [
  {
    name: "Nguyễn Thị Hà",
    location: "Hà Nội, Việt Nam",
    rating: 5,
    text: "Trải nghiệm tuyệt vời nhất! Việc lên kế hoạch rất mượt mà, và các điểm đến thật đúng ý tôi. Chắc chắn sẽ dùng lại cho chuyến đi sau.",
    avatar: "https://i.pravatar.cc/150?img=1",
  },
  {
    name: "Trần Minh Tuấn",
    location: "TP.HCM, Việt Nam",
    rating: 5,
    text: "Từ khách sạn đến vé tham quan, mọi thứ đều được sắp xếp hoàn hảo. Tôi và gia đình đã có một kỳ nghỉ đáng nhớ.",
    avatar: "https://i.pravatar.cc/150?img=13",
  },
  {
    name: "Lê Ngọc Anh",
    location: "Đà Nẵng, Việt Nam",
    rating: 4,
    text: "Gợi ý của AI rất thông minh, giúp tôi khám phá được những quán ăn địa phương siêu ngon mà ít người biết đến.",
    avatar: "https://i.pravatar.cc/150?img=5",
  },
  {
    name: "Phạm Văn Đức",
    location: "Cần Thơ, Việt Nam",
    rating: 5,
    text: "Ứng dụng dễ sử dụng, giao diện đẹp. Rất thích tính năng tùy chỉnh lịch trình, rất linh hoạt và tiện lợi.",
    avatar: "https://i.pravatar.cc/150?img=33",
  },
  {
    name: "Hoàng Mai Chi",
    location: "Hải Phòng, Việt Nam",
    rating: 5,
    text: "Dịch vụ hỗ trợ khách hàng rất nhiệt tình. Tôi gặp chút vấn đề nhỏ nhưng đã được giải quyết ngay lập tức.",
    avatar: "https://i.pravatar.cc/150?img=9",
  },
];

function Testimonial() {
  return (
    <section
      id="stories"
      className="py-32 bg-slate-50 dark:bg-black overflow-hidden relative transition-colors duration-300"
    >
      <div className="max-w-[1440px] mx-auto px-4 sm:px-6 lg:px-8 mb-20 relative z-20">
        <motion.div
          initial={{ opacity: 0, y: 30 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          transition={{ duration: 0.8, type: "spring" }}
          className="text-center max-w-3xl mx-auto"
        >
          <span className="text-slate-500 dark:text-white/40 font-medium text-[10px] tracking-[0.4em] uppercase mb-4 flex items-center justify-center gap-4">
            <div className="w-8 h-px bg-slate-300 dark:bg-white/20" />
            Đánh giá từ khách hàng
            <div className="w-8 h-px bg-slate-300 dark:bg-white/20" />
          </span>
          <h2 className="text-4xl md:text-5xl lg:text-7xl font-display font-bold text-slate-900 dark:text-white mb-6 leading-[1.05] tracking-tight transition-colors duration-300">
            Mọi người nói gì <br />{" "}
            <span className="text-transparent bg-clip-text bg-gradient-to-r from-blue-600 to-cyan-500 dark:from-blue-400 dark:to-cyan-300 italic font-serif font-light">
              về Vivu?
            </span>
          </h2>
          <p className="text-lg text-slate-600 dark:text-white/50 max-w-xl mx-auto font-light leading-relaxed">
            Hàng ngàn du khách đã tin tưởng và lựa chọn nền tảng của chúng tôi
            để kiến tạo nên những hành trình trong mơ.
          </p>
        </motion.div>
      </div>

      {/* Infinite Scrolling Testimonials */}
      <div className="relative w-full z-10 flex flex-col gap-6">
        {/* Soft edge gradients for smooth fade */}
        <div className="absolute left-0 top-0 bottom-0 w-32 md:w-64 bg-gradient-to-r from-slate-50 dark:from-black via-slate-50/80 dark:via-black/80 to-transparent z-20 pointer-events-none"></div>
        <div className="absolute right-0 top-0 bottom-0 w-32 md:w-64 bg-gradient-to-l from-slate-50 dark:from-black via-slate-50/80 dark:via-black/80 to-transparent z-20 pointer-events-none"></div>

        {/* First Row - Scrolls Left */}
        <div className="flex gap-6 animate-scroll-left pl-6 will-change-transform">
          {[...testimonials, ...testimonials, ...testimonials].map(
            (testimonial, index) => (
              <TestimonialCard
                key={`row1-${index}`}
                testimonial={testimonial}
              />
            ),
          )}
        </div>

        {/* Second Row - Scrolls Right (Offset speed and direction) */}
        <div className="flex gap-6 animate-scroll-right pl-6 transform -translate-x-1/4 will-change-transform">
          {[...testimonials, ...testimonials, ...testimonials]
            .reverse()
            .map((testimonial, index) => (
              <TestimonialCard
                key={`row2-${index}`}
                testimonial={testimonial}
              />
            ))}
        </div>
      </div>

      {/* Background glow for depth */}
      <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[800px] h-[400px] bg-blue-900/10 blur-[150px] rounded-[100%] pointer-events-none z-0" />

    </section>
  );
}

const TestimonialCard = ({ testimonial }) => (
  <div className="flex-shrink-0 w-[350px] md:w-[400px] bg-white dark:bg-white/5 backdrop-blur-xl p-8 lg:p-10 rounded-[2rem] border border-slate-200 dark:border-white/10 hover:border-slate-300 dark:hover:border-white/20 hover:bg-slate-50/80 dark:hover:bg-white/10 transition-all duration-500 group relative overflow-hidden shadow-sm dark:shadow-none">
    {/* Subtle noise texture */}
    <div className="absolute inset-0 bg-[url('https://grainy-gradients.vercel.app/noise.svg')] opacity-10 dark:opacity-20 pointer-events-none mix-blend-overlay" />

    <div className="flex justify-between items-start mb-8 relative z-10">
      <div className="flex gap-1 bg-slate-100 dark:bg-black/20 px-3 py-1.5 rounded-full border border-slate-200 dark:border-white/5">
        {[...Array(5)].map((_, i) => (
          <Star
            key={i}
            className={`w-3.5 h-3.5 ${i < testimonial.rating ? "fill-yellow-500 text-yellow-500 drop-shadow-[0_0_8px_rgba(234,179,8,0.5)]" : "text-slate-300 dark:text-white/20"}`}
          />
        ))}
      </div>
      <Quote className="text-slate-200 dark:text-white/10 w-10 h-10 rotate-180 group-hover:text-blue-500/20 transition-colors duration-500" />
    </div>

    <p className="text-slate-700 dark:text-white/70 italic mb-10 text-base leading-relaxed font-light line-clamp-4 relative z-10">
      "{testimonial.text}"
    </p>

    <div className="flex items-center gap-4 pt-6 border-t border-slate-100 dark:border-white/10 relative z-10">
      <div className="w-12 h-12 rounded-full p-[2px] bg-gradient-to-br from-blue-500 to-cyan-400 opacity-80 group-hover:opacity-100 transition-opacity duration-300">
        <img
          alt={testimonial.name}
          className="w-full h-full rounded-full object-cover border-2 border-white dark:border-black"
          src={testimonial.avatar}
          loading="lazy"
        />
      </div>
      <div>
        <h4 className="font-display font-bold text-slate-900 dark:text-white text-lg tracking-wide group-hover:text-blue-600 dark:group-hover:text-blue-100 transition-colors">
          {testimonial.name}
        </h4>
        <p className="text-xs text-slate-500 dark:text-white/40 tracking-wider uppercase mt-1">
          {testimonial.location}
        </p>
      </div>
    </div>
  </div>
);

export default Testimonial;
