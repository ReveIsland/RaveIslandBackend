import { useEffect, useState, type ReactNode } from "react";
import { AuthProvider } from "react-oidc-context";
import type { UserManagerSettings } from "oidc-client-ts";
import { getPostLoginPath } from "./authRedirect";

type AuthConfig = {
  authority: string;
  clientId: string;
  realm: string;
  scope: string;
};

export function RaveAuthProvider({ children }: { children: ReactNode }) {
  const [settings, setSettings] = useState<UserManagerSettings | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetch("/api/auth/config")
      .then((response) => {
        if (!response.ok) {
          throw new Error(`Failed to load auth config (${response.status})`);
        }
        return response.json() as Promise<AuthConfig>;
      })
      .then((config) => {
        setSettings({
          authority: config.authority,
          client_id: config.clientId,
          redirect_uri: `${window.location.origin}/`,
          post_logout_redirect_uri: `${window.location.origin}/`,
          response_type: "code",
          scope: config.scope ?? "openid roles",
          loadUserInfo: true,
          automaticSilentRenew: true,
        });
      })
      .catch((err: unknown) => {
        setError(err instanceof Error ? err.message : "Unknown error");
      });
  }, []);

  if (error) {
    return (
      <div className="flex min-h-screen items-center justify-center p-6">
        <p className="text-red-600">Auth configuration error: {error}</p>
      </div>
    );
  }

  if (!settings) {
    return (
      <div className="flex min-h-screen items-center justify-center p-6">
        <p className="text-muted-foreground">Loading authentication...</p>
      </div>
    );
  }

  return (
    <AuthProvider
      {...settings}
      onSigninCallback={(user) => {
        window.history.replaceState({}, document.title, getPostLoginPath(user?.state));
      }}
    >
      {children}
    </AuthProvider>
  );
}
