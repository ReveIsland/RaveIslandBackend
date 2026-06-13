import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "react-oidc-context";
import { ChevronDown, LogOut, Settings, UserRound } from "lucide-react";
import { useCurrentUser } from "../../auth/CurrentUserContext";
import { Avatar } from "../ui/avatar";
import { Button } from "../ui/button";
import { Separator } from "../ui/separator";
import { cn } from "../../lib/utils";

export function UserMenu() {
  const auth = useAuth();
  const { profile, isLoading } = useCurrentUser();
  const [open, setOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  const displayName = profile?.name ?? (isLoading ? "Loading..." : "User");
  const email = profile?.email ?? undefined;

  useEffect(() => {
    if (!open) {
      return;
    }

    function handlePointerDown(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setOpen(false);
      }
    }

    function handleEscape(event: KeyboardEvent) {
      if (event.key === "Escape") {
        setOpen(false);
      }
    }

    document.addEventListener("mousedown", handlePointerDown);
    document.addEventListener("keydown", handleEscape);
    return () => {
      document.removeEventListener("mousedown", handlePointerDown);
      document.removeEventListener("keydown", handleEscape);
    };
  }, [open]);

  return (
    <div ref={menuRef} className="relative">
      <Button
        variant="outline"
        size="sm"
        className="gap-2 pl-1.5"
        aria-haspopup="menu"
        aria-expanded={open}
        onClick={() => setOpen((current) => !current)}
      >
        <Avatar name={displayName} className="h-7 w-7 text-xs" />
        <span className="hidden max-w-28 truncate sm:inline">{displayName}</span>
        <ChevronDown className={cn("h-4 w-4 text-muted-foreground transition-transform", open && "rotate-180")} />
      </Button>

      {open && (
        <div
          role="menu"
          className="absolute right-0 z-20 mt-2 w-56 overflow-hidden rounded-lg border border-border bg-popover text-popover-foreground shadow-lg"
        >
          <div className="px-3 py-3">
            <div className="flex items-center gap-3">
              <Avatar name={displayName} />
              <div className="min-w-0">
                <p className="truncate text-sm font-medium">{displayName}</p>
                <p className="truncate text-xs text-muted-foreground">{email ?? "Signed in"}</p>
              </div>
            </div>
          </div>

          <Separator />

          <div className="p-1">
            <Link
              to="/profile"
              role="menuitem"
              className="flex w-full items-center gap-2 rounded-md px-3 py-2 text-sm hover:bg-accent hover:text-accent-foreground"
              onClick={() => setOpen(false)}
            >
              <Settings className="h-4 w-4" />
              Profile settings
            </Link>
            <Link
              to="/dashboard"
              role="menuitem"
              className="flex w-full items-center gap-2 rounded-md px-3 py-2 text-sm hover:bg-accent hover:text-accent-foreground"
              onClick={() => setOpen(false)}
            >
              <UserRound className="h-4 w-4" />
              Dashboard
            </Link>
          </div>

          <Separator />

          <div className="p-1">
            <button
              type="button"
              role="menuitem"
              className="flex w-full items-center gap-2 rounded-md px-3 py-2 text-sm text-destructive hover:bg-destructive/10"
              onClick={() => {
                setOpen(false);
                void auth.signoutRedirect();
              }}
            >
              <LogOut className="h-4 w-4" />
              Log out
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
