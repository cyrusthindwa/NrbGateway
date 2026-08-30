// NRB Gateway Console — Type Definitions

export interface AdminUser {
  id: string;
  name: string;
  email: string;
  status: "ACTIVE" | "DISABLED";
  createdAt: string;
}

export interface ManualPortalUser {
  id: string;
  email: string;
  companyId: string;
  companyName: string;
  status: "ACTIVE" | "DISABLED";
  createdAt: string;
  lastLoginAt: string | null;
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
  cacheHitRate: number | null;
  cacheHitRateTarget: number;
  nrbLinkStatus: "Healthy" | "Degraded" | "Down" | "Not yet monitored";
  nrbLinkLatency: number | null;
  nrbLastCheckedAt: string | null;
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

export interface LoginChallenge {
  adminId: string;
  expiresInSeconds: number;
  message: string;
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

export type NotificationChannelType = "EMAIL" | "SMS" | "WEBHOOK";

export interface NotificationChannel {
  id: string;
  channelType: NotificationChannelType;
  target: string;
  enabled: boolean;
  createdBy: string;
  createdAt: string;
}

export interface CreateAdminUserRequest {
  name: string;
  email: string;
  password: string;
}

export interface UpdateAdminUserRequest {
  name: string;
  email: string;
}

export interface ResetPasswordRequest {
  adminId: string;
  token: string;
  newPassword: string;
}

export interface RevalidationBatch {
  id: string;
  triggerType: "MANUAL" | "SCHEDULED";
  initiatedBy: string | null;
  initiatedByName: string | null;
  startedAt: string;
  completedAt: string | null;
  totalCount: number;
  validCount: number;
  expiredCount: number;
  deceasedCount: number;
  seeNrbCount: number;
  errorCount: number;
}

export interface NrbDowntimeIncident {
  id: string;
  startedAt: string;
  endedAt: string | null;
  detectedBy: string;
  notified: boolean;
  resolvedBy: string | null;
  resolvedByName: string | null;
}

export interface NrbStatus {
  status: "Healthy" | "Down" | "Not yet monitored";
  isUp: boolean | null;
  latencyMs: number | null;
  errorMessage: string | null;
  lastCheckedAt: string | null;
  openIncident: NrbDowntimeIncident | null;
}

export interface ProjectUsageToday {
  projectId: string;
  projectName: string;
  projectShortCode: string;
  totalCost: number;
  totalRequests: number;
}

export interface BillingToday {
  companyId: string;
  companyName: string;
  companyShortCode: string;
  companyTotalCost: number;
  companyTotalRequests: number;
  projects: ProjectUsageToday[];
}

export interface MonthlyUsageReport {
  id: string;
  projectId: string;
  projectName: string;
  projectShortCode: string;
  companyId: string;
  companyName: string;
  periodYear: number;
  periodMonth: number;
  requestCount: number;
  totalCost: number;
  generatedAt: string;
}

export interface BillingInvoice {
  id: string;
  companyId: string;
  companyName: string;
  companyShortCode: string;
  periodStart: string;
  periodEnd: string;
  totalAmount: number;
  status: "PENDING" | "INVOICED" | "PAID";
  generatedAt: string;
  paidAt: string | null;
}
