import { useEffect, useState } from "react";
import { useAuth } from "react-oidc-context";
import { Navigate } from "react-router-dom";
import { Activity, Database, Eye } from "lucide-react";
import { useCurrentUser } from "../auth/CurrentUserContext";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "../components/ui/card";
import { Skeleton } from "../components/ui/skeleton";
import { Badge } from "../components/ui/badge";

type StatsResponse = {
  viewCount: number;
  itemCount: number;
  cached: boolean;
};

function MetricCard({
  title,
  value,
  hint,
  icon: Icon,
}: {
  title: string;
  value: string;
  hint: string;
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
        <p className="text-xs text-muted-foreground">{hint}</p>
      </CardContent>
    </Card>
  );
}

export function AdminPage() {
  const auth = useAuth();
  const { profile, isLoading: isLoadingProfile } = useCurrentUser();
  const roles = profile?.roles ?? [];
  const [stats, setStats] = useState<StatsResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    if (!auth.user?.access_token || !roles.includes("admin")) {
      return;
    }

    setIsLoading(true);
    fetch("/api/admin/stats", {
      headers: {
        Authorization: `Bearer ${auth.user.access_token}`,
      },
    })
      .then(async (response) => {
        if (!response.ok) {
          throw new Error(`API returned ${response.status}`);
        }
        return response.json() as Promise<StatsResponse>;
      })
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

  if (!roles.includes("admin")) {
    return <Navigate to="/dashboard" replace />;
  }

  return (
    <div className="space-y-8">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="text-2xl font-bold tracking-tight">Analytics</h2>
          <p className="text-muted-foreground">
            Admin-only metrics from Redis cache and PostgreSQL.
          </p>
        </div>
        <Badge variant="secondary" className="w-fit">
          Admin access
        </Badge>
      </div>

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
              <MetricCard
                title="Page views"
                value={String(stats.viewCount)}
                hint="Incremented on each visit via Redis"
                icon={Eye}
              />
              <MetricCard
                title="Total items"
                value={String(stats.itemCount)}
                hint="Count from PostgreSQL"
                icon={Database}
              />
              <MetricCard
                title="Cache status"
                value={stats.cached ? "Active" : "Disabled"}
                hint="Distributed cache backing admin stats"
                icon={Activity}
              />
            </>
          )
        )}
      </div>

      <Card>
        <CardHeader>
          <CardTitle>About these metrics</CardTitle>
          <CardDescription>
            Each visit to this page calls `/api/admin/stats`, which increments a Redis counter and
            returns the current item count from the database.
          </CardDescription>
        </CardHeader>
      </Card>
    </div>
  );
}
