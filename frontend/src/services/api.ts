import type {
  LoginRequest,
  LoginResponse,
  Subsidiary,
  SubsidiaryApiKey,
  TierSetting,
  EnvironmentSetting,
  CachePolicy,
  AuditLogEntry,
  DashboardMetrics,
  RecentChange,
  DailyUsage,
  AdminUser,
  PaginatedResponse,
  RevalidationResult,
} from "@/types";

const API_BASE = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5050";

async function fetchWithAuth<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const token =
    typeof window !== "undefined" ? localStorage.getItem("nrb_token") : null;

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
    localStorage.removeItem("nrb_token");
    localStorage.removeItem("nrb_user");
    window.location.href = "/login";
    throw new Error("Unauthorized");
  }

  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: "Request failed" }));
    throw new Error(error.message || `HTTP ${response.status}`);
  }

  return response.json();
}

export const apiService = {
  // Auth
  login: (data: LoginRequest): Promise<LoginResponse> =>
    fetchWithAuth("/api/v1/portal/auth/login", {
      method: "POST",
      body: JSON.stringify(data),
    }),

  // Dashboard
  getDashboardMetrics: (): Promise<DashboardMetrics> =>
    fetchWithAuth("/api/v1/portal/dashboard/metrics"),

  getRecentChanges: (): Promise<RecentChange[]> =>
    fetchWithAuth("/api/v1/portal/dashboard/recent-changes"),

  getTierSettings: (): Promise<TierSetting[]> =>
    fetchWithAuth("/api/v1/portal/settings/tiers"),

  updateTierSetting: (tier: string, enabled: boolean): Promise<TierSetting> =>
    fetchWithAuth(`/api/v1/portal/settings/tiers/${tier}`, {
      method: "PUT",
      body: JSON.stringify({ enabled }),
    }),

  // Subsidiaries
  getSubsidiaries: (): Promise<Subsidiary[]> =>
    fetchWithAuth("/api/v1/portal/subsidiaries"),

  createSubsidiary: (data: { name: string; shortCode: string }): Promise<Subsidiary> =>
    fetchWithAuth("/api/v1/portal/subsidiaries", {
      method: "POST",
      body: JSON.stringify(data),
    }),

  deleteSubsidiary: (id: string): Promise<void> =>
    fetchWithAuth(`/api/v1/portal/subsidiaries/${id}`, { method: "DELETE" }),

  getSubsidiaryApiKeys: (id: string): Promise<SubsidiaryApiKey[]> =>
    fetchWithAuth(`/api/v1/portal/subsidiaries/${id}/api-keys`),

  createApiKey: (subsidiaryId: string): Promise<SubsidiaryApiKey> =>
    fetchWithAuth(`/api/v1/portal/subsidiaries/${subsidiaryId}/api-keys`, {
      method: "POST",
    }),

  rotateApiKey: (subsidiaryId: string, keyId: string): Promise<SubsidiaryApiKey> =>
    fetchWithAuth(
      `/api/v1/portal/subsidiaries/${subsidiaryId}/api-keys/${keyId}/rotate`,
      { method: "POST" }
    ),

  revokeApiKey: (subsidiaryId: string, keyId: string): Promise<void> =>
    fetchWithAuth(
      `/api/v1/portal/subsidiaries/${subsidiaryId}/api-keys/${keyId}/revoke`,
      { method: "POST" }
    ),

  // NRB Environment
  getEnvironmentSetting: (): Promise<EnvironmentSetting> =>
    fetchWithAuth("/api/v1/portal/settings/nrb-environment"),

  updateEnvironmentSetting: (data: Partial<EnvironmentSetting>): Promise<EnvironmentSetting> =>
    fetchWithAuth("/api/v1/portal/settings/nrb-environment", {
      method: "PUT",
      body: JSON.stringify(data),
    }),

  // Cache Policy
  getCachePolicy: (): Promise<CachePolicy> =>
    fetchWithAuth("/api/v1/portal/settings/cache-policy"),

  updateCachePolicy: (data: CachePolicy): Promise<CachePolicy> =>
    fetchWithAuth("/api/v1/portal/settings/cache-policy", {
      method: "PUT",
      body: JSON.stringify(data),
    }),

  // Audit Log
  getAuditLogs: (params: {
    page?: number;
    pageSize?: number;
    dateFrom?: string;
    dateTo?: string;
    admin?: string;
    actionType?: string;
  }): Promise<PaginatedResponse<AuditLogEntry>> => {
    const searchParams = new URLSearchParams();
    if (params.page) searchParams.set("page", String(params.page));
    if (params.pageSize) searchParams.set("pageSize", String(params.pageSize));
    if (params.dateFrom) searchParams.set("dateFrom", params.dateFrom);
    if (params.dateTo) searchParams.set("dateTo", params.dateTo);
    if (params.admin) searchParams.set("admin", params.admin);
    if (params.actionType) searchParams.set("actionType", params.actionType);
    return fetchWithAuth(`/api/v1/portal/audit-log?${searchParams}`);
  },

  // Admin Users
  getAdminUsers: (): Promise<PaginatedResponse<AdminUser>> =>
    fetchWithAuth("/api/v1/portal/admin-users"),

  // Subsidiary usage
  getSubsidiaryUsage: (id: string): Promise<DailyUsage[]> =>
    fetchWithAuth(`/api/v1/portal/subsidiaries/${id}/usage`),

  // Maintenance — batch revalidation of local NRB mirror
  revalidateAll: (): Promise<RevalidationResult> =>
    fetchWithAuth("/api/v1/portal/maintenance/revalidate", {
      method: "POST",
    }),
};
