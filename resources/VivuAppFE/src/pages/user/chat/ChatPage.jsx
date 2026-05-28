import React from "react";
import AppNavbar from "../../../components/layout/AppNavbar";
import CreateTripWizard from "../../../components/user/chat/CreateTripWizard";
import TripPreviewPanel from "../../../components/user/chat/TripPreviewPanel";
import { useCreateTripForm } from "../../../hooks/chat/useCreateTripForm";
import useUserTrips from "../../../hooks/trips/useUserTrips";

const ChatPage = () => {
  const formHook = useCreateTripForm();
  const { tripLimitInfo } = useUserTrips();

  return (
    <div className="flex flex-col h-[100dvh] w-screen bg-slate-50 overflow-hidden font-sans text-slate-800 lg:pb-0 pb-16">
      {/* Top Navigation - Shared Component with Vibrant Design */}
      <AppNavbar />

      {/* Main Layout */}
      <div className="flex flex-1 overflow-hidden max-w-[1920px] mx-auto w-full relative">
        {/* Background Gradient for the whole page */}
        <div className="absolute inset-0 bg-linear-to-br from-blue-50 via-indigo-50/30 to-purple-50 pointer-events-none -z-10" />

        {/* Main Form Area - Full on mobile, 60% on desktop (Glass Effect) */}
        <div className="w-full lg:w-[60%] min-w-0 shrink-0 lg:border-r border-white/50 bg-white/70 backdrop-blur-xl relative z-10 shadow-xl shadow-blue-100/20">
          <CreateTripWizard formHook={formHook} tripLimitInfo={tripLimitInfo} />
        </div>

        {/* Right Panel - 40% (Live Preview Workspace) */}
        <div className="w-[40%] shrink-0 hidden lg:flex flex-col bg-transparent">
          <TripPreviewPanel data={formHook.data} />
        </div>
      </div>
    </div>
  );
};

export default ChatPage;
