import { useEffect, useState } from "react";
import { apiFetch, type LookupValue } from "../lib/api";

const cache = new Map<string, LookupValue[]>();

export function useLookupValues(typeCode: string, token?: string, includeInactive = false) {
  const [values, setValues] = useState<LookupValue[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const cacheKey = `${typeCode}:${includeInactive}`;
    const cached = cache.get(cacheKey);
    if (cached) {
      setValues(cached);
      setIsLoading(false);
      return;
    }

    setIsLoading(true);
    const path = includeInactive
      ? `/api/lookups/${typeCode}/all`
      : `/api/lookups/${typeCode}`;

    apiFetch<LookupValue[]>(path, { token })
      .then((data) => {
        cache.set(cacheKey, data);
        setValues(data);
        setError(null);
      })
      .catch((err: unknown) => {
        setError(err instanceof Error ? err.message : "Failed to load lookup values");
        setValues([]);
      })
      .finally(() => setIsLoading(false));
  }, [typeCode, token, includeInactive]);

  return { values, isLoading, error };
}

export function invalidateLookupCache(typeCode?: string) {
  if (typeCode) {
    for (const key of cache.keys()) {
      if (key.startsWith(`${typeCode}:`)) {
        cache.delete(key);
      }
    }
  } else {
    cache.clear();
  }
}
