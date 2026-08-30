"use client";

import Sidebar from "./Sidebar";
import { useAuth } from "@/contexts/AuthContext";

export default function PortalLayout({ children }: { children: React.ReactNode }) {
  const { user } = useAuth();

  return (
    <div className="min-h-screen">
      <Sidebar />
      <div className="ml-64">
        <header className="sticky top-0 z-40 h-14 bg-white border-b border-slate-200 flex items-center justify-between px-6">
          <span className="text-sm font-semibold text-navy-800">NRB Gateway Console</span>
          <span className="text-xs text-slate-500">
            {user?.name} · {user?.email}
          </span>
        </header>
        <main className="p-8 bg-slate-100 min-h-[calc(100vh-3.5rem)]">
          {children}
        </main>
      </div>
    </div>
  );
}
