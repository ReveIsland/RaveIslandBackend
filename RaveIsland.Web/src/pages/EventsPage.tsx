import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "react-oidc-context";
import { Plus } from "lucide-react";
import { useCurrentUser } from "../auth/CurrentUserContext";
import {
  apiFetch,
  isPlatformAdmin,
  type EventItem,
} from "../lib/api";
import { Button, buttonVariants } from "../components/ui/button";
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
import { cn } from "../lib/utils";

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
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="text-2xl font-bold tracking-tight">Events</h2>
          <p className="text-muted-foreground">
            Create and manage events for your organization.
          </p>
        </div>
        <Link to="/events/new" className={cn(buttonVariants())}>
          <Plus className="mr-2 h-4 w-4" />
          Create event
        </Link>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Your events</CardTitle>
          <CardDescription>
            Select an event to edit or remove events you no longer need.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {error && <p className="mb-4 text-sm text-destructive">{error}</p>}
          {isLoading ? (
            <Skeleton className="h-32 w-full" />
          ) : events.length === 0 ? (
            <div className="flex flex-col items-center gap-4 py-12 text-center">
              <p className="text-sm text-muted-foreground">No events yet.</p>
              <Link to="/events/new" className={cn(buttonVariants({ variant: "outline" }))}>
                Create your first event
              </Link>
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Title</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Category</TableHead>
                  {admin && <TableHead>Tenant</TableHead>}
                  <TableHead>Created by</TableHead>
                  <TableHead>Updated</TableHead>
                  <TableHead>Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {events.map((eventItem) => (
                  <TableRow key={eventItem.id}>
                    <TableCell>
                      <div>
                        <div className="font-medium">{eventItem.title}</div>
                        {eventItem.tagline && (
                          <div className="text-xs text-muted-foreground">{eventItem.tagline}</div>
                        )}
                      </div>
                    </TableCell>
                    <TableCell>{eventItem.eventStatusName ?? "—"}</TableCell>
                    <TableCell>{eventItem.eventCategoryName ?? "—"}</TableCell>
                    {admin && <TableCell>{eventItem.tenantName}</TableCell>}
                    <TableCell>{eventItem.createdByName ?? "Unknown"}</TableCell>
                    <TableCell>
                      {new Date(eventItem.updatedAt).toLocaleDateString()}
                    </TableCell>
                    <TableCell className="space-x-2">
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
                        onClick={() => void handleDelete(eventItem.id)}
                      >
                        Delete
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
