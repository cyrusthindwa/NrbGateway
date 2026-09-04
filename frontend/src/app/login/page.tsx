"use client";

import { useState, useEffect, FormEvent } from "react";
import Image from "next/image";
import { useAuth } from "@/contexts/AuthContext";
import { Mail, Lock, AlertCircle, ShieldCheck, ArrowLeft, Clock } from "lucide-react";

export default function LoginPage() {
  const { requestOtp, verifyOtp, resendOtp } = useAuth();
  const [step, setStep] = useState<"credentials" | "otp">("credentials");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [adminId, setAdminId] = useState<string | null>(null);
  const [code, setCode] = useState("");
  const [error, setError] = useState("");
  const [info, setInfo] = useState("");
  const [isTimedOut, setIsTimedOut] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [resendCooldown, setResendCooldown] = useState(0);

  useEffect(() => {
    if (typeof window !== "undefined") {
      const params = new URLSearchParams(window.location.search);
      if (params.get("timeout") === "1" || params.get("reason") === "inactivity") {
        setIsTimedOut(true);
      }
    }
  }, []);

  useEffect(() => {
    if (resendCooldown <= 0) return;
    const timer = setTimeout(() => setResendCooldown((n) => n - 1), 1000);
    return () => clearTimeout(timer);
  }, [resendCooldown]);

  const handleCredentialsSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError("");
    setInfo("");
    setIsLoading(true);
    try {
      const challenge = await requestOtp(email, password);
      setAdminId(challenge.adminId);
      setInfo(challenge.message || "A verification code has been sent to your email.");
      setResendCooldown(60);
      setStep("otp");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Login failed. Please try again.");
    } finally {
      setIsLoading(false);
    }
  };

  const handleOtpSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!adminId) return;
    setError("");
    setIsLoading(true);
    try {
      await verifyOtp(adminId, code);
      // On success AuthContext stores the session and redirects to /dashboard.
    } catch (err) {
      setError(err instanceof Error ? err.message : "Verification failed. Please try again.");
    } finally {
      setIsLoading(false);
    }
  };

  const handleResend = async () => {
    if (!adminId || resendCooldown > 0 || isLoading) return;
    setError("");
    setIsLoading(true);
    try {
      const challenge = await resendOtp(adminId);
      setInfo(challenge.message || "A new code has been sent to your email.");
      setCode("");
      setResendCooldown(60);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to resend the code.");
    } finally {
      setIsLoading(false);
    }
  };

  const handleBack = () => {
    setStep("credentials");
    setAdminId(null);
    setCode("");
    setError("");
    setInfo("");
    setPassword("");
    setResendCooldown(0);
  };

  return (
    <div className="min-h-screen bg-navy-900 flex flex-col items-center justify-center px-4">
      {/* Brand */}
      <div className="mb-8 text-center flex flex-col items-center">
        <div className="inline-flex items-center justify-center p-3 rounded-2xl bg-white shadow-xl mb-4 border border-white/20">
          <Image
            src="/logo.png"
            alt="Continental Holdings Limited"
            width={72}
            height={72}
            className="h-16 w-auto object-contain"
            priority
          />
        </div>
        <h1 className="text-2xl font-bold text-white">NRB Gateway Console</h1>
        <p className="text-slate-400 text-sm mt-1">
          Continental Holdings Limited — ICT Admin
        </p>
      </div>

      {/* Login Card */}
      <div className="w-full max-w-md bg-white rounded-2xl shadow-2xl p-8">
        <h2 className="text-lg font-semibold text-navy-800 mb-6">
          {step === "credentials" ? "Sign In" : "Two-factor verification"}
        </h2>

        {isTimedOut && (
          <div className="flex items-start gap-2.5 p-3.5 mb-5 bg-amber-50 border border-amber-200 rounded-xl text-amber-800 text-sm animate-in fade-in">
            <Clock size={18} className="text-amber-600 shrink-0 mt-0.5" />
            <div>
              <p className="font-semibold text-amber-900">Session Expired</p>
              <p className="text-xs text-amber-700 mt-0.5">
                You were signed out due to 10 minutes of inactivity. Please sign in to continue.
              </p>
            </div>
          </div>
        )}

        {error && (
          <div className="flex items-center gap-2 p-3 mb-4 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
            <AlertCircle size={16} />
            <span>{error}</span>
          </div>
        )}

        {info && (
          <div className="flex items-center gap-2 p-3 mb-4 bg-green-50 border border-green-200 rounded-lg text-green-700 text-sm">
            <ShieldCheck size={16} />
            <span>{info}</span>
          </div>
        )}

        {step === "credentials" ? (
          <form onSubmit={handleCredentialsSubmit} className="space-y-5">
            <div>
              <label htmlFor="email" className="block text-sm font-medium text-slate-700 mb-1.5">
                Email Address
              </label>
              <div className="relative">
                <Mail size={18} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
                <input
                  id="email"
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="you@continental.mw"
                  required
                  className="w-full pl-10 pr-4 py-2.5 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-orange-500 focus:border-orange-500 outline-none transition"
                />
              </div>
            </div>

            <div>
              <label htmlFor="password" className="block text-sm font-medium text-slate-700 mb-1.5">
                Password
              </label>
              <div className="relative">
                <Lock size={18} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
                <input
                  id="password"
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="••••••••"
                  required
                  className="w-full pl-10 pr-4 py-2.5 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-orange-500 focus:border-orange-500 outline-none transition"
                />
              </div>
            </div>

            <button
              type="submit"
              disabled={isLoading}
              className="w-full py-2.5 bg-orange-500 hover:bg-orange-600 text-white font-medium rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isLoading ? "Signing in..." : "Continue"}
            </button>
          </form>
        ) : (
          <form onSubmit={handleOtpSubmit} className="space-y-5">
            <p className="text-sm text-slate-600">
              Enter the 6-digit code sent to{" "}
              <span className="font-medium text-navy-800">{email}</span>.
            </p>

            <div>
              <label htmlFor="otp" className="block text-sm font-medium text-slate-700 mb-1.5">
                Verification code
              </label>
              <input
                id="otp"
                inputMode="numeric"
                autoComplete="one-time-code"
                pattern="[0-9]*"
                maxLength={6}
                value={code}
                onChange={(e) => setCode(e.target.value.replace(/[^0-9]/g, ""))}
                placeholder="000000"
                autoFocus
                required
                className="w-full px-4 py-2.5 border border-slate-300 rounded-lg text-center text-2xl tracking-[0.5em] font-mono focus:ring-2 focus:ring-orange-500 focus:border-orange-500 outline-none transition"
              />
            </div>

            <button
              type="submit"
              disabled={isLoading || code.length !== 6}
              className="w-full py-2.5 bg-orange-500 hover:bg-orange-600 text-white font-medium rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isLoading ? "Verifying..." : "Verify & Sign In"}
            </button>

            <div className="flex items-center justify-between text-sm">
              <button
                type="button"
                onClick={handleBack}
                className="text-slate-500 hover:text-navy-800 transition-colors inline-flex items-center gap-1"
              >
                <ArrowLeft size={14} />
                Back
              </button>
              <button
                type="button"
                onClick={handleResend}
                disabled={resendCooldown > 0 || isLoading}
                className="text-orange-500 hover:text-orange-600 font-medium disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {resendCooldown > 0 ? `Resend in ${resendCooldown}s` : "Resend code"}
              </button>
            </div>
          </form>
        )}
      </div>

      <p className="text-slate-500 text-xs mt-8">
        &copy; {new Date().getFullYear()} Continental Holdings Limited. All rights reserved.
      </p>
    </div>
  );
}
