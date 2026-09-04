"use client";

import { useEffect, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import { Loader2 } from "lucide-react";

const PUBLIC_PATHS = ["/login", "/reset-password"];

export default function AuthGuard({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const [isAuthorized, setIsAuthorized] = useState(false);

  useEffect(() => {
    const isPublic = PUBLIC_PATHS.some((p) => pathname.startsWith(p));
    const token = typeof window !== "undefined" ? localStorage.getItem("manual_token") : null;

    if (!token && !isPublic) {
      setIsAuthorized(false);
      router.replace("/login");
    } else if (token && pathname === "/login") {
      setIsAuthorized(false);
      router.replace("/");
    } else {
      setIsAuthorized(true);
    }
  }, [pathname, router]);

  if (!isAuthorized && !PUBLIC_PATHS.some((p) => pathname.startsWith(p))) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-slate-50">
        <div className="flex flex-col items-center space-y-3">
          <Loader2 className="w-8 h-8 text-[#F48220] animate-spin" />
          <span className="text-sm font-semibold text-slate-600">Redirecting to sign in...</span>
        </div>
      </div>
    );
  }

  return <>{children}</>;
}
