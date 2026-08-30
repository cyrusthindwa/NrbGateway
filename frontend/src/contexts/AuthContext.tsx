"use client";

import React, { createContext, useContext, useState, useEffect, useCallback } from "react";
import { useRouter } from "next/navigation";
import type { LoginChallenge, LoginResponse } from "@/types";
import { apiService } from "@/services/api";

interface AuthContextType {
  user: LoginResponse | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  requestOtp: (email: string, password: string) => Promise<LoginChallenge>;
  verifyOtp: (adminId: string, code: string) => Promise<void>;
  resendOtp: (adminId: string) => Promise<LoginChallenge>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<LoginResponse | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    const storedToken = localStorage.getItem("nrb_token");
    const storedUser = localStorage.getItem("nrb_user");
    if (storedToken && storedUser) {
      setToken(storedToken);
      setUser(JSON.parse(storedUser));
    }
    setIsLoading(false);
  }, []);

  const requestOtp = useCallback(async (email: string, password: string) => {
    return await apiService.login({ email, password });
  }, []);

  const verifyOtp = useCallback(async (adminId: string, code: string) => {
    const response = await apiService.verifyOtp({ adminId, code });
    setToken(response.token);
    setUser(response);
    localStorage.setItem("nrb_token", response.token);
    localStorage.setItem("nrb_user", JSON.stringify(response));
    router.push("/dashboard");
  }, [router]);

  const resendOtp = useCallback(async (adminId: string) => {
    return await apiService.resendOtp({ adminId });
  }, []);

  const logout = useCallback(() => {
    setToken(null);
    setUser(null);
    localStorage.removeItem("nrb_token");
    localStorage.removeItem("nrb_user");
    router.push("/login");
  }, [router]);

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
