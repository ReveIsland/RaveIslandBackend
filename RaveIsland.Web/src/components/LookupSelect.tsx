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
        className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
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
