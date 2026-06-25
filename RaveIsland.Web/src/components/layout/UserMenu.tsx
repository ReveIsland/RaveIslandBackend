import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "react-oidc-context";
import { ChevronDown, CreditCard, LogOut, Settings, UserRound } from "lucide-react";
import { useCurrentUser } from "../../auth/CurrentUserContext";
import { Avatar } from "../ui/avatar";
import { Badge } from "../ui/badge";
import { Button } from "../ui/button";
import { Separator } from "../ui/separator";
import { cn } from "../../lib/utils";
import { isTenantAdmin } from "../../lib/api";

function resolveSubscriptionLabel(
  subscription: { planName: string | null; isSubscribed: boolean } | null | undefined,
) {
  if (!subscription) {
    return null;
  }

  if (subscription.planName) {
    return subscription.planName;
  }

  if (subscription.isSubscribed) {
    return "Subscribed";
  }

  return "No plan";
}

export function UserMenu() {
  const auth = useAuth();
  const { profile, isLoading } = useCurrentUser();
  const [open, setOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  const displayName = profile?.name ?? (isLoading ? "Loading..." : "User");
  const email = profile?.email ?? undefined;
  const subscription = profile?.organizationSubscription;
  const subscriptionLabel = resolveSubscriptionLabel(subscription);
  const showBillingLink = isTenantAdmin(profile?.roles ?? []);

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
        {subscriptionLabel && (
          <Badge variant="secondary" className="hidden px-1.5 py-0 text-[10px] sm:inline-flex">
            {subscriptionLabel}
          </Badge>
        )}
        <ChevronDown className={cn("h-4 w-4 text-muted-foreground transition-transform", open && "rotate-180")} />
      </Button>

      {open && (
        <div
          role="menu"
          className="absolute right-0 z-20 mt-2 w-64 overflow-hidden rounded-lg border border-border bg-popover text-popover-foreground shadow-lg"
        >
          <div className="px-3 py-3">
            <div className="flex items-center gap-3">
              <Avatar name={displayName} />
              <div className="min-w-0">
                <p className="truncate text-sm font-medium">{displayName}</p>
                <p className="truncate text-xs text-muted-foreground">{email ?? "Signed in"}</p>
              </div>
            </div>

            {profile?.tenantName && (
              <p className="mt-2 truncate text-xs text-muted-foreground">
                {profile.tenantName}
              </p>
            )}

            {subscription && (
              <div className="mt-3 rounded-md border border-border bg-muted/40 p-2.5">
                <p className="text-[11px] font-medium uppercase tracking-wide text-muted-foreground">
                  Organization plan
                </p>
                <div className="mt-1 flex flex-wrap items-center gap-2">
                  <span className="text-sm font-medium">
                    {subscriptionLabel}
                  </span>
                  {subscription.status && (
                    <Badge variant={subscription.isSubscribed ? "success" : "outline"} className="text-[10px]">
                      {subscription.status}
                    </Badge>
                  )}
                </div>
              </div>
            )}
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
            {showBillingLink && (
              <Link
                to="/settings/billing"
                role="menuitem"
                className="flex w-full items-center gap-2 rounded-md px-3 py-2 text-sm hover:bg-accent hover:text-accent-foreground"
                onClick={() => setOpen(false)}
              >
                <CreditCard className="h-4 w-4" />
                Billing
              </Link>
            )}
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
