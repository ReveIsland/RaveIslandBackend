import { useLookupValues } from "../hooks/useLookupValues";
import { cn } from "../lib/utils";

type LookupSelectProps = {
  typeCode: string;
  value: string;
  onChange: (value: string) => void;
  token?: string;
  label?: string;
  required?: boolean;
  placeholder?: string;
  className?: string;
  includeInactive?: boolean;
};

export function LookupSelect({
  typeCode,
  value,
  onChange,
  token,
  label,
  required,
  placeholder = "Select...",
  className,
  includeInactive = false,
}: LookupSelectProps) {
  const { values, isLoading } = useLookupValues(typeCode, token, includeInactive);

  return (
    <div className={cn("space-y-2", className)}>
      {label && (
        <label className="text-sm font-medium">
          {label}
          {required && " *"}
        </label>
      )}
      <select
        className="glass-subtle flex h-11 w-full rounded-xl px-4 py-2 text-sm transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/50"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        required={required}
        disabled={isLoading}
      >
        <option value="">{isLoading ? "Loading..." : placeholder}</option>
        {values.map((item) => (
          <option key={item.id} value={item.id}>
            {item.name}
          </option>
        ))}
      </select>
    </div>
  );
}
