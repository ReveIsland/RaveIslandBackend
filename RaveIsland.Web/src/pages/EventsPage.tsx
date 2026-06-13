import { useEffect, useState } from "react";
import { useAuth } from "react-oidc-context";
import { useCurrentUser } from "../auth/CurrentUserContext";
import {
  apiFetch,
  isPlatformAdmin,
  type EventItem,
  type Tenant,
} from "../lib/api";
import { Button } from "../components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "../components/ui/card";
import { Input } from "../components/ui/input";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "../components/ui/table";
import { Skeleton } from "../components/ui/skeleton";

export function EventsPage() {
  const auth = useAuth();
  const { profile } = useCurrentUser();
  const token = auth.user?.access_token;
  const roles = profile?.roles ?? [];
  const admin = isPlatformAdmin(roles);

  const [events, setEvents] = useState<EventItem[]>([]);
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [tenantId, setTenantId] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editTitle, setEditTitle] = useState("");
  const [editDescription, setEditDescription] = useState("");

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

    if (admin) {
      apiFetch<Tenant[]>("/api/tenants", { token })
        .then(setTenants)
        .catch(() => setTenants([]));
    }

    void loadEvents();
  }, [token, admin]);

  async function handleCreate(event: React.FormEvent) {
    event.preventDefault();
    if (!token) return;

    setIsSubmitting(true);
    setError(null);
    try {
      await apiFetch("/api/events", {
        method: "POST",
        token,
        body: JSON.stringify({
          title,
          description: description || null,
          tenantId: admin ? tenantId || null : null,
        }),
      });
      setTitle("");
      setDescription("");
      await loadEvents();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to create event");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleUpdate(eventId: string) {
    if (!token) return;
    try {
      await apiFetch(`/api/events/${eventId}`, {
        method: "PATCH",
        token,
        body: JSON.stringify({
          title: editTitle,
          description: editDescription,
        }),
      });
      setEditingId(null);
      await loadEvents();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to update event");
    }
  }

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

  function startEdit(eventItem: EventItem) {
    setEditingId(eventItem.id);
    setEditTitle(eventItem.title);
    setEditDescription(eventItem.description ?? "");
  }

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-2xl font-bold tracking-tight">Events</h2>
        <p className="text-muted-foreground">
          Create and manage events for your organization.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Create event</CardTitle>
          <CardDescription>Add a new event to the platform.</CardDescription>
        </CardHeader>
        <CardContent>
          <form className="grid gap-4 md:grid-cols-2" onSubmit={handleCreate}>
            {admin && (
              <div className="space-y-2 md:col-span-2">
                <label className="text-sm font-medium" htmlFor="event-tenant">
                  Tenant
                </label>
                <select
                  id="event-tenant"
                  className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                  value={tenantId}
                  onChange={(e) => setTenantId(e.target.value)}
                  required
                >
                  <option value="">Select tenant...</option>
                  {tenants.map((tenant) => (
                    <option key={tenant.id} value={tenant.id}>
                      {tenant.name}
                    </option>
                  ))}
                </select>
              </div>
            )}

            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium" htmlFor="event-title">
                Title
              </label>
              <Input
                id="event-title"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                required
              />
            </div>

            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium" htmlFor="event-description">
                Description
              </label>
              <Input
                id="event-description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
              />
            </div>

            <div className="md:col-span-2">
              <Button type="submit" disabled={isSubmitting || (admin && !tenantId)}>
                {isSubmitting ? "Creating..." : "Create event"}
              </Button>
            </div>
          </form>
          {error && <p className="mt-4 text-sm text-destructive">{error}</p>}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Your events</CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Skeleton className="h-32 w-full" />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Title</TableHead>
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
                      {editingId === eventItem.id ? (
                        <div className="space-y-2">
                          <Input value={editTitle} onChange={(e) => setEditTitle(e.target.value)} />
                          <Input
                            value={editDescription}
                            onChange={(e) => setEditDescription(e.target.value)}
                            placeholder="Description"
                          />
                        </div>
                      ) : (
                        <div>
                          <div className="font-medium">{eventItem.title}</div>
                          {eventItem.description && (
                            <div className="text-xs text-muted-foreground">{eventItem.description}</div>
                          )}
                        </div>
                      )}
                    </TableCell>
                    {admin && <TableCell>{eventItem.tenantName}</TableCell>}
                    <TableCell>{eventItem.createdByName ?? "Unknown"}</TableCell>
                    <TableCell>{new Date(eventItem.updatedAt).toLocaleDateString()}</TableCell>
                    <TableCell className="space-x-2">
                      {editingId === eventItem.id ? (
                        <>
                          <Button size="sm" onClick={() => void handleUpdate(eventItem.id)}>
                            Save
                          </Button>
                          <Button size="sm" variant="outline" onClick={() => setEditingId(null)}>
                            Cancel
                          </Button>
                        </>
                      ) : (
                        <>
                          <Button size="sm" variant="outline" onClick={() => startEdit(eventItem)}>
                            Edit
                          </Button>
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => void handleDelete(eventItem.id)}
                          >
                            Delete
                          </Button>
                        </>
                      )}
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
