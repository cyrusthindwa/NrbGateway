// NRB Gateway Console — Type Definitions

export interface AdminUser {
  id: string;
  name: string;
  email: string;
  status: "ACTIVE" | "DISABLED";
  createdAt: string;
}

export interface Company {
  id: string;
  name: string;
  shortCode: string;
  createdAt: string;
}

export interface Project {
  id: string;
  companyId: string;
  name: string;
  shortCode: string;
  createdAt: string;
}

export interface ProjectApiKey {
  id: string;
  projectId: string;
  keyPrefix: string;
  plaintextApiKey?: string;
  status: "ACTIVE" | "REVOKED";
  rateLimitPerMinute: number;
  createdAt: string;
  rotatedAtRevokedAt?: string;
}

export interface TierSetting {
  tier: "BASIC" | "TEXT_LOOKUP" | "INTERMEDIATE" | "ADVANCED";
  enabled: boolean;
  costPerRequest: number;
  updatedAt: string;
  updatedBy: string;
}

export interface EnvironmentSetting {
  id: string;
  environment: "TEST" | "PRODUCTION";
  basicEndpointUrl: string;
  textLookupEndpointUrl: string;
  intermediateEndpointUrl: string;
  advancedEndpointUrl: string;
  updatedAt: string;
  updatedBy: string;
}

export interface AuditLogEntry {
  id: string;
  timestamp: string;
  admin: string;
  settingChanged: string;
  oldValue: string;
  newValue: string;
  actionType: string;
}

export interface DashboardMetrics {
  activeProjects: number;
  activeProjectsChange: number;
  requestsToday: number;
  requestsTodayChange: number;
  cacheHitRate: number;
  cacheHitRateTarget: number;
  nrbLinkStatus: "Healthy" | "Degraded" | "Down";
  nrbLinkLatency: number;
}

export interface RecentChange {
  id: string;
  admin: string;
  changeDetails: string;
  timestamp: string;
}

export interface DailyUsage {
  day: string;
  requests: number;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  adminId: string;
  name: string;
  email: string;
}

export interface PaginatedResponse<T> {
  data: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface RevalidationResult {
  totalChecked: number;
  valid: number;
  expired: number;
  deceased: number;
  seeNrb: number;
  errors: number;
  startedAt: string;
  completedAt: string;
}
