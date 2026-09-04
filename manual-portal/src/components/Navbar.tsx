"use client";

import Link from "next/link";
import Image from "next/image";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { Search, History, LogOut, User } from "lucide-react";
import { ManualUser } from "@/types";

export default function Navbar() {
  const pathname = usePathname();
  const router = useRouter();
  const [user, setUser] = useState<ManualUser | null>(null);

  useEffect(() => {
    const stored = localStorage.getItem("manual_user");
    if (stored) {
      try {
        setUser(JSON.parse(stored));
      } catch {
        // Ignore parse error
      }
    }
  }, []);

  const handleLogout = () => {
    localStorage.removeItem("manual_token");
    localStorage.removeItem("manual_user");
    router.push("/login");
  };

  if (pathname === "/login") return null;

  return (
    <header className="bg-[#292D6B] text-white shadow-md">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16">
          {/* Brand Logo / Title */}
          <div className="flex items-center space-x-3">
            <Link href="/" className="flex items-center space-x-3 group">
              <div className="w-10 h-10 rounded-xl bg-white flex items-center justify-center p-1 shadow-sm shrink-0">
                <Image
                  src="/logo.png"
                  alt="Continental Holdings Limited"
                  width={36}
                  height={36}
                  className="h-8 w-auto object-contain"
                />
              </div>
              <div>
                <span className="text-base sm:text-lg font-bold tracking-wider text-white block leading-tight">
                  CONTINENTAL HOLDINGS
                </span>
                <span className="text-xs text-amber-200/90 font-medium tracking-wide uppercase">
                  NRB Verification Portal
                </span>
              </div>
            </Link>
          </div>

          {/* Navigation Links */}
          <nav className="flex space-x-1 sm:space-x-3">
            <Link
              href="/verify"
              className={`flex items-center space-x-2 px-3 py-2 rounded-md text-sm font-semibold transition-colors ${
                pathname.startsWith("/verify")
                  ? "bg-[#F48220] text-white shadow-sm"
                  : "text-slate-200 hover:bg-white/10 hover:text-white"
              }`}
            >
              <Search className="w-4 h-4" />
              <span>New Verification</span>
            </Link>

            <Link
              href="/history"
              className={`flex items-center space-x-2 px-3 py-2 rounded-md text-sm font-semibold transition-colors ${
                pathname === "/history"
                  ? "bg-[#F48220] text-white shadow-sm"
                  : "text-slate-200 hover:bg-white/10 hover:text-white"
              }`}
            >
              <History className="w-4 h-4" />
              <span>History</span>
            </Link>
          </nav>

          {/* User Account / Logout */}
          <div className="flex items-center space-x-3">
            {user && (
              <div className="hidden md:flex flex-col text-right">
                <span className="text-xs text-slate-300 font-medium">
                  {user.companyName}
                </span>
                <span className="text-xs text-white font-semibold flex items-center space-x-1 justify-end">
                  <User className="w-3 h-3 text-[#F48220] inline" />
                  <span>{user.email}</span>
                </span>
              </div>
            )}

            <button
              onClick={handleLogout}
              className="p-2 rounded-md text-slate-300 hover:text-white hover:bg-white/10 transition-colors"
              title="Sign Out"
            >
              <LogOut className="w-5 h-5" />
            </button>
          </div>
        </div>
      </div>
    </header>
  );
}
