import {
  LoginResponse,
  DashboardMetrics,
  VerificationResult,
  PaginatedLogHistory,
} from "@/types";

const API_BASE = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5050";

async function fetchWithAuth<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const token =
    typeof window !== "undefined" ? localStorage.getItem("manual_token") : null;

  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    ...(options.headers as Record<string, string>),
  };

  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  const response = await fetch(`${API_BASE}${endpoint}`, {
    ...options,
    headers,
  });

  if (response.status === 401) {
    if (typeof window !== "undefined") {
      localStorage.removeItem("manual_token");
      localStorage.removeItem("manual_user");
      window.location.href = "/login";
    }
    throw new Error("Unauthorized");
  }

  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: "Request failed" }));
    throw new Error(error.message || `HTTP ${response.status}`);
  }

  return response.json();
}

export const apiService = {
  login: async (email: string, password: string): Promise<LoginResponse> => {
    const response = await fetch(`${API_BASE}/api/v1/manual-portal/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password }),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: "Login failed" }));
      throw new Error(error.message || "Invalid credentials");
    }

    return response.json();
  },

  verify2Fa: async (userId: string, code: string): Promise<LoginResponse> => {
    const response = await fetch(`${API_BASE}/api/v1/manual-portal/auth/verify-2fa`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ userId, code }),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: "Verification failed" }));
      throw new Error(error.message || "Invalid verification code");
    }

    return response.json();
  },

  resend2Fa: async (userId: string): Promise<{ message: string }> => {
    const response = await fetch(`${API_BASE}/api/v1/manual-portal/auth/resend-2fa`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ userId }),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: "Resend failed" }));
      throw new Error(error.message || "Failed to resend code");
    }

    return response.json();
  },

  getDashboard: (): Promise<DashboardMetrics> =>
    fetchWithAuth("/api/v1/manual-portal/dashboard"),

  verify: (nationalId: string): Promise<VerificationResult> =>
    fetchWithAuth("/api/v1/manual-portal/verify", {
      method: "POST",
      body: JSON.stringify({ nationalId }),
    }),

  getHistory: (
    page = 1,
    pageSize = 10,
    dateFrom?: string,
    dateTo?: string
  ): Promise<PaginatedLogHistory> => {
    const searchParams = new URLSearchParams();
    searchParams.set("page", String(page));
    searchParams.set("pageSize", String(pageSize));
    if (dateFrom) searchParams.set("dateFrom", dateFrom);
    if (dateTo) searchParams.set("dateTo", dateTo);
    return fetchWithAuth(`/api/v1/manual-portal/history?${searchParams.toString()}`);
  },

  resetPassword: async (
    userId: string,
    token: string,
    newPassword: string
  ): Promise<{ message: string }> => {
    const response = await fetch(
      `${API_BASE}/api/v1/manual-portal/auth/reset-password`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId, token, newPassword }),
      }
    );

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: "Password reset failed" }));
      throw new Error(error.message || "Password reset failed");
    }

    return response.json();
  },
};
