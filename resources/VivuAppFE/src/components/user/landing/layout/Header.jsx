import { useState, useEffect } from "react";
import { Link } from "react-router-dom";
import { motion } from "framer-motion";
import {
  Menu,
  X,
  Search,
  ChevronDown,
  Mail,
  Phone,
  MapPin,
  Instagram,
  Youtube,
  Facebook,
  Globe,
} from "lucide-react";

import logoImg from "../../../../assets/images/vivu_logo-remove-background.com.png";

export default function Header() {
  const [scrolled, setScrolled] = useState(false);

  useEffect(() => {
    const handleScroll = () => {
      setScrolled(window.scrollY > 50);
    };
    window.addEventListener("scroll", handleScroll, { passive: true });
    return () => window.removeEventListener("scroll", handleScroll);
  }, []);

  return (
    <>
      <motion.header
        initial={{ y: -100, opacity: 0 }}
        animate={{ y: 0, opacity: 1 }}
        transition={{ duration: 0.5 }}
        className={`fixed w-full z-50 transition-all duration-300 ${
          scrolled
            ? "bg-white/95 dark:bg-background-dark/95 backdrop-blur-md border-b border-gray-100 dark:border-gray-800 shadow-md"
            : "bg-transparent border-b border-white/10"
        }`}
      >
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-20">
            <div className="flex items-center">
              <a className="flex items-center gap-2" href="#">
                <img
                  src={logoImg}
                  alt="Vivu Logo"
                  className="h-8 md:h-10 w-auto object-contain"
                />
              </a>
            </div>
            <nav className="hidden md:flex space-x-8">
              <a
                className={`hover:text-primary font-medium transition ${
                  scrolled ? "text-gray-600 dark:text-gray-300" : "text-white"
                }`}
                href="#destinations"
              >
                Điểm đến
              </a>
              <a
                className={`hover:text-primary font-medium transition ${
                  scrolled ? "text-gray-600 dark:text-gray-300" : "text-white"
                }`}
                href="#how-it-works"
              >
                Cách thức hoạt động
              </a>
              <a
                className={`hover:text-primary font-medium transition ${
                  scrolled ? "text-gray-600 dark:text-gray-300" : "text-white"
                }`}
                href="#experiences"
              >
                Trải nghiệm
              </a>
              <a
                className={`hover:text-primary font-medium transition ${
                  scrolled ? "text-gray-600 dark:text-gray-300" : "text-white"
                }`}
                href="#stories"
              >
                Câu chuyện
              </a>
            </nav>
            <div className="flex items-center space-x-4">
              <Link
                className={`hover:text-primary font-medium transition hidden sm:block ${
                  scrolled ? "text-gray-600 dark:text-gray-300" : "text-white"
                }`}
                to="/login"
              >
                Đăng nhập
              </Link>
              <Link
                className="bg-gradient-primary hover:opacity-90 text-white px-5 py-2.5 rounded-full font-medium transition shadow-lg shadow-blue-500/30"
                to="/register"
              >
                Bắt đầu
              </Link>
            </div>
          </div>
        </div>
      </motion.header>
    </>
  );
}
