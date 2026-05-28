import Header from "../../components/user/landing/layout/Header";
import Hero from "../../components/user/landing/sections/Hero";
import Destinations from "../../components/user/landing/sections/Destination";
import Experiences from "../../components/user/landing/sections/Experience";
import Footer from "../../components/user/landing/layout/Footer";
import WhyChooseUs from "@/components/user/landing/sections/WhyChooseUs";
import HowItWorks from "@/components/user/landing/sections/HowItWorks";
import Testimonial from "@/components/user/landing/sections/Testimonial";
import Stats from "@/components/user/landing/sections/Stats";
import JoinUs from "@/components/user/landing/sections/JoinUs";
import ScrollToTop from "../../components/user/landing/layout/ScrollToTop";
import { motion, useScroll, useSpring, useTransform } from "framer-motion";

export default function LandingPage() {
  const { scrollYProgress } = useScroll();
  const scaleX = useSpring(scrollYProgress, {
    stiffness: 100,
    damping: 30,
    restDelta: 0.001,
  });

  return (
    <div className="relative bg-white dark:bg-black text-slate-900 dark:text-white transition-colors duration-300 min-h-screen">
      {/* Global Progress Bar */}
      <motion.div
        className="fixed top-0 left-0 right-0 h-[2px] bg-black/80 dark:bg-white/80 origin-[0%] z-50"
        style={{ scaleX }}
      />

      <Header />
      <main className="relative z-10">
        <Hero />
        <WhyChooseUs />
        <Destinations />
        <HowItWorks />
        <Experiences />
        <Testimonial />
        {/* <Stats /> */}
        <JoinUs />
      </main>
      <ScrollToTop />
      <Footer />
    </div>
  );
}
