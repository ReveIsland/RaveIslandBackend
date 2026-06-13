import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "react-oidc-context";
import { apiFetch, type LookupType } from "../../lib/api";
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
import { buttonVariants } from "../../components/ui/button";
import { cn } from "../../lib/utils";

export function LookupTypesPage() {
  const auth = useAuth();
  const token = auth.user?.access_token;
  const [types, setTypes] = useState<LookupType[]>([]);
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function loadTypes() {
    if (!token) return;
    setIsLoading(true);
    try {
      const data = await apiFetch<LookupType[]>("/api/lookups/types", { token });
      setTypes(data);
      setError(null);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to load lookup types");
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadTypes();
  }, [token]);

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    if (!token) return;
    setIsSubmitting(true);
    setError(null);
    try {
      await apiFetch("/api/lookups/types", {
        method: "POST",
        token,
        body: JSON.stringify({ code, name, description: description || null }),
      });
      setCode("");
      setName("");
      setDescription("");
      await loadTypes();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to create lookup type");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-2xl font-bold tracking-tight">Reference Data Types</h2>
        <p className="text-muted-foreground">
          Manage platform-wide lookup categories for dropdowns and classifications.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Create lookup type</CardTitle>
          <CardDescription>Add a custom reference data category.</CardDescription>
        </CardHeader>
        <CardContent>
          <form className="grid gap-4 md:grid-cols-3" onSubmit={handleCreate}>
            <Input placeholder="Code (e.g. CustomType)" value={code} onChange={(e) => setCode(e.target.value)} required />
            <Input placeholder="Display name" value={name} onChange={(e) => setName(e.target.value)} required />
            <Input placeholder="Description (optional)" value={description} onChange={(e) => setDescription(e.target.value)} />
            <Button type="submit" disabled={isSubmitting} className="md:col-span-3 md:w-fit">
              {isSubmitting ? "Creating..." : "Create type"}
            </Button>
          </form>
          {error && <p className="mt-4 text-sm text-destructive">{error}</p>}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Lookup types</CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Skeleton className="h-32 w-full" />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Code</TableHead>
                  <TableHead>Name</TableHead>
                  <TableHead>System</TableHead>
                  <TableHead>Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {types.map((type) => (
                  <TableRow key={type.id}>
                    <TableCell className="font-mono text-sm">{type.code}</TableCell>
                    <TableCell>{type.name}</TableCell>
                    <TableCell>
                      {type.isSystem ? <Badge variant="secondary">System</Badge> : <Badge variant="outline">Custom</Badge>}
                    </TableCell>
                    <TableCell>
                      <Link to={`/admin/lookups/${type.code}`} className={cn(buttonVariants({ variant: "outline", size: "sm" }))}>
                        Manage values
                      </Link>
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
