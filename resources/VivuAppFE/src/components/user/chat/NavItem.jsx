import React from 'react';

const NavItem = ({ icon, label, count, active }) => (
  <div className={`flex items-center justify-between px-3 py-2 rounded-lg cursor-pointer transition-all group ${active ? 'bg-gray-100 text-gray-900' : 'text-gray-500 hover:bg-gray-50 hover:text-gray-900'}`}>
    <div className="flex items-center gap-3">
      {React.cloneElement(icon, { className: "w-5 h-5" })}
      <span className="font-medium text-sm">{label}</span>
    </div>
    {count && (
      <span className="text-xs bg-gray-200 text-gray-600 px-1.5 py-0.5 rounded-md font-semibold group-hover:bg-gray-300 transition-colors">
        {count}
      </span>
    )}
  </div>
);

export default NavItem;
