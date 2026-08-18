"use client";

import { useEffect, useState } from "react";
import PortalLayout from "@/components/layout/PortalLayout";
import { Card, PageHeader, Toggle, Button, Badge } from "@/components/ui/common";
import { useAuth } from "@/contexts/AuthContext";
import { useRouter } from "next/navigation";
import { apiService } from "@/services/api";
import { Shield, Search, Fingerprint, FileCheck, Copy, AlertTriangle } from "lucide-react";
import type { TierSetting, EnvironmentSetting } from "@/types";

const tierConfig = [
  {
    key: "BASIC",
    title: "Basic Verification",
    description: "Standard identity matching against national registry baseline data.",
    icon: Shield,
    tier: "Tier 1",
  },
  {
    key: "TEXT_LOOKUP",
    title: "Text-Based Lookup",
    description: "Fuzzy matching and parsing for complex or misspelled query parameters.",
    icon: Search,
    tier: "Tier 2",
  },
  {
    key: "INTERMEDIATE",
    title: "Intermediate Middleware",
    description: "Cross-referencing secondary databases for extended profile validation.",
    icon: Fingerprint,
    tier: "Tier 3",
  },
  {
    key: "ADVANCED",
    title: "Advanced Middleware",
    description: "Biometric flag checks and deep historical audit trailing integration.",
    icon: FileCheck,
    tier: "Tier 4",
  },
];

