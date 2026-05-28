import React, { useState } from "react";
import QRCode from "react-qr-code";
import { Copy, Check, X, Share2 } from "lucide-react";
import { motion, AnimatePresence } from "framer-motion";

const InviteMemberModal = ({ isOpen, onClose, inviteCode, tripTitle }) => {
  const [copiedLink, setCopiedLink] = useState(false);

  const inviteLink = `${window.location.origin}/join/${inviteCode}`;

  if (!isOpen) return null;

  const handleCopyLink = () => {
    navigator.clipboard.writeText(inviteLink);
    setCopiedLink(true);
    setTimeout(() => setCopiedLink(false), 2000);
  };

  return (
    <AnimatePresence>
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          onClick={onClose}
          className="absolute inset-0 bg-black/40 backdrop-blur-sm"
        />

        <motion.div
          initial={{ scale: 0.95, opacity: 0 }}
          animate={{ scale: 1, opacity: 1 }}
          exit={{ scale: 0.95, opacity: 0 }}
          className="relative bg-white rounded-2xl shadow-xl w-full max-w-md overflow-hidden"
        >
          {/* Header */}
          <div className="flex items-center justify-between p-6 border-b border-gray-100">
            <div>
              <h3 className="text-xl font-bold text-gray-800">
                Mời thành viên
              </h3>
              <p className="text-sm text-gray-500 mt-1">
                Chia sẻ mã này để mời bạn bè tham gia chuyến đi
              </p>
            </div>
            <button
              onClick={onClose}
              className="p-2 hover:bg-gray-100 rounded-full transition-colors"
            >
              <X size={20} className="text-gray-500" />
            </button>
          </div>

          {/* Body */}
          <div className="p-6 space-y-6">
            {/* Trip Info */}
            <div className="flex items-center gap-3 p-3 bg-blue-50/50 rounded-xl border border-blue-100">
              <div className="p-2 bg-blue-100 rounded-lg">
                <Share2 size={20} className="text-blue-600" />
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-xs text-blue-600 font-medium uppercase tracking-wider">
                  Chuyến đi
                </p>
                <p className="font-semibold text-gray-800 truncate">
                  {tripTitle}
                </p>
              </div>
            </div>

            {/* Link Display */}
            <div className="space-y-4">
              <div className="space-y-2">
                <label className="text-sm font-medium text-gray-700">
                  Link tham gia
                </label>
                <div className="flex gap-2">
                  <div className="flex-1 relative">
                    <input
                      type="text"
                      readOnly
                      value={inviteLink}
                      className="w-full pl-4 pr-12 py-3 bg-gray-50 border border-gray-200 rounded-xl text-sm font-medium text-gray-800 text-left focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all truncate"
                    />
                  </div>
                  <button
                    onClick={handleCopyLink}
                    className={`px-4 flex items-center gap-2 font-medium rounded-xl transition-all ${
                      copiedLink
                        ? "bg-green-100 text-green-700 border border-green-200"
                        : "bg-gray-100 text-gray-700 hover:bg-gray-200 border border-gray-200"
                    }`}
                  >
                    {copiedLink ? <Check size={20} /> : <Copy size={20} />}
                  </button>
                </div>
              </div>
            </div>

            {/* QR Code */}
            <div className="flex flex-col items-center justify-center p-6 bg-white rounded-xl border border-gray-100 shadow-sm">
              <div className="bg-white p-2 rounded-lg">
                <QRCode
                  value={inviteLink}
                  size={160}
                  style={{ height: "auto", maxWidth: "100%", width: "100%" }}
                  viewBox={`0 0 256 256`}
                />
              </div>
              <p className="text-sm text-gray-400 font-medium mt-4">
                Quét mã để tham gia ngay
              </p>
            </div>
          </div>
        </motion.div>
      </div>
    </AnimatePresence>
  );
};

export default InviteMemberModal;
