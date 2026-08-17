"use client";

import { useEffect, useState } from "react";
import PortalLayout from "@/components/layout/PortalLayout";
import { Card, PageHeader, Button, Badge, StatusDot } from "@/components/ui/common";
import { useAuth } from "@/contexts/AuthContext";
import { useRouter } from "next/navigation";
import { apiService } from "@/services/api";
import { Plus } from "lucide-react";
import type { AdminUser } from "@/types";

export default function AdminUsersPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();
  const [admins, setAdmins] = useState<AdminUser[]>([]);
  const [isLoading, setIsLoading] = useState(true);

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
      const result = await apiService.getAdminUsers();
      setAdmins(result.data);
    } catch (err) {
      console.error("Failed to load admin users:", err);
    } finally {
      setIsLoading(false);
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
        title="Admin Users"
        description="Manage administrator accounts for the NRB Gateway Console."
      >
        <Button variant="primary">
          <Plus size={16} />
          Add Admin
        </Button>
      </PageHeader>

      <Card>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-200 bg-slate-50">
                <th className="text-left px-5 py-3 text-xs font-medium text-slate-500 uppercase">
                  Name
                </th>
                <th className="text-left px-5 py-3 text-xs font-medium text-slate-500 uppercase">
                  Email
                </th>
                <th className="text-left px-5 py-3 text-xs font-medium text-slate-500 uppercase">
                  Status
                </th>
                <th className="text-left px-5 py-3 text-xs font-medium text-slate-500 uppercase">
                  Created
                </th>
                <th className="text-right px-5 py-3 text-xs font-medium text-slate-500 uppercase">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody>
              {admins.map((admin) => (
                <tr
                  key={admin.id}
                  className="border-b border-slate-100 hover:bg-slate-50"
                >
                  <td className="px-5 py-4">
                    <div className="flex items-center gap-3">
                      <div className="w-8 h-8 rounded-full bg-navy-700 flex items-center justify-center text-white text-xs font-medium">
                        {admin.name
                          .split(" ")
                          .map((n) => n[0])
                          .join("")}
                      </div>
                      <span className="font-medium text-navy-800">
                        {admin.name}
                      </span>
                    </div>
                  </td>
                  <td className="px-5 py-4 text-slate-600">{admin.email}</td>
                  <td className="px-5 py-4">
                    <Badge
                      variant={
                        admin.status === "ACTIVE" ? "success" : "danger"
                      }
                    >
                      <StatusDot status={admin.status} />
                      {admin.status}
                    </Badge>
                  </td>
                  <td className="px-5 py-4 text-slate-500">{admin.createdAt}</td>
                  <td className="px-5 py-4 text-right">
                    <Button variant="ghost" className="text-xs">
                      Edit
                    </Button>
                    <Button variant="ghost" className="text-xs text-red-500">
                      {admin.status === "ACTIVE" ? "Disable" : "Enable"}
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>
    </PortalLayout>
  );
}
