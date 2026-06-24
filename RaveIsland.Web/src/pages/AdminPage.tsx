import { useEffect, useState } from "react";
import { useAuth } from "react-oidc-context";
import { Navigate } from "react-router-dom";
import { Activity, Building2, CalendarDays, Database, Eye, Users } from "lucide-react";
import { useCurrentUser } from "../auth/CurrentUserContext";
import { apiFetch, isPlatformAdmin, type AdminStats } from "../lib/api";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "../components/ui/card";
import { Skeleton } from "../components/ui/skeleton";
import { Badge } from "../components/ui/badge";
import { StatCard } from "../components/layout/StatCard";
import { PageHeader } from "../components/layout/PageHeader";

export function AdminPage() {
  const auth = useAuth();
  const { profile, isLoading: isLoadingProfile } = useCurrentUser();
  const roles = profile?.roles ?? [];
  const [stats, setStats] = useState<AdminStats | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    if (!auth.user?.access_token || !isPlatformAdmin(roles)) {
      return;
    }

    setIsLoading(true);
    apiFetch<AdminStats>("/api/admin/stats", { token: auth.user.access_token })
      .then((data) => {
        setStats(data);
        setError(null);
      })
      .catch((err: unknown) =>
        setError(err instanceof Error ? err.message : "Failed to load stats"),
      )
      .finally(() => setIsLoading(false));
  }, [auth.user?.access_token, roles]);

  if (isLoadingProfile) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-32 w-full" />
      </div>
    );
  }

  if (!isPlatformAdmin(roles)) {
    return <Navigate to="/dashboard" replace />;
  }

  return (
    <div className="space-y-8">
      <PageHeader
        title="Analytics"
        description="Platform-wide metrics across all tenants."
        actions={
          <Badge variant="secondary" className="w-fit">
            Admin access
          </Badge>
        }
      />

      {error && (
        <Card className="border-destructive/40">
          <CardContent className="pt-6 text-sm text-destructive">{error}</CardContent>
        </Card>
      )}

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {isLoading ? (
          <>
            <Skeleton className="h-32 rounded-lg" />
            <Skeleton className="h-32 rounded-lg" />
            <Skeleton className="h-32 rounded-lg" />
          </>
        ) : (
          stats && (
            <>
              <StatCard
                title="Page views"
                value={String(stats.viewCount)}
                description="Incremented on each visit via Redis"
                icon={Eye}
                accent="violet"
              />
              <StatCard
                title="Total events"
                value={String(stats.eventCount)}
                description="Events across all tenants"
                icon={CalendarDays}
                accent="primary"
              />
              <StatCard
                title="Tenants"
                value={String(stats.tenantCount)}
                description="Registered event providers"
                icon={Building2}
                accent="emerald"
              />
              <StatCard
                title="Active users"
                value={String(stats.userCount)}
                description="Registered tenant members"
                icon={Users}
                accent="amber"
              />
              <StatCard
                title="Pending invites"
                value={String(stats.pendingInvites)}
                description="Invitations awaiting registration"
                icon={Activity}
                accent="rose"
              />
              <StatCard
                title="Cache status"
                value={stats.cached ? "Active" : "Disabled"}
                description="Distributed cache backing admin stats"
                icon={Database}
                accent="primary"
              />
            </>
          )
        )}
      </div>

      <Card>
        <CardHeader>
          <CardTitle>About these metrics</CardTitle>
          <CardDescription>
            Each visit to this page calls `/api/admin/stats`, which aggregates cross-tenant data
            from PostgreSQL and Redis.
          </CardDescription>
        </CardHeader>
      </Card>
    </div>
  );
}
