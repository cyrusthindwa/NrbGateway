"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import PortalLayout from "@/components/layout/PortalLayout";
import { Card, PageHeader, Badge } from "@/components/ui/common";
import { useAuth } from "@/contexts/AuthContext";
import { useRouter } from "next/navigation";
import { apiService } from "@/services/api";
import { AlertCircle, Wifi, WifiOff, Bell } from "lucide-react";
import type { NrbStatus, NrbDowntimeIncident } from "@/types";
import { formatDateTime } from "@/lib/format";

export default function UptimePage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();
  const [status, setStatus] = useState<NrbStatus | null>(null);
  const [incidents, setIncidents] = useState<NrbDowntimeIncident[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");

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
    setError("");
    try {
      const [s, i] = await Promise.all([apiService.getNrbStatus(), apiService.getNrbIncidents()]);
      setStatus(s);
      setIncidents(i);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load NRB status.");
    } finally {
      setIsLoading(false);
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

  const isHealthy = status?.status === "Healthy";
  const isDown = status?.status === "Down";

  return (
    <PortalLayout>
      <PageHeader
        title="NRB Uptime"
        description="Link health derived from live verification traffic and downtime incident history."
      >
        <Link href="/notification-channels" className="inline-flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium bg-white text-slate-700 border border-slate-300 hover:bg-slate-50 transition-colors">
          <Bell size={16} />
          Notification Channels
        </Link>
      </PageHeader>

      {error && (
        <div className="mb-4 flex items-center gap-2 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
          <AlertCircle size={16} />
          <span>{error}</span>
        </div>
      )}

      {/* Current status */}
      <Card className="p-6 mb-6">
        <div className="flex items-center gap-4">
          <div
            className={`w-14 h-14 rounded-full flex items-center justify-center ${
              isHealthy ? "bg-green-100 text-green-600" : isDown ? "bg-red-100 text-red-600" : "bg-slate-100 text-slate-500"
            }`}
          >
            {isDown ? <WifiOff size={28} /> : <Wifi size={28} />}
          </div>
          <div>
            <h3 className="text-xl font-bold text-navy-800">{status?.status ?? "—"}</h3>
            <p className="text-sm text-slate-500">
              {status?.latencyMs != null
                ? `Latency: ${status.latencyMs}ms · Last checked ${formatDateTime(status.lastCheckedAt)}`
                : status?.status === "Not yet monitored"
                  ? "No traffic has reached NRB yet, so there is no health data to report."
                  : "No recent check recorded."}
            </p>
            {status?.errorMessage && (
              <p className="text-xs text-red-600 mt-1">{status.errorMessage}</p>
            )}
          </div>
        </div>

        {status?.openIncident && (
          <div className="mt-4 p-4 bg-red-50 border border-red-200 rounded-lg">
            <p className="text-sm font-semibold text-red-800">
              ⚠ Ongoing downtime since {formatDateTime(status.openIncident.startedAt)}
            </p>
          </div>
        )}
      </Card>

      {/* Incident history */}
      <Card>
        <div className="p-5 border-b border-slate-100">
          <h3 className="text-sm font-semibold text-navy-800">Downtime Incidents</h3>
        </div>
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-100 text-left text-xs text-slate-500 uppercase">
              <th className="px-5 py-3">Started</th>
              <th className="px-5 py-3">Ended</th>
              <th className="px-5 py-3">Detected by</th>
              <th className="px-5 py-3">Status</th>
            </tr>
          </thead>
          <tbody>
            {incidents.map((inc) => (
              <tr key={inc.id} className="border-b border-slate-50">
                <td className="px-5 py-3 text-slate-600 font-mono text-xs">{formatDateTime(inc.startedAt)}</td>
                <td className="px-5 py-3 text-slate-600 font-mono text-xs">{formatDateTime(inc.endedAt)}</td>
                <td className="px-5 py-3 text-slate-600">{inc.detectedBy}</td>
                <td className="px-5 py-3">
                  <Badge variant={inc.endedAt ? "success" : "danger"}>
                    {inc.endedAt ? "RESOLVED" : "ONGOING"}
                  </Badge>
                </td>
              </tr>
            ))}
            {incidents.length === 0 && (
              <tr><td colSpan={4} className="px-5 py-8 text-center text-slate-500">No downtime incidents recorded.</td></tr>
            )}
          </tbody>
        </table>
      </Card>
    </PortalLayout>
  );
}
