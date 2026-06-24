import { useEffect, useState } from "react";
import { useAuth } from "react-oidc-context";
import { apiFetch, type Tenant } from "../../lib/api";
import { Button } from "../../components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "../../components/ui/card";
import { Input } from "../../components/ui/input";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "../../components/ui/table";
import { Badge } from "../../components/ui/badge";
import { Skeleton } from "../../components/ui/skeleton";

export function TenantsPage() {
  const auth = useAuth();
  const token = auth.user?.access_token;
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [name, setName] = useState("");
  const [slug, setSlug] = useState("");
  const [pendingPromotionCode, setPendingPromotionCode] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function loadTenants() {
    if (!token) return;
    setIsLoading(true);
    try {
      const data = await apiFetch<Tenant[]>("/api/tenants", { token });
      setTenants(data);
      setError(null);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to load tenants");
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadTenants();
  }, [token]);

  async function handleCreate(event: React.FormEvent) {
    event.preventDefault();
    if (!token) return;

    setIsSubmitting(true);
    setError(null);
    try {
      await apiFetch("/api/tenants", {
        method: "POST",
        token,
        body: JSON.stringify({
          name,
          slug: slug || null,
          pendingPromotionCode: pendingPromotionCode || null,
        }),
      });
      setName("");
      setSlug("");
      setPendingPromotionCode("");
      await loadTenants();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to create tenant");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-2xl font-bold tracking-tight">Event providers</h2>
        <p className="text-muted-foreground">
          Register tenants (event provider organizations) on the platform.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Create tenant</CardTitle>
          <CardDescription>Add a new event provider organization.</CardDescription>
        </CardHeader>
        <CardContent>
          <form className="grid gap-4 sm:grid-cols-2" onSubmit={handleCreate}>
            <div className="space-y-2">
              <label className="text-sm font-medium" htmlFor="tenant-name">
                Name
              </label>
              <Input
                id="tenant-name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Sunset Events Co."
                required
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium" htmlFor="tenant-slug">
                Slug (optional)
              </label>
              <Input
                id="tenant-slug"
                value={slug}
                onChange={(e) => setSlug(e.target.value)}
                placeholder="sunset-events"
              />
            </div>
            <div className="space-y-2 sm:col-span-2">
              <label className="text-sm font-medium" htmlFor="tenant-promo">
                Organizer promotion code (optional)
              </label>
              <Input
                id="tenant-promo"
                value={pendingPromotionCode}
                onChange={(e) => setPendingPromotionCode(e.target.value)}
                placeholder="ORG-LAUNCH20"
              />
            </div>
            <div className="sm:col-span-2">
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? "Creating..." : "Create tenant"}
              </Button>
            </div>
          </form>
          {error && <p className="mt-4 text-sm text-destructive">{error}</p>}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>All tenants</CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Skeleton className="h-32 w-full" />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Slug</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Created</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {tenants.map((tenant) => (
                  <TableRow key={tenant.id}>
                    <TableCell className="font-medium">{tenant.name}</TableCell>
                    <TableCell>{tenant.slug}</TableCell>
                    <TableCell>
                      <Badge variant={tenant.isActive ? "secondary" : "outline"}>
                        {tenant.isActive ? "Active" : "Inactive"}
                      </Badge>
                    </TableCell>
                    <TableCell>{new Date(tenant.createdAt).toLocaleDateString()}</TableCell>
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
