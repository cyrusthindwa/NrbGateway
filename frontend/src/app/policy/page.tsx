"use client";

import { useEffect, useState } from "react";
import PortalLayout from "@/components/layout/PortalLayout";
import { Card, PageHeader, Button } from "@/components/ui/common";
import { useAuth } from "@/contexts/AuthContext";
import { useRouter } from "next/navigation";
import { apiService } from "@/services/api";
import { Save, Shield } from "lucide-react";
import type { CachePolicy } from "@/types";

export default function PolicyPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();
  const [policy, setPolicy] = useState<CachePolicy>({
    biographicRecordFreshness: 30,
    biographicRecordFreshnessUnit: "DAYS",
    verificationEventFreshness: 24,
    verificationEventFreshnessUnit: "HOURS",
    auditLogRetentionDays: 90,
  });
  const [isSaving, setIsSaving] = useState(false);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      router.push("/login");
      return;
    }
    if (isAuthenticated) {
      loadPolicy();
    }
  }, [isAuthenticated, authLoading]);

  async function loadPolicy() {
    try {
      const p = await apiService.getCachePolicy();
      setPolicy(p);
    } catch {
      // Use defaults from state
    }
  }

  async function handleSave() {
    setIsSaving(true);
    setSaved(false);
    try {
      await apiService.updateCachePolicy(policy);
      setSaved(true);
      setTimeout(() => setSaved(false), 3000);
    } finally {
      setIsSaving(false);
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
        title="Cache & Retention Policy"
        description="Configure data freshness requirements and system retention limits for compliance."
      />

      <Card className="p-6 max-w-3xl">
        <h3 className="text-base font-semibold text-navy-800 mb-6">
          Policy Configuration
        </h3>

        {/* Data Freshness Rules */}
        <div className="mb-8">
          <h4 className="text-sm font-semibold text-slate-700 uppercase tracking-wide mb-4">
            DATA FRESHNESS RULES
          </h4>

          <div className="space-y-6">
            <div>
              <label className="block text-sm font-medium text-navy-800 mb-1">
                Biographic Record Freshness
              </label>
              <p className="text-xs text-slate-500 mb-2">
                Maximum allowed age for cached biographic data before re-validation.
              </p>
              <div className="flex items-center gap-3">
                <input
                  type="number"
                  value={policy.biographicRecordFreshness}
                  onChange={(e) =>
                    setPolicy((p) => ({
                      ...p,
                      biographicRecordFreshness: Number(e.target.value),
                    }))
                  }
                  className="w-24 border border-slate-300 rounded-lg px-3 py-2 text-sm"
                />
                <select
                  value={policy.biographicRecordFreshnessUnit}
                  onChange={(e) =>
                    setPolicy((p) => ({
                      ...p,
                      biographicRecordFreshnessUnit: e.target.value as CachePolicy["biographicRecordFreshnessUnit"],
                    }))
                  }
                  className="border border-slate-300 rounded-lg px-3 py-2 text-sm bg-white"
                >
                  <option value="HOURS">Hours</option>
                  <option value="DAYS">Days</option>
                  <option value="MONTHS">Months</option>
                </select>
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-navy-800 mb-1">
                Verification Event Freshness
              </label>
              <p className="text-xs text-slate-500 mb-2">
                Time-to-live for a verified status token.
              </p>
              <div className="flex items-center gap-3">
                <input
                  type="number"
                  value={policy.verificationEventFreshness}
                  onChange={(e) =>
                    setPolicy((p) => ({
                      ...p,
                      verificationEventFreshness: Number(e.target.value),
                    }))
                  }
                  className="w-24 border border-slate-300 rounded-lg px-3 py-2 text-sm"
                />
                <select
                  value={policy.verificationEventFreshnessUnit}
                  onChange={(e) =>
                    setPolicy((p) => ({
                      ...p,
                      verificationEventFreshnessUnit: e.target.value as CachePolicy["verificationEventFreshnessUnit"],
                    }))
                  }
                  className="border border-slate-300 rounded-lg px-3 py-2 text-sm bg-white"
                >
                  <option value="HOURS">Hours</option>
                  <option value="DAYS">Days</option>
                  <option value="MONTHS">Months</option>
                </select>
              </div>
            </div>
          </div>
        </div>

        {/* Retention Limits */}
        <div className="mb-8">
          <h4 className="text-sm font-semibold text-slate-700 uppercase tracking-wide mb-4">
            RETENTION LIMITS
          </h4>

          <div>
            <label className="block text-sm font-medium text-navy-800 mb-1">
              Audit Log Retention Period
            </label>
            <p className="text-xs text-slate-500 mb-2">
              Duration system transaction logs are stored before automatic purging.
            </p>
            <div className="flex items-center gap-3">
              <input
                type="number"
                value={policy.auditLogRetentionDays}
                onChange={(e) =>
                  setPolicy((p) => ({
                    ...p,
                    auditLogRetentionDays: Number(e.target.value),
                  }))
                }
                className="w-24 border border-slate-300 rounded-lg px-3 py-2 text-sm"
              />
              <span className="text-sm text-slate-600">Days</span>
            </div>
          </div>
        </div>

        {/* Actions */}
        <div className="flex items-center gap-3 pt-4 border-t border-slate-200">
          <Button variant="secondary">Cancel</Button>
          <Button variant="primary" onClick={handleSave} disabled={isSaving}>
            <Shield size={16} />
            {isSaving ? "Saving..." : saved ? "Saved!" : "Save Policy"}
          </Button>
        </div>
        {saved && (
          <p className="text-green-600 text-sm mt-3">Policy updated successfully.</p>
        )}
      </Card>
    </PortalLayout>
  );
}