export default function VerificationTiersPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();
  const [tiers, setTiers] = useState<TierSetting[]>([]);
  const [environment, setEnvironment] = useState<EnvironmentSetting | null>(null);
  const [envMode, setEnvMode] = useState<"TEST" | "PRODUCTION">("PRODUCTION");
  const [isLoading, setIsLoading] = useState(true);
  const [copied, setCopied] = useState<string | null>(null);

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
      const [t, e] = await Promise.all([
        apiService.getTierSettings(),
        apiService.getEnvironmentSetting(),
      ]);
      setTiers(t);
      setEnvironment(e);
      setEnvMode(e.environment);
    } catch (err) {
      console.error("Failed to load tier settings:", err);
    } finally {
      setIsLoading(false);
    }
  }

  async function handleTierToggle(tierKey: string, currentlyEnabled: boolean) {
    const newEnabled = !currentlyEnabled;

    setTiers((prev) =>
      prev.map((t) =>
        t.tier === tierKey ? { ...t, enabled: newEnabled } : t
      )
    );

    try {
      await apiService.updateTierSetting(tierKey, newEnabled);
    } catch (err) {
      console.error("Failed to update tier setting:", err);
      setTiers((prev) =>
        prev.map((t) =>
          t.tier === tierKey ? { ...t, enabled: currentlyEnabled } : t
        )
      );
    }
  }

  async function handleEnvChange(mode: "TEST" | "PRODUCTION") {
    setEnvMode(mode);
    try {
      await apiService.updateEnvironmentSetting({ environment: mode });
    } catch {
      // Silently handle
    }
  }

  function copyToClipboard(text: string, label: string) {
    navigator.clipboard.writeText(text);
    setCopied(label);
    setTimeout(() => setCopied(null), 2000);
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

  const getTierFromList = (key: string): TierSetting => {
    const found = tiers.find((t) => t.tier === key);
    return found || { tier: key as TierSetting["tier"], enabled: false, costPerRequest: 0, updatedAt: "", updatedBy: "" };
  };

  return (
    <PortalLayout>
      <PageHeader
        title="Verification Tiers"
        description="Manage and configure the active verification endpoints for the gateway. Changes made here apply across all configured API keys unless overridden at the project level."
      />

      {/* Tier Cards */}
      <div className="grid grid-cols-2 gap-5 mb-8">
        {tierConfig.map((config) => {
          const tier = getTierFromList(config.key);
          const Icon = config.icon;

          return (
            <Card key={config.key} className="p-5">
              <div className="flex items-start justify-between">
                <div className="flex items-start gap-3">
                  <div
                    className={`w-10 h-10 rounded-lg flex items-center justify-center ${
                      tier.enabled
                        ? "bg-green-100 text-green-600"
                        : "bg-blue-100 text-blue-600"
                    }`}
                  >
                    <Icon size={20} />
                  </div>
                  <div>
                    <h4 className="text-sm font-semibold text-navy-800">
                      {config.title}
                    </h4>
                    <p className="text-xs text-slate-500 mt-0.5">
                      {config.description}
                    </p>
                    <div className="mt-2">
                      <Badge variant={tier.enabled ? "success" : "info"}>
                        {tier.enabled ? "🟢 ACTIVE" : "🔵 INACTIVE"} | {config.tier}
                      </Badge>
                    </div>
                  </div>
                </div>
                <Toggle
                  enabled={tier.enabled}
                  onChange={() => handleTierToggle(tier.tier, tier.enabled)}
                />
              </div>
            </Card>
          );
        })}
      </div>

      {/* NRB Environment Configuration */}
      <Card className="p-6">
        <h3 className="text-base font-semibold text-navy-800 mb-4">
          NRB Environment Configuration
        </h3>

        {/* Environment Toggle */}
        <div className="flex items-center gap-4 mb-6">
          <button
            onClick={() => handleEnvChange("TEST")}
            className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
              envMode === "TEST"
                ? "bg-slate-200 text-slate-700"
                : "bg-slate-100 text-slate-500 hover:bg-slate-200"
            }`}
          >
            TEST
          </button>
          <button
            onClick={() => handleEnvChange("PRODUCTION")}
            className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
              envMode === "PRODUCTION"
                ? "bg-orange-500 text-white"
                : "bg-slate-100 text-slate-500 hover:bg-slate-200"
            }`}
          >
            🔘 PROD
          </button>
        </div>

        {envMode === "PRODUCTION" && (
          <div className="flex items-start gap-3 p-4 bg-orange-50 border border-orange-200 rounded-lg mb-6">
            <AlertTriangle size={18} className="text-orange-500 flex-shrink-0 mt-0.5" />
            <div>
              <p className="text-sm font-medium text-orange-800">
                ⚠ Production Environment Active
              </p>
              <p className="text-xs text-orange-700 mt-1">
                You are viewing configuration endpoints for the live production
                environment. Proceed with caution as connections are routed to
                the central registry.
              </p>
            </div>
          </div>
        )}

        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">
              BASE API URL
            </label>
            <div className="flex items-center gap-2">
              <input
                type="text"
                readOnly
                value={
                  environment?.intermediateEndpointUrl ||
                  "https://api.prod.nrb.gov/v2/gateway"
                }
                className="flex-1 border border-slate-300 rounded-lg px-3 py-2 text-sm bg-slate-50 text-slate-600"
              />
              <button
                onClick={() =>
                  copyToClipboard(
                    environment?.intermediateEndpointUrl ||
                      "https://api.prod.nrb.gov/v2/gateway",
                    "url"
                  )
                }
                className="p-2 hover:bg-slate-100 rounded-lg text-slate-500"
              >
                <Copy size={16} />
                {copied === "url" && (
                  <span className="text-xs text-green-600 ml-1">Copied!</span>
                )}
              </button>
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">
              SOCKET CONNECTION
            </label>
            <div className="flex items-center gap-2">
              <input
                type="text"
                readOnly
                value="wss://stream.prod.nrb.gov/events"
                className="flex-1 border border-slate-300 rounded-lg px-3 py-2 text-sm bg-slate-50 text-slate-600"
              />
              <button
                onClick={() =>
                  copyToClipboard(
                    "wss://stream.prod.nrb.gov/events",
                    "socket"
                  )
                }
                className="p-2 hover:bg-slate-100 rounded-lg text-slate-500"
              >
                <Copy size={16} />
                {copied === "socket" && (
                  <span className="text-xs text-green-600 ml-1">Copied!</span>
                )}
              </button>
            </div>
          </div>
        </div>
      </Card>
    </PortalLayout>
  );
}
