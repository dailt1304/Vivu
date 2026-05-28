import { motion } from "framer-motion";
import { Calendar, User, ArrowUpRight } from "lucide-react";
import { Link } from "react-router-dom";

// Vite requires importing local assets so they can be processed and fingerprinted
import imgHoiAn from "../../../../assets/images/hoi_an.jpg";
import imgTaXua from "../../../../assets/images/picture-1-1735864765-23-width1200height690.png";
import imgHaNoi from "../../../../assets/images/pho-ta-hien-1.jpg";

const blogPosts = [
  {
    title: "10 Món Ăn Đường Phố Hà Nội Không Thể Bỏ Qua",
    excerpt:
      "Khám phá hương vị tinh tế của phở, bún chả và cà phê trứng giữa lòng thủ đô ngàn năm văn hiến.",
    image: imgHaNoi,
    author: "Minh Anh",
    date: "15 Th5, 2024",
    readTime: "5 phút đọc",
    category: "Ẩm thực",
  },
  {
    title: "Kinh Nghiệm Du Lịch Hội An Tự Túc 3 Ngày 2 Đêm",
    excerpt:
      "Lịch trình chi tiết khám phá phố cổ, thả đèn hoa đăng và thưởng thức cao lầu trứ danh.",
    image: imgHoiAn,
    author: "Tuấn Hoàng",
    date: "20 Th5, 2024",
    readTime: "8 phút đọc",
    category: "Cẩm nang",
  },
  {
    title: "Săn Mây Tà Xùa: Hành Trình Chạm Tới Thiên Đường",
    excerpt: "Bí kíp săn mây thành công và điểm sống ảo cực chất.",
    image: imgTaXua,
    author: "Hương Ly",
    date: "10 Th6, 2024",
    readTime: "6 phút đọc",
    category: "Khám phá",
  },
];

