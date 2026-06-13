import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { useAuth } from "react-oidc-context";

export type CurrentUserProfile = {
  name: string;
  email: string | null;
  roles: string[];
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
};

export function CurrentUserProvider({ children }: { children: ReactNode }) {
  const auth = useAuth();
  const [profile, setProfile] = useState<CurrentUserProfile | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [refreshKey, setRefreshKey] = useState(0);

  useEffect(() => {
    const token = auth.user?.access_token;
    if (!token) {
      setProfile(null);
      setIsLoading(false);
      setError(null);
      return;
    }

    let cancelled = false;
    setIsLoading(true);
    setError(null);

    fetch("/api/me", {
      headers: { Authorization: `Bearer ${token}` },
    })
      .then(async (response) => {
        if (!response.ok) {
          throw new Error(`Profile API returned ${response.status}`);
        }
        return response.json() as Promise<MeResponse>;
      })
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
      refresh: () => setRefreshKey((current) => current + 1),
    }),
    [profile, isLoading, error],
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
