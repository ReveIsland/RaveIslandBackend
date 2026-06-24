import { Link } from "react-router-dom";
import {
  ArrowRight,
  Building2,
  CalendarDays,
  Plus,
  ShieldCheck,
  UserRound,
  Users,
} from "lucide-react";
import { useCurrentUser } from "../auth/CurrentUserContext";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "../components/ui/card";
import { Badge } from "../components/ui/badge";
import { Skeleton } from "../components/ui/skeleton";
import { StatCard } from "../components/layout/StatCard";
import { isPlatformAdmin, isTenantAdmin, isTenantMember } from "../lib/api";
import { cn } from "../lib/utils";

const quickActions = [
  {
    to: "/events",
    label: "Manage events",
    description: "View and edit your organization's events",
    icon: CalendarDays,
    visible: isTenantMember,
  },
  {
    to: "/events/new",
    label: "Create event",
    description: "Start a new event from scratch",
    icon: Plus,
    visible: isTenantMember,
  },
  {
    to: "/admin/users",
    label: "Manage users",
    description: "Invite team members and assign roles",
    icon: Users,
    visible: (roles: string[]) => isPlatformAdmin(roles) || isTenantAdmin(roles),
  },
  {
    to: "/admin/tenants",
    label: "Manage tenants",
    description: "Platform-wide tenant administration",
    icon: Building2,
    visible: isPlatformAdmin,
  },
];

export function DashboardPage() {
  const { profile, isLoading, error } = useCurrentUser();
  const roles = profile?.roles ?? [];
  const visibleActions = quickActions.filter((action) => action.visible(roles));

  return (
    <div className="space-y-8">
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard
          title="Account"
          value={isLoading ? "—" : profile?.name?.split(" ")[0] ?? "Active"}
          description="Signed in via Keycloak OIDC"
          icon={UserRound}
          accent="violet"
        />
        <StatCard
          title="Organization"
          value={isLoading ? "—" : profile?.tenantName ?? (isPlatformAdmin(roles) ? "Platform" : "—")}
          description="Your tenant context"
          icon={Building2}
          accent="primary"
        />
        <StatCard
          title="Assigned roles"
          value={isLoading ? "—" : String(roles.length)}
          description="From Keycloak realm"
          icon={ShieldCheck}
          accent="emerald"
        />
        <StatCard
          title="Events"
          value="Manage"
          description="Create and view tenant events"
          icon={CalendarDays}
          accent="amber"
        />
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Profile</CardTitle>
            <CardDescription>Your account details from the API</CardDescription>
          </CardHeader>
          <CardContent>
            {error && <p className="text-sm text-destructive">{error}</p>}
            {isLoading && !error && (
              <div className="space-y-3">
                <Skeleton className="h-4 w-2/3" />
                <Skeleton className="h-4 w-1/2" />
                <Skeleton className="h-6 w-1/3" />
              </div>
            )}
            {!isLoading && !error && profile && (
              <dl className="space-y-4 text-sm">
                <div>
                  <dt className="text-muted-foreground">Name</dt>
                  <dd className="mt-1 font-medium">{profile.name}</dd>
                </div>
                <div>
                  <dt className="text-muted-foreground">Email</dt>
                  <dd className="mt-1 font-medium">{profile.email ?? "Not provided"}</dd>
                </div>
                {profile.tenantName && (
                  <div>
                    <dt className="text-muted-foreground">Tenant</dt>
                    <dd className="mt-1 font-medium">{profile.tenantName}</dd>
                  </div>
                )}
                <div>
                  <dt className="text-muted-foreground">Roles</dt>
                  <dd className="mt-2 flex flex-wrap gap-2">
                    {profile.roles.length > 0 ? (
                      profile.roles.map((role) => (
                        <Badge key={role} variant="secondary">
                          {role}
                        </Badge>
                      ))
                    ) : (
                      <Badge variant="outline">None</Badge>
                    )}
                  </dd>
                </div>
              </dl>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Quick actions</CardTitle>
            <CardDescription>Common tasks based on your role</CardDescription>
          </CardHeader>
          <CardContent className="flex flex-col gap-2">
            {visibleActions.length === 0 ? (
              <p className="text-sm text-muted-foreground">
                No actions available for your current role.
              </p>
            ) : (
              visibleActions.map(({ to, label, description, icon: Icon }) => (
                <Link
                  key={to}
                  to={to}
                  className={cn(
                    "group flex items-center gap-4 rounded-lg border border-border bg-card p-4 transition-all hover:border-primary/30 hover:bg-muted/50",
                  )}
                >
                  <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary/10 transition-colors group-hover:bg-primary/15">
                    <Icon className="h-5 w-5 text-primary" />
                  </div>
                  <div className="min-w-0 flex-1">
                    <p className="font-medium">{label}</p>
                    <p className="text-xs text-muted-foreground">{description}</p>
                  </div>
                  <ArrowRight className="h-4 w-4 shrink-0 text-muted-foreground opacity-0 transition-opacity group-hover:opacity-100" />
                </Link>
              ))
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
