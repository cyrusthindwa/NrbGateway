"use client";

import Link from "next/link";
import Image from "next/image";
import { usePathname } from "next/navigation";
import { useAuth } from "@/contexts/AuthContext";
import {
  LayoutDashboard,
  ShieldCheck,
  ScrollText,
  Users,
  UserPlus,
  LogOut,
  Key,
  Building2,
  Bell,
  CreditCard,
  RefreshCw,
  Activity,
  Settings,
} from "lucide-react";

const navItems = [
  { href: "/dashboard", label: "Dashboard", icon: LayoutDashboard },
  { href: "/companies", label: "Companies", icon: Building2 },
  { href: "/projects", label: "Projects", icon: Key },
  { href: "/verification-tiers", label: "Verification Tiers", icon: ShieldCheck },
  { href: "/audit-log", label: "Audit Log", icon: ScrollText },
  { href: "/admin-users", label: "Admin Users", icon: Users },
  { href: "/manual-portal-users", label: "Manual Portal Users", icon: UserPlus },
  { href: "/notification-channels", label: "Notification Channels", icon: Bell },
  { href: "/billing", label: "Billing", icon: CreditCard },
  { href: "/revalidation", label: "Revalidation", icon: RefreshCw },
  { href: "/uptime", label: "NRB Uptime", icon: Activity },
  { href: "/settings", label: "Settings", icon: Settings },
];

export default function Sidebar() {
  const pathname = usePathname();
  const { user, logout } = useAuth();

  return (
    <aside className="fixed left-0 top-0 h-full w-64 bg-navy-900 flex flex-col z-50">
      {/* Logo / Brand */}
      <div className="px-6 py-5 border-b border-navy-700">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-white flex items-center justify-center p-1 shadow-sm shrink-0">
            <Image
              src="/logo.png"
              alt="CHL Logo"
              width={36}
              height={36}
              className="h-8 w-auto object-contain"
            />
          </div>
          <div>
            <h1 className="text-white font-bold text-sm leading-tight">
              NRB Gateway
            </h1>
            <p className="text-slate-400 text-xs font-medium">ICT Admin Console</p>
          </div>
        </div>
      </div>

      {/* Navigation */}
      <nav className="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
        {navItems.map((item) => {
          const isActive =
            pathname === item.href ||
            (item.href !== "/dashboard" && pathname.startsWith(item.href));
          const Icon = item.icon;

          return (
            <Link
              key={item.href}
              href={item.href}
              className={`flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors ${
                isActive
                  ? "bg-orange-500 text-white"
                  : "text-slate-300 hover:bg-navy-700 hover:text-white"
              }`}
            >
              <Icon size={18} />
              <span>{item.label}</span>
            </Link>
          );
        })}
      </nav>

      {/* User & Logout */}
      <div className="px-4 py-4 border-t border-navy-700">
        <div className="flex items-center gap-3 mb-3">
          <div className="w-8 h-8 rounded-full bg-navy-600 flex items-center justify-center text-white text-xs font-medium">
            {user?.name?.charAt(0) || "A"}
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-white text-sm font-medium truncate">
              {user?.name || "Admin"}
            </p>
            <p className="text-slate-400 text-xs truncate">
              {user?.email || ""}
            </p>
          </div>
        </div>
        <button
          onClick={logout}
          className="flex items-center gap-2 w-full px-3 py-2 text-sm text-slate-400 hover:text-white hover:bg-navy-700 rounded-lg transition-colors"
        >
          <LogOut size={16} />
          <span>Logout</span>
        </button>
      </div>
    </aside>
  );
}
