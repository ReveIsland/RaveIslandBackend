import { NavLink, Outlet, useLocation } from "react-router-dom";
import type { ComponentType } from "react";
import { CurrentUserProvider, useCurrentUser } from "../../auth/CurrentUserContext";
import {
  BarChart3,
  Building2,
  CalendarDays,
  Database,
  CreditCard,
  LayoutDashboard,
  Users,
  Waves,
} from "lucide-react";
import { Badge } from "../ui/badge";
import { ThemeToggle } from "../theme/ThemeToggle";
import { UserMenu } from "./UserMenu";
import { cn } from "../../lib/utils";
import { isPlatformAdmin, isTenantAdmin, isTenantMember } from "../../lib/api";

const navItems: Array<{
  to: string;
  label: string;
  icon: ComponentType<{ className?: string }>;
  visible?: (roles: string[]) => boolean;
}> = [
  { to: "/dashboard", label: "Dashboard", icon: LayoutDashboard },
  {
    to: "/events",
    label: "Events",
    icon: CalendarDays,
    visible: (roles) => isTenantMember(roles),
  },
  {
    to: "/admin/lookups",
    label: "Reference Data",
    icon: Database,
    visible: (roles) => isPlatformAdmin(roles),
  },
  {
    to: "/admin/tenants",
    label: "Tenants",
    icon: Building2,
    visible: (roles) => isPlatformAdmin(roles),
  },
  {
    to: "/settings/billing",
    label: "Billing",
    icon: CreditCard,
    visible: (roles) => isTenantAdmin(roles),
  },
  {
    to: "/admin/users",
    label: "Users",
    icon: Users,
    visible: (roles) => isPlatformAdmin(roles) || isTenantAdmin(roles),
  },
  {
    to: "/admin",
    label: "Analytics",
    icon: BarChart3,
    visible: (roles) => isPlatformAdmin(roles),
  },
];

const pageTitles: Record<string, string> = {
  "/dashboard": "Dashboard",
  "/profile": "Profile settings",
  "/settings/billing": "Billing",
  "/events": "Events",
  "/events/new": "Create event",
  "/admin": "Analytics",
  "/admin/tenants": "Tenants",
  "/admin/lookups": "Reference Data",
  "/admin/users": "User management",
};

function resolvePageTitle(pathname: string): string | null {
  if (pathname.endsWith("/edit") && pathname.startsWith("/events/")) {
    return null;
  }
  if (pathname.endsWith("/analytics") && pathname.startsWith("/events/")) {
    return "Event analytics";
  }
  if (pathname.endsWith("/check-in") && pathname.startsWith("/events/")) {
    return "Check-in";
  }
  if (pathname.startsWith("/admin/lookups/")) {
    return "Lookup values";
  }

  return pageTitles[pathname] ?? "Admin Panel";
}

function AdminLayoutShell() {
  const { profile } = useCurrentUser();
  const location = useLocation();
  const roles = profile?.roles ?? [];
  const displayName = profile?.name ?? "User";
  const pageTitle = resolvePageTitle(location.pathname);

  const visibleNavItems = navItems.filter((item) => !item.visible || item.visible(roles));

  return (
    <div className="ambient-bg flex min-h-screen">
      <aside className="glass-strong hidden w-64 shrink-0 border-r border-sidebar-border md:flex md:flex-col">
        <div className="flex h-16 items-center gap-3 border-b border-sidebar-border px-6">
          <span className="gradient-primary flex h-9 w-9 items-center justify-center rounded-xl shadow-lg shadow-primary/30">
            <Waves className="h-5 w-5 text-primary-foreground" />
          </span>
          <div>
            <p className="text-sm font-semibold text-sidebar-foreground">Rave Island</p>
            <p className="text-xs text-muted-foreground">Admin Panel</p>
          </div>
        </div>

        <nav className="flex flex-1 flex-col gap-1 p-4">
          {visibleNavItems.map((item) => {
            const Icon = item.icon;
            return (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) =>
                  cn(
                    "group relative flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors",
                    isActive
                      ? "glass bg-sidebar-accent text-sidebar-accent-foreground ring-1 ring-primary/25"
                      : "text-sidebar-foreground hover:bg-sidebar-accent/60",
                  )
                }
              >
                {({ isActive }) => (
                  <>
                    {isActive && (
                      <span className="absolute left-0 top-1/2 h-6 w-1 -translate-y-1/2 rounded-r-full bg-primary" />
                    )}
                    <Icon className={cn("h-4 w-4", isActive && "text-primary")} />
                    {item.label}
                  </>
                )}
              </NavLink>
            );
          })}
        </nav>

        <div className="border-t border-sidebar-border p-4">
          <div className="glass-subtle rounded-xl p-3">
            <p className="truncate text-sm font-medium">{displayName}</p>
            {profile?.tenantName && (
              <p className="truncate text-xs text-muted-foreground">{profile.tenantName}</p>
            )}
            <div className="mt-2 flex flex-wrap gap-1">
              {roles.length > 0 ? (
                roles.map((role) => (
                  <Badge key={role} variant="secondary" className="text-[10px]">
                    {role}
                  </Badge>
                ))
              ) : (
                <Badge variant="outline" className="text-[10px]">
                  no role
                </Badge>
              )}
            </div>
          </div>
        </div>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="glass-strong sticky top-0 z-10 flex h-16 items-center justify-between gap-4 border-b border-border px-4 md:px-8">
          <div>
            {pageTitle && (
              <h1 className="text-xl font-semibold tracking-tight">{pageTitle}</h1>
            )}
          </div>

          <div className="flex items-center gap-2">
            <ThemeToggle />
            <UserMenu />
          </div>
        </header>

        <div className="glass-subtle border-b border-border px-4 py-3 md:hidden">
          <div className="flex gap-2 overflow-x-auto">
            {visibleNavItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) =>
                  cn(
                    "whitespace-nowrap rounded-md px-3 py-1.5 text-sm",
                    isActive ? "gradient-primary text-primary-foreground" : "glass-subtle text-muted-foreground",
                  )
                }
              >
                {item.label}
              </NavLink>
            ))}
          </div>
        </div>

        <main className="flex-1 p-4 md:p-8">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

export function AdminLayout() {
  return (
    <CurrentUserProvider>
      <AdminLayoutShell />
    </CurrentUserProvider>
  );
}
