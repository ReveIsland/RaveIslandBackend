import { Link } from "react-router-dom";
import { Building2, CalendarDays, ShieldCheck, UserRound } from "lucide-react";
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
import { isPlatformAdmin, isTenantAdmin, isTenantMember } from "../lib/api";
import { cn } from "../lib/utils";

function StatCard({
  title,
  value,
  description,
  icon: Icon,
}: {
  title: string;
  value: string;
  description: string;
  icon: React.ComponentType<{ className?: string }>;
}) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium">{title}</CardTitle>
        <Icon className="h-4 w-4 text-muted-foreground" />
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold">{value}</div>
        <p className="text-xs text-muted-foreground">{description}</p>
      </CardContent>
    </Card>
  );
}

export function DashboardPage() {
  const { profile, isLoading, error } = useCurrentUser();
  const roles = profile?.roles ?? [];

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-2xl font-bold tracking-tight">Welcome back</h2>
        <p className="text-muted-foreground">
          Your multi-tenant event provider dashboard.
        </p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard
          title="Account"
          value={isLoading ? "—" : profile?.name?.split(" ")[0] ?? "Active"}
          description="Signed in via Keycloak OIDC"
          icon={UserRound}
        />
        <StatCard
          title="Organization"
          value={isLoading ? "—" : profile?.tenantName ?? (isPlatformAdmin(roles) ? "Platform" : "—")}
          description="Your tenant context"
          icon={Building2}
        />
        <StatCard
          title="Assigned roles"
          value={isLoading ? "—" : String(roles.length)}
          description="From Keycloak realm"
          icon={ShieldCheck}
        />
        <StatCard
          title="Events"
          value="Manage"
          description="Create and view tenant events"
          icon={CalendarDays}
        />
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Profile</CardTitle>
            <CardDescription>Loaded from `/api/me`</CardDescription>
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
          <CardContent className="flex flex-col gap-3">
            {isTenantMember(roles) && (
              <>
                <Link
                  to="/events"
                  className={cn(
                    "inline-flex h-10 items-center justify-start rounded-lg border border-border bg-card px-4 text-sm font-medium hover:bg-muted",
                  )}
                >
                  Manage events
                </Link>
                <Link
                  to="/events/new"
                  className={cn(
                    "inline-flex h-10 items-center justify-start rounded-lg border border-border bg-card px-4 text-sm font-medium hover:bg-muted",
                  )}
                >
                  Create event
                </Link>
              </>
            )}
            {(isPlatformAdmin(roles) || isTenantAdmin(roles)) && (
              <Link
                to="/admin/users"
                className={cn(
                  "inline-flex h-10 items-center justify-start rounded-lg border border-border bg-card px-4 text-sm font-medium hover:bg-muted",
                )}
              >
                Manage users
              </Link>
            )}
            {isPlatformAdmin(roles) && (
              <Link
                to="/admin/tenants"
                className={cn(
                  "inline-flex h-10 items-center justify-start rounded-lg border border-border bg-card px-4 text-sm font-medium hover:bg-muted",
                )}
              >
                Manage tenants
              </Link>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
