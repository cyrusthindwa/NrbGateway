"use client";

import Sidebar from "./Sidebar";

export default function PortalLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-screen">
      <Sidebar />
      <main className="flex-1 ml-64 p-8 bg-slate-100 min-h-screen">
        {children}
      </main>
    </div>
  );
}
