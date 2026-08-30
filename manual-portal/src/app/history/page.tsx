"use client";

import { useEffect, useState } from "react";
import { format } from "date-fns";
import { Search, Calendar, ChevronLeft, ChevronRight, CheckCircle2, AlertTriangle, XCircle, RefreshCw, Filter } from "lucide-react";
import { apiService } from "@/services/api";
import { LogItem, PaginatedLogHistory } from "@/types";

export default function HistoryPage() {
  const [history, setHistory] = useState<PaginatedLogHistory | null>(null);
  const [page, setPage] = useState(1);
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchHistory = async (targetPage = 1) => {
    setLoading(true);
    setError(null);
    try {
      const data = await apiService.getHistory(
        targetPage,
        10,
        dateFrom || undefined,
        dateTo || undefined
      );
      setHistory(data);
      setPage(targetPage);
    } catch (err: any) {
      setError(err.message || "Failed to load history.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchHistory(1);
  }, []);

  const handleFilterSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    fetchHistory(1);
  };

  const handleClearFilters = () => {
    setDateFrom("");
    setDateTo("");
    fetchHistory(1);
  };

  const renderStatusBadge = (status: string) => {
    const isVal = status.toUpperCase().includes("VALID");
    const isNf = status.toUpperCase().includes("NOT FOUND");
    const isExp = status.toUpperCase().includes("EXPIRED");

    if (isVal) {
      return (
        <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-bold bg-green-100 text-green-800">
          <CheckCircle2 className="w-3.5 h-3.5 mr-1 text-green-600" />
          {status}
        </span>
      );
    } else if (isExp || isNf) {
      return (
        <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-bold bg-red-100 text-red-800">
          <XCircle className="w-3.5 h-3.5 mr-1 text-red-600" />
          {status}
        </span>
      );
    } else {
      return (
        <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-bold bg-amber-100 text-amber-800">
          <AlertTriangle className="w-3.5 h-3.5 mr-1 text-amber-600" />
          {status}
        </span>
      );
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-[#292D6B]">Verification History</h1>
        <p className="text-xs text-slate-500 mt-1">
          Complete log of past identity verification queries performed under your user account.
        </p>
      </div>

      {/* Filter Bar */}
      <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm">
        <form onSubmit={handleFilterSubmit} className="flex flex-wrap items-end gap-4">
          <div>
            <label className="block text-xs font-bold text-slate-600 uppercase tracking-wider mb-1">
              Date From
            </label>
            <input
              type="date"
              value={dateFrom}
              onChange={(e) => setDateFrom(e.target.value)}
              className="px-3 py-2 border border-slate-300 rounded-lg text-xs text-slate-800 focus:ring-2 focus:ring-[#F48220] focus:border-transparent outline-none"
            />
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-600 uppercase tracking-wider mb-1">
              Date To
            </label>
            <input
              type="date"
              value={dateTo}
              onChange={(e) => setDateTo(e.target.value)}
              className="px-3 py-2 border border-slate-300 rounded-lg text-xs text-slate-800 focus:ring-2 focus:ring-[#F48220] focus:border-transparent outline-none"
            />
          </div>

          <div className="flex space-x-2">
            <button
              type="submit"
              className="px-4 py-2 bg-[#292D6B] hover:bg-[#1e2254] text-white text-xs font-bold rounded-lg shadow-sm transition-colors cursor-pointer flex items-center space-x-1"
            >
              <Filter className="w-3.5 h-3.5" />
              <span>Apply Filter</span>
            </button>
            {(dateFrom || dateTo) && (
              <button
                type="button"
                onClick={handleClearFilters}
                className="px-3 py-2 bg-slate-100 hover:bg-slate-200 text-slate-600 text-xs font-semibold rounded-lg transition-colors cursor-pointer"
              >
                Clear
              </button>
            )}
          </div>
        </form>
      </div>

      {/* Table Section */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        {loading ? (
          <div className="p-12 text-center text-slate-400 text-sm">
            Loading history records...
          </div>
        ) : error ? (
          <div className="p-12 text-center text-red-500 text-sm">{error}</div>
        ) : !history || history.items.length === 0 ? (
          <div className="p-12 text-center text-slate-400">
            <Search className="w-10 h-10 mx-auto text-slate-300 mb-3" />
            <p className="text-sm font-medium">No verification records found matching your query.</p>
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-slate-100">
                <thead className="bg-slate-50">
                  <tr>
                    <th className="px-6 py-3.5 text-left text-xs font-bold text-slate-500 uppercase tracking-wider">
                      Date & Time (UTC)
                    </th>
                    <th className="px-6 py-3.5 text-left text-xs font-bold text-slate-500 uppercase tracking-wider">
                      National ID (Masked)
                    </th>
                    <th className="px-6 py-3.5 text-left text-xs font-bold text-slate-500 uppercase tracking-wider">
                      Result Status
                    </th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-slate-100">
                  {history.items.map((item: LogItem) => (
                    <tr key={item.id} className="hover:bg-slate-50/80 transition-colors">
                      <td className="px-6 py-4 whitespace-nowrap text-xs text-slate-600 font-medium">
                        {format(new Date(item.requestedAt), "dd MMM yyyy, HH:mm:ss")}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm font-mono font-bold text-slate-800">
                        {item.nationalIdMasked}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        {renderStatusBadge(item.resultStatus)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Pagination Controls */}
            {history.totalPages > 1 && (
              <div className="px-6 py-4 border-t border-slate-100 flex items-center justify-between bg-slate-50">
                <div className="text-xs text-slate-500">
                  Showing page <span className="font-bold">{history.page}</span> of{" "}
                  <span className="font-bold">{history.totalPages}</span> ({history.totalCount} total lookups)
                </div>
                <div className="flex space-x-2">
                  <button
                    disabled={page <= 1}
                    onClick={() => fetchHistory(page - 1)}
                    className="p-1.5 rounded border border-slate-300 text-slate-600 hover:bg-white disabled:opacity-40 transition-colors cursor-pointer"
                  >
                    <ChevronLeft className="w-4 h-4" />
                  </button>
                  <button
                    disabled={page >= history.totalPages}
                    onClick={() => fetchHistory(page + 1)}
                    className="p-1.5 rounded border border-slate-300 text-slate-600 hover:bg-white disabled:opacity-40 transition-colors cursor-pointer"
                  >
                    <ChevronRight className="w-4 h-4" />
                  </button>
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}
