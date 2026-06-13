import { useEffect, useState } from "react";
import { useAuth } from "react-oidc-context";
import { useCurrentUser } from "../../auth/CurrentUserContext";
import {
  apiFetch,
  isPlatformAdmin,
  type Tenant,
  type UserInvitation,
  type UserMember,
} from "../../lib/api";
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

type UsersResponse = {
  members: UserMember[];
  invitations: UserInvitation[];
};

export function UsersPage() {
  const auth = useAuth();
  const { profile } = useCurrentUser();
  const token = auth.user?.access_token;
  const roles = profile?.roles ?? [];
  const admin = isPlatformAdmin(roles);

  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [users, setUsers] = useState<UsersResponse | null>(null);
  const [selectedTenantId, setSelectedTenantId] = useState("");
  const [email, setEmail] = useState("");
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [role, setRole] = useState("tenant-user");
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function loadUsers(tenantId?: string) {
    if (!token) return;
    setIsLoading(true);
    try {
      const query = tenantId ? `?tenantId=${tenantId}` : "";
      const data = await apiFetch<UsersResponse>(`/api/users${query}`, { token });
      setUsers(data);
      setError(null);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to load users");
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

    void loadUsers(admin ? selectedTenantId || undefined : undefined);
  }, [token, admin, selectedTenantId]);

  async function handleInvite(event: React.FormEvent) {
    event.preventDefault();
    if (!token) return;

    setIsSubmitting(true);
    setError(null);
    try {
      await apiFetch("/api/users/invite", {
        method: "POST",
        token,
        body: JSON.stringify({
          email,
          firstName,
          lastName,
          role,
          tenantId: admin ? selectedTenantId || null : null,
        }),
      });
      setEmail("");
      setFirstName("");
      setLastName("");
      await loadUsers(admin ? selectedTenantId || undefined : undefined);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to send invitation");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleRevoke(invitationId: string) {
    if (!token) return;
    try {
      await apiFetch(`/api/invitations/${invitationId}/revoke`, {
        method: "POST",
        token,
      });
      await loadUsers(admin ? selectedTenantId || undefined : undefined);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to revoke invitation");
    }
  }

  async function handleToggleMember(membership: UserMember) {
    if (!token) return;
    try {
      await apiFetch(`/api/users/${membership.id}`, {
        method: "PATCH",
        token,
        body: JSON.stringify({ isActive: !membership.isActive }),
      });
      await loadUsers(admin ? selectedTenantId || undefined : undefined);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to update user");
    }
  }

  function statusBadgeVariant(status: string) {
    switch (status) {
      case "Registered":
      case "InviteSent":
        return "secondary" as const;
      case "Revoked":
      case "Expired":
        return "outline" as const;
      default:
        return "outline" as const;
    }
  }

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-2xl font-bold tracking-tight">User management</h2>
        <p className="text-muted-foreground">
          Invite users and track registration status.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Invite user</CardTitle>
          <CardDescription>
            Invitations are sent by email. Self-registration is disabled.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form className="grid gap-4 md:grid-cols-2" onSubmit={handleInvite}>
            {admin && (
              <div className="space-y-2 md:col-span-2">
                <label className="text-sm font-medium" htmlFor="tenant-select">
                  Tenant
                </label>
                <select
                  id="tenant-select"
                  className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                  value={selectedTenantId}
                  onChange={(e) => setSelectedTenantId(e.target.value)}
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
              <label className="text-sm font-medium" htmlFor="invite-email">
                Email
              </label>
              <Input
                id="invite-email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium" htmlFor="invite-role">
                Role
              </label>
              <select
                id="invite-role"
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                value={role}
                onChange={(e) => setRole(e.target.value)}
              >
                <option value="tenant-user">tenant-user</option>
                {admin && <option value="tenant-admin">tenant-admin</option>}
              </select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium" htmlFor="invite-first">
                First name
              </label>
              <Input
                id="invite-first"
                value={firstName}
                onChange={(e) => setFirstName(e.target.value)}
                required
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium" htmlFor="invite-last">
                Last name
              </label>
              <Input
                id="invite-last"
                value={lastName}
                onChange={(e) => setLastName(e.target.value)}
                required
              />
            </div>

            <div className="md:col-span-2">
              <Button type="submit" disabled={isSubmitting || (admin && !selectedTenantId)}>
                {isSubmitting ? "Sending..." : "Send invitation"}
              </Button>
            </div>
          </form>
          {error && <p className="mt-4 text-sm text-destructive">{error}</p>}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Users & invitations</CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Skeleton className="h-32 w-full" />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name / Email</TableHead>
                  <TableHead>Tenant</TableHead>
                  <TableHead>Role</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {users?.members.map((member) => (
                  <TableRow key={member.id}>
                    <TableCell>{member.keycloakUserId.slice(0, 8)}...</TableCell>
                    <TableCell>{member.tenantName}</TableCell>
                    <TableCell>{member.role}</TableCell>
                    <TableCell>
                      <Badge variant={statusBadgeVariant(member.status)}>{member.status}</Badge>
                    </TableCell>
                    <TableCell>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => void handleToggleMember(member)}
                      >
                        {member.isActive ? "Disable" : "Enable"}
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
                {users?.invitations.map((invitation) => (
                  <TableRow key={invitation.id}>
                    <TableCell>
                      <div className="font-medium">
                        {invitation.firstName} {invitation.lastName}
                      </div>
                      <div className="text-xs text-muted-foreground">{invitation.email}</div>
                    </TableCell>
                    <TableCell>{invitation.tenantName}</TableCell>
                    <TableCell>{invitation.role}</TableCell>
                    <TableCell>
                      <Badge variant={statusBadgeVariant(invitation.status)}>
                        {invitation.status}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      {invitation.status !== "Registered" && invitation.status !== "Revoked" && (
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => void handleRevoke(invitation.id)}
                        >
                          Revoke
                        </Button>
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
