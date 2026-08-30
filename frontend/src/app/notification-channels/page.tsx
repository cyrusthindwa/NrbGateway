"use client";

import { useEffect, useState, FormEvent } from "react";
import PortalLayout from "@/components/layout/PortalLayout";
import { Card, PageHeader, Button, Badge } from "@/components/ui/common";
import { useAuth } from "@/contexts/AuthContext";
import { useRouter } from "next/navigation";
import { apiService } from "@/services/api";
import { Plus, AlertCircle } from "lucide-react";
import type { NotificationChannel } from "@/types";

export default function NotificationChannelsPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();
  const [channels, setChannels] = useState<NotificationChannel[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  const [showCreate, setShowCreate] = useState(false);
  const [formType, setFormType] = useState("EMAIL");
  const [formTarget, setFormTarget] = useState("");

  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      router.push("/login");
      return;
    }
    if (isAuthenticated) {
      loadChannels();
    }
  }, [isAuthenticated, authLoading]);

  async function loadChannels() {
    try {
      const data = await apiService.getNotificationChannels();
      setChannels(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load notification channels.");
    } finally {
      setIsLoading(false);
    }
  }

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setError("");
    try {
      await apiService.createNotificationChannel({ channelType: formType, target: formTarget });
      setMessage("Notification channel added.");
      setShowCreate(false);
      setFormTarget("");
      loadChannels();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to add channel.");
    }
  }

  async function handleToggle(channel: NotificationChannel) {
    setError("");
    try {
      await apiService.updateNotificationChannelStatus(channel.id, !channel.enabled);
      loadChannels();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update channel.");
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
        title="Notification Channels"
        description="Configure who gets alerted when the NRB link goes down."
      >
        <Button variant="primary" onClick={() => setShowCreate(true)}>
          <Plus size={16} />
          Add Channel
        </Button>
      </PageHeader>

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

      <Card>
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50">
              <th className="text-left px-5 py-3 text-xs font-medium text-slate-500 uppercase">Type</th>
              <th className="text-left px-5 py-3 text-xs font-medium text-slate-500 uppercase">Target</th>
              <th className="text-left px-5 py-3 text-xs font-medium text-slate-500 uppercase">Status</th>
              <th className="text-right px-5 py-3 text-xs font-medium text-slate-500 uppercase">Action</th>
            </tr>
          </thead>
          <tbody>
            {channels.map((channel) => (
              <tr key={channel.id} className="border-b border-slate-100 hover:bg-slate-50">
                <td className="px-5 py-4">
                  <Badge variant="info">{channel.channelType}</Badge>
                </td>
                <td className="px-5 py-4 text-slate-700">{channel.target}</td>
                <td className="px-5 py-4">
                  <Badge variant={channel.enabled ? "success" : "default"}>
                    {channel.enabled ? "ENABLED" : "DISABLED"}
                  </Badge>
                </td>
                <td className="px-5 py-4 text-right">
                  <Button variant="ghost" className="text-xs" onClick={() => handleToggle(channel)}>
                    {channel.enabled ? "Disable" : "Enable"}
                  </Button>
                </td>
              </tr>
            ))}
            {channels.length === 0 && (
              <tr>
                <td colSpan={4} className="text-center py-12 text-slate-500">
                  No notification channels configured yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </Card>

      {showCreate && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl p-6 w-full max-w-md shadow-xl">
            <h3 className="text-lg font-semibold text-navy-800 mb-4">Add Notification Channel</h3>
            <form onSubmit={handleCreate} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Channel Type</label>
                <select
                  value={formType}
                  onChange={(e) => setFormType(e.target.value)}
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm bg-white"
                >
                  <option value="EMAIL">Email</option>
                  <option value="SMS">SMS</option>
                  <option value="WEBHOOK">Webhook</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Target</label>
                <input
                  value={formTarget}
                  onChange={(e) => setFormTarget(e.target.value)}
                  placeholder="e.g., alerts@continental.mw or +265…"
                  required
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm"
                />
              </div>
              <div className="flex items-center gap-3 mt-6">
                <Button variant="secondary" type="button" onClick={() => setShowCreate(false)}>
                  Cancel
                </Button>
                <Button variant="primary" type="submit">
                  Create
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}
    </PortalLayout>
  );
}
