import type {
  LoginRequest,
  LoginChallenge,
  LoginResponse,
  Company,
  Project,
  ProjectApiKey,
  TierSetting,
  EnvironmentSetting,
  AuditLogEntry,
  DashboardMetrics,
  RecentChange,
  DailyUsage,
  AdminUser,
  ManualPortalUser,
  NotificationChannel,
  RevalidationBatch,
  NrbStatus,
  NrbDowntimeIncident,
  BillingToday,
  MonthlyUsageReport,
  BillingInvoice,
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

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json();
}

export const apiService = {
  // Auth
  login: (data: LoginRequest): Promise<LoginChallenge> =>
    fetchWithAuth("/api/v1/portal/auth/login", {
      method: "POST",
      body: JSON.stringify(data),
    }),

  verifyOtp: (data: { adminId: string; code: string }): Promise<LoginResponse> =>
    fetchWithAuth("/api/v1/portal/auth/login/verify-otp", {
      method: "POST",
      body: JSON.stringify(data),
    }),

  resendOtp: (data: { adminId: string }): Promise<LoginChallenge> =>
    fetchWithAuth("/api/v1/portal/auth/login/resend-otp", {
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

  updateTierSetting: (
    tier: string,
    enabled: boolean,
    costPerRequest?: number
  ): Promise<TierSetting> =>
    fetchWithAuth(`/api/v1/portal/settings/tiers/${tier}`, {
      method: "PUT",
      body: JSON.stringify({ enabled, costPerRequest }),
    }),

  // Companies
  getCompanies: (): Promise<Company[]> =>
    fetchWithAuth("/api/v1/portal/companies"),

  createCompany: (data: { name: string; shortCode: string }): Promise<Company> =>
    fetchWithAuth("/api/v1/portal/companies", {
      method: "POST",
      body: JSON.stringify(data),
    }),

  updateCompany: (id: string, data: { name: string; shortCode: string }): Promise<Company> =>
    fetchWithAuth(`/api/v1/portal/companies/${id}`, {
      method: "PUT",
      body: JSON.stringify(data),
    }),

  // Projects
  getProjects: (): Promise<Project[]> =>
    fetchWithAuth("/api/v1/portal/projects"),

  createProject: (data: { companyId: string; name: string; shortCode: string }): Promise<Project> =>
    fetchWithAuth("/api/v1/portal/projects", {
      method: "POST",
      body: JSON.stringify(data),
    }),

  deleteProject: (id: string): Promise<void> =>
    fetchWithAuth(`/api/v1/portal/projects/${id}`, { method: "DELETE" }),

  getProjectApiKeys: (id: string): Promise<ProjectApiKey[]> =>
    fetchWithAuth(`/api/v1/portal/projects/${id}/api-keys`),

  createApiKey: (projectId: string): Promise<ProjectApiKey> =>
    fetchWithAuth(`/api/v1/portal/projects/${projectId}/api-keys`, {
      method: "POST",
    }),

  rotateApiKey: (projectId: string, keyId: string): Promise<ProjectApiKey> =>
    fetchWithAuth(
      `/api/v1/portal/projects/${projectId}/api-keys/${keyId}/rotate`,
      { method: "POST" }
    ),

  revokeApiKey: (projectId: string, keyId: string): Promise<void> =>
    fetchWithAuth(
      `/api/v1/portal/projects/${projectId}/api-keys/${keyId}/revoke`,
      { method: "POST" }
    ),

  updateRateLimit: (
    projectId: string,
    keyId: string,
    rateLimitPerMinute: number
  ): Promise<ProjectApiKey> =>
    fetchWithAuth(
      `/api/v1/portal/projects/${projectId}/api-keys/${keyId}/rate-limit`,
      {
        method: "PUT",
        body: JSON.stringify({ rateLimitPerMinute }),
      }
    ),

  // NRB Environment
  getEnvironmentSetting: (): Promise<EnvironmentSetting> =>
    fetchWithAuth("/api/v1/portal/settings/nrb-environment"),

  updateEnvironmentSetting: (data: Partial<EnvironmentSetting>): Promise<EnvironmentSetting> =>
    fetchWithAuth("/api/v1/portal/settings/nrb-environment", {
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

  rollbackAuditEntry: (id: string): Promise<{ message: string }> =>
    fetchWithAuth(`/api/v1/portal/audit-log/${id}/rollback`, { method: "POST" }),

  // Admin Users
  getAdminUsers: (params?: {
    page?: number;
    pageSize?: number;
  }): Promise<PaginatedResponse<AdminUser>> => {
    const searchParams = new URLSearchParams();
    if (params?.page) searchParams.set("page", String(params.page));
    if (params?.pageSize) searchParams.set("pageSize", String(params.pageSize));
    const qs = searchParams.toString();
    return fetchWithAuth(`/api/v1/portal/admin-users${qs ? `?${qs}` : ""}`);
  },

  createAdminUser: (data: {
    name: string;
    email: string;
    password: string;
  }): Promise<AdminUser> =>
    fetchWithAuth("/api/v1/portal/admin-users", {
      method: "POST",
      body: JSON.stringify(data),
    }),

  updateAdminUser: (
    id: string,
    data: { name: string; email: string }
  ): Promise<AdminUser> =>
    fetchWithAuth(`/api/v1/portal/admin-users/${id}`, {
      method: "PUT",
      body: JSON.stringify(data),
    }),

  updateAdminStatus: (
    id: string,
    status: "ACTIVE" | "DISABLED"
  ): Promise<AdminUser> =>
    fetchWithAuth(`/api/v1/portal/admin-users/${id}/status`, {
      method: "PATCH",
      body: JSON.stringify({ status }),
    }),

  resetAdminPassword: (id: string): Promise<{ message: string }> =>
    fetchWithAuth(`/api/v1/portal/admin-users/${id}/reset-password`, {
      method: "POST",
    }),

  resetPassword: (data: {
    adminId: string;
    token: string;
    newPassword: string;
  }): Promise<{ message: string }> =>
    fetchWithAuth("/api/v1/portal/auth/reset-password", {
      method: "POST",
      body: JSON.stringify(data),
    }),

  // Manual Portal Users (staff who verify identities in the manual portal)
  getManualPortalUsers: (): Promise<ManualPortalUser[]> =>
    fetchWithAuth("/api/v1/portal/manual-portal-users"),

  createManualPortalUser: (data: {
    email: string;
    companyId: string;
    password: string;
  }): Promise<ManualPortalUser> =>
    fetchWithAuth("/api/v1/portal/manual-portal-users", {
      method: "POST",
      body: JSON.stringify(data),
    }),

  updateManualPortalUserStatus: (
    id: string,
    status: "ACTIVE" | "DISABLED"
  ): Promise<ManualPortalUser> =>
    fetchWithAuth(`/api/v1/portal/manual-portal-users/${id}/status`, {
      method: "PATCH",
      body: JSON.stringify({ status }),
    }),

  resetManualPortalUserPassword: (id: string): Promise<{ message: string }> =>
    fetchWithAuth(`/api/v1/portal/manual-portal-users/${id}/reset-password`, {
      method: "POST",
    }),

  // Notification channels
  getNotificationChannels: (): Promise<NotificationChannel[]> =>
    fetchWithAuth("/api/v1/portal/notification-channels"),

  createNotificationChannel: (data: {
    channelType: string;
    target: string;
  }): Promise<NotificationChannel> =>
    fetchWithAuth("/api/v1/portal/notification-channels", {
      method: "POST",
      body: JSON.stringify(data),
    }),

  updateNotificationChannelStatus: (
    id: string,
    enabled: boolean
  ): Promise<NotificationChannel> =>
    fetchWithAuth(`/api/v1/portal/notification-channels/${id}/status`, {
      method: "PATCH",
      body: JSON.stringify({ enabled }),
    }),

  // Project usage
  getProjectUsage: (id: string): Promise<DailyUsage[]> =>
    fetchWithAuth(`/api/v1/portal/projects/${id}/usage`),

  // Maintenance — batch revalidation of local NRB mirror
  revalidateAll: (): Promise<RevalidationResult> =>
    fetchWithAuth("/api/v1/portal/maintenance/revalidate", {
      method: "POST",
    }),

  getRevalidationBatches: (): Promise<RevalidationBatch[]> =>
    fetchWithAuth("/api/v1/portal/maintenance/revalidation-batches"),

  getRevalidationBatch: (id: string): Promise<RevalidationBatch> =>
    fetchWithAuth(`/api/v1/portal/maintenance/revalidation-batches/${id}`),

  // NRB uptime / downtime
  getNrbStatus: (): Promise<NrbStatus> =>
    fetchWithAuth("/api/v1/portal/nrb-status"),

  getNrbIncidents: (): Promise<NrbDowntimeIncident[]> =>
    fetchWithAuth("/api/v1/portal/nrb-status/incidents"),

  // Billing
  getBillingToday: (): Promise<BillingToday[]> =>
    fetchWithAuth("/api/v1/portal/billing/today"),

  getMonthlyReports: (params?: {
    year?: number;
    month?: number;
  }): Promise<MonthlyUsageReport[]> => {
    const searchParams = new URLSearchParams();
    if (params?.year) searchParams.set("year", String(params.year));
    if (params?.month) searchParams.set("month", String(params.month));
    const qs = searchParams.toString();
    return fetchWithAuth(`/api/v1/portal/billing/monthly-reports${qs ? `?${qs}` : ""}`);
  },

  generateMonthlyReports: (data: {
    periodYear: number;
    periodMonth: number;
  }): Promise<{ message: string }> =>
    fetchWithAuth("/api/v1/portal/billing/monthly-reports/generate", {
      method: "POST",
      body: JSON.stringify(data),
    }),

  getInvoices: (): Promise<BillingInvoice[]> =>
    fetchWithAuth("/api/v1/portal/billing/invoices"),

  generateInvoice: (data: {
    companyId: string;
    periodYear: number;
    periodMonth: number;
  }): Promise<BillingInvoice> =>
    fetchWithAuth("/api/v1/portal/billing/invoices/generate", {
      method: "POST",
      body: JSON.stringify(data),
    }),
};
