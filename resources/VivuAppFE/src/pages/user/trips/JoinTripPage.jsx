import React, { useState, useEffect } from "react";
import { useParams, useNavigate, useLocation } from "react-router-dom";
import {
  Users,
  ArrowRight,
  Loader2,
  CheckCircle,
  AlertCircle,
} from "lucide-react";
import tripMemberApi from "../../../api/tripMemberApi";
import toast from "../../../utils/toast";
import { useAuth } from "../../../contexts/auth-context";

const JoinTripPage = () => {
  const { inviteCode } = useParams();
  const navigate = useNavigate();
  const location = useLocation();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const { isAuthenticated } = useAuth();

  // Check Auth
  useEffect(() => {
    if (!isAuthenticated) {
      // Redirect to login, save current location to return after login
      navigate("/login", { state: { from: location }, replace: true });
    }
  }, [isAuthenticated, navigate, location]);

  const handleJoin = async () => {
    if (!inviteCode) return;

    setLoading(true);
    setError(null);

    try {
      const res = await tripMemberApi.joinTrip(inviteCode);
      if (res.success) {
        toast.success("Tham gia chuyến đi thành công!");
        // res.data should contain tripId or trip object.
        // Handle both camelCase and PascalCase, and ensure it's not the whole object
        let tripId = res.data?.id || res.data?.Id;

        // If res.data itself is a string/IP (unlikely but possible)
        if (typeof res.data === "string") tripId = res.data;

        // Ensure tripId is valid (truthy and NOT an object)
        if (tripId && typeof tripId !== "object") {
          navigate(`/trips/${tripId}`);
        } else {
          console.error("Invalid Trip ID received:", res.data);
          toast.info(
            "Đã tham gia! Vui lòng tìm chuyến đi trong danh sách của bạn.",
          );
          navigate("/my-trips");
        }
      } else {
        setError(res.message || "Không thể tham gia. Mã có thể không hợp lệ.");
      }
    } catch (err) {
      console.error(err);
      setError("Đã xảy ra lỗi khi tham gia chuyến đi.");
    } finally {
      setLoading(false);
    }
  };

  const handleBackHome = () => {
    navigate("/chat");
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 p-4 relative overflow-hidden">
      {/* Background decoration */}
      <div className="absolute top-0 left-0 w-full h-full overflow-hidden pointer-events-none">
        <div className="absolute top-[-10%] right-[-5%] w-[500px] h-[500px] bg-blue-400/20 rounded-full blur-3xl" />
        <div className="absolute bottom-[-10%] left-[-5%] w-[500px] h-[500px] bg-indigo-400/20 rounded-full blur-3xl" />
      </div>

      <div className="max-w-md w-full bg-white/80 backdrop-blur-xl rounded-3xl shadow-2xl border border-white/50 p-8 relative z-10 text-center">
        <div className="w-20 h-20 bg-gradient-primary rounded-full flex items-center justify-center text-white mx-auto mb-6 shadow-lg shadow-blue-500/30">
          <Users size={32} />
        </div>

        <h1 className="text-2xl font-bold text-gray-800 mb-2">
          Lời mời tham gia
        </h1>
        <p className="text-gray-500 mb-8 leading-relaxed">
          Bạn nhận được lời mời tham gia chuyến đi trên Vivu App.
          <br /> Xác nhận để cùng lên kế hoạch nhé!
        </p>

        {/* Code Display */}
        <div className="bg-slate-50 border border-slate-200 rounded-2xl p-4 mb-8">
          <p className="text-xs text-gray-400 uppercase font-bold tracking-wider mb-1">
            Mã tham gia
          </p>
          <p className="text-2xl font-mono font-bold text-slate-700 tracking-widest selection:bg-blue-100">
            {inviteCode}
          </p>
        </div>

        {error && (
          <div className="flex items-center gap-2 p-3 bg-red-50 text-red-600 rounded-xl text-sm mb-6 text-left">
            <AlertCircle size={18} className="shrink-0" />
            <span>{error}</span>
          </div>
        )}

        <div className="space-y-3">
          <button
            onClick={handleJoin}
            disabled={loading}
            className="w-full py-3.5 bg-gradient-primary hover:opacity-90 text-white font-bold rounded-xl shadow-lg shadow-blue-500/30 flex items-center justify-center gap-2 transition-all active:scale-95 disabled:opacity-70 disabled:pointer-events-none"
          >
            {loading ? (
              <>
                <Loader2 size={20} className="animate-spin" />
                Đang xử lý...
              </>
            ) : (
              <>
                Tham gia ngay
                <ArrowRight size={20} />
              </>
            )}
          </button>

          <button
            onClick={handleBackHome}
            className="w-full py-3.5 text-gray-500 font-medium hover:bg-gray-100 rounded-xl transition-colors"
          >
            Bỏ qua, về trang chủ
          </button>
        </div>
      </div>
    </div>
  );
};

export default JoinTripPage;
