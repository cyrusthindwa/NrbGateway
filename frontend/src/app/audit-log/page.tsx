"use client";

import { useEffect, useState, useCallback } from "react";
import PortalLayout from "@/components/layout/PortalLayout";
import { Card, PageHeader, Button, Badge } from "@/components/ui/common";
import { useAuth } from "@/contexts/AuthContext";
import { useRouter } from "next/navigation";
import { apiService } from "@/services/api";
import { ChevronLeft, ChevronRight, Download } from "lucide-react";
import type { AuditLogEntry } from "@/types";

export default function AuditLogPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();
  const [logs, setLogs] = useState<AuditLogEntry[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [filters, setFilters] = useState({
    dateRange: "last7",
    admin: "all",
    actionType: "all",
  });

  const pageSize = 10;

  const loadLogs = useCallback(async () => {
    setIsLoading(true);
    try {
      const result = await apiService.getAuditLogs({ page, pageSize });
      setLogs(result.data);
      setTotal(result.total);
      setTotalPages(result.totalPages);
    } catch (err) {
      console.error("Failed to load audit logs:", err);
    } finally {
      setIsLoading(false);
    }
  }, [page]);

  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      router.push("/login");
      return;
    }
    if (isAuthenticated) {
      loadLogs();
    }
  }, [isAuthenticated, authLoading, loadLogs]);

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
        <Button variant="primary">
          <Download size={16} />
          Export CSV
        </Button>
      </PageHeader>

      {/* Filters */}
      <Card className="p-5 mb-6">
        <h4 className="text-sm font-semibold text-navy-800 mb-3">Filter Criteria</h4>
        <div className="flex items-end gap-4 flex-wrap">
          <div>
            <label className="block text-xs font-medium text-slate-500 mb-1">
              Date Range
            </label>
            <select
              value={filters.dateRange}
              onChange={(e) =>
                setFilters((f) => ({ ...f, dateRange: e.target.value }))
              }
              className="border border-slate-300 rounded-lg px-3 py-2 text-sm bg-white"
            >
              <option value="last7">Last 7 Days</option>
              <option value="last30">Last 30 Days</option>
              <option value="last90">Last 90 Days</option>
              <option value="custom">Custom Range</option>
            </select>
          </div>

          <div>
            <label className="block text-xs font-medium text-slate-500 mb-1">
              Administrator
            </label>
            <select
              value={filters.admin}
              onChange={(e) =>
                setFilters((f) => ({ ...f, admin: e.target.value }))
              }
              className="border border-slate-300 rounded-lg px-3 py-2 text-sm bg-white"
            >
              <option value="all">All Administrators</option>
              <option value="sysadmin">sysadmin@nrb.gov</option>
              <option value="sec_ops">sec_ops@nrb.gov</option>
            </select>
          </div>

          <div>
            <label className="block text-xs font-medium text-slate-500 mb-1">
              Action Type
            </label>
            <select
              value={filters.actionType}
              onChange={(e) =>
                setFilters((f) => ({ ...f, actionType: e.target.value }))
              }
              className="border border-slate-300 rounded-lg px-3 py-2 text-sm bg-white"
            >
              <option value="all">All Actions</option>
              <option value="UPDATE">Update</option>
              <option value="CREATE">Create</option>
              <option value="REVOKE">Revoke</option>
              <option value="DELETE">Delete</option>
            </select>
          </div>

          <Button variant="primary" onClick={loadLogs}>
            Apply Filters
          </Button>
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
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td colSpan={5} className="text-center py-12 text-slate-500">
                    Loading...
                  </td>
                </tr>
              ) : logs.length === 0 ? (
                <tr>
                  <td colSpan={5} className="text-center py-12 text-slate-500">
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
                      {log.timestamp}
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
