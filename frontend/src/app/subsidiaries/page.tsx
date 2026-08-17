"use client";

import { useEffect, useState } from "react";
import PortalLayout from "@/components/layout/PortalLayout";
import { Card, PageHeader, Button, Badge, StatusDot } from "@/components/ui/common";
import { useAuth } from "@/contexts/AuthContext";
import { useRouter } from "next/navigation";
import { apiService } from "@/services/api";
import { Plus, Trash2, ExternalLink } from "lucide-react";
import type { Subsidiary } from "@/types";

export default function SubsidiariesPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();
  const [subsidiaries, setSubsidiaries] = useState<Subsidiary[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [showCreate, setShowCreate] = useState(false);
  const [newName, setNewName] = useState("");
  const [newCode, setNewCode] = useState("");
  const [createError, setCreateError] = useState("");

  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      router.push("/login");
      return;
    }
    if (isAuthenticated) {
      loadSubsidiaries();
    }
  }, [isAuthenticated, authLoading]);

  async function loadSubsidiaries() {
    try {
      const data = await apiService.getSubsidiaries();
      setSubsidiaries(data);
    } catch (err) {
      console.error("Failed to load subsidiaries:", err);
    } finally {
      setIsLoading(false);
    }
  }

  async function handleCreate() {
    if (!newName.trim() || !newCode.trim()) {
      setCreateError("Name and Short Code are required.");
      return;
    }
    setCreateError("");
    try {
      await apiService.createSubsidiary({ name: newName, shortCode: newCode });
      setShowCreate(false);
      setNewName("");
      setNewCode("");
      loadSubsidiaries();
    } catch (err) {
      setCreateError(err instanceof Error ? err.message : "Failed to create subsidiary.");
    }
  }

  async function handleDelete(id: string) {
    if (!confirm("Are you sure you want to delete this subsidiary?")) return;
    try {
      await apiService.deleteSubsidiary(id);
      setSubsidiaries((prev) => prev.filter((s) => s.id !== id));
    } catch {
      // Handle silently
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
        title="API Keys"
        description="Manage subsidiary organizations and their gateway API keys."
      >
        <Button variant="primary" onClick={() => setShowCreate(true)}>
          <Plus size={16} />
          Add Project
        </Button>
      </PageHeader>

      {/* Create Modal */}
      {showCreate && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl p-6 w-full max-w-md shadow-xl">
            <h3 className="text-lg font-semibold text-navy-800 mb-4">
              Add New Subsidiary
            </h3>
            {createError && (
              <p className="text-red-600 text-sm mb-3">{createError}</p>
            )}
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  Name
                </label>
                <input
                  type="text"
                  value={newName}
                  onChange={(e) => setNewName(e.target.value)}
                  placeholder="e.g., CDH Investment Bank"
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  Short Code
                </label>
                <input
                  type="text"
                  value={newCode}
                  onChange={(e) => setNewCode(e.target.value.toUpperCase())}
                  placeholder="e.g., CDH_IB"
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm"
                />
              </div>
            </div>
            <div className="flex items-center gap-3 mt-6">
              <Button
                variant="secondary"
                onClick={() => {
                  setShowCreate(false);
                  setCreateError("");
                }}
              >
                Cancel
              </Button>
              <Button variant="primary" onClick={handleCreate}>
                Create
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Subsidiaries Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
        {subsidiaries.map((sub) => (
          <Card key={sub.id} className="p-5 hover:shadow-md transition-shadow">
            <div className="flex items-start justify-between mb-3">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-lg bg-navy-800 flex items-center justify-center text-white font-bold text-sm">
                  {sub.shortCode.substring(0, 2)}
                </div>
                <div>
                  <h4 className="text-sm font-semibold text-navy-800">
                    {sub.name}
                  </h4>
                  <p className="text-xs text-slate-500">{sub.shortCode}</p>
                </div>
              </div>
              <StatusDot status="ACTIVE" />
            </div>
            <p className="text-xs text-slate-400 mb-4">
              Created {sub.createdAt}
            </p>
            <div className="flex items-center gap-2">
              <Button
                variant="secondary"
                onClick={() => router.push(`/subsidiaries/${sub.id}`)}
                className="flex-1 justify-center text-xs"
              >
                <ExternalLink size={14} />
                Manage Keys
              </Button>
              <button
                onClick={() => handleDelete(sub.id)}
                className="p-2 text-slate-400 hover:text-red-500 hover:bg-red-50 rounded-lg transition-colors"
                title="Delete"
              >
                <Trash2 size={16} />
              </button>
            </div>
          </Card>
        ))}
      </div>

      {subsidiaries.length === 0 && (
        <Card className="p-12 text-center">
          <p className="text-slate-500">No subsidiaries configured yet.</p>
          <Button
            variant="primary"
            className="mt-4"
            onClick={() => setShowCreate(true)}
          >
            <Plus size={16} />
            Add Your First Subsidiary
          </Button>
        </Card>
      )}
    </PortalLayout>
  );
}
