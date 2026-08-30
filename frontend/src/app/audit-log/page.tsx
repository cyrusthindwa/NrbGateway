"use client";

import { useEffect, useState, useCallback } from "react";
import PortalLayout from "@/components/layout/PortalLayout";
import { Card, PageHeader, Button } from "@/components/ui/common";
import { useAuth } from "@/contexts/AuthContext";
import { useRouter } from "next/navigation";
import { apiService } from "@/services/api";
import { ChevronLeft, ChevronRight, Download, RotateCcw, AlertCircle } from "lucide-react";
import type { AuditLogEntry, AdminUser } from "@/types";
import { formatDateTime } from "@/lib/format";

const ACTION_TYPES = [
  { value: "TIER_TOGGLE", label: "Tier Toggle" },
  { value: "RATE_LIMIT", label: "Rate Limit" },
  { value: "NRB_ENVIRONMENT", label: "NRB Environment" },
  { value: "PROJECT_KEY", label: "Project Key" },
  { value: "ADMIN_USER", label: "Admin User" },
  { value: "COMPANY", label: "Company" },
  { value: "PROJECT", label: "Project" },
  { value: "NOTIFICATION_CHANNEL", label: "Notification Channel" },
  { value: "AUDIT_RETENTION", label: "Audit Retention" },
];

const ROLLBACKABLE = new Set(["TIER_TOGGLE", "RATE_LIMIT", "NRB_ENVIRONMENT"]);

