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
  tagline?: string | null;
  description: string;
  eventCategoryId?: string;
  eventCategoryName?: string;
  eventStatusId?: string;
  eventStatusName?: string;
  theme?: string | null;
  organizerReference?: string | null;
  venueTypeId?: string | null;
  primaryGenreId?: string | null;
  secondaryGenreId?: string | null;
  soundSystem?: string | null;
  ageRestrictionId?: string | null;
  cancellationPolicyId?: string | null;
  entryPolicy?: string | null;
  prohibitedItems?: string | null;
  termsAndConditions?: string | null;
  visibilityTypeId?: string;
  visibilityTypeName?: string;
  inviteCode?: string | null;
  requiresApproval?: boolean;
  slug?: string | null;
  metaTitle?: string | null;
  metaDescription?: string | null;
  createdByUserId: string;
  createdByName: string | null;
  createdAt: string;
  updatedAt: string;
  venue?: EventVenue | null;
  schedules?: EventScheduleItem[];
  media?: EventMediaItem[];
  artists?: EventLineupItem[];
  ticketTypes?: EventTicketTypeItem[];
  promoCodes?: EventPromoCodeItem[];
  facilities?: { lookupValueId: string; name: string }[];
  productionFeatures?: { lookupValueId: string; name: string }[];
};

export type EventVenue = {
  id: string;
  venueName: string;
  address: string;
  city: string;
  districtId: string;
  districtName?: string;
  province?: string | null;
  googleMapsUrl?: string | null;
  latitude: number;
  longitude: number;
  landmarkInstructions?: string | null;
};

export type EventScheduleItem = {
  id: string;
  dayNumber: number;
  eventDate: string;
  startTime: string;
  endTime: string;
  gatesOpenTime?: string | null;
  lastEntryTime?: string | null;
};

export type EventMediaItem = {
  id: string;
  mediaType: string;
  storageUrl: string;
  thumbnailUrl?: string | null;
  displayOrder: number;
  fileName: string;
};

export type EventLineupItem = {
  id: string;
  artistId: string;
  artistName?: string;
  stageName?: string;
  setStart?: string | null;
  setEnd?: string | null;
  displayOrder: number;
  primaryGenreId?: string | null;
};

export type EventTicketTypeItem = {
  id: string;
  name: string;
  description?: string | null;
  price: number;
  quantity: number;
  quantitySold: number;
  saleStart?: string | null;
  saleEnd?: string | null;
  maxPerUser?: number | null;
  isActive: boolean;
  displayOrder: number;
};

export type EventPromoCodeItem = {
  id: string;
  code: string;
  discountType: string;
  discountValue: number;
  expiresAt?: string | null;
  usageLimit?: number | null;
  usageCount: number;
  isActive: boolean;
};

export type ArtistItem = {
  id: string;
  tenantId: string;
  name: string;
  stageName?: string | null;
  bio?: string | null;
  profileImageUrl?: string | null;
  artistTypeId?: string | null;
  primaryGenreId?: string | null;
};

export type LookupType = {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  isSystem: boolean;
  createdAt: string;
};

export type LookupValue = {
  id: string;
  lookupTypeId: string;
  code: string;
  name: string;
  displayOrder: number;
  isActive: boolean;
  isSystem: boolean;
  iconUrl?: string | null;
  metadataJson?: string | null;
};

export type EventAnalytics = {
  ticketsSold: number;
  totalCapacity: number;
  revenue: number;
  attendance: number;
  conversionRate: number;
  promoUsage: number;
  ticketTypeBreakdown: Array<{
    id: string;
    name: string;
    quantity: number;
    quantitySold: number;
    revenue: number;
  }>;
};

export type PublishReadiness = {
  isReady: boolean;
  errors: string[];
};

export async function apiUpload<T>(
  path: string,
  formData: FormData,
  token?: string,
): Promise<T> {
  const response = await fetch(path, {
    method: "POST",
    headers: token ? { Authorization: `Bearer ${token}` } : {},
    body: formData,
  });

  if (!response.ok) {
    throw new ApiError(await parseError(response), response.status);
  }

  return response.json() as Promise<T>;
}

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
