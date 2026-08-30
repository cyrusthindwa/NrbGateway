"use client";

import { useEffect, useState, FormEvent } from "react";
import PortalLayout from "@/components/layout/PortalLayout";
import { Card, PageHeader, Button, Badge } from "@/components/ui/common";
import { useAuth } from "@/contexts/AuthContext";
import { useRouter } from "next/navigation";
import { apiService } from "@/services/api";
import { AlertCircle, RefreshCw } from "lucide-react";
import type { BillingToday, MonthlyUsageReport, BillingInvoice, Company } from "@/types";

export default function BillingPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();
  const [today, setToday] = useState<BillingToday[]>([]);
  const [reports, setReports] = useState<MonthlyUsageReport[]>([]);
  const [invoices, setInvoices] = useState<BillingInvoice[]>([]);
  const [companies, setCompanies] = useState<Company[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  const [reportYear, setReportYear] = useState(new Date().getFullYear());
  const [reportMonth, setReportMonth] = useState(new Date().getMonth() + 1);
  const [invoiceCompanyId, setInvoiceCompanyId] = useState("");
  const [invoiceYear, setInvoiceYear] = useState(new Date().getFullYear());
  const [invoiceMonth, setInvoiceMonth] = useState(new Date().getMonth() + 1);

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
      const [t, r, i, c] = await Promise.all([
        apiService.getBillingToday(),
        apiService.getMonthlyReports(),
        apiService.getInvoices(),
        apiService.getCompanies(),
      ]);
      setToday(t);
      setReports(r);
      setInvoices(i);
      setCompanies(c);
      if (c.length > 0 && !invoiceCompanyId) setInvoiceCompanyId(c[0].id);
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
      setMessage("Monthly usage reports generated.");
      loadData();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to generate reports.");
    }
  }

  async function handleGenerateInvoice(e: FormEvent) {
    e.preventDefault();
    setError("");
    setMessage("");
    try {
      await apiService.generateInvoice({
        companyId: invoiceCompanyId,
        periodYear: invoiceYear,
        periodMonth: invoiceMonth,
      });
      setMessage("Invoice generated.");
      loadData();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to generate invoice.");
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
      <PageHeader title="Billing" description="Usage and invoices, per project and rolled up per company." />

      {message && (
        <div className="mb-4 p-3 bg-green-50 border border-green-200 rounded-lg text-green-700 text-sm">{message}</div>
      )}
      {error && (
        <div className="mb-4 flex items-center gap-2 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
          <AlertCircle size={16} />
          <span>{error}</span>
        </div>
      )}

      {/* Today so far */}
      <Card className="p-6 mb-6">
        <h3 className="text-sm font-semibold text-navy-800 mb-4">Today so far</h3>
        <div className="space-y-5">
          {today.map((company) => (
            <div key={company.companyId} className="border border-slate-100 rounded-lg p-4">
              <div className="flex items-center justify-between mb-2">
                <span className="font-medium text-navy-800">{company.companyName}</span>
                <span className="text-sm text-slate-600">
                  {company.companyTotalRequests} requests · MWK {company.companyTotalCost.toFixed(2)}
                </span>
              </div>
              <table className="w-full text-sm">
                <tbody>
                  {company.projects.map((p) => (
                    <tr key={p.projectId} className="border-t border-slate-50">
                      <td className="py-2 text-slate-600">{p.projectName}</td>
                      <td className="py-2 text-right text-slate-500">{p.totalRequests}</td>
                      <td className="py-2 text-right text-slate-600">MWK {p.totalCost.toFixed(2)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ))}
          {today.length === 0 && <p className="text-slate-500 text-sm">No companies configured.</p>}
        </div>
      </Card>

      <div className="grid grid-cols-2 gap-6">
        {/* Monthly usage reports */}
        <Card className="p-6">
          <h3 className="text-sm font-semibold text-navy-800 mb-4">Monthly Usage Reports</h3>
          <form onSubmit={handleGenerateReports} className="flex items-end gap-2 mb-4">
            <div>
              <label className="block text-xs text-slate-500 mb-1">Year</label>
              <input type="number" value={reportYear} onChange={(e) => setReportYear(Number(e.target.value))} className="w-24 border border-slate-300 rounded-lg px-2 py-1.5 text-sm" />
            </div>
            <div>
              <label className="block text-xs text-slate-500 mb-1">Month</label>
              <input type="number" min={1} max={12} value={reportMonth} onChange={(e) => setReportMonth(Number(e.target.value))} className="w-20 border border-slate-300 rounded-lg px-2 py-1.5 text-sm" />
            </div>
            <Button variant="secondary" type="submit" className="text-xs">
              <RefreshCw size={14} /> Generate
            </Button>
          </form>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-slate-100 text-left text-xs text-slate-500 uppercase">
                  <th className="py-2">Period</th>
                  <th className="py-2">Project</th>
                  <th className="py-2 text-right">Requests</th>
                  <th className="py-2 text-right">Cost</th>
                </tr>
              </thead>
              <tbody>
                {reports.map((r) => (
                  <tr key={r.id} className="border-b border-slate-50">
                    <td className="py-2 text-slate-600">{r.periodYear}-{String(r.periodMonth).padStart(2, "0")}</td>
                    <td className="py-2 text-slate-600">{r.projectName}</td>
                    <td className="py-2 text-right text-slate-600">{r.requestCount}</td>
                    <td className="py-2 text-right text-slate-600">MWK {r.totalCost.toFixed(2)}</td>
                  </tr>
                ))}
                {reports.length === 0 && (
                  <tr><td colSpan={4} className="py-8 text-center text-slate-500">No reports yet.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </Card>

        {/* Invoices */}
        <Card className="p-6">
          <h3 className="text-sm font-semibold text-navy-800 mb-4">Invoices</h3>
          <form onSubmit={handleGenerateInvoice} className="flex items-end gap-2 mb-4">
            <div>
              <label className="block text-xs text-slate-500 mb-1">Company</label>
              <select value={invoiceCompanyId} onChange={(e) => setInvoiceCompanyId(e.target.value)} className="border border-slate-300 rounded-lg px-2 py-1.5 text-sm bg-white">
                {companies.map((c) => (
                  <option key={c.id} value={c.id}>{c.name}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-xs text-slate-500 mb-1">Year</label>
              <input type="number" value={invoiceYear} onChange={(e) => setInvoiceYear(Number(e.target.value))} className="w-20 border border-slate-300 rounded-lg px-2 py-1.5 text-sm" />
            </div>
            <div>
              <label className="block text-xs text-slate-500 mb-1">Month</label>
              <input type="number" min={1} max={12} value={invoiceMonth} onChange={(e) => setInvoiceMonth(Number(e.target.value))} className="w-16 border border-slate-300 rounded-lg px-2 py-1.5 text-sm" />
            </div>
            <Button variant="secondary" type="submit" className="text-xs">Generate</Button>
          </form>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-slate-100 text-left text-xs text-slate-500 uppercase">
                  <th className="py-2">Company</th>
                  <th className="py-2">Period</th>
                  <th className="py-2 text-right">Amount</th>
                  <th className="py-2 text-right">Status</th>
                </tr>
              </thead>
              <tbody>
                {invoices.map((inv) => (
                  <tr key={inv.id} className="border-b border-slate-50">
                    <td className="py-2 text-slate-600">{inv.companyName}</td>
                    <td className="py-2 text-slate-600">{inv.periodStart} → {inv.periodEnd}</td>
                    <td className="py-2 text-right text-slate-600">MWK {inv.totalAmount.toFixed(2)}</td>
                    <td className="py-2 text-right">
                      <Badge variant={inv.status === "PAID" ? "success" : inv.status === "PENDING" ? "warning" : "info"}>
                        {inv.status}
                      </Badge>
                    </td>
                  </tr>
                ))}
                {invoices.length === 0 && (
                  <tr><td colSpan={4} className="py-8 text-center text-slate-500">No invoices yet.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </Card>
      </div>
    </PortalLayout>
  );
}
