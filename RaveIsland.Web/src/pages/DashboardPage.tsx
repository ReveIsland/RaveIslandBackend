import { useCallback, useEffect, useState, type FormEvent } from "react";
import { useAuth } from "react-oidc-context";
import { Database, Package, ShieldCheck, UserRound } from "lucide-react";
import { Button } from "../components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "../components/ui/card";
import { Input } from "../components/ui/input";
import { Badge } from "../components/ui/badge";
import { Skeleton } from "../components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "../components/ui/table";

type MeResponse = {
  name?: string;
  email?: string;
  roles?: string[];
};

type ItemResponse = {
  id: string;
  title: string;
  createdBy?: string;
  createdAt: string;
};

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
  const auth = useAuth();
  const [data, setData] = useState<MeResponse | null>(null);
  const [items, setItems] = useState<ItemResponse[]>([]);
  const [title, setTitle] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [itemsError, setItemsError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [isLoadingProfile, setIsLoadingProfile] = useState(true);
  const [isLoadingItems, setIsLoadingItems] = useState(true);

  const authHeaders = useCallback(() => {
    if (!auth.user?.access_token) {
      return undefined;
    }

    return {
      Authorization: `Bearer ${auth.user.access_token}`,
      "Content-Type": "application/json",
    };
  }, [auth.user?.access_token]);

  const loadItems = useCallback(async () => {
    const headers = authHeaders();
    if (!headers) {
      return;
    }

    setIsLoadingItems(true);
    try {
      const response = await fetch("/api/items", { headers });
      if (!response.ok) {
        throw new Error(`Items API returned ${response.status}`);
      }

      setItems((await response.json()) as ItemResponse[]);
      setItemsError(null);
    } catch (err: unknown) {
      setItemsError(err instanceof Error ? err.message : "Failed to load items");
    } finally {
      setIsLoadingItems(false);
    }
  }, [authHeaders]);

  useEffect(() => {
    const headers = authHeaders();
    if (!headers) {
      return;
    }

    setIsLoadingProfile(true);
    fetch("/api/me", { headers })
      .then(async (response) => {
        if (!response.ok) {
          throw new Error(`API returned ${response.status}`);
        }
        return response.json() as Promise<MeResponse>;
      })
      .then((profile) => {
        setData(profile);
        setError(null);
      })
      .catch((err: unknown) =>
        setError(err instanceof Error ? err.message : "Failed to load profile"),
      )
      .finally(() => setIsLoadingProfile(false));
  }, [authHeaders]);

  useEffect(() => {
    void loadItems();
  }, [loadItems]);

  async function handleCreateItem(event: FormEvent) {
    event.preventDefault();
    const headers = authHeaders();
    if (!headers || !title.trim()) {
      return;
    }

    setIsSaving(true);
    setItemsError(null);

    try {
      const response = await fetch("/api/items", {
        method: "POST",
        headers,
        body: JSON.stringify({ title: title.trim() }),
      });

      if (!response.ok) {
        throw new Error(`Create item failed with ${response.status}`);
      }

      setTitle("");
      await loadItems();
    } catch (err: unknown) {
      setItemsError(err instanceof Error ? err.message : "Failed to create item");
    } finally {
      setIsSaving(false);
    }
  }

  const roleCount = data?.roles?.length ?? 0;

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-2xl font-bold tracking-tight">Welcome back</h2>
        <p className="text-muted-foreground">
          Manage your profile and PostgreSQL-backed items from the admin panel.
        </p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard
          title="Total items"
          value={isLoadingItems ? "—" : String(items.length)}
          description="Stored in PostgreSQL"
          icon={Package}
        />
        <StatCard
          title="Assigned roles"
          value={isLoadingProfile ? "—" : String(roleCount)}
          description="From Keycloak realm"
          icon={ShieldCheck}
        />
        <StatCard
          title="Account"
          value={isLoadingProfile ? "—" : data?.name?.split(" ")[0] ?? "Active"}
          description="Signed in via OIDC"
          icon={UserRound}
        />
        <StatCard
          title="Data source"
          value="Live"
          description="Aspire API + Redis cache"
          icon={Database}
        />
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <Card className="lg:col-span-1">
          <CardHeader>
            <CardTitle>Profile</CardTitle>
            <CardDescription>Loaded from `/api/me`</CardDescription>
          </CardHeader>
          <CardContent>
            {error && <p className="text-sm text-destructive">{error}</p>}
            {isLoadingProfile && !error && (
              <div className="space-y-3">
                <Skeleton className="h-4 w-2/3" />
                <Skeleton className="h-4 w-1/2" />
                <Skeleton className="h-6 w-1/3" />
              </div>
            )}
            {!isLoadingProfile && !error && data && (
              <dl className="space-y-4 text-sm">
                <div>
                  <dt className="text-muted-foreground">Name</dt>
                  <dd className="mt-1 font-medium">{data.name ?? "Unknown"}</dd>
                </div>
                <div>
                  <dt className="text-muted-foreground">Email</dt>
                  <dd className="mt-1 font-medium">{data.email ?? "Not provided"}</dd>
                </div>
                <div>
                  <dt className="text-muted-foreground">Roles</dt>
                  <dd className="mt-2 flex flex-wrap gap-2">
                    {(data.roles ?? []).length > 0 ? (
                      data.roles!.map((role) => (
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

        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle>Items</CardTitle>
            <CardDescription>Create and review records persisted by the API.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <form className="flex flex-col gap-2 sm:flex-row" onSubmit={handleCreateItem}>
              <Input
                placeholder="New item title"
                value={title}
                onChange={(event) => setTitle(event.target.value)}
              />
              <Button type="submit" disabled={isSaving || !title.trim()} className="sm:w-auto">
                {isSaving ? "Saving..." : "Add item"}
              </Button>
            </form>

            {itemsError && <p className="text-sm text-destructive">{itemsError}</p>}

            <div className="rounded-md border border-border">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Title</TableHead>
                    <TableHead>Created by</TableHead>
                    <TableHead className="text-right">Created at</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {isLoadingItems && (
                    <TableRow>
                      <TableCell colSpan={3}>
                        <div className="space-y-2 py-2">
                          <Skeleton className="h-4 w-full" />
                          <Skeleton className="h-4 w-5/6" />
                        </div>
                      </TableCell>
                    </TableRow>
                  )}
                  {!isLoadingItems && items.length === 0 && !itemsError && (
                    <TableRow>
                      <TableCell colSpan={3} className="text-center text-muted-foreground">
                        No items yet. Add your first one above.
                      </TableCell>
                    </TableRow>
                  )}
                  {!isLoadingItems &&
                    items.map((item) => (
                      <TableRow key={item.id}>
                        <TableCell className="font-medium">{item.title}</TableCell>
                        <TableCell>{item.createdBy ?? "Unknown"}</TableCell>
                        <TableCell className="text-right text-muted-foreground">
                          {new Date(item.createdAt).toLocaleString()}
                        </TableCell>
                      </TableRow>
                    ))}
                </TableBody>
              </Table>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
