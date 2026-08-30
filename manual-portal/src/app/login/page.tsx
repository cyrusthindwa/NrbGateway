"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { ShieldCheck, Lock, Mail, AlertCircle, Loader2, KeyRound, ArrowLeft, RefreshCw } from "lucide-react";
import { apiService } from "@/services/api";

export default function LoginPage() {
  const router = useRouter();
  const [step, setStep] = useState<"LOGIN" | "2FA">("LOGIN");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [otpCode, setOtpCode] = useState("");
  const [userId, setUserId] = useState<string | null>(null);

  const [loading, setLoading] = useState(false);
  const [resending, setResending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [infoMessage, setInfoMessage] = useState<string | null>(null);
  const [cooldown, setCooldown] = useState(0);

  useEffect(() => {
    let timer: NodeJS.Timeout;
    if (cooldown > 0) {
      timer = setInterval(() => setCooldown((c) => c - 1), 1000);
    }
    return () => clearInterval(timer);
  }, [cooldown]);

  const handleLoginSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email || !password) {
      setError("Please enter both email and password.");
      return;
    }

    setLoading(true);
    setError(null);
    setInfoMessage(null);

    try {
      const res = await apiService.login(email, password);

      if (res.requires2Fa && res.userId) {
        setUserId(res.userId);
        setStep("2FA");
        setInfoMessage(res.message || "A 6-digit verification code has been sent to your email.");
        setCooldown(60);
      } else if (res.token) {
        // Direct login if 2FA is bypassed
        localStorage.setItem("manual_token", res.token);
        localStorage.setItem(
          "manual_user",
          JSON.stringify({
            id: res.userId,
            email: res.email,
            companyId: res.companyId,
            companyName: res.companyName,
          })
        );
        router.push("/");
      }
    } catch (err: any) {
      setError(err.message || "Invalid credentials. Please try again.");
    } finally {
      setLoading(false);
    }
  };

  const handleVerify2FaSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!userId || !otpCode) {
      setError("Please enter the 6-digit verification code.");
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const res = await apiService.verify2Fa(userId, otpCode);
      if (res.token) {
        localStorage.setItem("manual_token", res.token);
        localStorage.setItem(
          "manual_user",
          JSON.stringify({
            id: res.userId,
            email: res.email,
            companyId: res.companyId,
            companyName: res.companyName,
          })
        );
        router.push("/");
      } else {
        setError(res.message || "Verification failed.");
      }
    } catch (err: any) {
      setError(err.message || "Invalid verification code. Please try again.");
    } finally {
      setLoading(false);
    }
  };

  const handleResendCode = async () => {
    if (!userId || cooldown > 0) return;

    setResending(true);
    setError(null);
    setInfoMessage(null);

    try {
      const res = await apiService.resend2Fa(userId);
      setInfoMessage(res.message || "A new verification code has been sent.");
      setCooldown(60);
    } catch (err: any) {
      setError(err.message || "Failed to resend code. Please try again.");
    } finally {
      setResending(false);
    }
  };

  return (
    <div className="min-h-[80vh] flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8 bg-white p-8 rounded-xl shadow-lg border border-slate-200">
        {/* Brand Header */}
        <div className="text-center">
          <div className="mx-auto w-14 h-14 bg-[#292D6B] rounded-xl flex items-center justify-center text-[#F48220] shadow-md mb-4">
            <ShieldCheck className="w-8 h-8 text-[#F48220]" />
          </div>
          <h1 className="text-2xl font-extrabold text-[#292D6B] tracking-tight">
            CONTINENTAL HOLDINGS
          </h1>
          <p className="mt-1 text-sm font-semibold text-[#F48220] uppercase tracking-wider">
            NRB Verification Portal
          </p>
          <p className="mt-2 text-xs text-slate-500">
            {step === "LOGIN"
              ? "Manual Identity Verification for Subsidiary Staff"
              : "Two-Factor Authentication Verification"}
          </p>
        </div>

        {/* Info Message */}
        {infoMessage && (
          <div className="bg-blue-50 border-l-4 border-blue-500 p-4 rounded text-sm text-blue-700 flex items-start space-x-3">
            <KeyRound className="w-5 h-5 text-blue-500 shrink-0 mt-0.5" />
            <span>{infoMessage}</span>
          </div>
        )}

        {/* Error Alert */}
        {error && (
          <div className="bg-red-50 border-l-4 border-red-500 p-4 rounded text-sm text-red-700 flex items-start space-x-3">
            <AlertCircle className="w-5 h-5 text-red-500 shrink-0 mt-0.5" />
            <span>{error}</span>
          </div>
        )}

        {/* STEP 1: EMAIL & PASSWORD */}
        {step === "LOGIN" && (
          <form className="mt-6 space-y-6" onSubmit={handleLoginSubmit}>
            <div className="space-y-4">
              <div>
                <label
                  htmlFor="email"
                  className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1"
                >
                  Email Address
                </label>
                <div className="relative">
                  <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                    <Mail className="h-5 w-5 text-slate-400" />
                  </div>
                  <input
                    id="email"
                    name="email"
                    type="email"
                    autoComplete="email"
                    required
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    className="pl-10 block w-full px-3 py-2.5 border border-slate-300 rounded-lg text-sm placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-[#F48220] focus:border-transparent transition-all"
                    placeholder="e.g. agent@cdhbank.mw"
                  />
                </div>
              </div>

              <div>
                <label
                  htmlFor="password"
                  className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1"
                >
                  Password
                </label>
                <div className="relative">
                  <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                    <Lock className="h-5 w-5 text-slate-400" />
                  </div>
                  <input
                    id="password"
                    name="password"
                    type="password"
                    autoComplete="current-password"
                    required
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    className="pl-10 block w-full px-3 py-2.5 border border-slate-300 rounded-lg text-sm placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-[#F48220] focus:border-transparent transition-all"
                    placeholder="••••••••"
                  />
                </div>
              </div>
            </div>

            <div>
              <button
                type="submit"
                disabled={loading}
                className="w-full flex justify-center py-3 px-4 border border-transparent rounded-lg shadow-md text-sm font-bold text-white bg-[#F48220] hover:bg-[#db6e10] focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-[#F48220] disabled:opacity-50 transition-colors cursor-pointer"
              >
                {loading ? (
                  <span className="flex items-center space-x-2">
                    <Loader2 className="w-5 h-5 animate-spin" />
                    <span>Verifying Credentials...</span>
                  </span>
                ) : (
                  "Continue to 2FA"
                )}
              </button>
            </div>
          </form>
        )}

        {/* STEP 2: 2FA OTP CODE VERIFICATION */}
        {step === "2FA" && (
          <form className="mt-6 space-y-6" onSubmit={handleVerify2FaSubmit}>
            <div className="space-y-4">
              <div>
                <label
                  htmlFor="otpCode"
                  className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1"
                >
                  6-Digit Verification Code
                </label>
                <div className="relative">
                  <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                    <KeyRound className="h-5 w-5 text-slate-400" />
                  </div>
                  <input
                    id="otpCode"
                    name="otpCode"
                    type="text"
                    maxLength={6}
                    required
                    value={otpCode}
                    onChange={(e) => setOtpCode(e.target.value.replace(/\D/g, ""))}
                    className="pl-10 block w-full px-3 py-3 border border-slate-300 rounded-lg text-lg font-bold tracking-widest text-center placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-[#F48220] focus:border-transparent transition-all"
                    placeholder="123456"
                  />
                </div>
              </div>
            </div>

            <div className="space-y-3">
              <button
                type="submit"
                disabled={loading || otpCode.length !== 6}
                className="w-full flex justify-center py-3 px-4 border border-transparent rounded-lg shadow-md text-sm font-bold text-white bg-[#F48220] hover:bg-[#db6e10] focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-[#F48220] disabled:opacity-50 transition-colors cursor-pointer"
              >
                {loading ? (
                  <span className="flex items-center space-x-2">
                    <Loader2 className="w-5 h-5 animate-spin" />
                    <span>Verifying Code...</span>
                  </span>
                ) : (
                  "Verify & Sign In"
                )}
              </button>

              <div className="flex items-center justify-between text-xs pt-2">
                <button
                  type="button"
                  onClick={() => {
                    setStep("LOGIN");
                    setError(null);
                    setInfoMessage(null);
                  }}
                  className="text-slate-500 hover:text-slate-700 flex items-center space-x-1 cursor-pointer"
                >
                  <ArrowLeft className="w-3.5 h-3.5" />
                  <span>Back to Login</span>
                </button>

                <button
                  type="button"
                  disabled={resending || cooldown > 0}
                  onClick={handleResendCode}
                  className="text-[#292D6B] font-semibold hover:underline disabled:opacity-50 flex items-center space-x-1 cursor-pointer"
                >
                  {resending ? (
                    <Loader2 className="w-3.5 h-3.5 animate-spin" />
                  ) : (
                    <RefreshCw className="w-3.5 h-3.5" />
                  )}
                  <span>
                    {cooldown > 0 ? `Resend code (${cooldown}s)` : "Resend code"}
                  </span>
                </button>
              </div>
            </div>
          </form>
        )}

        <div className="text-center pt-4 border-t border-slate-100">
          <p className="text-xs text-slate-400">
            Accounts are provisioned by CHL ICT Administrators.
          </p>
        </div>
      </div>
    </div>
  );
}
