import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { useAuth } from "react-oidc-context";
import { apiFetch } from "../lib/api";

export type OrganizationSubscription = {
  planName: string | null;
  status: string | null;
  isSubscribed: boolean;
};

export type CurrentUserProfile = {
  name: string;
  email: string | null;
  roles: string[];
  tenantId: string | null;
  tenantName: string | null;
  organizationSubscription: OrganizationSubscription | null;
};

type CurrentUserContextValue = {
  profile: CurrentUserProfile | null;
  isLoading: boolean;
  error: string | null;
  refresh: () => void;
};

const CurrentUserContext = createContext<CurrentUserContextValue | null>(null);

type MeResponse = {
  name?: string | null;
  email?: string | null;
  roles?: string[];
  tenantId?: string | null;
  tenantName?: string | null;
  organizationSubscription?: OrganizationSubscription | null;
};

export function CurrentUserProvider({ children }: { children: ReactNode }) {
  const auth = useAuth();
  const [profile, setProfile] = useState<CurrentUserProfile | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [refreshKey, setRefreshKey] = useState(0);
  const hasLoadedRef = useRef(false);
  const refresh = useCallback(() => setRefreshKey((current) => current + 1), []);

  useEffect(() => {
    const token = auth.user?.access_token;
    if (!token) {
      hasLoadedRef.current = false;
      setProfile(null);
      setIsLoading(false);
      setError(null);
      return;
    }

    let cancelled = false;
    if (!hasLoadedRef.current) {
      setIsLoading(true);
    }
    setError(null);

    apiFetch<MeResponse>("/api/me", { token })
      .then((data) => {
        if (cancelled) {
          return;
        }

        const fallbackName =
          typeof auth.user?.profile?.preferred_username === "string"
            ? auth.user.profile.preferred_username
            : typeof auth.user?.profile?.name === "string"
              ? auth.user.profile.name
              : "User";

        setProfile({
          name: data.name?.trim() || fallbackName,
          email: data.email?.trim() || null,
          roles: data.roles ?? [],
          tenantId: data.tenantId ?? null,
          tenantName: data.tenantName ?? null,
          organizationSubscription: data.organizationSubscription ?? null,
        });
      })
      .catch((err: unknown) => {
        if (cancelled) {
          return;
        }

        setError(err instanceof Error ? err.message : "Failed to load profile");
        setProfile(null);
      })
      .finally(() => {
        if (!cancelled) {
          hasLoadedRef.current = true;
          setIsLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [auth.user?.access_token, auth.user?.profile, refreshKey]);

  const value = useMemo(
    () => ({
      profile,
      isLoading,
      error,
      refresh,
    }),
    [profile, isLoading, error, refresh],
  );

  return <CurrentUserContext.Provider value={value}>{children}</CurrentUserContext.Provider>;
}

export function useCurrentUser() {
  const context = useContext(CurrentUserContext);
  if (!context) {
    throw new Error("useCurrentUser must be used within CurrentUserProvider");
  }

  return context;
}
