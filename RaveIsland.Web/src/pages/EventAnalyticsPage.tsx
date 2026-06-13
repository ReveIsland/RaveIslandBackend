import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useAuth } from "react-oidc-context";
import { ArrowLeft } from "lucide-react";
import { apiFetch, type EventAnalytics } from "../lib/api";
import { buttonVariants } from "../components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "../components/ui/card";
import { Skeleton } from "../components/ui/skeleton";
import { cn } from "../lib/utils";

export function EventAnalyticsPage() {
  const { eventId } = useParams<{ eventId: string }>();
  const auth = useAuth();
  const token = auth.user?.access_token;
  const [analytics, setAnalytics] = useState<EventAnalytics | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    if (!token || !eventId) return;
    apiFetch<EventAnalytics>(`/api/events/${eventId}/analytics`, { token })
      .then(setAnalytics)
      .catch((err: unknown) => setError(err instanceof Error ? err.message : "Failed to load analytics"))
      .finally(() => setIsLoading(false));
  }, [token, eventId]);

  return (
    <div className="space-y-6">
      <Link to={`/events/${eventId}/edit`} className={cn(buttonVariants({ variant: "outline", size: "sm" }))}>
        <ArrowLeft className="mr-2 h-4 w-4" /> Back to event
      </Link>

      <div>
        <h2 className="text-2xl font-bold tracking-tight">Event analytics</h2>
        <p className="text-muted-foreground">Ticket sales, revenue, and attendance metrics.</p>
      </div>

      {isLoading ? (
        <Skeleton className="h-48 w-full" />
      ) : error ? (
        <p className="text-destructive">{error}</p>
      ) : analytics ? (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          <Card>
            <CardHeader><CardTitle>Tickets sold</CardTitle></CardHeader>
            <CardContent><p className="text-3xl font-bold">{analytics.ticketsSold}</p></CardContent>
          </Card>
          <Card>
            <CardHeader><CardTitle>Revenue</CardTitle></CardHeader>
            <CardContent><p className="text-3xl font-bold">LKR {analytics.revenue.toLocaleString()}</p></CardContent>
          </Card>
          <Card>
            <CardHeader><CardTitle>Attendance</CardTitle></CardHeader>
            <CardContent><p className="text-3xl font-bold">{analytics.attendance}</p></CardContent>
          </Card>
          <Card>
            <CardHeader><CardTitle>Conversion</CardTitle></CardHeader>
            <CardContent><p className="text-3xl font-bold">{analytics.conversionRate}%</p></CardContent>
          </Card>
          <Card>
            <CardHeader><CardTitle>Promo usage</CardTitle></CardHeader>
            <CardContent><p className="text-3xl font-bold">{analytics.promoUsage}</p></CardContent>
          </Card>
          <Card className="md:col-span-2 lg:col-span-3">
            <CardHeader>
              <CardTitle>Ticket type breakdown</CardTitle>
              <CardDescription>Sales by ticket category</CardDescription>
            </CardHeader>
            <CardContent>
              <ul className="space-y-2">
                {analytics.ticketTypeBreakdown.map((t) => (
                  <li key={t.id} className="flex justify-between text-sm">
                    <span>{t.name}</span>
                    <span>{t.quantitySold} / {t.quantity} — LKR {t.revenue.toLocaleString()}</span>
                  </li>
                ))}
              </ul>
            </CardContent>
          </Card>
        </div>
      ) : null}
    </div>
  );
}
