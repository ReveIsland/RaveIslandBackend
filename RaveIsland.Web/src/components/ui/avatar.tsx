import { cn } from "../../lib/utils";

function getInitials(name: string) {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) {
    return "?";
  }

  if (parts.length === 1) {
    return parts[0]!.slice(0, 2).toUpperCase();
  }

  return `${parts[0]![0] ?? ""}${parts[1]![0] ?? ""}`.toUpperCase();
}

export function Avatar({
  name,
  className,
}: {
  name: string;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-primary text-sm font-semibold text-primary-foreground",
        className,
      )}
      aria-hidden="true"
    >
      {getInitials(name)}
    </div>
  );
}

export function getDisplayName(profile: Record<string, unknown> | undefined) {
  if (!profile) {
    return "User";
  }

  const preferred = profile.preferred_username;
  if (typeof preferred === "string" && preferred.trim()) {
    return preferred;
  }

  const name = profile.name;
  if (typeof name === "string" && name.trim()) {
    return name;
  }

  return "User";
}
