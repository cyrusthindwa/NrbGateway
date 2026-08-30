"use client";

import { useEffect, useState, FormEvent } from "react";
import PortalLayout from "@/components/layout/PortalLayout";
import { Card, PageHeader, Button, Badge, StatusDot } from "@/components/ui/common";
import { useAuth } from "@/contexts/AuthContext";
import { useRouter } from "next/navigation";
import { apiService } from "@/services/api";
import { Plus, KeyRound, AlertCircle } from "lucide-react";
import type { ManualPortalUser, Company } from "@/types";
import { formatDateTime } from "@/lib/format";

export default function ManualPortalUsersPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();
  const [users, setUsers] = useState<ManualPortalUser[]>([]);
  const [companies, setCompanies] = useState<Company[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  const [showModal, setShowModal] = useState(false);
  const [formEmail, setFormEmail] = useState("");
  const [formCompanyId, setFormCompanyId] = useState("");
  const [formPassword, setFormPassword] = useState("");

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
      const [userList, companyList] = await Promise.all([
        apiService.getManualPortalUsers(),
        apiService.getCompanies(),
      ]);
      setUsers(userList);
      setCompanies(companyList);
      if (companyList.length > 0 && !formCompanyId) {
        setFormCompanyId(companyList[0].id);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load manual portal users.");
    } finally {
      setIsLoading(false);
    }
  }

  function openCreate() {
    setFormEmail("");
    setFormPassword("");
    if (companies.length > 0 && !formCompanyId) {
      setFormCompanyId(companies[0].id);
    }
    setError("");
    setShowModal(true);
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError("");
    try {
      await apiService.createManualPortalUser({
        email: formEmail,
        companyId: formCompanyId,
        password: formPassword,
      });
      setMessage("Manual portal user created.");
      setShowModal(false);
      loadData();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Operation failed.");
    }
  }

  async function handleToggleStatus(user: ManualPortalUser) {
    const next = user.status === "ACTIVE" ? "DISABLED" : "ACTIVE";
    const verb = next === "DISABLED" ? "disable" : "enable";
    if (!confirm(`Are you sure you want to ${verb} ${user.email}?`)) return;
    setError("");
    try {
      await apiService.updateManualPortalUserStatus(user.id, next);
      setMessage(`Manual portal user ${verb}d.`);
      loadData();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Operation failed.");
    }
  }

  async function handleResetPassword(user: ManualPortalUser) {
    if (!confirm(`Send a password reset link to ${user.email}?`)) return;
    setError("");
    try {
      const res = await apiService.resetManualPortalUserPassword(user.id);
      setMessage(res.message || "Password reset link sent.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Operation failed.");
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
        title="Manual Portal Users"
        description="Manage staff accounts that verify identities through the Manual Verification Portal."
      >
        <Button variant="primary" onClick={openCreate}>
          <Plus size={16} />
          Add User
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
                <th className="text-left px-5 py-3 text-xs font-medium text-slate-500 uppercase">Email</th>
                <th className="text-left px-5 py-3 text-xs font-medium text-slate-500 uppercase">Company</th>
                <th className="text-left px-5 py-3 text-xs font-medium text-slate-500 uppercase">Status</th>
                <th className="text-left px-5 py-3 text-xs font-medium text-slate-500 uppercase">Last Login</th>
                <th className="text-right px-5 py-3 text-xs font-medium text-slate-500 uppercase">Actions</th>
              </tr>
            </thead>
            <tbody>
              {users.map((user) => (
                <tr key={user.id} className="border-b border-slate-100 hover:bg-slate-50">
                  <td className="px-5 py-4 font-medium text-navy-800">{user.email}</td>
                  <td className="px-5 py-4 text-slate-600">{user.companyName}</td>
                  <td className="px-5 py-4">
                    <Badge variant={user.status === "ACTIVE" ? "success" : "danger"}>
                      <StatusDot status={user.status} />
                      {user.status}
                    </Badge>
                  </td>
                  <td className="px-5 py-4 text-slate-500">
                    {user.lastLoginAt ? formatDateTime(user.lastLoginAt) : "Never"}
                  </td>
                  <td className="px-5 py-4 text-right whitespace-nowrap">
                    <Button variant="ghost" className="text-xs" onClick={() => handleResetPassword(user)}>
                      <KeyRound size={14} />
                      Reset Password
                    </Button>
                    <Button
                      variant="ghost"
                      className={user.status === "ACTIVE" ? "text-xs text-red-500" : "text-xs text-green-600"}
                      onClick={() => handleToggleStatus(user)}
                    >
                      {user.status === "ACTIVE" ? "Disable" : "Enable"}
                    </Button>
                  </td>
                </tr>
              ))}
              {users.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-5 py-10 text-center text-slate-500">
                    No manual portal users yet.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </Card>

      {showModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl p-6 w-full max-w-md shadow-xl">
            <h3 className="text-lg font-semibold text-navy-800 mb-4">Add Manual Portal User</h3>
            <form onSubmit={handleSubmit} className="space-y-4">
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
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Company</label>
                <select
                  value={formCompanyId}
                  onChange={(e) => setFormCompanyId(e.target.value)}
                  required
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm bg-white"
                >
                  {companies.map((company) => (
                    <option key={company.id} value={company.id}>
                      {company.name}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Temporary Password</label>
                <input
                  type="password"
                  value={formPassword}
                  onChange={(e) => setFormPassword(e.target.value)}
                  required
                  minLength={8}
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm"
                />
              </div>
              <div className="flex items-center gap-3 mt-6">
                <Button variant="secondary" type="button" onClick={() => setShowModal(false)}>
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
