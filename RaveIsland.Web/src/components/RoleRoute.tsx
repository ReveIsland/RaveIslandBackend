import { Navigate, Outlet } from "react-router-dom";
import { useCurrentUser } from "../auth/CurrentUserContext";
import { hasRole } from "../lib/api";

type RoleRouteProps = {
  anyOf: string[];
  fallback?: string;
};

export function RoleRoute({ anyOf, fallback = "/dashboard" }: RoleRouteProps) {
  const { profile, isLoading } = useCurrentUser();

  if (isLoading) {
    return (
      <div className="flex min-h-[40vh] items-center justify-center text-sm text-muted-foreground">
        Checking permissions...
      </div>
    );
  }

  const roles = profile?.roles ?? [];
  const allowed = anyOf.some((role) => hasRole(roles, role));

  if (!allowed) {
    return <Navigate to={fallback} replace />;
  }

  return <Outlet />;
}
