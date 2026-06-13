import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useAuth } from "react-oidc-context";
import { ArrowLeft } from "lucide-react";
import { apiFetch, type LookupValue } from "../../lib/api";
import { invalidateLookupCache } from "../../hooks/useLookupValues";
import { Button, buttonVariants } from "../../components/ui/button";
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
import { cn } from "../../lib/utils";

export function LookupValuesPage() {
  const { typeCode } = useParams<{ typeCode: string }>();
  const auth = useAuth();
  const token = auth.user?.access_token;
  const [values, setValues] = useState<LookupValue[]>([]);
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function loadValues() {
    if (!token || !typeCode) return;
    setIsLoading(true);
    try {
      const data = await apiFetch<LookupValue[]>(`/api/lookups/${typeCode}/all`, { token });
      setValues(data);
      setError(null);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to load values");
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadValues();
  }, [token, typeCode]);

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    if (!token || !typeCode) return;
    setIsSubmitting(true);
    try {
      await apiFetch(`/api/lookups/${typeCode}/values`, {
        method: "POST",
        token,
        body: JSON.stringify({ code, name, displayOrder: null, iconUrl: null, metadataJson: null }),
      });
      invalidateLookupCache(typeCode);
      setCode("");
      setName("");
      await loadValues();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to create value");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function toggleActive(id: string, isActive: boolean) {
    if (!token) return;
    await apiFetch(`/api/lookups/values/${id}`, {
      method: "PATCH",
      token,
      body: JSON.stringify({ isActive: !isActive }),
    });
    invalidateLookupCache(typeCode);
    await loadValues();
  }

  return (
    <div className="space-y-8">
      <Link to="/admin/lookups" className={cn(buttonVariants({ variant: "outline", size: "sm" }))}>
        <ArrowLeft className="mr-2 h-4 w-4" />
        Back to types
      </Link>

      <div>
        <h2 className="text-2xl font-bold tracking-tight">{typeCode} values</h2>
        <p className="text-muted-foreground">Manage values for this reference data type.</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Add value</CardTitle>
        </CardHeader>
        <CardContent>
          <form className="flex flex-wrap gap-4" onSubmit={handleCreate}>
            <Input className="max-w-xs" placeholder="Code" value={code} onChange={(e) => setCode(e.target.value)} required />
            <Input className="max-w-xs" placeholder="Name" value={name} onChange={(e) => setName(e.target.value)} required />
            <Button type="submit" disabled={isSubmitting}>{isSubmitting ? "Adding..." : "Add value"}</Button>
          </form>
          {error && <p className="mt-4 text-sm text-destructive">{error}</p>}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Values</CardTitle>
          <CardDescription>Toggle active status or reorder via display order in API.</CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Skeleton className="h-32 w-full" />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Order</TableHead>
                  <TableHead>Code</TableHead>
                  <TableHead>Name</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {values.map((value) => (
                  <TableRow key={value.id}>
                    <TableCell>{value.displayOrder}</TableCell>
                    <TableCell className="font-mono text-sm">{value.code}</TableCell>
                    <TableCell>{value.name}</TableCell>
                    <TableCell>
                      {value.isActive ? (
                        <Badge variant="secondary">Active</Badge>
                      ) : (
                        <Badge variant="outline">Inactive</Badge>
                      )}
                    </TableCell>
                    <TableCell>
                      <Button size="sm" variant="outline" onClick={() => void toggleActive(value.id, value.isActive)}>
                        {value.isActive ? "Deactivate" : "Activate"}
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
