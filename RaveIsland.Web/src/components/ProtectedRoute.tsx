import { Outlet, useLocation } from "react-router-dom";
import { useAuth } from "react-oidc-context";
import { useEffect } from "react";
import type { AuthRedirectState } from "../auth/authRedirect";

export function ProtectedRoute() {
  const auth = useAuth();
  const location = useLocation();

  useEffect(() => {
    if (auth.isLoading || auth.isAuthenticated || auth.activeNavigator) {
      return;
    }

    const returnTo = `${location.pathname}${location.search}`;
    void auth.signinRedirect({ state: { returnTo } satisfies AuthRedirectState });
  }, [auth, location.pathname, location.search]);

  if (auth.isLoading || auth.activeNavigator === "signinRedirect") {
    return (
      <div className="flex min-h-[40vh] items-center justify-center">
        <p className="text-muted-foreground">Redirecting to sign in...</p>
      </div>
    );
  }

  if (auth.error) {
    return (
      <div className="flex min-h-[40vh] items-center justify-center">
        <p className="text-red-600">Sign-in failed: {auth.error.message}</p>
      </div>
    );
  }

  if (!auth.isAuthenticated) {
    return (
      <div className="flex min-h-[40vh] items-center justify-center">
        <p className="text-muted-foreground">Redirecting to sign in...</p>
      </div>
    );
  }

  return <Outlet />;
}
