import { useLookupValues } from "../hooks/useLookupValues";
import { cn } from "../lib/utils";

type LookupMultiSelectProps = {
  typeCode: string;
  selectedIds: string[];
  onChange: (ids: string[]) => void;
  token?: string;
  label?: string;
  className?: string;
};

export function LookupMultiSelect({
  typeCode,
  selectedIds,
  onChange,
  token,
  label,
  className,
}: LookupMultiSelectProps) {
  const { values, isLoading } = useLookupValues(typeCode, token);

  function toggle(id: string) {
    if (selectedIds.includes(id)) {
      onChange(selectedIds.filter((x) => x !== id));
    } else {
      onChange([...selectedIds, id]);
    }
  }

  return (
    <div className={cn("space-y-2", className)}>
      {label && <label className="text-sm font-medium">{label}</label>}
      {isLoading ? (
        <p className="text-sm text-muted-foreground">Loading...</p>
      ) : (
        <div className="grid gap-2 sm:grid-cols-2">
          {values.map((item) => {
            const selected = selectedIds.includes(item.id);
            return (
              <label
                key={item.id}
                className={cn(
                  "flex cursor-pointer items-center gap-2.5 rounded-xl px-3 py-2.5 text-sm transition-all",
                  selected
                    ? "glass ring-1 ring-primary/30"
                    : "glass-subtle hover:border-white/20",
                )}
              >
                <input
                  type="checkbox"
                  className="h-4 w-4 rounded border-white/20 accent-primary"
                  checked={selected}
                  onChange={() => toggle(item.id)}
                />
                {item.name}
              </label>
            );
          })}
        </div>
      )}
    </div>
  );
}
