import { Mail, Shield, UserRound } from "lucide-react";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "../components/ui/card";
import { Badge } from "../components/ui/badge";
import { Skeleton } from "../components/ui/skeleton";
import { useCurrentUser } from "../auth/CurrentUserContext";

export function ProfileSettingsPage() {
  const { profile, isLoading, error } = useCurrentUser();

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <div>
        <h2 className="text-2xl font-bold tracking-tight">Profile settings</h2>
        <p className="text-muted-foreground">
          View your account details from Keycloak. Profile changes are managed by your identity provider.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <UserRound className="h-5 w-5" />
            Account
          </CardTitle>
          <CardDescription>Information loaded from `/api/me`.</CardDescription>
        </CardHeader>
        <CardContent>
          {error && <p className="text-sm text-destructive">{error}</p>}

          {isLoading && !error && (
            <div className="space-y-3">
              <Skeleton className="h-4 w-2/3" />
              <Skeleton className="h-4 w-1/2" />
              <Skeleton className="h-6 w-1/3" />
            </div>
          )}

          {!isLoading && !error && profile && (
            <dl className="grid gap-5 sm:grid-cols-2">
              <div>
                <dt className="flex items-center gap-2 text-sm text-muted-foreground">
                  <UserRound className="h-4 w-4" />
                  Display name
                </dt>
                <dd className="mt-1 font-medium">{profile.name}</dd>
              </div>
              <div>
                <dt className="flex items-center gap-2 text-sm text-muted-foreground">
                  <Mail className="h-4 w-4" />
                  Email
                </dt>
                <dd className="mt-1 font-medium">{profile.email ?? "Not provided"}</dd>
              </div>
              <div className="sm:col-span-2">
                <dt className="flex items-center gap-2 text-sm text-muted-foreground">
                  <Shield className="h-4 w-4" />
                  Roles
                </dt>
                <dd className="mt-2 flex flex-wrap gap-2">
                  {profile.roles.length > 0 ? (
                    profile.roles.map((role) => (
                      <Badge key={role} variant="secondary">
                        {role}
                      </Badge>
                    ))
                  ) : (
                    <Badge variant="outline">None</Badge>
                  )}
                </dd>
              </div>
              {profile.tenantName && (
                <div className="sm:col-span-2">
                  <dt className="text-sm text-muted-foreground">Tenant</dt>
                  <dd className="mt-1 font-medium">{profile.tenantName}</dd>
                </div>
              )}
            </dl>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
