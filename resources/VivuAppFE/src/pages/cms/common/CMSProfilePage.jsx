import React, { useState, useMemo, useCallback, useEffect } from "react";
import { useAuth } from "../../../contexts/auth-context";
import { useUpdateProfile } from "../../../hooks/users/useUsers";
import { toast } from "sonner";
import { User, Mail, Phone, ShieldCheck, Loader2 } from "lucide-react";
import { Input } from "../../../components/ui/input";

const CMSProfilePage = () => {
  const { user, refreshUser } = useAuth();
  const { updateProfile, isUpdating } = useUpdateProfile();
  
  // -- THEMING --
  const { focusRing, btnColor, badgeBg, iconColor, isAdmin } = useMemo(() => {
    const isAdminRole = user?.roles?.map(r => r.toUpperCase()).includes("ADMIN");
    return {
      focusRing: isAdminRole ? "focus:border-emerald-500 focus:ring-emerald-500/20" : "focus:border-blue-500 focus:ring-blue-500/20",
      btnColor: isAdminRole ? "bg-emerald-600 hover:bg-emerald-700 disabled:bg-emerald-400" : "bg-blue-600 hover:bg-blue-700 disabled:bg-blue-400",
      badgeBg: isAdminRole ? "bg-emerald-100 text-emerald-700" : "bg-blue-100 text-blue-700",
      iconColor: isAdminRole ? "text-emerald-500" : "text-blue-500",
      isAdmin: isAdminRole,
    };
  }, [user?.roles]);

  // -- PROFILE FORM STATE --
  const [formData, setFormData] = useState({
    fullName: user?.fullName || user?.name || "",
    phone: user?.phone || "",
  });

  // ✅ Vercel rule: rerender-derived-state-no-effect — Sync formData khi user context thay đổi
  useEffect(() => {
    setFormData({
      fullName: user?.fullName || user?.name || "",
      phone: user?.phone || "",
    });
  }, [user?.fullName, user?.name, user?.phone]);

  const handleProfileChange = useCallback((e) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  }, []);

  const handleProfileSubmit = useCallback(async (e) => {
    e.preventDefault();
    try {
      const formPayload = {
        fullName: formData.fullName || undefined,
        phone: formData.phone || undefined,
      };
      const response = await updateProfile(formPayload);
      if (response?.success && response?.data) {
        refreshUser(response.data);
        toast.success("Cập nhật thông tin thành công!");
      }
    } catch (error) {
      toast.error("Có lỗi xảy ra khi cập nhật thông tin.");
      console.error(error);
    }
  }, [formData, updateProfile, refreshUser]);


  return (
    <div className="max-w-4xl mx-auto p-6 space-y-8">
      {/* HEADER */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
            <div className={`p-3 rounded-xl ${badgeBg}`}>
                <ShieldCheck className="w-6 h-6" />
            </div>
            <div>
                <h1 className="text-2xl font-bold text-zinc-900">Hồ sơ Quản trị viên</h1>
                <p className="text-sm text-zinc-500 font-medium">Quản lý và cập nhật thông tin cá nhân</p>
            </div>
        </div>
      </div>

      <div className="space-y-6">
        
        {/* PROFILE DATA */}
        <div className="space-y-6">
            <div className="bg-white rounded-2xl border border-zinc-200 shadow-sm p-6">
                <h2 className="text-lg font-bold text-zinc-900 mb-6 flex items-center gap-2">
                    <User className={iconColor} size={20}/>
                    <span>Thông tin cá nhân</span>
                </h2>

                <form onSubmit={handleProfileSubmit} className="space-y-6">
                    <div className="flex items-center gap-6">
                        <div>
                            <div className="flex items-center gap-3">
                                <h3 className="text-xl font-bold text-zinc-900">{user?.fullName || user?.name || "Người dùng"}</h3>
                                <span className={`px-2.5 py-0.5 rounded-full text-xs font-bold uppercase tracking-wider ${badgeBg}`}>
                                    {isAdmin ? "Admin" : "Moderator"}
                                </span>
                            </div>
                            <p className="text-zinc-500 font-medium mt-1">{user?.email}</p>
                        </div>
                    </div>

                    <div className="space-y-4 pt-4 border-t border-zinc-100">
 
                        <div className="space-y-2">
                            <label htmlFor="email" className="text-zinc-700 text-sm font-medium">Địa chỉ Email <span className="text-zinc-400 font-normal">(Không thể thay đổi)</span></label>
                            <Input 
                                id="email" 
                                value={user?.email || ""} 
                                disabled 
                                className="bg-zinc-50 text-zinc-500 font-medium"
                                icon={<Mail size={16} className="text-zinc-400" />}
                            />
                        </div>

                        <div className="space-y-2">
                            <label htmlFor="fullName" className="text-zinc-700 text-sm font-medium">Họ và Tên</label>
                            <Input 
                                id="fullName" 
                                name="fullName"
                                value={formData.fullName} 
                                onChange={handleProfileChange}
                                className={`font-medium ${focusRing}`}
                                icon={<User size={16} className="text-zinc-400" />}
                            />
                        </div>

                        <div className="space-y-2">
                            <label htmlFor="phone" className="text-zinc-700 text-sm font-medium">Số điện thoại</label>
                            <Input 
                                id="phone" 
                                name="phone"
                                value={formData.phone} 
                                onChange={handleProfileChange}
                                className={`font-medium ${focusRing}`}
                                icon={<Phone size={16} className="text-zinc-400" />}
                            />
                        </div>
                    </div>

                    <div className="pt-4">
                        <button
                            type="submit"
                            disabled={isUpdating}
                            className={`w-full py-2.5 px-4 text-white font-semibold rounded-lg shadow-sm transition-all focus:ring-2 focus:ring-offset-2 flex justify-center items-center gap-2 ${btnColor}`}
                        >
                            {isUpdating ? <Loader2 size={18} className="animate-spin" /> : "Lưu thay đổi"}
                        </button>
                    </div>
                </form>
            </div>
        </div>
      </div>
    </div>
  );
};

export default CMSProfilePage;
