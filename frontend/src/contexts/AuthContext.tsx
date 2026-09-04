"use client";

import React, { createContext, useContext, useState, useEffect, useCallback } from "react";
import { useRouter } from "next/navigation";
import type { LoginChallenge, LoginResponse } from "@/types";
import { apiService } from "@/services/api";

const INACTIVITY_TIMEOUT_MS = 10 * 60 * 1000; // 10 minutes of inactivity
const ACTIVITY_THROTTLE_MS = 2000; // Throttle storage writes to every 2 seconds

interface AuthContextType {
  user: LoginResponse | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  requestOtp: (email: string, password: string) => Promise<LoginChallenge>;
  verifyOtp: (adminId: string, code: string) => Promise<void>;
  resendOtp: (adminId: string) => Promise<LoginChallenge>;
  logout: (reason?: "manual" | "inactivity" | unknown) => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<LoginResponse | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const router = useRouter();

  const logout = useCallback((reason?: "manual" | "inactivity" | unknown) => {
    setToken(null);
    setUser(null);
    localStorage.removeItem("nrb_token");
    localStorage.removeItem("nrb_user");
    localStorage.removeItem("nrb_last_activity");

    if (reason === "inactivity") {
      router.push("/login?timeout=1");
    } else {
      router.push("/login");
    }
  }, [router]);

  // Initial load authentication check
  useEffect(() => {
    const storedToken = localStorage.getItem("nrb_token");
    const storedUser = localStorage.getItem("nrb_user");
    const storedActivity = localStorage.getItem("nrb_last_activity");

    if (storedToken && storedUser) {
      const lastActivityTime = storedActivity ? parseInt(storedActivity, 10) : Date.now();
      const now = Date.now();

      // Check if session expired while browser was closed or in background
      if (now - lastActivityTime >= INACTIVITY_TIMEOUT_MS) {
        localStorage.removeItem("nrb_token");
        localStorage.removeItem("nrb_user");
        localStorage.removeItem("nrb_last_activity");
        setToken(null);
        setUser(null);
        setIsLoading(false);
        router.push("/login?timeout=1");
        return;
      }

      setToken(storedToken);
      setUser(JSON.parse(storedUser));
      localStorage.setItem("nrb_last_activity", now.toString());
    }
    setIsLoading(false);
  }, [router]);

  // Activity tracking and session timeout watcher
  useEffect(() => {
    if (!token) return;

    let lastThrottledWrite = Date.now();

    const recordUserActivity = () => {
      const now = Date.now();
      const storedActivity = localStorage.getItem("nrb_last_activity");
      const lastActivityTime = storedActivity ? parseInt(storedActivity, 10) : now;

      // If already past 10 minutes, trigger timeout immediately
      if (now - lastActivityTime >= INACTIVITY_TIMEOUT_MS) {
        logout("inactivity");
        return;
      }

      // Throttle writing to localStorage to prevent high frequency writes
      if (now - lastThrottledWrite >= ACTIVITY_THROTTLE_MS) {
        lastThrottledWrite = now;
        localStorage.setItem("nrb_last_activity", now.toString());
      }
    };

    // Heartbeat interval check every 5 seconds
    const intervalId = setInterval(() => {
      const now = Date.now();
      const storedActivity = localStorage.getItem("nrb_last_activity");
      if (!storedActivity) return;

      const lastActivityTime = parseInt(storedActivity, 10);
      if (now - lastActivityTime >= INACTIVITY_TIMEOUT_MS) {
        logout("inactivity");
      }
    }, 5000);

    // List of user interaction events
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

    // Check when user switches back to tab or unminimizes browser
    const handleVisibilityChange = () => {
      if (document.visibilityState === "visible") {
        const now = Date.now();
        const storedActivity = localStorage.getItem("nrb_last_activity");
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

    // Multi-tab sync via storage events
    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === "nrb_token" && !e.newValue) {
        // Another tab logged out
        setToken(null);
        setUser(null);
        router.push("/login");
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
  }, [token, logout, router]);

  const requestOtp = useCallback(async (email: string, password: string) => {
    return await apiService.login({ email, password });
  }, []);

  const verifyOtp = useCallback(async (adminId: string, code: string) => {
    const response = await apiService.verifyOtp({ adminId, code });
    setToken(response.token);
    setUser(response);
    const now = Date.now().toString();
    localStorage.setItem("nrb_token", response.token);
    localStorage.setItem("nrb_user", JSON.stringify(response));
    localStorage.setItem("nrb_last_activity", now);
    router.push("/dashboard");
  }, [router]);

  const resendOtp = useCallback(async (adminId: string) => {
    return await apiService.resendOtp({ adminId });
  }, []);

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        isAuthenticated: !!token,
        isLoading,
        requestOtp,
        verifyOtp,
        resendOtp,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
