import React, { useState, useRef, useEffect } from "react";
import {
  MoreVertical,
  Shield,
  Trash2,
  LogOut,
  User,
  ChevronDown,
  Check,
} from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "../../../ui/dropdown-menu";

const MemberItem = ({
  member,
  currentUser,
  isOwner, // Current user is owner of the trip
  onUpdateRole,
  onRemove,
  onLeave,
}) => {
  const isMe =
    (currentUser?.id && currentUser.id === member.userId) ||
    (currentUser?.userId && currentUser.userId === member.userId);
  const isMemberOwner = member.role === "Owner" || member.role === "owner";

  // Logic hiển thị actions
  // ... (comments kept same)

  return (
    <div className="flex items-center justify-between p-3 hover:bg-slate-50 rounded-xl transition-colors group">
      {/* User Info */}
      <div className="flex items-center gap-3">
        <div className="relative">
          <img
            src={
              member.avatarUrl ||
              `https://ui-avatars.com/api/?name=${encodeURIComponent(member.fullName || "User")}&background=random`
            }
            alt={member.fullName}
            className="w-10 h-10 rounded-full object-cover border border-slate-200 shadow-sm"
          />
          {isMemberOwner && (
            <div
              className="absolute -bottom-1 -right-1 bg-yellow-400 text-white p-0.5 rounded-full border-2 border-white"
              title="Trưởng nhóm"
            >
              <Shield size={10} fill="currentColor" />
            </div>
          )}
        </div>

        <div className="flex flex-col">
          <div className="flex items-center gap-2">
            <span className="font-semibold text-slate-700">
              {member.fullName}
            </span>
            {isMe && (
              <span className="px-1.5 py-0.5 bg-blue-100 text-blue-600 text-[10px] font-bold rounded-md">
                BẠN
              </span>
            )}
          </div>
          <span className="text-xs text-slate-500 capitalize">
            {member.role?.toLowerCase() === "owner" 
              ? "Trưởng nhóm" 
              : member.role?.toLowerCase() === "editor" 
              ? "Chỉnh sửa" 
              : "Người xem"}
          </span>
        </div>
      </div>

      {/* Actions */}
      <div className="flex items-center gap-2">
        {/* Case 1: I am Owner, editing Others (NOT ME) */}
        {isOwner && !isMe && (
          <div className="flex items-center gap-2">
            <RoleDropdown
              currentRole={member.role}
              onSelect={(role) => onUpdateRole(member.userId, role)}
            />

            <button
              onClick={() => onRemove(member.userId)}
              className="p-1.5 text-rose-500 bg-rose-50 hover:bg-rose-100 rounded-lg transition-colors"
              title="Mời ra khỏi nhóm"
            >
              <Trash2 size={16} />
            </button>
          </div>
        )}

        {/* Case 3: Leave Trip (For Myself, if NOT Owner) */}
        {isMe && !isMemberOwner && (
          <button
            onClick={() => onLeave()}
            className="p-1.5 text-slate-400 hover:text-rose-500 hover:bg-rose-50 rounded-lg transition-colors"
            title="Rời nhóm"
          >
            <LogOut size={16} />
          </button>
        )}
      </div>
    </div>
  );
};

const RoleDropdown = ({ currentRole, onSelect }) => {
  const roleMap = {
    "Chỉnh sửa": 3,
    "Người xem": 2,
  };

  const displayRole = currentRole?.toLowerCase() === "editor" ? "Chỉnh sửa" : "Người xem";

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button
          className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-bold transition-all border bg-blue-50 text-blue-600 border-transparent hover:border-blue-200 outline-none data-[state=open]:bg-blue-100 data-[state=open]:text-blue-700 data-[state=open]:border-blue-200 group"
        >
          {displayRole}
          <ChevronDown
            size={12}
            className="transition-transform duration-200 group-data-[state=open]:rotate-180"
          />
        </button>
      </DropdownMenuTrigger>

      <DropdownMenuContent align="end" sideOffset={4} className="w-32 bg-white rounded-xl shadow-xl border border-slate-100 py-1 z-[100] animate-in fade-in zoom-in-95 duration-200">
        <DropdownMenuItem 
          onClick={() => onSelect(roleMap["Chỉnh sửa"])}
          className="flex items-center justify-between w-full px-3 py-2 text-xs font-medium text-slate-600 hover:bg-slate-50 hover:text-slate-900 transition-colors cursor-pointer outline-none focus:bg-slate-50 focus:text-slate-900 focus:outline-none focus:border-none"
        >
          Chỉnh sửa
          {displayRole === "Chỉnh sửa" && <Check size={12} className="text-blue-600" />}
        </DropdownMenuItem>
        
        <DropdownMenuItem 
          onClick={() => onSelect(roleMap["Người xem"])}
          className="flex items-center justify-between w-full px-3 py-2 text-xs font-medium text-slate-600 hover:bg-slate-50 hover:text-slate-900 transition-colors cursor-pointer outline-none focus:bg-slate-50 focus:text-slate-900 focus:outline-none focus:border-none"
        >
          Người xem
          {displayRole === "Người xem" && <Check size={12} className="text-blue-600" />}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
};

export default MemberItem;
