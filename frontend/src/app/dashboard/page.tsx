"use client";

import { useEffect, useState } from "react";
import PortalLayout from "@/components/layout/PortalLayout";
import { StatCard, Card, PageHeader, Badge, Toggle } from "@/components/ui/common";
import { useAuth } from "@/contexts/AuthContext";
import { useRouter } from "next/navigation";
import { apiService } from "@/services/api";
import {
  Building2,
  Activity,
  Zap,
  Wifi,
  RefreshCw,
} from "lucide-react";
import type { DashboardMetrics, RecentChange, TierSetting, RevalidationResult } from "@/types";

export default function DashboardPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();
  const [metrics, setMetrics] = useState<DashboardMetrics | null>(null);
  const [recentChanges, setRecentChanges] = useState<RecentChange[]>([]);
  const [tiers, setTiers] = useState<TierSetting[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [tierLoading, setTierLoading] = useState<Record<string, boolean>>({});
  const [revalidating, setRevalidating] = useState(false);
  const [revalidationResult, setRevalidationResult] = useState<RevalidationResult | null>(null);

  async function handleRevalidate() {
    if (revalidating) return;
    setRevalidating(true);
    setRevalidationResult(null);
    try {
      const result = await apiService.revalidateAll();
      setRevalidationResult(result);
    } catch (err) {
      console.error("Revalidation failed:", err);
    } finally {
      setRevalidating(false);
    }
  }

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
    try {
      const [m, rc, t] = await Promise.all([
        apiService.getDashboardMetrics(),
        apiService.getRecentChanges(),
        apiService.getTierSettings(),
      ]);
      setMetrics(m);
      setRecentChanges(rc);
      setTiers(t);
    } catch (err) {
      console.error("Failed to load dashboard data:", err);
    } finally {
      setIsLoading(false);
    }
  }

  async function handleTierToggle(tierKey: string, currentlyEnabled: boolean) {
    const newEnabled = !currentlyEnabled;
    setTierLoading((prev) => ({ ...prev, [tierKey]: true }));

    // Optimistic update: toggle UI immediately
    setTiers((prev) =>
      prev.map((t) =>
        t.tier === tierKey ? { ...t, enabled: newEnabled } : t
      )
    );

    try {
      await apiService.updateTierSetting(tierKey, newEnabled);
    } catch (err) {
      console.error("Failed to update tier setting:", err);
      // Revert on failure
      setTiers((prev) =>
        prev.map((t) =>
          t.tier === tierKey ? { ...t, enabled: currentlyEnabled } : t
        )
      );
    } finally {
      setTierLoading((prev) => ({ ...prev, [tierKey]: false }));
    }
  }

  const tierLabels: Record<string, { title: string; description: string }> = {
    BASIC: { title: "Basic", description: "NRB verification" },
    TEXT_LOOKUP: { title: "Text Lookup", description: "Demographic matching" },
    INTERMEDIATE: { title: "Intermediate", description: "Biometric facial match" },
    ADVANCED: { title: "Advanced", description: "Document + Biometrics" },
  };

  if (authLoading || isLoading) {
    return (
      <PortalLayout>
        <div className="flex items-center justify-center h-64">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-orange-500" />
        </div>
      </PortalLayout>
    );
  }

  const formatNumber = (n: number) => {
    if (n >= 1000000) return `${(n / 1000000).toFixed(1)}M`;
    if (n >= 1000) return `${(n / 1000).toFixed(1)}K`;
    return String(n);
  };

  return (
    <PortalLayout>
      <PageHeader title="Console">
        <button
          onClick={handleRevalidate}
          disabled={revalidating}
          className="inline-flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium bg-orange-500 text-white hover:bg-orange-600 transition-colors disabled:opacity-50"
        >
          <RefreshCw size={16} className={revalidating ? "animate-spin" : ""} />
          {revalidating ? "Re-validating..." : "Re-validate All PINs"}
        </button>
      </PageHeader>

      {/* Revalidation result banner */}
      {revalidationResult && (
        <div className="mb-6 p-4 bg-blue-50 border border-blue-200 rounded-lg">
          <p className="text-sm font-semibold text-blue-800 mb-1">
            Re-validation Complete
          </p>
          <p className="text-xs text-blue-700">
            Checked {revalidationResult.totalChecked} PINs — {revalidationResult.valid} valid,{" "}
            {revalidationResult.expired} expired, {revalidationResult.deceased} deceased,{" "}
            {revalidationResult.seeNrb} need NRB review, {revalidationResult.errors} errors.
          </p>
        </div>
      )}

      {/* Metrics Cards */}
      <div className="grid grid-cols-4 gap-5 mb-8">
        <StatCard
          label="ACTIVE PROJECTS"
          value={metrics?.activeProjects ?? "-"}
          subValue={`↑ ${metrics?.activeProjectsChange ?? 0} this month`}
          icon={<Building2 size={20} />}
          color="green"
        />
        <StatCard
          label="REQUESTS TODAY"
          value={metrics ? formatNumber(metrics.requestsToday) : "-"}
          subValue={`↑ ${metrics?.requestsTodayChange ?? 0}%`}
          icon={<Activity size={20} />}
          color="blue"
        />
        <StatCard
          label="CACHE HIT RATE"
          value={`${metrics?.cacheHitRate ?? 0}%`}
          subValue={`Target: ${metrics?.cacheHitRateTarget ?? 0}%`}
          icon={<Zap size={20} />}
          color="orange"
        />
        <StatCard
          label="NRB LINK STATUS"
          value={`${metrics?.nrbLinkStatus ?? "-"} (${metrics?.nrbLinkLatency ?? 0}ms)`}
          subValue={`Latency: ${metrics?.nrbLinkLatency ?? 0}ms`}
          icon={<Wifi size={20} />}
        />
      </div>

      {/* Recent Changes & Verification Tiers */}
      <div className="grid grid-cols-2 gap-6">
        {/* Recent Changes */}
        <Card>
          <div className="p-5 border-b border-slate-100">
            <h3 className="text-sm font-semibold text-navy-800">
              Recent Configuration Changes
            </h3>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-slate-100">
                  <th className="text-left px-5 py-3 text-xs font-medium text-slate-500 uppercase">
                    ADMIN
                  </th>
                  <th className="text-left px-5 py-3 text-xs font-medium text-slate-500 uppercase">
                    CHANGE DETAILS
                  </th>
                  <th className="text-right px-5 py-3 text-xs font-medium text-slate-500 uppercase">
                    TIMESTAMP
                  </th>
                </tr>
              </thead>
              <tbody>
                {recentChanges.map((change) => (
                  <tr
                    key={change.id}
                    className="border-b border-slate-50 hover:bg-slate-50"
                  >
                    <td className="px-5 py-3 font-medium text-navy-800">
                      {change.admin}
                    </td>
                    <td className="px-5 py-3 text-slate-600">
                      {change.changeDetails}
                    </td>
                    <td className="px-5 py-3 text-slate-500 text-right">
                      {change.timestamp}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="p-4 text-center">
            <button className="text-sm text-orange-500 hover:text-orange-600 font-medium">
              VIEW ALL →
            </button>
          </div>
        </Card>

        {/* Verification Tiers Status */}
        <Card>
          <div className="p-5 border-b border-slate-100">
            <h3 className="text-sm font-semibold text-navy-800">
              Verification Tiers Status
            </h3>
            <p className="text-xs text-slate-500 mt-1">
              Global availability toggle
            </p>
          </div>
          <div className="p-5 space-y-4">
            {tiers.map((tier) => (
              <div
                key={tier.tier}
                className="flex items-center justify-between py-3 border-b border-slate-50 last:border-0"
              >
                <div>
                  <p className="text-sm font-medium text-navy-800">
                    {tierLabels[tier.tier]?.title || tier.tier}
                  </p>
                  <p className="text-xs text-slate-500">
                    {tierLabels[tier.tier]?.description || ""}
                  </p>
                </div>
                <Toggle
                  enabled={tier.enabled}
                  onChange={() => handleTierToggle(tier.tier, tier.enabled)}
                />
              </div>
            ))}
          </div>
          <div className="px-5 pb-5">
            <button className="w-full py-2.5 bg-orange-500 hover:bg-orange-600 text-white font-medium rounded-lg text-sm transition-colors">
              APPLY CONFIGURATION
            </button>
          </div>
        </Card>
      </div>
    </PortalLayout>
  );
}
