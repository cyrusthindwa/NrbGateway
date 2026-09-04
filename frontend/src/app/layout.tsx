import type { Metadata } from "next";
import { Comfortaa } from "next/font/google";
import { AuthProvider } from "@/contexts/AuthContext";
import "./globals.css";

const comfortaa = Comfortaa({
  subsets: ["latin"],
  variable: "--font-comfortaa",
  display: "swap",
});

export const metadata: Metadata = {
  title: "NRB Gateway Console — Continental Holdings Limited",
  description: "ICT Admin Console for the NRB Verification Gateway",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" className={`h-full antialiased ${comfortaa.variable}`}>
      <body className="min-h-full font-sans">
        <AuthProvider>{children}</AuthProvider>
      </body>
    </html>
  );
}
