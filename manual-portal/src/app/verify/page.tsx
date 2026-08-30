"use client";

import { useState } from "react";
import { Search, Loader2, CheckCircle2, AlertTriangle, XCircle, ArrowLeft, RefreshCw, UserCheck, Shield, Calendar, CreditCard } from "lucide-react";
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

  const renderStatusHeader = (cardStatus?: string | null, found?: boolean) => {
    const status = cardStatus || (found ? "VALID RECORD" : "NOT FOUND");
    const isVal = status.toUpperCase().includes("VALID");
    const isNf = status.toUpperCase().includes("NOT FOUND");
    const isExp = status.toUpperCase().includes("EXPIRED");

    if (isVal && found) {
      return (
        <div className="bg-emerald-50 border border-emerald-200 rounded-xl p-5 text-center flex flex-col items-center justify-center space-y-2">
          <div className="w-12 h-12 rounded-full bg-emerald-100 flex items-center justify-center text-emerald-600">
            <CheckCircle2 className="w-8 h-8" />
          </div>
          <div>
            <span className="inline-block px-3 py-1 rounded-full text-xs font-black tracking-wider uppercase bg-emerald-600 text-white shadow-sm">
              VALID RECORD
            </span>
            <p className="text-xs text-emerald-800 font-medium mt-1">
              National ID matches an active record in the NRB registry.
            </p>
          </div>
        </div>
      );
    } else if (isNf || !found) {
      return (
        <div className="bg-red-50 border border-red-200 rounded-xl p-5 text-center flex flex-col items-center justify-center space-y-2">
          <div className="w-12 h-12 rounded-full bg-red-100 flex items-center justify-center text-red-600">
            <XCircle className="w-8 h-8" />
          </div>
          <div>
            <span className="inline-block px-3 py-1 rounded-full text-xs font-black tracking-wider uppercase bg-red-600 text-white shadow-sm">
              NOT FOUND / INVALID
            </span>
            <p className="text-xs text-red-800 font-medium mt-1">
              No matching biographic record found in the NRB registry.
            </p>
          </div>
        </div>
      );
    } else {
      return (
        <div className="bg-amber-50 border border-amber-200 rounded-xl p-5 text-center flex flex-col items-center justify-center space-y-2">
          <div className="w-12 h-12 rounded-full bg-amber-100 flex items-center justify-center text-amber-600">
            <AlertTriangle className="w-8 h-8" />
          </div>
          <div>
            <span className="inline-block px-3 py-1 rounded-full text-xs font-black tracking-wider uppercase bg-amber-600 text-white shadow-sm">
              {status}
            </span>
            <p className="text-xs text-amber-800 font-medium mt-1">
              Flagged status returned by the NRB registry. Manual review required.
            </p>
          </div>
        </div>
      );
    }
  };

  return (
    <div className="max-w-3xl mx-auto space-y-8">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-[#292D6B]">Manual Identity Verification</h1>
        <p className="text-xs text-slate-500 mt-1">
          Query the NRB registry to retrieve biographic details for human-in-the-loop KYC comparison.
        </p>
      </div>

      {/* Input Form Screen (when no result yet) */}
      {!result && (
        <div className="bg-white rounded-xl p-6 sm:p-8 border border-slate-200 shadow-sm space-y-6">
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
                  placeholder="e.g. 199012345678"
                  className="block w-full px-4 py-3.5 border border-slate-300 rounded-xl text-lg font-mono font-bold tracking-wider text-slate-800 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-[#F48220] focus:border-transparent transition-all uppercase"
                />
              </div>
              <p className="text-xs text-slate-400 mt-2">
                Enter the person's official National Registration Bureau ID number.
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
                    <span>Querying NRB Gateway...</span>
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
          {/* Status Header Badge */}
          {renderStatusHeader(result.cardStatus, result.found)}

          {/* Biographic Details Two-Column Card */}
          {result.found ? (
            <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
              <div className="bg-[#292D6B] px-6 py-4 text-white flex items-center justify-between">
                <div className="flex items-center space-x-2">
                  <UserCheck className="w-5 h-5 text-[#F48220]" />
                  <span className="font-bold text-sm tracking-wide">
                    NRB Biographic Details
                  </span>
                </div>
                <span className="text-xs font-mono bg-white/10 px-3 py-1 rounded-full text-slate-200">
                  ID: {result.idNumber}
                </span>
              </div>

              <div className="p-6 grid grid-cols-1 md:grid-cols-2 gap-6">
                {/* Full Name */}
                <div className="space-y-1 pb-4 md:pb-0 border-b md:border-b-0 md:border-r border-slate-100 pr-4">
                  <span className="text-xs font-bold text-slate-400 uppercase tracking-wider block">
                    Full Name
                  </span>
                  <p className="text-base font-extrabold text-[#292D6B]">
                    {[result.firstName, result.otherNames, result.surname]
                      .filter(Boolean)
                      .join(" ")}
                  </p>
                </div>

                {/* Date of Birth */}
                <div className="space-y-1 pb-4 md:pb-0 border-b md:border-b-0 border-slate-100">
                  <span className="text-xs font-bold text-slate-400 uppercase tracking-wider block">
                    Date of Birth
                  </span>
                  <p className="text-base font-bold text-slate-800 flex items-center space-x-2">
                    <Calendar className="w-4 h-4 text-slate-400" />
                    <span>{result.dateOfBirth}</span>
                  </p>
                </div>

                {/* Gender */}
                <div className="space-y-1 pb-4 md:pb-0 border-b md:border-b-0 md:border-r border-slate-100 pr-4">
                  <span className="text-xs font-bold text-slate-400 uppercase tracking-wider block">
                    Gender
                  </span>
                  <p className="text-base font-bold text-slate-800">
                    {result.gender || "—"}
                  </p>
                </div>

                {/* ID Status */}
                <div className="space-y-1 pb-4 md:pb-0 border-b md:border-b-0 border-slate-100">
                  <span className="text-xs font-bold text-slate-400 uppercase tracking-wider block">
                    ID Card Status
                  </span>
                  <p className="text-base font-bold text-slate-800">
                    {result.cardStatus || "VALID"}
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
          ) : (
            <div className="bg-white p-6 rounded-xl border border-slate-200 text-center text-slate-500">
              <p className="text-sm">
                No verified biographic records were returned for National ID number{" "}
                <span className="font-mono font-bold text-slate-700">{result.idNumber}</span>.
              </p>
            </div>
          )}

          {/* Action Row */}
          <div className="flex justify-center pt-2">
            <button
              onClick={handleReset}
              className="inline-flex items-center space-x-2 bg-[#F48220] hover:bg-[#db6e10] text-white px-6 py-3 rounded-xl font-bold shadow-md transition-colors cursor-pointer"
            >
              <RefreshCw className="w-5 h-5" />
              <span>Perform New Verification</span>
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
