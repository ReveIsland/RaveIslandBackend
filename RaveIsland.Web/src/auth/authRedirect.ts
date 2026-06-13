export type AuthRedirectState = {
  returnTo?: string;
};

export function getPostLoginPath(state: unknown): string {
  if (typeof state !== "object" || state === null) {
    return "/dashboard";
  }

  const returnTo = (state as AuthRedirectState).returnTo;
  if (typeof returnTo === "string" && returnTo.startsWith("/")) {
    return returnTo;
  }

  return "/dashboard";
}
