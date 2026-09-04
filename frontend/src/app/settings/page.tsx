"use client";

import { useEffect, useState, FormEvent } from "react";
import PortalLayout from "@/components/layout/PortalLayout";
import { Card, PageHeader, Button, Badge } from "@/components/ui/common";
import { useAuth } from "@/contexts/AuthContext";
import { useRouter } from "next/navigation";
import { apiService } from "@/services/api";
import {
  Plus,
  Globe,
  Trash2,
  AlertCircle,
  CheckCircle2,
  Info,
  Shield,
  ExternalLink,
  Search,
} from "lucide-react";
import type { CorsOrigin } from "@/types";

export default function SettingsPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();

  const [origins, setOrigins] = useState<CorsOrigin[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [error, setError] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  // Create Modal State
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [formOrigin, setFormOrigin] = useState("");
  const [formDescription, setFormDescription] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Delete Confirm Modal State
  const [originToDelete, setOriginToDelete] = useState<CorsOrigin | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      router.push("/login");
      return;
    }
    if (isAuthenticated) {
      loadOrigins();
    }
  }, [isAuthenticated, authLoading]);

  async function loadOrigins() {
    setIsLoading(true);
    try {
      const data = await apiService.getCorsOrigins();
      setOrigins(data);
      setError("");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load CORS origins.");
    } finally {
      setIsLoading(false);
    }
  }

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setError("");
    setSuccessMessage("");
    setIsSubmitting(true);

    const trimmedOrigin = formOrigin.trim().replace(/\/+$/, "");

    try {
      await apiService.createCorsOrigin({
        origin: trimmedOrigin,
        description: formDescription.trim() || undefined,
      });

      setSuccessMessage(`Origin '${trimmedOrigin}' added successfully and is now active.`);
      setShowCreateModal(false);
      setFormOrigin("");
      setFormDescription("");
      await loadOrigins();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to add CORS origin.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleToggle(origin: CorsOrigin) {
    setError("");
    setSuccessMessage("");
    const newStatus = !origin.isEnabled;

    try {
      await apiService.updateCorsOriginStatus(origin.id, newStatus);
      setSuccessMessage(
        `Origin '${origin.origin}' has been ${newStatus ? "enabled" : "disabled"}.`
      );
      setOrigins((prev) =>
        prev.map((item) =>
          item.id === origin.id ? { ...item, isEnabled: newStatus } : item
        )
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update origin status.");
    }
  }

  async function handleDelete() {
    if (!originToDelete) return;
    setError("");
    setSuccessMessage("");
    setIsDeleting(true);

    try {
      await apiService.deleteCorsOrigin(originToDelete.id);
      setSuccessMessage(`Origin '${originToDelete.origin}' removed from allowed list.`);
      setOrigins((prev) => prev.filter((item) => item.id !== originToDelete.id));
      setOriginToDelete(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to delete origin.");
    } finally {
      setIsDeleting(false);
    }
  }

  const filteredOrigins = origins.filter((o) => {
    const term = searchTerm.toLowerCase();
    return (
      o.origin.toLowerCase().includes(term) ||
      (o.description && o.description.toLowerCase().includes(term))
    );
  });

  return (
    <PortalLayout>
      <PageHeader
        title="System Settings"
        description="Configure dynamic CORS origins and API security policies without requiring backend redeployments."
      >
        <Button variant="primary" onClick={() => setShowCreateModal(true)}>
          <Plus size={16} />
          Add Allowed Origin
        </Button>
      </PageHeader>

      {/* Success Notification */}
      {successMessage && (
        <div className="mb-6 flex items-center justify-between p-4 bg-green-50 border border-green-200 rounded-xl text-green-800 text-sm">
          <div className="flex items-center gap-2">
            <CheckCircle2 size={18} className="text-green-600 shrink-0" />
            <span>{successMessage}</span>
          </div>
          <button
            onClick={() => setSuccessMessage("")}
            className="text-green-600 hover:text-green-800 text-xs font-semibold"
          >
            Dismiss
          </button>
        </div>
      )}

      {/* Error Notification */}
      {error && (
        <div className="mb-6 flex items-center justify-between p-4 bg-red-50 border border-red-200 rounded-xl text-red-800 text-sm">
          <div className="flex items-center gap-2">
            <AlertCircle size={18} className="text-red-600 shrink-0" />
            <span>{error}</span>
          </div>
          <button
            onClick={() => setError("")}
            className="text-red-600 hover:text-red-800 text-xs font-semibold"
          >
            Dismiss
          </button>
        </div>
      )}

      {/* Real-time Dynamic Info Banner */}
      <div className="mb-6 bg-blue-50 border border-blue-200 rounded-xl p-4 text-sm text-blue-900 flex items-start gap-3">
        <Info size={20} className="text-blue-600 shrink-0 mt-0.5" />
        <div>
          <h4 className="font-semibold text-blue-950">Zero-Downtime Dynamic CORS Management</h4>
          <p className="mt-1 text-blue-800 leading-relaxed">
            All origins listed here are synchronized directly into the running Gateway API memory.
            Adding, toggling, or removing origins takes effect instantly across all web clients without
            requiring server restarts or new container deployments.
          </p>
        </div>
      </div>

      {/* CORS Origins Management Card */}
      <Card className="overflow-hidden border border-slate-200">
        <div className="p-5 border-b border-slate-200 bg-white flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div>
            <h3 className="font-bold text-navy-900 text-base flex items-center gap-2">
              <Shield size={18} className="text-orange-500" />
              Allowed CORS Origins ({origins.length})
            </h3>
            <p className="text-xs text-slate-500 mt-0.5">
              Authorized web domains permitted to make cross-origin requests to the NRB Gateway API
            </p>
          </div>

          <div className="relative w-full sm:w-64">
            <Search
              size={16}
              className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400"
            />
            <input
              type="text"
              placeholder="Filter origins..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full pl-9 pr-3 py-1.5 text-sm border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500 focus:border-orange-500"
            />
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-200 bg-slate-50 text-left">
                <th className="px-5 py-3.5 text-xs font-semibold text-slate-600 uppercase tracking-wider">
                  Origin Domain
                </th>
                <th className="px-5 py-3.5 text-xs font-semibold text-slate-600 uppercase tracking-wider">
                  Description / Purpose
                </th>
                <th className="px-5 py-3.5 text-xs font-semibold text-slate-600 uppercase tracking-wider">
                  Status
                </th>
                <th className="px-5 py-3.5 text-xs font-semibold text-slate-600 uppercase tracking-wider">
                  Added On
                </th>
                <th className="px-5 py-3.5 text-xs font-semibold text-slate-600 uppercase tracking-wider text-right">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {isLoading ? (
                <tr>
                  <td colSpan={5} className="px-5 py-12 text-center text-slate-500">
                    <div className="inline-block animate-spin rounded-full h-5 w-5 border-2 border-orange-500 border-t-transparent mr-2 align-middle"></div>
                    Loading CORS origins...
                  </td>
                </tr>
              ) : filteredOrigins.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-5 py-12 text-center text-slate-500">
                    {searchTerm ? "No origins match your search filter." : "No CORS origins configured yet."}
                  </td>
                </tr>
              ) : (
                filteredOrigins.map((item) => (
                  <tr key={item.id} className="hover:bg-slate-50/80 transition-colors">
                    <td className="px-5 py-4">
                      <div className="flex items-center gap-2.5">
                        <div className="w-8 h-8 rounded-lg bg-orange-50 text-orange-600 flex items-center justify-center shrink-0">
                          <Globe size={16} />
                        </div>
                        <div>
                          <span className="font-mono text-sm font-semibold text-navy-900">
                            {item.origin}
                          </span>
                        </div>
                      </div>
                    </td>
                    <td className="px-5 py-4 text-slate-600">
                      {item.description || (
                        <span className="text-slate-400 italic">No description</span>
                      )}
                    </td>
                    <td className="px-5 py-4">
                      <Badge variant={item.isEnabled ? "success" : "default"}>
                        {item.isEnabled ? "ACTIVE" : "DISABLED"}
                      </Badge>
                    </td>
                    <td className="px-5 py-4 text-slate-500 text-xs">
                      {new Date(item.createdAt).toLocaleDateString("en-GB", {
                        day: "2-digit",
                        month: "short",
                        year: "numeric",
                      })}
                    </td>
                    <td className="px-5 py-4 text-right">
                      <div className="flex items-center justify-end gap-2">
                        <Button
                          variant="ghost"
                          className={`text-xs px-2.5 py-1 h-auto ${
                            item.isEnabled
                              ? "text-slate-600 hover:text-amber-700 hover:bg-amber-50"
                              : "text-green-700 hover:text-green-800 hover:bg-green-50"
                          }`}
                          onClick={() => handleToggle(item)}
                        >
                          {item.isEnabled ? "Disable" : "Enable"}
                        </Button>
                        <button
                          type="button"
                          onClick={() => setOriginToDelete(item)}
                          className="p-1.5 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                          title="Delete origin"
                        >
                          <Trash2 size={16} />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </Card>

      {/* Add Origin Modal */}
      {showCreateModal && (
        <div className="fixed inset-0 bg-black/50 backdrop-blur-sm flex items-center justify-center z-50 p-4 animate-in fade-in">
          <div className="bg-white rounded-2xl p-6 w-full max-w-lg shadow-2xl border border-slate-200">
            <div className="flex items-center justify-between mb-4 pb-3 border-b border-slate-100">
              <div className="flex items-center gap-2">
                <div className="w-8 h-8 rounded-lg bg-orange-100 text-orange-600 flex items-center justify-center">
                  <Globe size={18} />
                </div>
                <h3 className="text-lg font-bold text-navy-900">Add Allowed CORS Origin</h3>
              </div>
              <button
                type="button"
                onClick={() => setShowCreateModal(false)}
                className="text-slate-400 hover:text-slate-600 rounded-lg p-1"
              >
                &times;
              </button>
            </div>

            <form onSubmit={handleCreate} className="space-y-4">
              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-1.5">
                  Origin URL <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  value={formOrigin}
                  onChange={(e) => setFormOrigin(e.target.value)}
                  placeholder="e.g. https://portal.continental.mw"
                  required
                  className="w-full border border-slate-300 rounded-lg px-3.5 py-2.5 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-orange-500 focus:border-orange-500"
                />
                <p className="text-xs text-slate-500 mt-1.5">
                  Must include protocol (<code>http://</code> or <code>https://</code>) and port if non-standard. Trailing slashes will be removed automatically.
                </p>
              </div>

              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-1.5">
                  Description / Service Name
                </label>
                <input
                  type="text"
                  value={formDescription}
                  onChange={(e) => setFormDescription(e.target.value)}
                  placeholder="e.g. Continental Asset Management Customer Portal"
                  className="w-full border border-slate-300 rounded-lg px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-orange-500 focus:border-orange-500"
                />
              </div>

              <div className="bg-slate-50 p-3.5 rounded-lg text-xs text-slate-600 space-y-1">
                <div className="font-semibold text-slate-700">Real-time Activation:</div>
                <p>
                  Once saved, the gateway will immediately recognize preflight (OPTIONS) and cross-origin calls from this domain without restarting the server.
                </p>
              </div>

              <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-100">
                <Button
                  variant="secondary"
                  type="button"
                  onClick={() => setShowCreateModal(false)}
                  disabled={isSubmitting}
                >
                  Cancel
                </Button>
                <Button variant="primary" type="submit" disabled={isSubmitting}>
                  {isSubmitting ? "Adding..." : "Add Origin"}
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Delete Confirmation Modal */}
      {originToDelete && (
        <div className="fixed inset-0 bg-black/50 backdrop-blur-sm flex items-center justify-center z-50 p-4 animate-in fade-in">
          <div className="bg-white rounded-2xl p-6 w-full max-w-md shadow-2xl border border-slate-200">
            <div className="flex items-center gap-3 mb-4">
              <div className="w-10 h-10 rounded-xl bg-red-100 text-red-600 flex items-center justify-center shrink-0">
                <Trash2 size={20} />
              </div>
              <div>
                <h3 className="text-lg font-bold text-navy-900">Remove Allowed Origin?</h3>
                <p className="text-xs text-slate-500">This action takes effect immediately.</p>
              </div>
            </div>

            <p className="text-sm text-slate-600 mb-4 leading-relaxed">
              Are you sure you want to delete <code className="font-semibold text-navy-900 bg-slate-100 px-1.5 py-0.5 rounded">{originToDelete.origin}</code>?
              Clients connecting from this domain will no longer be permitted to access the API.
            </p>

            <div className="flex items-center justify-end gap-3 pt-3 border-t border-slate-100">
              <Button
                variant="secondary"
                type="button"
                onClick={() => setOriginToDelete(null)}
                disabled={isDeleting}
              >
                Cancel
              </Button>
              <Button
                variant="danger"
                type="button"
                onClick={handleDelete}
                disabled={isDeleting}
              >
                {isDeleting ? "Deleting..." : "Delete Origin"}
              </Button>
            </div>
          </div>
        </div>
      )}
    </PortalLayout>
  );
}
