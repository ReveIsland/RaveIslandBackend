import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useAuth } from "react-oidc-context";
import { ArrowLeft } from "lucide-react";
import { useCurrentUser } from "../auth/CurrentUserContext";
import {
  apiFetch,
  isPlatformAdmin,
  type EventItem,
  type Tenant,
} from "../lib/api";
import { Button, buttonVariants } from "../components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "../components/ui/card";
import { Input } from "../components/ui/input";
import { Skeleton } from "../components/ui/skeleton";
import { cn } from "../lib/utils";

export function EventFormPage() {
  const { eventId } = useParams<{ eventId: string }>();
  const isEdit = Boolean(eventId);
  const navigate = useNavigate();
  const auth = useAuth();
  const { profile } = useCurrentUser();
  const token = auth.user?.access_token;
  const roles = profile?.roles ?? [];
  const admin = isPlatformAdmin(roles);

  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [tenantId, setTenantId] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(isEdit);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (!token) return;

    if (admin && !isEdit) {
      apiFetch<Tenant[]>("/api/tenants", { token })
        .then(setTenants)
        .catch(() => setTenants([]));
    }
  }, [token, admin, isEdit]);

  useEffect(() => {
    if (!token || !isEdit || !eventId) return;

    setIsLoading(true);
    apiFetch<EventItem>(`/api/events/${eventId}`, { token })
      .then((eventItem) => {
        setTitle(eventItem.title);
        setDescription(eventItem.description ?? "");
        setTenantId(eventItem.tenantId);
        setError(null);
      })
      .catch((err: unknown) => {
        setError(err instanceof Error ? err.message : "Failed to load event");
      })
      .finally(() => setIsLoading(false));
  }, [token, isEdit, eventId]);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (!token) return;

    setIsSubmitting(true);
    setError(null);

    try {
      if (isEdit && eventId) {
        await apiFetch(`/api/events/${eventId}`, {
          method: "PATCH",
          token,
          body: JSON.stringify({
            title,
            description: description || null,
          }),
        });
      } else {
        await apiFetch("/api/events", {
          method: "POST",
          token,
          body: JSON.stringify({
            title,
            description: description || null,
            tenantId: admin ? tenantId || null : null,
          }),
        });
      }

      navigate("/events");
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to save event");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <div className="flex items-center gap-4">
        <Link to="/events" className={cn(buttonVariants({ variant: "outline", size: "sm" }))}>
          <ArrowLeft className="mr-2 h-4 w-4" />
          Back to events
        </Link>
      </div>

      <div>
        <h2 className="text-2xl font-bold tracking-tight">
          {isEdit ? "Edit event" : "Create event"}
        </h2>
        <p className="text-muted-foreground">
          {isEdit
            ? "Update the details for this event."
            : "Add a new event to the platform."}
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{isEdit ? "Event details" : "New event"}</CardTitle>
          <CardDescription>
            {isEdit
              ? "Changes are saved to your organization."
              : "Fill in the information below to create an event."}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="space-y-4">
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-10 w-32" />
            </div>
          ) : (
            <form className="grid gap-4" onSubmit={handleSubmit}>
              {admin && !isEdit && (
                <div className="space-y-2">
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

              <div className="space-y-2">
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

              <div className="space-y-2">
                <label className="text-sm font-medium" htmlFor="event-description">
                  Description
                </label>
                <Input
                  id="event-description"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                />
              </div>

              <div className="flex gap-3 pt-2">
                <Button
                  type="submit"
                  disabled={isSubmitting || (!isEdit && admin && !tenantId)}
                >
                  {isSubmitting
                    ? isEdit
                      ? "Saving..."
                      : "Creating..."
                    : isEdit
                      ? "Save changes"
                      : "Create event"}
                </Button>
                <Link to="/events" className={cn(buttonVariants({ variant: "outline" }))}>
                  Cancel
                </Link>
              </div>

              {error && <p className="text-sm text-destructive">{error}</p>}
            </form>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