export default function Experiences() {
  const [heroPost, ...secondaryPosts] = blogPosts;

  return (
    <section
      id="experiences"
      className="py-32 bg-white dark:bg-black relative overflow-hidden transition-colors duration-300"
    >
      {/* Cinematic Ambient Lighting */}
      <div className="absolute top-0 right-1/4 w-[800px] h-[800px] bg-blue-900/10 rounded-full blur-[120px] pointer-events-none" />
      <div className="absolute bottom-[-20%] left-[-10%] w-[600px] h-[600px] bg-cyan-900/10 rounded-full blur-[100px] pointer-events-none" />

      <div className="max-w-[1440px] mx-auto px-4 sm:px-6 lg:px-8 relative z-10">
        <motion.div
          initial={{ opacity: 0, y: 30 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, margin: "-10%" }}
          transition={{ duration: 0.8, type: "spring" }}
          className="flex flex-col md:flex-row md:items-end justify-between gap-6 mb-16"
        >
          <div>
            <span className="text-slate-500 dark:text-white/40 font-medium text-[10px] tracking-[0.4em] uppercase mb-4 flex items-center gap-4">
              <div className="w-8 h-px bg-slate-300 dark:bg-white/20" />
              Chia sẻ kinh nghiệm
            </span>
            <h2 className="text-5xl md:text-6xl lg:text-8xl font-display font-bold text-slate-900 dark:text-white leading-[0.95] tracking-tight mb-8 transition-colors duration-300">
              Nhật ký <br />
              <span className="text-transparent bg-clip-text bg-gradient-to-r from-blue-600 to-cyan-500 dark:from-blue-400 dark:to-cyan-300 italic font-serif font-light">
                hành trình
              </span>
            </h2>
          </div>
          <Link
            to="/inspiration"
            className="text-slate-700 dark:text-white hover:text-slate-900 dark:hover:text-white flex items-center gap-2 group transition-all text-[10px] font-bold uppercase tracking-[0.2em] bg-slate-100 dark:bg-white/5 hover:bg-slate-200 dark:hover:bg-white/10 px-6 py-4 rounded-full border border-slate-200 dark:border-white/5 hover:border-slate-300 dark:hover:border-white/20 active:scale-95 duration-300"
          >
            Đọc thêm câu chuyện
            <ArrowUpRight className="w-4 h-4 group-hover:translate-x-1 group-hover:-translate-y-1 transition-transform duration-300" />
          </Link>
        </motion.div>

        {/* Asymmetric Bento Grid */}
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
          {/* Hero Post - Spans 7 columns */}
          <motion.div
            initial={{ opacity: 0, y: 30 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true, margin: "-5%" }}
            transition={{ duration: 0.8, delay: 0.1, type: "spring" }}
            className="lg:col-span-7 group cursor-pointer"
          >
            <div className="relative rounded-[2.5rem] overflow-hidden bg-zinc-950/50 border border-white/[0.05] h-[500px] lg:h-[600px]">
              <img
                src={heroPost.image}
                alt={heroPost.title}
                className="absolute inset-0 w-full h-full object-cover transition-transform duration-[2s] ease-[cubic-bezier(0.2,1,0.3,1)] group-hover:scale-105 brightness-[0.7] group-hover:brightness-[0.9]"
              />
              <div className="absolute inset-0 bg-black/10 bg-[url('https://grainy-gradients.vercel.app/noise.svg')] mix-blend-overlay opacity-50 z-10 pointer-events-none" />
              <div className="absolute inset-0 bg-gradient-to-t from-black/90 via-black/30 to-transparent z-10" />

              <div className="absolute inset-x-0 bottom-0 p-8 lg:p-12 z-20 flex flex-col justify-end h-full">
                <div className="flex items-center gap-3 mb-6">
                  <span className="px-4 py-1.5 rounded-full bg-white/10 backdrop-blur-md border border-white/10 text-white text-xs font-bold uppercase tracking-wider">
                    {heroPost.category}
                  </span>
                  <span className="text-white/60 text-sm flex items-center gap-1.5 font-medium">
                    <Calendar className="w-4 h-4" /> {heroPost.date}
                  </span>
                </div>

                <h3 className="text-3xl lg:text-5xl font-display font-bold text-white mb-4 leading-tight group-hover:text-blue-200 transition-colors duration-500">
                  {heroPost.title}
                </h3>

                <p className="text-white/70 text-base lg:text-lg max-w-xl line-clamp-2 md:line-clamp-3 mb-8 font-light leading-relaxed">
                  {heroPost.excerpt}
                </p>

                <div className="flex items-center gap-4 mt-auto pt-6 border-t border-white/10">
                  <div className="w-10 h-10 rounded-full bg-gradient-to-br from-blue-500 to-cyan-400 p-[2px]">
                    <div className="w-full h-full rounded-full bg-black flex items-center justify-center overflow-hidden">
                      <User className="w-5 h-5 text-white/50" />
                    </div>
                  </div>
                  <div className="flex flex-col">
                    <span className="text-white text-sm font-bold">
                      {heroPost.author}
                    </span>
                    <span className="text-white/40 text-xs uppercase tracking-widest">
                      {heroPost.readTime}
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </motion.div>

          {/* Secondary Posts - Spans 5 columns */}
          <div className="lg:col-span-5 grid grid-rows-2 gap-6">
            {secondaryPosts.map((post, index) => (
              <motion.div
                key={index}
                initial={{ opacity: 0, x: 30 }}
                whileInView={{ opacity: 1, x: 0 }}
                viewport={{ once: true, margin: "-5%" }}
                transition={{
                  duration: 0.8,
                  delay: 0.2 + index * 0.1,
                  type: "spring",
                }}
                className="relative rounded-[2rem] overflow-hidden bg-slate-50 dark:bg-zinc-900 border border-slate-200 dark:border-white/[0.05] group cursor-pointer flex flex-col sm:flex-row min-h-[240px] lg:h-full lg:min-h-0 transition-colors duration-300"
              >
                {/* Image side */}
                <div className="w-full sm:w-2/5 h-48 sm:h-full relative overflow-hidden">
                  <img
                    src={post.image}
                    alt={post.title}
                    className="absolute inset-0 w-full h-full object-cover transition-transform duration-[2s] ease-[cubic-bezier(0.2,1,0.3,1)] group-hover:scale-110 brightness-[1] dark:brightness-[0.8]"
                  />
                  <div className="absolute inset-0 bg-black/10 bg-[url('https://grainy-gradients.vercel.app/noise.svg')] mix-blend-overlay opacity-20 dark:opacity-50 z-10 pointer-events-none" />
                  <div className="absolute top-4 left-4 z-20">
                    <span className="px-3 py-1 rounded-lg bg-black/40 backdrop-blur-md border border-white/10 text-white text-[10px] font-bold uppercase tracking-wider">
                      {post.category}
                    </span>
                  </div>
                </div>

                {/* Content side */}
                <div className="w-full sm:w-3/5 p-6 md:p-8 flex flex-col justify-center bg-white/80 dark:bg-zinc-950/80 backdrop-blur-3xl z-10 transition-colors duration-300">
                  <div className="flex items-center gap-2 text-slate-500 dark:text-white/40 text-xs font-medium mb-3">
                    <Calendar className="w-3.5 h-3.5" /> {post.date}
                  </div>
                  <h3 className="text-xl font-display font-bold text-slate-900 dark:text-white mb-3 leading-snug group-hover:text-cyan-600 dark:group-hover:text-cyan-200 transition-colors duration-300">
                    {post.title}
                  </h3>
                  <div className="mt-auto flex items-center justify-between">
                    <span className="text-slate-600 dark:text-white/60 text-sm font-medium flex items-center gap-2">
                      <div className="w-6 h-6 rounded-full bg-slate-200 dark:bg-white/10 flex items-center justify-center">
                        <User className="w-3 h-3 text-slate-400 dark:text-white/50" />
                      </div>
                      {post.author}
                    </span>
                    <div className="w-8 h-8 rounded-full border border-white/10 flex items-center justify-center bg-white/5 group-hover:bg-gradient-primary group-hover:text-white group-hover:border-transparent transition-all duration-300">
                      <ArrowUpRight className="w-4 h-4 text-white/50 group-hover:text-white" />
                    </div>
                  </div>
                </div>
              </motion.div>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}
