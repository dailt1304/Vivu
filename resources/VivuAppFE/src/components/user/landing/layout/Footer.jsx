import { Globe } from "lucide-react";
import logoImg from "../../../../assets/images/vivu_logo-remove-background.com.png";

export default function Footer() {
  return (
    <footer className="bg-gray-900 text-gray-400 py-12 border-t border-gray-800">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-5 gap-8 mb-12">
          <div className="col-span-2 lg:col-span-2">
            <a className="flex items-center gap-2 mb-4" href="#">
              <img
                src={logoImg}
                alt="Vivu Logo"
                className="h-10 md:h-12 w-auto object-contain"
              />
            </a>
            <p className="mb-6 max-w-sm">
              Người bạn đồng hành tin cậy cho những chuyến đi khám phá thế giới.
              Chúng tôi giúp việc lên kế hoạch du lịch trở nên dễ dàng và đáng
              nhớ hơn bao giờ hết.
            </p>
          </div>
          <div>
            <h4 className="text-white font-bold mb-4">Về Vivu</h4>
            <ul className="space-y-2 text-sm">
              <li>
                <a className="hover:text-primary transition" href="#">
                  Về chúng tôi
                </a>
              </li>
              <li>
                <a className="hover:text-primary transition" href="#">
                  Tuyển dụng
                </a>
              </li>
              <li>
                <a className="hover:text-primary transition" href="#">
                  Báo chí
                </a>
              </li>
              <li>
                <a className="hover:text-primary transition" href="#">
                  Blog du lịch
                </a>
              </li>
              <li>
                <a className="hover:text-primary transition" href="#">
                  Hợp tác
                </a>
              </li>
            </ul>
          </div>
          <div>
            <h4 className="text-white font-bold mb-4">Hỗ trợ</h4>
            <ul className="space-y-2 text-sm">
              <li>
                <a className="hover:text-primary transition" href="#">
                  Trung tâm trợ giúp
                </a>
              </li>
              <li>
                <a className="hover:text-primary transition" href="#">
                  Liên hệ
                </a>
              </li>
              <li>
                <a className="hover:text-primary transition" href="#">
                  Câu hỏi thường gặp
                </a>
              </li>
              <li>
                <a className="hover:text-primary transition" href="#">
                  Bảo hiểm du lịch
                </a>
              </li>
              <li>
                <a className="hover:text-primary transition" href="#">
                  Chính sách hoàn hủy
                </a>
              </li>
            </ul>
          </div>
          <div>
            <h4 className="text-white font-bold mb-4">Điểm đến</h4>
            <ul className="space-y-2 text-sm">
              <li>
                <a className="hover:text-primary transition" href="#">
                  Miền Bắc
                </a>
              </li>
              <li>
                <a className="hover:text-primary transition" href="#">
                  Miền Trung
                </a>
              </li>
              <li>
                <a className="hover:text-primary transition" href="#">
                  Miền Nam
                </a>
              </li>
              <li>
                <a className="hover:text-primary transition" href="#">
                  Quốc tế
                </a>
              </li>
              <li>
                <a className="hover:text-primary transition" href="#">
                  Tour hot
                </a>
              </li>
            </ul>
          </div>
        </div>
        <div className="border-t border-gray-800 pt-8 flex flex-col md:flex-row justify-between items-center gap-4 text-xs">
          <p>© 2026 Vivu. Bảo lưu mọi quyền.</p>
          <div className="flex space-x-6">
            <a className="hover:text-white transition" href="#">
              Chính sách bảo mật
            </a>
            <a className="hover:text-white transition" href="#">
              Điều khoản sử dụng
            </a>
            <a className="hover:text-white transition" href="#">
              Chính sách cookie
            </a>
            <a className="hover:text-white transition" href="#">
              Sitemap
            </a>
          </div>
        </div>
      </div>
    </footer>
  );
}
