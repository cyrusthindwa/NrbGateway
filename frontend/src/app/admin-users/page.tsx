"use client";

import { useEffect, useState, FormEvent } from "react";
import PortalLayout from "@/components/layout/PortalLayout";
import { Card, PageHeader, Button, Badge, StatusDot } from "@/components/ui/common";
import { useAuth } from "@/contexts/AuthContext";
import { useRouter } from "next/navigation";
import { apiService } from "@/services/api";
import { Plus, Pencil, KeyRound, AlertCircle } from "lucide-react";
import type { AdminUser } from "@/types";
import { formatDateTime } from "@/lib/format";

export default function AdminUsersPage() {
  const { isAuthenticated, isLoading: authLoading, user: currentUser } = useAuth();
  const router = useRouter();
  const [admins, setAdmins] = useState<AdminUser[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  const [showModal, setShowModal] = useState(false);
  const [editing, setEditing] = useState<AdminUser | null>(null);
  const [formName, setFormName] = useState("");
  const [formEmail, setFormEmail] = useState("");
  const [formPassword, setFormPassword] = useState("");

  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      router.push("/login");
      return;
    }
    if (isAuthenticated) {
      loadAdmins();
    }
  }, [isAuthenticated, authLoading]);

  async function loadAdmins() {
    try {
      const result = await apiService.getAdminUsers({ page: 1, pageSize: 100 });
      setAdmins(result.data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load admin users.");
    } finally {
      setIsLoading(false);
    }
  }

  function openCreate() {
    setFormName("");
    setFormEmail("");
    setFormPassword("");
    setEditing(null);
    setError("");
    setShowModal(true);
  }

  function openEdit(admin: AdminUser) {
    setFormName(admin.name);
    setFormEmail(admin.email);
    setFormPassword("");
    setEditing(admin);
    setError("");
    setShowModal(true);
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError("");
    try {
      if (editing) {
        await apiService.updateAdminUser(editing.id, { name: formName, email: formEmail });
        setMessage("Admin updated.");
      } else {
        await apiService.createAdminUser({ name: formName, email: formEmail, password: formPassword });
        setMessage("Admin created.");
      }
      setShowModal(false);
      loadAdmins();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Operation failed.");
    }
  }

  async function handleToggleStatus(admin: AdminUser) {
    const next = admin.status === "ACTIVE" ? "DISABLED" : "ACTIVE";
    const verb = next === "DISABLED" ? "disable" : "enable";
    if (!confirm(`Are you sure you want to ${verb} ${admin.email}?`)) return;
    setError("");
    try {
      await apiService.updateAdminStatus(admin.id, next);
      setMessage(`Admin ${verb}d.`);
      loadAdmins();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Operation failed.");
    }
  }

  async function handleResetPassword(admin: AdminUser) {
    if (!confirm(`Send a password reset link to ${admin.email}?`)) return;
    setError("");
    try {
      const res = await apiService.resetAdminPassword(admin.id);
      setMessage(res.message || "Password reset link sent.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Operation failed.");
    }
  }

  const initials = (name: string) =>
    name.split(/\s+/).filter(Boolean).map((n) => n[0]).join("").slice(0, 2).toUpperCase();

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
        title="Admin Users"
        description="Manage administrator accounts for the NRB Gateway Console."
      >
        <Button variant="primary" onClick={openCreate}>
          <Plus size={16} />
          Add Admin
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
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-200 bg-slate-50">
                <th className="text-left px-5 py-3 text-xs font-medium text-slate-500 uppercase">Name</th>
                <th className="text-left px-5 py-3 text-xs font-medium text-slate-500 uppercase">Email</th>
                <th className="text-left px-5 py-3 text-xs font-medium text-slate-500 uppercase">Status</th>
                <th className="text-left px-5 py-3 text-xs font-medium text-slate-500 uppercase">Created</th>
                <th className="text-right px-5 py-3 text-xs font-medium text-slate-500 uppercase">Actions</th>
              </tr>
            </thead>
            <tbody>
              {admins.map((admin) => (
                <tr key={admin.id} className="border-b border-slate-100 hover:bg-slate-50">
                  <td className="px-5 py-4">
                    <div className="flex items-center gap-3">
                      <div className="w-8 h-8 rounded-full bg-navy-700 flex items-center justify-center text-white text-xs font-medium">
                        {initials(admin.name)}
                      </div>
                      <span className="font-medium text-navy-800">{admin.name}</span>
                    </div>
                  </td>
                  <td className="px-5 py-4 text-slate-600">{admin.email}</td>
                  <td className="px-5 py-4">
                    <Badge variant={admin.status === "ACTIVE" ? "success" : "danger"}>
                      <StatusDot status={admin.status} />
                      {admin.status}
                    </Badge>
                  </td>
                  <td className="px-5 py-4 text-slate-500">{formatDateTime(admin.createdAt)}</td>
                  <td className="px-5 py-4 text-right whitespace-nowrap">
                    <Button variant="ghost" className="text-xs" onClick={() => openEdit(admin)}>
                      <Pencil size={14} />
                      Edit
                    </Button>
                    <Button variant="ghost" className="text-xs" onClick={() => handleResetPassword(admin)}>
                      <KeyRound size={14} />
                      Reset Password
                    </Button>
                    <Button
                      variant="ghost"
                      className={admin.status === "ACTIVE" ? "text-xs text-red-500" : "text-xs text-green-600"}
                      disabled={admin.status === "ACTIVE" && admin.id === currentUser?.adminId}
                      onClick={() => handleToggleStatus(admin)}
                    >
                      {admin.status === "ACTIVE" ? "Disable" : "Enable"}
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>

      {showModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl p-6 w-full max-w-md shadow-xl">
            <h3 className="text-lg font-semibold text-navy-800 mb-4">
              {editing ? "Edit Admin" : "Add Admin"}
            </h3>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Name</label>
                <input
                  value={formName}
                  onChange={(e) => setFormName(e.target.value)}
                  required
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Email</label>
                <input
                  type="email"
                  value={formEmail}
                  onChange={(e) => setFormEmail(e.target.value)}
                  required
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm"
                />
              </div>
              {!editing && (
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1">Password</label>
                  <input
                    type="password"
                    value={formPassword}
                    onChange={(e) => setFormPassword(e.target.value)}
                    required
                    minLength={8}
                    className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm"
                  />
                </div>
              )}
              <div className="flex items-center gap-3 mt-6">
                <Button variant="secondary" type="button" onClick={() => setShowModal(false)}>
                  Cancel
                </Button>
                <Button variant="primary" type="submit">
                  {editing ? "Save" : "Create"}
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}
    </PortalLayout>
  );
}
