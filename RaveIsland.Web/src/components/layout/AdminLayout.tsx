import { NavLink, Outlet, useLocation } from "react-router-dom";
import type { ComponentType } from "react";
import { CurrentUserProvider, useCurrentUser } from "../../auth/CurrentUserContext";
import {
  BarChart3,
  Building2,
  CalendarDays,
  Database,
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
  "/events": "Events",
  "/events/new": "Create event",
  "/admin": "Analytics",
  "/admin/tenants": "Tenants",
  "/admin/lookups": "Reference Data",
  "/admin/users": "User management",
};

function resolvePageTitle(pathname: string): string {
  if (pathname.endsWith("/edit") && pathname.startsWith("/events/")) {
    return "Edit event";
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
    <div className="flex min-h-screen bg-background">
      <aside className="hidden w-64 shrink-0 border-r border-sidebar-border bg-sidebar md:flex md:flex-col">
        <div className="flex h-16 items-center gap-2 border-b border-sidebar-border px-6">
          <Waves className="h-5 w-5 text-primary" />
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
                    "flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors",
                    isActive
                      ? "bg-sidebar-accent text-sidebar-accent-foreground"
                      : "text-sidebar-foreground hover:bg-sidebar-accent/70",
                  )
                }
              >
                <Icon className="h-4 w-4" />
                {item.label}
              </NavLink>
            );
          })}
        </nav>

        <div className="border-t border-sidebar-border p-4">
          <div className="rounded-lg border border-sidebar-border bg-background/40 p-3">
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
        <header className="sticky top-0 z-10 flex h-16 items-center justify-between gap-4 border-b border-border bg-background/90 px-4 backdrop-blur md:px-8">
          <div>
            <p className="text-xs uppercase tracking-wide text-muted-foreground">Overview</p>
            <h1 className="text-lg font-semibold">{pageTitle}</h1>
          </div>

          <div className="flex items-center gap-2">
            <ThemeToggle />
            <UserMenu />
          </div>
        </header>

        <div className="border-b border-border px-4 py-3 md:hidden">
          <div className="flex gap-2 overflow-x-auto">
            {visibleNavItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) =>
                  cn(
                    "whitespace-nowrap rounded-md px-3 py-1.5 text-sm",
                    isActive ? "bg-accent text-accent-foreground" : "text-muted-foreground",
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
