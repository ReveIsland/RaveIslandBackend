import { useAuth } from "react-oidc-context";
import { Navigate } from "react-router-dom";
import { Sparkles } from "lucide-react";
import { Button } from "../components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../components/ui/card";
import type { AuthRedirectState } from "../auth/authRedirect";

export function LandingPage() {
  const auth = useAuth();

  if (auth.isAuthenticated) {
    return <Navigate to="/dashboard" replace />;
  }

  const oauthError = new URLSearchParams(window.location.search).get("error_description");

  return (
    <div className="grid gap-8 md:grid-cols-[1.2fr_1fr] md:items-center">
      <div className="space-y-4">
        <p className="inline-flex items-center gap-2 rounded-full bg-primary/10 px-3 py-1 text-xs font-medium text-primary">
          <Sparkles className="h-3.5 w-3.5" /> Aspire + Keycloak + Redis
        </p>
        <h1 className="text-4xl font-bold tracking-tight md:text-5xl">
          Welcome to Rave Island
        </h1>
        <p className="max-w-xl text-muted-foreground">
          Sign in with Keycloak to access your dashboard and admin analytics backed by the API and Redis cache.
        </p>
        {(auth.error || oauthError) && (
          <p className="text-sm text-destructive">
            Sign-in failed: {auth.error?.message ?? oauthError}
          </p>
        )}
        {!auth.isAuthenticated && (
          <Button
            size="lg"
            onClick={() =>
              void auth.signinRedirect({
                state: { returnTo: "/dashboard" } satisfies AuthRedirectState,
              })
            }
          >
            Sign in to continue
          </Button>
        )}
      </div>
      <Card>
        <CardHeader>
          <CardTitle>What you can do</CardTitle>
          <CardDescription>
            Explore protected API routes from the React app with OIDC tokens issued by the raveisland realm.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <ul className="list-disc space-y-2 pl-5 text-sm text-muted-foreground">
            <li>View your profile on the dashboard</li>
            <li>Access admin analytics if you have the admin role</li>
            <li>Toggle light and dark mode in the admin panel</li>
          </ul>
        </CardContent>
      </Card>
    </div>
  );
}
