"use client";

import { useEffect, useState, FormEvent } from "react";
import PortalLayout from "@/components/layout/PortalLayout";
import { Card, PageHeader, Button } from "@/components/ui/common";
import { useAuth } from "@/contexts/AuthContext";
import { useRouter } from "next/navigation";
import { apiService } from "@/services/api";
import { AlertCircle, RefreshCw, FileText } from "lucide-react";
import type { BillingToday, MonthlyUsageReport } from "@/types";

export default function BillingPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();
  const [today, setToday] = useState<BillingToday[]>([]);
  const [reports, setReports] = useState<MonthlyUsageReport[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  const [reportYear, setReportYear] = useState(new Date().getFullYear());
  const [reportMonth, setReportMonth] = useState(new Date().getMonth() + 1);

  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      router.push("/login");
      return;
    }
    if (isAuthenticated) {
      loadData();
    }
  }, [isAuthenticated, authLoading]);

  async function loadData() {
    setIsLoading(true);
    setError("");
    try {
      const [t, r] = await Promise.all([
        apiService.getBillingToday(),
        apiService.getMonthlyReports(),
      ]);
      setToday(t);
      setReports(r);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load billing data.");
    } finally {
      setIsLoading(false);
    }
  }

  async function handleGenerateReports(e: FormEvent) {
    e.preventDefault();
    setError("");
    setMessage("");
    try {
      await apiService.generateMonthlyReports({ periodYear: reportYear, periodMonth: reportMonth });
      setMessage("Monthly usage reports generated successfully.");
      loadData();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to generate reports.");
    }
  }

  if (authLoading || isLoading) {
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
        title="Usage & Billing Reports"
        description="NRB billable usage reports, per project and aggregated per company."
      />

      {message && (
        <div className="mb-4 p-3 bg-green-50 border border-green-200 rounded-lg text-green-700 text-sm">
          {message}
        </div>
      )}
      {error && (
        <div className="mb-4 flex items-center gap-2 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
          <AlertCircle size={16} />
          <span>{error}</span>
        </div>
      )}

      {/* Today so far */}
      <Card className="p-6 mb-6">
        <div className="flex items-center justify-between mb-4">
          <div>
            <h3 className="text-base font-semibold text-navy-800">Today so far</h3>
            <p className="text-xs text-slate-500">Live billable NRB cache-miss queries served today</p>
          </div>
        </div>
        <div className="space-y-5">
          {today.map((company) => (
            <div key={company.companyId} className="border border-slate-100 rounded-xl p-4 bg-slate-50/50">
              <div className="flex items-center justify-between mb-2">
                <span className="font-semibold text-navy-900">{company.companyName}</span>
                <span className="text-sm font-bold text-slate-700">
                  {company.companyTotalRequests} requests · MWK {company.companyTotalCost.toFixed(2)}
                </span>
              </div>
              <table className="w-full text-sm table-fixed">
                <thead>
                  <tr className="text-left text-xs font-semibold text-slate-400 uppercase tracking-wider">
                    <th className="w-1/2 pb-2 font-medium">Project</th>
                    <th className="w-1/4 pb-2 text-right font-medium">Billable Requests</th>
                    <th className="w-1/4 pb-2 text-right font-medium">Total Cost</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {company.projects.map((p) => (
                    <tr key={p.projectId} className="hover:bg-slate-100/50 transition-colors">
                      <td className="py-2.5 text-slate-700 font-medium truncate pr-4">{p.projectName}</td>
                      <td className="py-2.5 text-right font-mono text-slate-600">{p.totalRequests}</td>
                      <td className="py-2.5 text-right font-mono font-semibold text-slate-800">
                        MWK {p.totalCost.toFixed(2)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ))}
          {today.length === 0 && <p className="text-slate-500 text-sm">No companies configured.</p>}
        </div>
      </Card>

      {/* Monthly usage reports */}
      <Card className="p-6">
        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4 mb-6">
          <div>
            <h3 className="text-base font-semibold text-navy-800">Monthly Usage Reports</h3>
            <p className="text-xs text-slate-500">Historical aggregated NRB usage reports by month and project</p>
          </div>
          <form onSubmit={handleGenerateReports} className="flex items-end gap-2">
            <div>
              <label className="block text-xs font-medium text-slate-500 mb-1">Year</label>
              <input
                type="number"
                value={reportYear}
                onChange={(e) => setReportYear(Number(e.target.value))}
                className="w-24 border border-slate-300 rounded-lg px-2.5 py-1.5 text-sm"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-500 mb-1">Month</label>
              <input
                type="number"
                min={1}
                max={12}
                value={reportMonth}
                onChange={(e) => setReportMonth(Number(e.target.value))}
                className="w-20 border border-slate-300 rounded-lg px-2.5 py-1.5 text-sm"
              />
            </div>
            <Button variant="secondary" type="submit" className="text-xs">
              <RefreshCw size={14} className="mr-1" /> Generate
            </Button>
          </form>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-200 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">
                <th className="py-3 px-2">Period</th>
                <th className="py-3 px-2">Company</th>
                <th className="py-3 px-2">Project</th>
                <th className="py-3 px-2 text-right">Billable Requests (NRB)</th>
                <th className="py-3 px-2 text-right">Total Cost</th>
                <th className="py-3 px-2 text-right">Generated At</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {reports.map((r) => (
                <tr key={r.id} className="hover:bg-slate-50/60 transition-colors">
                  <td className="py-3 px-2 font-mono font-medium text-slate-700">
                    {r.periodYear}-{String(r.periodMonth).padStart(2, "0")}
                  </td>
                  <td className="py-3 px-2 text-slate-800 font-medium">{r.companyName}</td>
                  <td className="py-3 px-2 text-slate-600">{r.projectName}</td>
                  <td className="py-3 px-2 text-right font-mono text-slate-700">{r.requestCount}</td>
                  <td className="py-3 px-2 text-right font-mono font-semibold text-slate-800">
                    MWK {r.totalCost.toFixed(2)}
                  </td>
                  <td className="py-3 px-2 text-right text-xs text-slate-500">
                    {new Date(r.generatedAt).toLocaleString()}
                  </td>
                </tr>
              ))}
              {reports.length === 0 && (
                <tr>
                  <td colSpan={6} className="py-12 text-center text-slate-400">
                    <FileText className="w-8 h-8 mx-auto mb-2 text-slate-300" />
                    <p className="text-sm font-medium">No usage reports generated yet.</p>
                    <p className="text-xs text-slate-400">Select a year and month above and click Generate.</p>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </Card>
    </PortalLayout>
  );
}
