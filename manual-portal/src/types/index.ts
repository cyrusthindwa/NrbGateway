export interface ManualUser {
  id: string;
  email: string;
  companyId: string;
  companyName: string;
  mustChangePassword?: boolean;
}

export interface LoginResponse {
  requires2Fa: boolean;
  userId?: string;
  email?: string;
  token?: string;
  companyId?: string;
  companyName?: string;
  message?: string;
  mustChangePassword?: boolean;
}

export interface VerificationResult {
  verificationId: string;
  idNumber: string;
  surname: string;
  firstName: string;
  otherNames?: string | null;
  dateOfBirth: string;
  gender: string;
  nationality?: string | null;
  civilStatus?: string | null;
  birthDistrict?: string | null;
  residenceAddress?: string | null;
  nrbRegisteredPhone?: string | null;
  middlewareStatus?: string | null;
  photoRef?: string | null;
  fingerprintRef?: string | null;
  servedFrom: string;
  found: boolean;
  timestamp: string;
  cardStatus?: string | null;
  issueDate?: string | null;
  expiryDate?: string | null;
}

export interface LogItem {
  id: string;
  nationalIdMasked: string;
  resultStatus: string;
  gatewayRequestId?: string | null;
  requestedAt: string;
}

export interface DashboardMetrics {
  verificationsThisMonth: number;
  recentVerifications: LogItem[];
}

export interface PaginatedLogHistory {
  items: LogItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