export default function AuditLogPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();
  const [logs, setLogs] = useState<AuditLogEntry[]>([]);
  const [admins, setAdmins] = useState<AdminUser[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [adminFilter, setAdminFilter] = useState("all");
  const [actionTypeFilter, setActionTypeFilter] = useState("all");

  const pageSize = 10;

  const loadLogs = useCallback(async () => {
    setIsLoading(true);
    setError("");
    try {
      const result = await apiService.getAuditLogs({
        page,
        pageSize,
        admin: adminFilter === "all" ? undefined : adminFilter,
        actionType: actionTypeFilter === "all" ? undefined : actionTypeFilter,
      });
      setLogs(result.data);
      setTotal(result.total);
      setTotalPages(result.totalPages);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load audit logs.");
    } finally {
      setIsLoading(false);
    }
  }, [page, adminFilter, actionTypeFilter]);

  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      router.push("/login");
      return;
    }
    if (isAuthenticated) {
      loadLogs();
      apiService
        .getAdminUsers({ page: 1, pageSize: 100 })
        .then((r) => setAdmins(r.data))
        .catch(() => {});
    }
  }, [isAuthenticated, authLoading, loadLogs]);

  async function handleRollback(id: string) {
    if (!confirm("Roll back this change to its previous value?")) return;
    setError("");
    try {
      await apiService.rollbackAuditEntry(id);
      loadLogs();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Rollback failed.");
    }
  }

  async function handleExportCsv() {
    setError("");
    try {
      const result = await apiService.getAuditLogs({
        page: 1,
        pageSize: 1000,
        admin: adminFilter === "all" ? undefined : adminFilter,
        actionType: actionTypeFilter === "all" ? undefined : actionTypeFilter,
      });
      const esc = (v: string) => `"${(v ?? "").replace(/"/g, '""')}"`;
      const lines = [
        ["Timestamp (UTC)", "Admin", "Setting Changed", "Old Value", "New Value"].join(","),
        ...result.data.map((r) =>
          [r.timestamp, r.admin, r.settingChanged, r.oldValue, r.newValue].map(esc).join(",")
        ),
      ];
      const blob = new Blob([lines.join("\n")], { type: "text/csv;charset=utf-8;" });
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = "audit-log.csv";
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Export failed.");
    }
  }

  if (authLoading) {
    return (
      <PortalLayout>
        <div className="flex items-center justify-center h-64">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-orange-500" />
        </div>
      </PortalLayout>
    );
  }

  return (
    <PortalLayout>
      <PageHeader
        title="System Audit Log"
        description="Immutable record of all administrative configuration changes."
      >
        <Button variant="primary" onClick={handleExportCsv}>
          <Download size={16} />
          Export CSV
        </Button>
      </PageHeader>

      {error && (
        <div className="mb-4 flex items-center gap-2 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
          <AlertCircle size={16} />
          <span>{error}</span>
        </div>
      )}

      {/* Filters */}
      <Card className="p-5 mb-6">
        <h4 className="text-sm font-semibold text-navy-800 mb-3">Filter Criteria</h4>
        <div className="flex items-end gap-4 flex-wrap">
          <div>
            <label className="block text-xs font-medium text-slate-500 mb-1">
              Administrator
            </label>
            <select
              value={adminFilter}
              onChange={(e) => {
                setAdminFilter(e.target.value);
                setPage(1);
              }}
              className="border border-slate-300 rounded-lg px-3 py-2 text-sm bg-white"
            >
              <option value="all">All Administrators</option>
              {admins.map((a) => (
                <option key={a.id} value={a.id}>
                  {a.name}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-xs font-medium text-slate-500 mb-1">
              Action Type
            </label>
            <select
              value={actionTypeFilter}
              onChange={(e) => {
                setActionTypeFilter(e.target.value);
                setPage(1);
              }}
              className="border border-slate-300 rounded-lg px-3 py-2 text-sm bg-white"
            >
              <option value="all">All Actions</option>
              {ACTION_TYPES.map((t) => (
                <option key={t.value} value={t.value}>
                  {t.label}
                </option>
              ))}
            </select>
          </div>
        </div>
      </Card>

      {/* Table */}
      <Card>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-200 bg-slate-50">
                <th className="text-left px-5 py-3 text-xs font-medium text-slate-500 uppercase">
                  Timestamp (UTC)
                </th>
                <th className="text-left px-5 py-3 text-xs font-medium text-slate-500 uppercase">
                  Admin
                </th>
                <th className="text-left px-5 py-3 text-xs font-medium text-slate-500 uppercase">
                  Setting Changed
                </th>
                <th className="text-left px-5 py-3 text-xs font-medium text-slate-500 uppercase">
                  Old Value
                </th>
                <th className="text-left px-5 py-3 text-xs font-medium text-slate-500 uppercase">
                  New Value
                </th>
                <th className="text-right px-5 py-3 text-xs font-medium text-slate-500 uppercase">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td colSpan={6} className="text-center py-12 text-slate-500">
                    Loading...
                  </td>
                </tr>
              ) : logs.length === 0 ? (
                <tr>
                  <td colSpan={6} className="text-center py-12 text-slate-500">
                    No audit log entries found.
                  </td>
                </tr>
              ) : (
                logs.map((log) => (
                  <tr
                    key={log.id}
                    className="border-b border-slate-100 hover:bg-slate-50"
                  >
                    <td className="px-5 py-3 text-slate-600 font-mono text-xs">
                      {formatDateTime(log.timestamp)}
                    </td>
                    <td className="px-5 py-3 font-medium text-navy-800">
                      {log.admin}
                    </td>
                    <td className="px-5 py-3 text-slate-600">
                      {log.settingChanged}
                    </td>
                    <td className="px-5 py-3">
                      <span className="text-red-600 bg-red-50 px-2 py-0.5 rounded text-xs">
                        {log.oldValue}
                      </span>
                    </td>
                    <td className="px-5 py-3">
                      <span className="text-green-600 bg-green-50 px-2 py-0.5 rounded text-xs">
                        {log.newValue}
                      </span>
                    </td>
                    <td className="px-5 py-3 text-right">
                      {ROLLBACKABLE.has(log.actionType) && (
                        <Button
                          variant="ghost"
                          className="text-xs"
                          onClick={() => handleRollback(log.id)}
                        >
                          <RotateCcw size={14} />
                          Rollback
                        </Button>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        <div className="flex items-center justify-between px-5 py-3 border-t border-slate-200">
          <p className="text-sm text-slate-500">
            Showing {(page - 1) * pageSize + 1} to{" "}
            {Math.min(page * pageSize, total)} of {total} entries
          </p>
          <div className="flex items-center gap-1">
            <button
              disabled={page <= 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              className="p-2 rounded-lg hover:bg-slate-100 disabled:opacity-30 disabled:cursor-not-allowed"
            >
              <ChevronLeft size={16} />
            </button>
            {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
              const pageNum = i + 1;
              return (
                <button
                  key={pageNum}
                  onClick={() => setPage(pageNum)}
                  className={`w-8 h-8 rounded-lg text-sm ${
                    page === pageNum
                      ? "bg-orange-500 text-white"
                      : "hover:bg-slate-100 text-slate-600"
                  }`}
                >
                  {pageNum}
                </button>
              );
            })}
            {totalPages > 5 && <span className="px-1 text-slate-400">...</span>}
            <button
              disabled={page >= totalPages}
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              className="p-2 rounded-lg hover:bg-slate-100 disabled:opacity-30 disabled:cursor-not-allowed"
            >
              <ChevronRight size={16} />
            </button>
          </div>
        </div>
      </Card>
    </PortalLayout>
  );
}
