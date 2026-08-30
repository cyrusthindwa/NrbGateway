"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { Search, History, CheckCircle2, AlertTriangle, XCircle, ArrowRight, Activity, Calendar } from "lucide-react";
import { format } from "date-fns";
import { apiService } from "@/services/api";
import { DashboardMetrics, LogItem } from "@/types";

export default function DashboardPage() {
  const [metrics, setMetrics] = useState<DashboardMetrics | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadMetrics() {
      try {
        const data = await apiService.getDashboard();
        setMetrics(data);
      } catch (err: any) {
        setError(err.message || "Failed to load dashboard data.");
      } finally {
        setLoading(false);
      }
    }
    loadMetrics();
  }, []);

  const renderStatusBadge = (status: string) => {
    const isVal = status.toUpperCase().includes("VALID");
    const isNf = status.toUpperCase().includes("NOT FOUND");
    const isExp = status.toUpperCase().includes("EXPIRED");

    if (isVal) {
      return (
        <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-bold bg-green-100 text-green-800">
          <CheckCircle2 className="w-3.5 h-3.5 mr-1 text-green-600" />
          {status}
        </span>
      );
    } else if (isExp || isNf) {
      return (
        <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-bold bg-red-100 text-red-800">
          <XCircle className="w-3.5 h-3.5 mr-1 text-red-600" />
          {status}
        </span>
      );
    } else {
      return (
        <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-bold bg-amber-100 text-amber-800">
          <AlertTriangle className="w-3.5 h-3.5 mr-1 text-amber-600" />
          {status}
        </span>
      );
    }
  };

  return (
    <div className="space-y-8">
      {/* Header Banner */}
      <div className="bg-gradient-to-r from-[#292D6B] to-[#1e2254] rounded-2xl p-6 sm:p-8 text-white shadow-lg relative overflow-hidden">
        <div className="relative z-10 max-w-2xl">
          <h1 className="text-2xl sm:text-3xl font-extrabold tracking-tight">
            NRB Manual Verification Gateway
          </h1>
          <p className="mt-2 text-sm text-slate-200">
            Verify National Registration Bureau biographic details for manual KYC checks.
          </p>
          <div className="mt-6">
            <Link
              href="/verify"
              className="inline-flex items-center space-x-2 bg-[#F48220] hover:bg-[#db6e10] text-white px-6 py-3 rounded-lg font-bold shadow-md transition-all transform hover:-translate-y-0.5"
            >
              <Search className="w-5 h-5" />
              <span>Start New Verification</span>
            </Link>
          </div>
        </div>
      </div>

      {/* Metrics Row */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Verifications This Month Card */}
        <div className="bg-white rounded-xl p-6 border border-slate-200 shadow-sm flex items-center justify-between">
          <div>
            <span className="text-xs font-bold text-slate-500 uppercase tracking-wider block">
              Verifications This Month
            </span>
            <div className="text-3xl font-extrabold text-[#292D6B] mt-2">
              {loading ? "..." : metrics?.verificationsThisMonth ?? 0}
            </div>
            <span className="text-xs text-slate-400 mt-1 block">
              Total lookups performed in current month
            </span>
          </div>
          <div className="w-12 h-12 bg-blue-50 text-[#292D6B] rounded-xl flex items-center justify-center">
            <Activity className="w-6 h-6 text-[#292D6B]" />
          </div>
        </div>

        {/* Quick Help Card */}
        <div className="bg-white rounded-xl p-6 border border-slate-200 shadow-sm flex items-center justify-between">
          <div>
            <span className="text-xs font-bold text-slate-500 uppercase tracking-wider block">
              KYC Check Guide
            </span>
            <p className="text-xs text-slate-600 mt-2 max-w-sm">
              Enter the 8-character National ID PIN to view verified name, DOB, gender, and card status against the NRB registry.
            </p>
          </div>
          <div className="w-12 h-12 bg-amber-50 text-[#F48220] rounded-xl flex items-center justify-center shrink-0 ml-4">
            <Calendar className="w-6 h-6 text-[#F48220]" />
          </div>
        </div>
      </div>

      {/* Recent Verifications Section */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <div className="p-6 border-b border-slate-100 flex items-center justify-between">
          <div>
            <h2 className="text-lg font-bold text-[#292D6B]">Recent Verifications</h2>
            <p className="text-xs text-slate-500">Your last 5 manual lookups</p>
          </div>
          <Link
            href="/history"
            className="text-xs font-bold text-[#F48220] hover:text-[#db6e10] flex items-center space-x-1"
          >
            <span>View Full History</span>
            <ArrowRight className="w-4 h-4" />
          </Link>
        </div>

        {loading ? (
          <div className="p-8 text-center text-slate-400 text-sm">
            Loading recent verifications...
          </div>
        ) : error ? (
          <div className="p-8 text-center text-red-500 text-sm">{error}</div>
        ) : !metrics?.recentVerifications || metrics.recentVerifications.length === 0 ? (
          <div className="p-12 text-center text-slate-400">
            <Search className="w-10 h-10 mx-auto text-slate-300 mb-3" />
            <p className="text-sm font-medium">No verifications performed yet this month.</p>
            <Link
              href="/verify"
              className="inline-block mt-3 text-xs font-bold text-[#F48220] hover:underline"
            >
              Perform your first verification →
            </Link>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-100">
              <thead className="bg-slate-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider">
                    National ID (Masked)
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider">
                    Status
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider">
                    Date & Time
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-slate-100">
                {metrics.recentVerifications.map((item: LogItem) => (
                  <tr key={item.id} className="hover:bg-slate-50/80 transition-colors">
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-mono font-bold text-slate-800">
                      {item.nationalIdMasked}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      {renderStatusBadge(item.resultStatus)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-xs text-slate-500">
                      {format(new Date(item.requestedAt), "dd MMM yyyy, HH:mm:ss")}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
