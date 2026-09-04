"use client";

import { useEffect, useState, useCallback } from "react";
import { usePathname, useRouter } from "next/navigation";
import { Loader2 } from "lucide-react";

const PUBLIC_PATHS = ["/login", "/reset-password"];
const INACTIVITY_TIMEOUT_MS = 10 * 60 * 1000; // 10 minutes
const ACTIVITY_THROTTLE_MS = 2000; // Throttle storage writes to every 2 seconds

export default function AuthGuard({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const [isAuthorized, setIsAuthorized] = useState(false);

  const logout = useCallback((reason: "manual" | "inactivity" = "manual") => {
    localStorage.removeItem("manual_token");
    localStorage.removeItem("manual_user");
    localStorage.removeItem("manual_last_activity");
    setIsAuthorized(false);

    if (reason === "inactivity") {
      router.replace("/login?timeout=1");
    } else {
      router.replace("/login");
    }
  }, [router]);

  useEffect(() => {
    const isPublic = PUBLIC_PATHS.some((p) => pathname.startsWith(p));
    const token = typeof window !== "undefined" ? localStorage.getItem("manual_token") : null;

    if (!token && !isPublic) {
      setIsAuthorized(false);
      router.replace("/login");
      return;
    }

    if (token && pathname === "/login") {
      setIsAuthorized(false);
      router.replace("/");
      return;
    }

    if (token && !isPublic) {
      const storedActivity = localStorage.getItem("manual_last_activity");
      const lastActivityTime = storedActivity ? parseInt(storedActivity, 10) : Date.now();
      const now = Date.now();

      // Check if session has already expired
      if (now - lastActivityTime >= INACTIVITY_TIMEOUT_MS) {
        logout("inactivity");
        return;
      }

      localStorage.setItem("manual_last_activity", now.toString());
      setIsAuthorized(true);
    } else {
      setIsAuthorized(true);
    }
  }, [pathname, router, logout]);

  // Inactivity tracking when authorized on protected routes
  useEffect(() => {
    const isPublic = PUBLIC_PATHS.some((p) => pathname.startsWith(p));
    if (isPublic || !isAuthorized) return;

    let lastThrottledWrite = Date.now();

    const recordUserActivity = () => {
      const now = Date.now();
      const storedActivity = localStorage.getItem("manual_last_activity");
      const lastActivityTime = storedActivity ? parseInt(storedActivity, 10) : now;

      if (now - lastActivityTime >= INACTIVITY_TIMEOUT_MS) {
        logout("inactivity");
        return;
      }

      if (now - lastThrottledWrite >= ACTIVITY_THROTTLE_MS) {
        lastThrottledWrite = now;
        localStorage.setItem("manual_last_activity", now.toString());
      }
    };

    // Heartbeat check every 5 seconds
    const intervalId = setInterval(() => {
      const now = Date.now();
      const storedActivity = localStorage.getItem("manual_last_activity");
      if (!storedActivity) return;

      const lastActivityTime = parseInt(storedActivity, 10);
      if (now - lastActivityTime >= INACTIVITY_TIMEOUT_MS) {
        logout("inactivity");
      }
    }, 5000);

    const activityEvents: (keyof WindowEventMap)[] = [
      "mousedown",
      "mousemove",
      "keydown",
      "scroll",
      "touchstart",
      "click",
      "focus",
    ];

    const handleEvent = () => recordUserActivity();

    activityEvents.forEach((evt) => {
      window.addEventListener(evt, handleEvent, { passive: true });
    });

    const handleVisibilityChange = () => {
      if (document.visibilityState === "visible") {
        const now = Date.now();
        const storedActivity = localStorage.getItem("manual_last_activity");
        if (storedActivity) {
          const lastActivityTime = parseInt(storedActivity, 10);
          if (now - lastActivityTime >= INACTIVITY_TIMEOUT_MS) {
            logout("inactivity");
            return;
          }
        }
        recordUserActivity();
      }
    };

    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === "manual_token" && !e.newValue) {
        setIsAuthorized(false);
        router.replace("/login");
      }
    };

    document.addEventListener("visibilitychange", handleVisibilityChange);
    window.addEventListener("storage", handleStorageChange);

    return () => {
      clearInterval(intervalId);
      activityEvents.forEach((evt) => {
        window.removeEventListener(evt, handleEvent);
      });
      document.removeEventListener("visibilitychange", handleVisibilityChange);
      window.removeEventListener("storage", handleStorageChange);
    };
  }, [pathname, isAuthorized, logout, router]);

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
