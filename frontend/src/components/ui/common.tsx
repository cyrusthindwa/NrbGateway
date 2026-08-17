import type { ReactNode } from "react";

interface StatCardProps {
  label: string;
  value: string | number;
  subValue?: string;
  icon?: ReactNode;
  color?: "default" | "green" | "orange" | "blue";
}

export function StatCard({ label, value, subValue, icon, color = "default" }: StatCardProps) {
  const colorClasses = {
    default: "bg-white",
    green: "bg-white border-l-4 border-green-500",
    orange: "bg-white border-l-4 border-orange-500",
    blue: "bg-white border-l-4 border-blue-500",
  };

  return (
    <div className={`${colorClasses[color]} rounded-xl p-5 shadow-sm`}>
      <div className="flex items-start justify-between">
        <div>
          <p className="text-sm text-slate-500 font-medium">{label}</p>
          <p className="text-2xl font-bold text-navy-800 mt-1">{value}</p>
          {subValue && (
            <p className="text-xs text-slate-400 mt-1">{subValue}</p>
          )}
        </div>
        {icon && (
          <div className="w-10 h-10 rounded-lg bg-slate-100 flex items-center justify-center text-slate-500">
            {icon}
          </div>
        )}
      </div>
    </div>
  );
}

export function Card({
  children,
  className = "",
}: {
  children: ReactNode;
  className?: string;
}) {
  return (
    <div className={`bg-white rounded-xl shadow-sm ${className}`}>
      {children}
    </div>
  );
}

export function PageHeader({
  title,
  description,
  children,
}: {
  title: string;
  description?: string;
  children?: ReactNode;
}) {
  return (
    <div className="flex items-start justify-between mb-6">
      <div>
        <h2 className="text-xl font-bold text-navy-800">{title}</h2>
        {description && (
          <p className="text-sm text-slate-500 mt-1">{description}</p>
        )}
      </div>
      {children && <div>{children}</div>}
    </div>
  );
}

export function Badge({
  children,
  variant = "default",
}: {
  children: ReactNode;
  variant?: "success" | "danger" | "warning" | "info" | "default";
}) {
  const variants = {
    success: "bg-green-100 text-green-700",
    danger: "bg-red-100 text-red-700",
    warning: "bg-orange-50 text-orange-700",
    info: "bg-blue-100 text-blue-700",
    default: "bg-slate-100 text-slate-600",
  };

  return (
    <span
      className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${variants[variant]}`}
    >
      {children}
    </span>
  );
}

export function Toggle({
  enabled,
  onChange,
  label,
}: {
  enabled: boolean;
  onChange: (enabled: boolean) => void;
  label?: string;
}) {
  return (
    <div className="inline-flex items-center gap-2">
      <div
        role="switch"
        aria-checked={enabled}
        tabIndex={0}
        onClick={() => onChange(!enabled)}
        onKeyDown={(e) => {
          if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            onChange(!enabled);
          }
        }}
        style={{
          width: 44,
          height: 24,
          borderRadius: 12,
          backgroundColor: enabled ? "#f58220" : "#cbd5e1",
          cursor: "pointer",
          position: "relative",
          transition: "background-color 0.2s ease",
          outline: "none",
        }}
      >
        <span
          style={{
            display: "inline-block",
            width: 16,
            height: 16,
            borderRadius: "50%",
            backgroundColor: "#ffffff",
            position: "absolute",
            top: 4,
            left: enabled ? 24 : 4,
            transition: "left 0.2s ease",
          }}
        />
      </div>
      {label && <span className="text-sm text-slate-600">{label}</span>}
    </div>
  );
}

export function Button({
  children,
  variant = "primary",
  className = "",
  ...props
}: {
  children: ReactNode;
  variant?: "primary" | "secondary" | "danger" | "ghost";
} & React.ButtonHTMLAttributes<HTMLButtonElement>) {
  const variants = {
    primary:
      "bg-orange-500 text-white hover:bg-orange-600 shadow-sm",
    secondary:
      "bg-white text-slate-700 border border-slate-300 hover:bg-slate-50",
    danger:
      "bg-red-500 text-white hover:bg-red-600 shadow-sm",
    ghost:
      "text-slate-600 hover:bg-slate-100",
  };

  return (
    <button
      className={`inline-flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed ${variants[variant]} ${className}`}
      {...props}
    >
      {children}
    </button>
  );
}

export function StatusDot({ status }: { status: "ACTIVE" | "INACTIVE" | "REVOKED" | "DISABLED" }) {
  const colors = {
    ACTIVE: "bg-green-500",
    INACTIVE: "bg-slate-400",
    REVOKED: "bg-red-500",
    DISABLED: "bg-slate-400",
  };

  return <span className={`inline-block w-2 h-2 rounded-full ${colors[status]} mr-2`} />;
}
