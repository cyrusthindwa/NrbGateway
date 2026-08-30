import type { Metadata } from "next";
import Navbar from "@/components/Navbar";
import "./globals.css";

export const metadata: Metadata = {
  title: "CHL NRB Verification Portal",
  description: "Human-in-the-loop National Registration Bureau KYC verification interface for Continental Holdings Limited group companies.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body className="min-h-screen bg-slate-50 flex flex-col text-slate-800 antialiased font-sans">
        <Navbar />
        <main className="flex-1 max-w-7xl w-full mx-auto p-4 sm:p-6 lg:p-8">
          {children}
        </main>
        <footer className="bg-white border-t border-slate-200 py-4 text-center text-xs text-slate-500">
          <p>© {new Date().getFullYear()} Continental Holdings Limited — NRB Verification Gateway Ecosystem (Ref: CICT/10032601/NRB)</p>
        </footer>
      </body>
    </html>
  );
}
