"use client";

import { useState } from "react";
import {
  Search,
  Loader2,
  CheckCircle2,
  AlertTriangle,
  XCircle,
  RefreshCw,
  UserCheck,
  Calendar,
  CreditCard,
  MapPin,
  Phone,
  Globe,
  Heart,
  ShieldCheck,
  Database,
  Building,
} from "lucide-react";
import { apiService } from "@/services/api";
import { VerificationResult } from "@/types";

export default function VerifyPage() {
  const [nationalId, setNationalId] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<VerificationResult | null>(null);

  const handleVerify = async (e: React.FormEvent) => {
    e.preventDefault();
    const cleanId = nationalId.trim();
    if (!cleanId) {
      setError("Please enter a National ID number.");
      return;
    }

    setLoading(true);
    setError(null);
    setResult(null);

    try {
      const data = await apiService.verify(cleanId);
      setResult(data);
    } catch (err: any) {
      setError(err.message || "Failed to complete verification request.");
    } finally {
      setLoading(false);
    }
  };

  const handleReset = () => {
    setNationalId("");
    setResult(null);
    setError(null);
  };

  const renderStatusHeader = (cardStatus?: string | null, found?: boolean, servedFrom?: string) => {
    const status = cardStatus || (found ? "VALID RECORD" : "NOT FOUND");
    const isVal = status.toUpperCase().includes("VALID");
    const isNf = status.toUpperCase().includes("NOT FOUND");
    const isExp = status.toUpperCase().includes("EXPIRED");
    const isDeceased = status.toUpperCase().includes("DECEASED");

    if (isVal && found) {
      return (
        <div className="bg-emerald-50 border border-emerald-200 rounded-2xl p-5 shadow-sm flex flex-col sm:flex-row items-center justify-between gap-4">
          <div className="flex items-center space-x-4">
            <div className="w-12 h-12 rounded-xl bg-emerald-100 flex items-center justify-center text-emerald-600 shrink-0">
              <CheckCircle2 className="w-7 h-7" />
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <span className="inline-block px-3 py-0.5 rounded-full text-xs font-black tracking-wider uppercase bg-emerald-600 text-white shadow-sm">
                  VALID RECORD
                </span>
                {servedFrom && (
                  <span className="inline-flex items-center space-x-1 px-2.5 py-0.5 rounded-full text-xs font-bold bg-emerald-100 text-emerald-800">
                    <Database className="w-3 h-3" />
                    <span>{servedFrom.toUpperCase()}</span>
                  </span>
                )}
              </div>
              <p className="text-xs text-emerald-800 font-medium mt-1">
                National ID matches an active record in the National Registration Bureau registry.
              </p>
            </div>
          </div>
        </div>
      );
    } else if (isNf || !found) {
      return (
        <div className="bg-red-50 border border-red-200 rounded-2xl p-5 shadow-sm flex flex-col sm:flex-row items-center justify-between gap-4">
          <div className="flex items-center space-x-4">
            <div className="w-12 h-12 rounded-xl bg-red-100 flex items-center justify-center text-red-600 shrink-0">
              <XCircle className="w-7 h-7" />
            </div>
            <div>
              <span className="inline-block px-3 py-0.5 rounded-full text-xs font-black tracking-wider uppercase bg-red-600 text-white shadow-sm">
                NOT FOUND / INVALID
              </span>
              <p className="text-xs text-red-800 font-medium mt-1">
                No matching biographic record found in the NRB registry.
              </p>
            </div>
          </div>
        </div>
      );
    } else {
      return (
        <div className="bg-amber-50 border border-amber-200 rounded-2xl p-5 shadow-sm flex flex-col sm:flex-row items-center justify-between gap-4">
          <div className="flex items-center space-x-4">
            <div className="w-12 h-12 rounded-xl bg-amber-100 flex items-center justify-center text-amber-600 shrink-0">
              <AlertTriangle className="w-7 h-7" />
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <span className="inline-block px-3 py-0.5 rounded-full text-xs font-black tracking-wider uppercase bg-amber-600 text-white shadow-sm">
                  {status}
                </span>
                {servedFrom && (
                  <span className="inline-flex items-center space-x-1 px-2.5 py-0.5 rounded-full text-xs font-bold bg-amber-100 text-amber-800">
                    <Database className="w-3 h-3" />
                    <span>{servedFrom.toUpperCase()}</span>
                  </span>
                )}
              </div>
              <p className="text-xs text-amber-800 font-medium mt-1">
                {isDeceased
                  ? "Person is flagged as deceased in the national register."
                  : isExp
                  ? "National ID card has expired. Renewal verification required."
                  : "Flagged status returned by the NRB registry. Manual review required."}
              </p>
            </div>
          </div>
        </div>
      );
    }
  };

  return (
    <div className="max-w-4xl mx-auto space-y-8">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-[#292D6B]">Manual Identity Verification</h1>
        <p className="text-xs text-slate-500 mt-1">
          Query the NRB registry to retrieve full biographic & demographic details for human-in-the-loop KYC validation.
        </p>
      </div>

      {/* Input Form Screen (when no result yet) */}
      {!result && (
        <div className="bg-white rounded-2xl p-6 sm:p-8 border border-slate-200 shadow-sm space-y-6">
          <form onSubmit={handleVerify} className="space-y-6">
            <div>
              <label
                htmlFor="nationalId"
                className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-2"
              >
                National ID Number (PIN)
              </label>
              <div className="relative">
                <input
                  id="nationalId"
                  type="text"
                  required
                  value={nationalId}
                  onChange={(e) => setNationalId(e.target.value)}
                  placeholder="e.g. AB123456"
                  className="block w-full px-4 py-3.5 border border-slate-300 rounded-xl text-lg font-mono font-bold tracking-wider text-slate-800 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-[#F48220] focus:border-transparent transition-all uppercase"
                />
              </div>
              <p className="text-xs text-slate-400 mt-2">
                Enter the client's official National Registration Bureau ID number to retrieve complete registry data.
              </p>
            </div>

            {error && (
              <div className="bg-red-50 border-l-4 border-red-500 p-4 rounded text-sm text-red-700 flex items-start space-x-3">
                <XCircle className="w-5 h-5 text-red-500 shrink-0 mt-0.5" />
                <span>{error}</span>
              </div>
            )}

            <div>
              <button
                type="submit"
                disabled={loading}
                className="w-full flex justify-center items-center space-x-2 py-3.5 px-6 border border-transparent rounded-xl shadow-md text-base font-bold text-white bg-[#F48220] hover:bg-[#db6e10] focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-[#F48220] disabled:opacity-50 transition-colors cursor-pointer"
              >
                {loading ? (
                  <>
                    <Loader2 className="w-5 h-5 animate-spin" />
                    <span>Querying NRB Registry...</span>
                  </>
                ) : (
                  <>
                    <Search className="w-5 h-5" />
                    <span>Verify Identity</span>
                  </>
                )}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Verification Result Display Screen */}
      {result && (
        <div className="space-y-6">
          {/* Status Header Banner */}
          {renderStatusHeader(result.cardStatus, result.found, result.servedFrom)}

          {result.found ? (
            <div className="space-y-6">
              {/* Card 1: Primary Biographic Information */}
              <div className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
                <div className="bg-[#292D6B] px-6 py-3.5 text-white flex items-center justify-between">
                  <div className="flex items-center space-x-2">
                    <UserCheck className="w-5 h-5 text-[#F48220]" />
                    <span className="font-bold text-sm tracking-wide">
                      Primary Biographic Details
                    </span>
                  </div>
                  <span className="text-xs font-mono bg-white/10 px-3 py-1 rounded-full text-slate-200 font-bold">
                    PIN: {result.idNumber}
                  </span>
                </div>

                <div className="p-6 grid grid-cols-1 md:grid-cols-3 gap-6">
                  {/* Full Name */}
                  <div className="space-y-1 md:col-span-2 pb-4 md:pb-0 border-b md:border-b-0 md:border-r border-slate-100 pr-4">
                    <span className="text-xs font-bold text-slate-400 uppercase tracking-wider block">
                      Full Legal Name
                    </span>
                    <p className="text-lg font-extrabold text-[#292D6B]">
                      {[result.firstName, result.otherNames, result.surname]
                        .filter(Boolean)
                        .join(" ")}
                    </p>
                    <div className="text-xs text-slate-500 pt-1 flex gap-4">
                      <span>First: <strong className="text-slate-700">{result.firstName || "—"}</strong></span>
                      <span>Middle: <strong className="text-slate-700">{result.otherNames || "—"}</strong></span>
                      <span>Surname: <strong className="text-slate-700">{result.surname || "—"}</strong></span>
                    </div>
                  </div>

                  {/* Gender */}
                  <div className="space-y-1 pb-4 md:pb-0 border-b md:border-b-0 border-slate-100">
                    <span className="text-xs font-bold text-slate-400 uppercase tracking-wider block">
                      Gender
                    </span>
                    <p className="text-base font-bold text-slate-800">
                      {result.gender || "—"}
                    </p>
                  </div>

                  {/* Date of Birth */}
                  <div className="space-y-1 pr-4 md:border-r border-slate-100">
                    <span className="text-xs font-bold text-slate-400 uppercase tracking-wider block">
                      Date of Birth
                    </span>
                    <p className="text-base font-bold text-slate-800 flex items-center space-x-2">
                      <Calendar className="w-4 h-4 text-[#F48220]" />
                      <span>{result.dateOfBirth}</span>
                    </p>
                  </div>

                  {/* Nationality */}
                  <div className="space-y-1 pr-4 md:border-r border-slate-100">
                    <span className="text-xs font-bold text-slate-400 uppercase tracking-wider block">
                      Nationality
                    </span>
                    <p className="text-base font-bold text-slate-800 flex items-center space-x-2">
                      <Globe className="w-4 h-4 text-[#292D6B]" />
                      <span>{result.nationality || "MALAWIAN"}</span>
                    </p>
                  </div>

                  {/* Civil / Marital Status */}
                  <div className="space-y-1">
                    <span className="text-xs font-bold text-slate-400 uppercase tracking-wider block">
                      Marital / Civil Status
                    </span>
                    <p className="text-base font-bold text-slate-800 flex items-center space-x-2">
                      <Heart className="w-4 h-4 text-pink-500" />
                      <span>{result.civilStatus || "—"}</span>
                    </p>
                  </div>
                </div>
              </div>

              {/* Card 2: Contact & Location Information */}
              <div className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
                <div className="bg-[#292D6B] px-6 py-3.5 text-white flex items-center justify-between">
                  <div className="flex items-center space-x-2">
                    <MapPin className="w-5 h-5 text-[#F48220]" />
                    <span className="font-bold text-sm tracking-wide">
                      Location & Contact Details
                    </span>
                  </div>
                </div>

                <div className="p-6 grid grid-cols-1 md:grid-cols-3 gap-6">
                  {/* District of Birth */}
                  <div className="space-y-1 pr-4 md:border-r border-slate-100">
                    <span className="text-xs font-bold text-slate-400 uppercase tracking-wider block">
                      District of Birth
                    </span>
                    <p className="text-base font-bold text-slate-800 flex items-center space-x-2">
                      <Building className="w-4 h-4 text-slate-400" />
                      <span>{result.birthDistrict || "—"}</span>
                    </p>
                  </div>

                  {/* Residential Address */}
                  <div className="space-y-1 md:col-span-2">
                    <span className="text-xs font-bold text-slate-400 uppercase tracking-wider block">
                      Physical / Residential Address
                    </span>
                    <p className="text-base font-semibold text-slate-800 flex items-center space-x-2">
                      <MapPin className="w-4 h-4 text-[#F48220] shrink-0" />
                      <span>{result.residenceAddress || "—"}</span>
                    </p>
                  </div>

                  {/* Registered Phone */}
                  <div className="space-y-1 md:col-span-3 pt-4 border-t border-slate-100">
                    <span className="text-xs font-bold text-slate-400 uppercase tracking-wider block">
                      NRB Registered Telephone
                    </span>
                    <p className="text-base font-mono font-bold text-slate-800 flex items-center space-x-2">
                      <Phone className="w-4 h-4 text-emerald-600" />
                      <span>{result.nrbRegisteredPhone || "—"}</span>
                    </p>
                  </div>
                </div>
              </div>

              {/* Card 3: Card Validity & Registry Metadata */}
              <div className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
                <div className="bg-[#292D6B] px-6 py-3.5 text-white flex items-center justify-between">
                  <div className="flex items-center space-x-2">
                    <CreditCard className="w-5 h-5 text-[#F48220]" />
                    <span className="font-bold text-sm tracking-wide">
                      Card Validity & Registry Status
                    </span>
                  </div>
                </div>

                <div className="p-6 grid grid-cols-1 md:grid-cols-4 gap-6">
                  {/* Card Status */}
                  <div className="space-y-1 pr-4 md:border-r border-slate-100">
                    <span className="text-xs font-bold text-slate-400 uppercase tracking-wider block">
                      Card Status
                    </span>
                    <p className="text-base font-bold text-slate-800">
                      {result.cardStatus || "VALID"}
                    </p>
                  </div>

                  {/* Middleware / Biometric Status */}
                  <div className="space-y-1 pr-4 md:border-r border-slate-100">
                    <span className="text-xs font-bold text-slate-400 uppercase tracking-wider block">
                      Biometric Status
                    </span>
                    <p className="text-base font-bold text-slate-800 flex items-center space-x-1.5">
                      <ShieldCheck className="w-4 h-4 text-emerald-600" />
                      <span>{result.middlewareStatus || "CLEAR"}</span>
                    </p>
                  </div>

                  {/* Issue Date */}
                  <div className="space-y-1 pr-4 md:border-r border-slate-100">
                    <span className="text-xs font-bold text-slate-400 uppercase tracking-wider block">
                      Issue Date
                    </span>
                    <p className="text-sm font-semibold text-slate-700">
                      {result.issueDate || "—"}
                    </p>
                  </div>

                  {/* Expiry Date */}
                  <div className="space-y-1">
                    <span className="text-xs font-bold text-slate-400 uppercase tracking-wider block">
                      Expiry Date
                    </span>
                    <p className="text-sm font-semibold text-slate-700">
                      {result.expiryDate || "—"}
                    </p>
                  </div>
                </div>
              </div>
            </div>
          ) : (
            <div className="bg-white p-8 rounded-2xl border border-slate-200 text-center text-slate-500 space-y-2">
              <XCircle className="w-10 h-10 text-red-400 mx-auto" />
              <p className="text-base font-bold text-slate-700">
                No Record Found
              </p>
              <p className="text-xs text-slate-500">
                No matching biographic record exists in the NRB registry for National ID PIN{" "}
                <span className="font-mono font-bold text-slate-800">{result.idNumber}</span>.
              </p>
            </div>
          )}

          {/* Action Row */}
          <div className="flex justify-center pt-4">
            <button
              onClick={handleReset}
              className="inline-flex items-center space-x-2 bg-[#F48220] hover:bg-[#db6e10] text-white px-8 py-3.5 rounded-xl font-bold shadow-md transition-colors cursor-pointer"
            >
              <RefreshCw className="w-5 h-5" />
              <span>Verify Another Client</span>
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
