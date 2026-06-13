export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

async function parseError(response: Response): Promise<string> {
  try {
    const data = (await response.json()) as { error?: string; detail?: string };
    return data.error ?? data.detail ?? `Request failed (${response.status})`;
  } catch {
    return `Request failed (${response.status})`;
  }
}

export async function apiFetch<T>(
  path: string,
  options: RequestInit & { token?: string } = {},
): Promise<T> {
  const { token, headers, ...rest } = options;
  const response = await fetch(path, {
    ...rest,
    headers: {
      ...(headers ?? {}),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(rest.body ? { "Content-Type": "application/json" } : {}),
    },
  });

  if (!response.ok) {
    throw new ApiError(await parseError(response), response.status);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export type Tenant = {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
  createdAt: string;
};

export type EventItem = {
  id: string;
  tenantId: string;
  tenantName?: string;
  title: string;
  description: string | null;
  createdByUserId: string;
  createdByName: string | null;
  createdAt: string;
  updatedAt: string;
};

export type UserMember = {
  id: string;
  type: "member";
  tenantId: string;
  tenantName: string;
  keycloakUserId: string;
  role: string;
  isActive: boolean;
  status: string;
  createdAt: string;
};

export type UserInvitation = {
  id: string;
  type: "invitation";
  tenantId: string;
  tenantName: string;
  keycloakUserId: string | null;
  role: string;
  isActive: boolean;
  email: string;
  firstName: string;
  lastName: string;
  status: string;
  expiresAt: string;
  sentAt: string | null;
  createdAt: string;
};

export type AdminStats = {
  viewCount: number;
  eventCount: number;
  tenantCount: number;
  userCount: number;
  pendingInvites: number;
  cached: boolean;
};

export type InvitationPreview = {
  email: string;
  firstName: string;
  lastName: string;
  intendedRole: string;
  tenantName: string;
  expiresAt: string;
  status: string;
};

export function hasRole(roles: string[], role: string) {
  return roles.some((r) => r.toLowerCase() === role.toLowerCase());
}

export function isPlatformAdmin(roles: string[]) {
  return hasRole(roles, "admin");
}

export function isTenantAdmin(roles: string[]) {
  return hasRole(roles, "tenant-admin");
}

export function isTenantMember(roles: string[]) {
  return isPlatformAdmin(roles) || isTenantAdmin(roles) || hasRole(roles, "tenant-user");
}
