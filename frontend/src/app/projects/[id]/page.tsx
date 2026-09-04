"use client";

import { useEffect, useState } from "react";
import PortalLayout from "@/components/layout/PortalLayout";
import { Card, PageHeader, Button, Badge, StatusDot } from "@/components/ui/common";
import { useAuth } from "@/contexts/AuthContext";
import { useRouter, useParams } from "next/navigation";
import { apiService } from "@/services/api";
import { ArrowLeft, Key, RotateCw, Ban, Save, Copy, Check, X } from "lucide-react";
import type { Project, ProjectApiKey, DailyUsage } from "@/types";
import { formatDateTime } from "@/lib/format";
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from "recharts";

export default function ProjectDetailPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();
  const params = useParams();
  const id = params.id as string;
  const [project, setProject] = useState<Project | null>(null);
  const [apiKeys, setApiKeys] = useState<ProjectApiKey[]>([]);
  const [usage, setUsage] = useState<DailyUsage[]>([]);
  const [rateLimits, setRateLimits] = useState<Record<string, number>>({});
  const [savingRateLimit, setSavingRateLimit] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [newKeyPlaintext, setNewKeyPlaintext] = useState<string | null>(null);
  const [copiedKey, setCopiedKey] = useState(false);

  function copyTextToClipboard(text: string) {
    try {
      if (typeof navigator !== "undefined" && navigator.clipboard && window.isSecureContext) {
        navigator.clipboard.writeText(text);
      } else {
        const textarea = document.createElement("textarea");
        textarea.value = text;
        textarea.style.position = "fixed";
        textarea.style.left = "-999999px";
        textarea.style.top = "-999999px";
        document.body.appendChild(textarea);
        textarea.focus();
        textarea.select();
        document.execCommand("copy");
        textarea.remove();
      }
      setCopiedKey(true);
      setTimeout(() => setCopiedKey(false), 2000);
    } catch {
      // Ignore if clipboard denied
    }
  }

  function handleCopyAndClose() {
    try {
      if (newKeyPlaintext) {
        copyTextToClipboard(newKeyPlaintext);
      }
    } finally {
      setNewKeyPlaintext(null);
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
  }, [isAuthenticated, authLoading, id]);

  async function loadData() {
    try {
      const [projects, keys, u] = await Promise.all([
        apiService.getProjects(),
        apiService.getProjectApiKeys(id),
        apiService.getProjectUsage(id),
      ]);
      const proj = projects.find((p) => p.id === id) || null;
      setProject(proj);
      setApiKeys(keys);
      setUsage(u);
      const limits: Record<string, number> = {};
      keys.forEach((k) => {
        limits[k.id] = k.rateLimitPerMinute;
      });
      setRateLimits(limits);
    } catch (err) {
      console.error("Failed to load project data:", err);
    } finally {
      setIsLoading(false);
    }
  }

  async function handleCreateKey() {
    try {
      const key = await apiService.createApiKey(id);
      setNewKeyPlaintext(key.plaintextApiKey || null);
      loadData();
    } catch (err) {
      console.error("Failed to create API key:", err);
    }
  }

  async function handleRotateKey(keyId: string) {
    try {
      const rotated = await apiService.rotateApiKey(id, keyId);
      setNewKeyPlaintext(rotated.plaintextApiKey || null);
      loadData();
    } catch {
      // handled
    }
  }

  async function handleRevokeKey(keyId: string) {
    if (!confirm("Revoke this API key? This action cannot be undone.")) return;
    try {
      await apiService.revokeApiKey(id, keyId);
      loadData();
    } catch {
      // handled
    }
  }

  async function handleSaveRateLimit(keyId: string) {
    const value = rateLimits[keyId];
    if (value == null || value <= 0) return;
    setSavingRateLimit(keyId);
    try {
      await apiService.updateRateLimit(id, keyId, value);
      loadData();
    } catch {
      // handled
    } finally {
      setSavingRateLimit(null);
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
      {/* Header */}
      <div className="flex items-center gap-3 mb-6">
        <button
          onClick={() => router.push("/projects")}
          className="p-2 hover:bg-white rounded-lg transition-colors"
        >
          <ArrowLeft size={20} className="text-slate-500" />
        </button>
        <div>
          <div className="flex items-center gap-3">
            <h2 className="text-xl font-bold text-navy-800">
              {project?.name || "Project"}
            </h2>
            <Badge variant="success">Active</Badge>
          </div>
          <p className="text-sm text-slate-500">{project?.shortCode}</p>
        </div>
      </div>

      {/* New Key Modal */}
      {newKeyPlaintext && (
        <div className="fixed inset-0 bg-black/50 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl p-6 w-full max-w-lg shadow-2xl border border-slate-100 animate-in fade-in zoom-in-95 duration-150">
            <div className="flex items-center justify-between mb-2">
              <h3 className="text-lg font-bold text-navy-900">
                New API Key Generated
              </h3>
              <button
                type="button"
                onClick={() => setNewKeyPlaintext(null)}
                className="text-slate-400 hover:text-slate-600 p-1 rounded-lg hover:bg-slate-100 transition-colors"
                title="Close"
              >
                <X size={20} />
              </button>
            </div>
            <p className="text-sm text-slate-500 mb-4">
              Copy this key now and store it in a secure location. You will not be able to view this full key again.
            </p>
            <div className="relative bg-slate-900 rounded-xl p-4 font-mono text-sm text-emerald-400 break-all mb-5 border border-slate-800 shadow-inner flex items-center justify-between gap-3">
              <span className="select-all">{newKeyPlaintext}</span>
              <button
                type="button"
                onClick={() => copyTextToClipboard(newKeyPlaintext)}
                className="shrink-0 p-2 bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white rounded-lg transition-colors"
                title="Copy to clipboard"
              >
                {copiedKey ? <Check size={16} className="text-emerald-400" /> : <Copy size={16} />}
              </button>
            </div>
            <div className="flex items-center justify-end gap-3">
              <Button
                variant="secondary"
                onClick={() => setNewKeyPlaintext(null)}
              >
                Close
              </Button>
              <Button
                variant="primary"
                onClick={handleCopyAndClose}
              >
                {copiedKey ? <Check size={16} className="mr-1.5" /> : <Copy size={16} className="mr-1.5" />}
                Copy & Close
              </Button>
            </div>
          </div>
        </div>
      )}

      <div className="grid grid-cols-2 gap-6">
        {/* API Key Management */}
        <Card className="p-6">
          <h3 className="text-sm font-semibold text-navy-800 mb-4">
            API Key Management
          </h3>

          {apiKeys.length === 0 ? (
            <div className="text-center py-8">
              <p className="text-slate-500 text-sm mb-4">
                No API keys configured for this project.
              </p>
              <Button variant="primary" onClick={handleCreateKey}>
                <Key size={16} />
                Generate API Key
              </Button>
            </div>
          ) : (
            apiKeys.map((key) => (
              <div key={key.id} className="space-y-3">
                <div className="bg-slate-50 rounded-lg p-4">
                  <div className="flex items-center gap-2 mb-2">
                    <StatusDot status={key.status} />
                    <span className="text-xs font-medium text-slate-500 uppercase">
                      API KEY
                    </span>
                  </div>
                  <p className="font-mono text-sm text-navy-800 mb-3">
                    {key.keyPrefix}
                    {"*".repeat(28)}
                    {key.keyPrefix.length > 0 ? key.keyPrefix.slice(-4) : "****"}
                  </p>
                  <div className="flex items-center gap-4 text-xs text-slate-500 mb-3">
                    <span>CREATED: {formatDateTime(key.createdAt)}</span>
                    {key.rotatedAtRevokedAt && (
                      <span>LAST ROTATED: {formatDateTime(key.rotatedAtRevokedAt)}</span>
                    )}
                  </div>
                  {key.status === "ACTIVE" ? (
                    <div className="flex items-center gap-2">
                      <Button
                        variant="secondary"
                        onClick={() => handleRotateKey(key.id)}
                        className="text-xs"
                      >
                        <RotateCw size={14} />
                        Rotate Key
                      </Button>
                      <Button
                        variant="danger"
                        onClick={() => handleRevokeKey(key.id)}
                        className="text-xs"
                      >
                        <Ban size={14} />
                        Revoke Access
                      </Button>
                    </div>
                  ) : (
                    <Badge variant="danger">REVOKED</Badge>
                  )}
                </div>

                {/* Rate Limits */}
                <div>
                  <h4 className="text-xs font-semibold text-slate-500 uppercase mb-2">
                    Rate Limits
                  </h4>
                  <div className="flex items-center gap-3">
                    <div>
                      <label className="block text-xs text-slate-500 mb-1">
                        REQUESTS PER MINUTE (RPM)
                      </label>
                      <input
                        type="number"
                        value={rateLimits[key.id] ?? 0}
                        onChange={(e) =>
                          setRateLimits((p) => ({
                            ...p,
                            [key.id]: Number(e.target.value),
                          }))
                        }
                        className="w-24 border border-slate-300 rounded-lg px-3 py-2 text-sm"
                      />
                    </div>
                    <span className="text-sm text-slate-500 mt-5">req/min</span>
                  </div>
                  <Button
                    variant="primary"
                    onClick={() => handleSaveRateLimit(key.id)}
                    className="mt-3"
                    disabled={savingRateLimit === key.id}
                  >
                    <Save size={14} />
                    {savingRateLimit === key.id ? "Saving..." : "Save Limits"}
                  </Button>
                </div>
              </div>
            ))
          )}

          {apiKeys.length > 0 && (
            <Button
              variant="secondary"
              onClick={handleCreateKey}
              className="mt-4 w-full"
            >
              <Key size={16} />
              Generate New API Key
            </Button>
          )}
        </Card>

        {/* Usage Chart */}
        <Card className="p-6">
          <h3 className="text-sm font-semibold text-navy-800 mb-4">
            Usage (Last 7 Days)
          </h3>
          {usage.length > 0 ? (
            <ResponsiveContainer width="100%" height={280}>
              <BarChart data={usage} margin={{ top: 5, right: 5, left: -20, bottom: 5 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                <XAxis
                  dataKey="day"
                  tick={{ fontSize: 12, fill: "#64748b" }}
                  axisLine={false}
                  tickLine={false}
                />
                <YAxis
                  tick={{ fontSize: 12, fill: "#64748b" }}
                  axisLine={false}
                  tickLine={false}
                  tickFormatter={(v) => `${(v / 1000).toFixed(1)}k`}
                />
                <Tooltip />
                <Bar dataKey="requests" fill="#f97316" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          ) : (
            <div className="flex items-center justify-center h-64 text-slate-400 text-sm">
              No usage data available.
            </div>
          )}
        </Card>
      </div>
    </PortalLayout>
  );
}
