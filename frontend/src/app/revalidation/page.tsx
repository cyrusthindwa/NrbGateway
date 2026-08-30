"use client";

import { useEffect, useState } from "react";
import PortalLayout from "@/components/layout/PortalLayout";
import { Card, PageHeader, Button, Badge } from "@/components/ui/common";
import { useAuth } from "@/contexts/AuthContext";
import { useRouter } from "next/navigation";
import { apiService } from "@/services/api";
import { RefreshCw, AlertCircle } from "lucide-react";
import type { RevalidationBatch, RevalidationResult } from "@/types";
import { formatDateTime } from "@/lib/format";

export default function RevalidationPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();
  const [batches, setBatches] = useState<RevalidationBatch[]>([]);
  const [selected, setSelected] = useState<RevalidationBatch | null>(null);
  const [result, setResult] = useState<RevalidationResult | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [triggering, setTriggering] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      router.push("/login");
      return;
    }
    if (isAuthenticated) {
      loadBatches();
    }
  }, [isAuthenticated, authLoading]);

  async function loadBatches() {
    setError("");
    try {
      const data = await apiService.getRevalidationBatches();
      setBatches(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load batches.");
    } finally {
      setIsLoading(false);
    }
  }

  async function handleTrigger() {
    if (triggering) return;
    setTriggering(true);
    setError("");
    setResult(null);
    try {
      const r = await apiService.revalidateAll();
      setResult(r);
      loadBatches();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Revalidation failed.");
    } finally {
      setTriggering(false);
    }
  }

  async function handleSelect(id: string) {
    setError("");
    try {
      const batch = await apiService.getRevalidationBatch(id);
      setSelected(batch);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load batch detail.");
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
      <PageHeader title="Revalidation" description="Re-check every PIN in the local NRB mirror against NRB.">
        <Button variant="primary" onClick={handleTrigger} disabled={triggering}>
          <RefreshCw size={16} className={triggering ? "animate-spin" : ""} />
          {triggering ? "Running..." : "Run Revalidation"}
        </Button>
      </PageHeader>

      {error && (
        <div className="mb-4 flex items-center gap-2 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
          <AlertCircle size={16} />
          <span>{error}</span>
        </div>
      )}

      {result && (
        <div className="mb-6 p-4 bg-blue-50 border border-blue-200 rounded-lg">
          <p className="text-sm font-semibold text-blue-800 mb-1">Re-validation Complete</p>
          <p className="text-xs text-blue-700">
            Checked {result.totalChecked} PINs — {result.valid} valid, {result.expired} expired,{" "}
            {result.deceased} deceased, {result.seeNrb} need NRB review, {result.errors} errors.
          </p>
        </div>
      )}

      <div className="grid grid-cols-2 gap-6">
        {/* Batch history */}
        <Card>
          <div className="p-5 border-b border-slate-100">
            <h3 className="text-sm font-semibold text-navy-800">Batch History</h3>
          </div>
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-100 text-left text-xs text-slate-500 uppercase">
                <th className="px-5 py-3">Started</th>
                <th className="px-5 py-3 text-right">Checked</th>
                <th className="px-5 py-3 text-right">Status</th>
              </tr>
            </thead>
            <tbody>
              {batches.map((b) => (
                <tr key={b.id} className="border-b border-slate-50 hover:bg-slate-50 cursor-pointer" onClick={() => handleSelect(b.id)}>
                  <td className="px-5 py-3 text-slate-600 font-mono text-xs">{formatDateTime(b.startedAt)}</td>
                  <td className="px-5 py-3 text-right text-slate-600">{b.totalCount}</td>
                  <td className="px-5 py-3 text-right">
                    <Badge variant={b.completedAt ? "success" : "warning"}>
                      {b.completedAt ? "COMPLETE" : "RUNNING"}
                    </Badge>
                  </td>
                </tr>
              ))}
              {batches.length === 0 && (
                <tr><td colSpan={3} className="px-5 py-8 text-center text-slate-500">No batches yet.</td></tr>
              )}
            </tbody>
          </table>
        </Card>

        {/* Detail */}
        <Card className="p-6">
          <h3 className="text-sm font-semibold text-navy-800 mb-4">Batch Detail</h3>
          {selected ? (
            <div className="space-y-4 text-sm">
              <div className="grid grid-cols-2 gap-3">
                <div><p className="text-xs text-slate-500">Started</p><p className="text-slate-700 font-mono text-xs">{formatDateTime(selected.startedAt)}</p></div>
                <div><p className="text-xs text-slate-500">Completed</p><p className="text-slate-700 font-mono text-xs">{formatDateTime(selected.completedAt)}</p></div>
                <div><p className="text-xs text-slate-500">Trigger</p><p className="text-slate-700">{selected.triggerType}</p></div>
                <div><p className="text-xs text-slate-500">Initiated by</p><p className="text-slate-700">{selected.initiatedByName ?? "—"}</p></div>
              </div>
              <div className="grid grid-cols-3 gap-3 border-t border-slate-100 pt-4">
                <div className="text-center"><p className="text-2xl font-bold text-navy-800">{selected.totalCount}</p><p className="text-xs text-slate-500">Checked</p></div>
                <div className="text-center"><p className="text-2xl font-bold text-green-600">{selected.validCount}</p><p className="text-xs text-slate-500">Valid</p></div>
                <div className="text-center"><p className="text-2xl font-bold text-orange-500">{selected.expiredCount}</p><p className="text-xs text-slate-500">Expired</p></div>
                <div className="text-center"><p className="text-2xl font-bold text-red-600">{selected.deceasedCount}</p><p className="text-xs text-slate-500">Deceased</p></div>
                <div className="text-center"><p className="text-2xl font-bold text-blue-600">{selected.seeNrbCount}</p><p className="text-xs text-slate-500">See NRB</p></div>
                <div className="text-center"><p className="text-2xl font-bold text-slate-500">{selected.errorCount}</p><p className="text-xs text-slate-500">Errors</p></div>
              </div>
            </div>
          ) : (
            <p className="text-slate-500 text-sm">Select a batch to see its results.</p>
          )}
        </Card>
      </div>
    </PortalLayout>
  );
}
