"use client";

import { useEffect, useState } from "react";
import PortalLayout from "@/components/layout/PortalLayout";
import { Card, PageHeader, Button, Badge, StatusDot } from "@/components/ui/common";
import { useAuth } from "@/contexts/AuthContext";
import { useRouter } from "next/navigation";
import { apiService } from "@/services/api";
import { Plus, Trash2, ExternalLink } from "lucide-react";
import type { Company, Project } from "@/types";

export default function ProjectsPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();
  const [projects, setProjects] = useState<Project[]>([]);
  const [companies, setCompanies] = useState<Company[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [showCreate, setShowCreate] = useState(false);
  const [newCompanyId, setNewCompanyId] = useState("");
  const [newName, setNewName] = useState("");
  const [newCode, setNewCode] = useState("");
  const [createError, setCreateError] = useState("");

  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      router.push("/login");
      return;
    }
    if (isAuthenticated) {
      loadProjects();
    }
  }, [isAuthenticated, authLoading]);

  async function loadProjects() {
    try {
      const [data, comps] = await Promise.all([
        apiService.getProjects(),
        apiService.getCompanies(),
      ]);
      setProjects(data);
      setCompanies(comps);
      if (comps.length > 0 && !newCompanyId) {
        setNewCompanyId(comps[0].id);
      }
    } catch (err) {
      console.error("Failed to load projects:", err);
    } finally {
      setIsLoading(false);
    }
  }

  async function handleCreate() {
    if (!newCompanyId || !newName.trim() || !newCode.trim()) {
      setCreateError("Company, Name and Short Code are required.");
      return;
    }
    setCreateError("");
    try {
      await apiService.createProject({
        companyId: newCompanyId,
        name: newName,
        shortCode: newCode,
      });
      setShowCreate(false);
      setNewName("");
      setNewCode("");
      loadProjects();
    } catch (err) {
      setCreateError(err instanceof Error ? err.message : "Failed to create project.");
    }
  }

  async function handleDelete(id: string) {
    if (!confirm("Are you sure you want to delete this project?")) return;
    try {
      await apiService.deleteProject(id);
      setProjects((prev) => prev.filter((p) => p.id !== id));
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
        title="Projects"
        description="Manage company projects and their gateway API keys."
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
              Add New Project
            </h3>
            {createError && (
              <p className="text-red-600 text-sm mb-3">{createError}</p>
            )}
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  Company
                </label>
                <select
                  value={newCompanyId}
                  onChange={(e) => setNewCompanyId(e.target.value)}
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm bg-white"
                >
                  <option value="">Select a company…</option>
                  {companies.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  Name
                </label>
                <input
                  type="text"
                  value={newName}
                  onChange={(e) => setNewName(e.target.value)}
                  placeholder="e.g., CDH Investment Bank — Gateway"
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
                  placeholder="e.g., CDHIB"
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

      {/* Projects Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
        {projects.map((project) => (
          <Card key={project.id} className="p-5 hover:shadow-md transition-shadow">
            <div className="flex items-start justify-between mb-3">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-lg bg-navy-800 flex items-center justify-center text-white font-bold text-sm">
                  {project.shortCode.substring(0, 2)}
                </div>
                <div>
                  <h4 className="text-sm font-semibold text-navy-800">
                    {project.name}
                  </h4>
                  <p className="text-xs text-slate-500">{project.shortCode}</p>
                </div>
              </div>
              <StatusDot status="ACTIVE" />
            </div>
            <p className="text-xs text-slate-400 mb-4">
              Created {project.createdAt}
            </p>
            <div className="flex items-center gap-2">
              <Button
                variant="secondary"
                onClick={() => router.push(`/projects/${project.id}`)}
                className="flex-1 justify-center text-xs"
              >
                <ExternalLink size={14} />
                Manage Keys
              </Button>
              <button
                onClick={() => handleDelete(project.id)}
                className="p-2 text-slate-400 hover:text-red-500 hover:bg-red-50 rounded-lg transition-colors"
                title="Delete"
              >
                <Trash2 size={16} />
              </button>
            </div>
          </Card>
        ))}
      </div>

      {projects.length === 0 && (
        <Card className="p-12 text-center">
          <p className="text-slate-500">No projects configured yet.</p>
          <Button
            variant="primary"
            className="mt-4"
            onClick={() => setShowCreate(true)}
          >
            <Plus size={16} />
            Add Your First Project
          </Button>
        </Card>
      )}
    </PortalLayout>
  );
}
