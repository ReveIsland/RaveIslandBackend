import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "react-oidc-context";
import { CalendarDays, Plus } from "lucide-react";
import { useCurrentUser } from "../auth/CurrentUserContext";
import {
  apiFetch,
  isPlatformAdmin,
  type EventItem,
} from "../lib/api";
import { Button, buttonVariants } from "../components/ui/button";
import { Badge } from "../components/ui/badge";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "../components/ui/card";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "../components/ui/table";
import { Skeleton } from "../components/ui/skeleton";
import { PageHeader } from "../components/layout/PageHeader";
import { EmptyState } from "../components/layout/EmptyState";
import { cn } from "../lib/utils";

function statusBadgeVariant(
  status?: string | null,
): "success" | "warning" | "secondary" | "destructive" | "outline" {
  const normalized = status?.toLowerCase() ?? "";
  if (normalized.includes("publish") || normalized.includes("live")) return "success";
  if (normalized.includes("draft")) return "warning";
  if (normalized.includes("cancel")) return "destructive";
  return "secondary";
}

export function EventsPage() {
  const auth = useAuth();
  const { profile } = useCurrentUser();
  const token = auth.user?.access_token;
  const roles = profile?.roles ?? [];
  const admin = isPlatformAdmin(roles);

  const [events, setEvents] = useState<EventItem[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  async function loadEvents() {
    if (!token) return;
    setIsLoading(true);
    try {
      const data = await apiFetch<EventItem[]>("/api/events", { token });
      setEvents(data);
      setError(null);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to load events");
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    if (!token) return;
    void loadEvents();
  }, [token]);

  async function handleDelete(eventId: string) {
    if (!token) return;
    try {
      await apiFetch(`/api/events/${eventId}`, {
        method: "DELETE",
        token,
      });
      await loadEvents();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to delete event");
    }
  }

  return (
    <div className="space-y-8">
      <PageHeader
        title="Events"
        description="Create and manage events for your organization."
        actions={
          <Link to="/events/new" className={cn(buttonVariants())}>
            <Plus className="h-4 w-4" />
            Create event
          </Link>
        }
      />

      <Card>
        <CardHeader>
          <CardTitle>Your events</CardTitle>
          <CardDescription>
            Select an event to edit, view stats, or remove events you no longer need.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {error && <p className="mb-4 text-sm text-destructive">{error}</p>}
          {isLoading ? (
            <div className="space-y-3">
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-10 w-full" />
            </div>
          ) : events.length === 0 ? (
            <EmptyState
              icon={CalendarDays}
              title="No events yet"
              description="Create your first event to start managing tickets, check-in, and analytics."
              action={
                <Link to="/events/new" className={cn(buttonVariants({ variant: "outline" }))}>
                  <Plus className="h-4 w-4" />
                  Create your first event
                </Link>
              }
            />
          ) : (
            <div className="overflow-x-auto rounded-lg border border-border">
              <Table>
                <TableHeader>
                  <TableRow className="bg-muted/40 hover:bg-muted/40">
                    <TableHead>Title</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Category</TableHead>
                    {admin && <TableHead>Tenant</TableHead>}
                    <TableHead>Created by</TableHead>
                    <TableHead>Updated</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {events.map((eventItem) => (
                    <TableRow key={eventItem.id} className="group">
                      <TableCell>
                        <div>
                          <div className="font-medium">{eventItem.title}</div>
                          {eventItem.tagline && (
                            <div className="text-xs text-muted-foreground">{eventItem.tagline}</div>
                          )}
                        </div>
                      </TableCell>
                      <TableCell>
                        <Badge variant={statusBadgeVariant(eventItem.eventStatusName)}>
                          {eventItem.eventStatusName ?? "—"}
                        </Badge>
                      </TableCell>
                      <TableCell>{eventItem.eventCategoryName ?? "—"}</TableCell>
                      {admin && <TableCell>{eventItem.tenantName}</TableCell>}
                      <TableCell>{eventItem.createdByName ?? "Unknown"}</TableCell>
                      <TableCell className="text-muted-foreground">
                        {new Date(eventItem.updatedAt).toLocaleDateString()}
                      </TableCell>
                      <TableCell>
                        <div className="flex justify-end gap-1.5 opacity-90 transition-opacity group-hover:opacity-100">
                          <Link
                            to={`/events/${eventItem.id}/edit`}
                            className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
                          >
                            Edit
                          </Link>
                          <Link
                            to={`/events/${eventItem.id}/analytics`}
                            className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
                          >
                            Stats
                          </Link>
                          <Button
                            size="sm"
                            variant="outline"
                            className="text-destructive hover:bg-destructive/10 hover:text-destructive"
                            onClick={() => void handleDelete(eventItem.id)}
                          >
                            Delete
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
