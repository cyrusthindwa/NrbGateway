"use client";

import { useEffect, useState, FormEvent } from "react";
import PortalLayout from "@/components/layout/PortalLayout";
import { Card, PageHeader, Button, Badge } from "@/components/ui/common";
import { useAuth } from "@/contexts/AuthContext";
import { useRouter } from "next/navigation";
import { apiService } from "@/services/api";
import { Plus, Pencil, AlertCircle } from "lucide-react";
import type { Company } from "@/types";

export default function CompaniesPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();
  const [companies, setCompanies] = useState<Company[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  const [showModal, setShowModal] = useState(false);
  const [editing, setEditing] = useState<Company | null>(null);
  const [formName, setFormName] = useState("");
  const [formCode, setFormCode] = useState("");

  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      router.push("/login");
      return;
    }
    if (isAuthenticated) {
      loadCompanies();
    }
  }, [isAuthenticated, authLoading]);

  async function loadCompanies() {
    try {
      const data = await apiService.getCompanies();
      setCompanies(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load companies.");
    } finally {
      setIsLoading(false);
    }
  }

  function openCreate() {
    setFormName("");
    setFormCode("");
    setEditing(null);
    setError("");
    setShowModal(true);
  }

  function openEdit(company: Company) {
    setFormName(company.name);
    setFormCode(company.shortCode);
    setEditing(company);
    setError("");
    setShowModal(true);
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError("");
    try {
      if (editing) {
        await apiService.updateCompany(editing.id, { name: formName, shortCode: formCode });
        setMessage("Company updated.");
      } else {
        await apiService.createCompany({ name: formName, shortCode: formCode });
        setMessage("Company created.");
      }
      setShowModal(false);
      loadCompanies();
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
        title="Companies"
        description="Group companies whose projects use the NRB gateway."
      >
        <Button variant="primary" onClick={openCreate}>
          <Plus size={16} />
          Add Company
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

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
        {companies.map((company) => (
          <Card key={company.id} className="p-5">
            <div className="flex items-start justify-between mb-3">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-lg bg-navy-800 flex items-center justify-center text-white font-bold text-sm">
                  {company.shortCode.substring(0, 2)}
                </div>
                <div>
                  <h4 className="text-sm font-semibold text-navy-800">{company.name}</h4>
                  <p className="text-xs text-slate-500">{company.shortCode}</p>
                </div>
              </div>
              <Badge variant="info">Active</Badge>
            </div>
            <div className="flex items-center justify-end">
              <Button variant="secondary" className="text-xs" onClick={() => openEdit(company)}>
                <Pencil size={14} />
                Edit
              </Button>
            </div>
          </Card>
        ))}
      </div>

      {companies.length === 0 && !isLoading && (
        <Card className="p-12 text-center">
          <p className="text-slate-500">No companies configured yet.</p>
          <Button variant="primary" className="mt-4" onClick={openCreate}>
            <Plus size={16} />
            Add Your First Company
          </Button>
        </Card>
      )}

      {showModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl p-6 w-full max-w-md shadow-xl">
            <h3 className="text-lg font-semibold text-navy-800 mb-4">
              {editing ? "Edit Company" : "Add Company"}
            </h3>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Name</label>
                <input
                  value={formName}
                  onChange={(e) => setFormName(e.target.value)}
                  placeholder="e.g., CDH Investment Bank"
                  required
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Short Code</label>
                <input
                  value={formCode}
                  onChange={(e) => setFormCode(e.target.value.toUpperCase())}
                  placeholder="e.g., CDHIB"
                  required
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm"
                />
              </div>
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
