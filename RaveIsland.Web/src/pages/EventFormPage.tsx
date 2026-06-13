import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useAuth } from "react-oidc-context";
import { ArrowLeft } from "lucide-react";
import { useCurrentUser } from "../auth/CurrentUserContext";
import { LookupSelect } from "../components/LookupSelect";
import { LookupMultiSelect } from "../components/LookupMultiSelect";
import {
  apiFetch,
  apiUpload,
  isPlatformAdmin,
  type ArtistItem,
  type EventItem,
  type EventScheduleItem,
  type EventTicketTypeItem,
  type EventPromoCodeItem,
  type PublishReadiness,
  type Tenant,
} from "../lib/api";
import { Button, buttonVariants } from "../components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "../components/ui/card";
import { Input } from "../components/ui/input";
import { Skeleton } from "../components/ui/skeleton";
import { Badge } from "../components/ui/badge";
import { cn } from "../lib/utils";

const TABS = [
  "basic",
  "schedule",
  "venue",
  "media",
  "lineup",
  "tickets",
  "promos",
  "policies",
  "facilities",
  "production",
  "visibility",
  "publish",
] as const;

type Tab = (typeof TABS)[number];

const selectClass = "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm";

export function EventFormPage() {
  const { eventId } = useParams<{ eventId: string }>();
  const isEdit = Boolean(eventId);
  const navigate = useNavigate();
  const auth = useAuth();
  const { profile } = useCurrentUser();
  const token = auth.user?.access_token;
  const roles = profile?.roles ?? [];
  const admin = isPlatformAdmin(roles);

  const [activeTab, setActiveTab] = useState<Tab>("basic");
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [artists, setArtists] = useState<ArtistItem[]>([]);
  const [title, setTitle] = useState("");
  const [tagline, setTagline] = useState("");
  const [description, setDescription] = useState("");
  const [eventCategoryId, setEventCategoryId] = useState("");
  const [eventStatusId, setEventStatusId] = useState("");
  const [theme, setTheme] = useState("");
  const [organizerReference, setOrganizerReference] = useState("");
  const [tenantId, setTenantId] = useState("");
  const [schedules, setSchedules] = useState<EventScheduleItem[]>([]);
  const [venueName, setVenueName] = useState("");
  const [address, setAddress] = useState("");
  const [city, setCity] = useState("");
  const [districtId, setDistrictId] = useState("");
  const [province, setProvince] = useState("");
  const [latitude, setLatitude] = useState("");
  const [longitude, setLongitude] = useState("");
  const [landmarkInstructions, setLandmarkInstructions] = useState("");
  const [venueTypeId, setVenueTypeId] = useState("");
  const [primaryGenreId, setPrimaryGenreId] = useState("");
  const [secondaryGenreId, setSecondaryGenreId] = useState("");
  const [soundSystem, setSoundSystem] = useState("");
  const [ageRestrictionId, setAgeRestrictionId] = useState("");
  const [cancellationPolicyId, setCancellationPolicyId] = useState("");
  const [entryPolicy, setEntryPolicy] = useState("");
  const [prohibitedItems, setProhibitedItems] = useState("");
  const [termsAndConditions, setTermsAndConditions] = useState("");
  const [visibilityTypeId, setVisibilityTypeId] = useState("");
  const [inviteCode, setInviteCode] = useState("");
  const [requiresApproval, setRequiresApproval] = useState(false);
  const [facilityIds, setFacilityIds] = useState<string[]>([]);
  const [productionFeatureIds, setProductionFeatureIds] = useState<string[]>([]);
  const [ticketTypes, setTicketTypes] = useState<EventTicketTypeItem[]>([]);
  const [promoCodes, setPromoCodes] = useState<EventPromoCodeItem[]>([]);
  const [lineupArtistId, setLineupArtistId] = useState("");
  const [publishReadiness, setPublishReadiness] = useState<PublishReadiness | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(isEdit);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (!token) return;
    if (admin && !isEdit) {
      apiFetch<Tenant[]>("/api/tenants", { token }).then(setTenants).catch(() => setTenants([]));
    }
    apiFetch<ArtistItem[]>("/api/artists", { token }).then(setArtists).catch(() => setArtists([]));
  }, [token, admin, isEdit]);

  useEffect(() => {
    if (!token || !isEdit || !eventId) return;
    setIsLoading(true);
    apiFetch<EventItem>(`/api/events/${eventId}`, { token })
      .then((ev) => {
        setTitle(ev.title);
        setTagline(ev.tagline ?? "");
        setDescription(ev.description ?? "");
        setEventCategoryId(ev.eventCategoryId ?? "");
        setEventStatusId(ev.eventStatusId ?? "");
        setTheme(ev.theme ?? "");
        setOrganizerReference(ev.organizerReference ?? "");
        setTenantId(ev.tenantId);
        setSchedules(ev.schedules ?? []);
        if (ev.venue) {
          setVenueName(ev.venue.venueName);
          setAddress(ev.venue.address);
          setCity(ev.venue.city);
          setDistrictId(ev.venue.districtId);
          setProvince(ev.venue.province ?? "");
          setLatitude(String(ev.venue.latitude));
          setLongitude(String(ev.venue.longitude));
          setLandmarkInstructions(ev.venue.landmarkInstructions ?? "");
        }
        setVenueTypeId(ev.venueTypeId ?? "");
        setPrimaryGenreId(ev.primaryGenreId ?? "");
        setSecondaryGenreId(ev.secondaryGenreId ?? "");
        setSoundSystem(ev.soundSystem ?? "");
        setAgeRestrictionId(ev.ageRestrictionId ?? "");
        setCancellationPolicyId(ev.cancellationPolicyId ?? "");
        setEntryPolicy(ev.entryPolicy ?? "");
        setProhibitedItems(ev.prohibitedItems ?? "");
        setTermsAndConditions(ev.termsAndConditions ?? "");
        setVisibilityTypeId(ev.visibilityTypeId ?? "");
        setInviteCode(ev.inviteCode ?? "");
        setRequiresApproval(ev.requiresApproval ?? false);
        setFacilityIds(ev.facilities?.map((f) => f.lookupValueId) ?? []);
        setProductionFeatureIds(ev.productionFeatures?.map((f) => f.lookupValueId) ?? []);
        setTicketTypes(ev.ticketTypes ?? []);
        setPromoCodes(ev.promoCodes ?? []);
        setError(null);
      })
      .catch((err: unknown) => setError(err instanceof Error ? err.message : "Failed to load event"))
      .finally(() => setIsLoading(false));
  }, [token, isEdit, eventId]);

  useEffect(() => {
    if (!token || !eventId || activeTab !== "publish") return;
    apiFetch<PublishReadiness>(`/api/events/${eventId}/publish-readiness`, { token })
      .then(setPublishReadiness)
      .catch(() => setPublishReadiness(null));
  }, [token, eventId, activeTab]);

  async function handleCreateBasic(e: React.FormEvent) {
    e.preventDefault();
    if (!token) return;
    setIsSubmitting(true);
    setError(null);
    try {
      const result = await apiFetch<{ id: string }>("/api/events", {
        method: "POST",
        token,
        body: JSON.stringify({
          title,
          description,
          eventCategoryId,
          tagline: tagline || null,
          theme: theme || null,
          eventStatusId: eventStatusId || null,
          organizerReference: organizerReference || null,
          tenantId: admin ? tenantId || null : null,
        }),
      });
      navigate(`/events/${result.id}/edit`);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to create event");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function saveBasic() {
    if (!token || !eventId) return;
    setIsSubmitting(true);
    setError(null);
    try {
      await apiFetch(`/api/events/${eventId}`, {
        method: "PATCH",
        token,
        body: JSON.stringify({
          title,
          description,
          tagline: tagline || null,
          theme: theme || null,
          eventCategoryId: eventCategoryId || null,
          eventStatusId: eventStatusId || null,
          organizerReference: organizerReference || null,
        }),
      });
      setSuccess("Basic info saved.");
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to save");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function saveSchedules() {
    if (!token || !eventId) return;
    setIsSubmitting(true);
    try {
      await apiFetch(`/api/events/${eventId}/schedules`, {
        method: "PUT",
        token,
        body: JSON.stringify({
          schedules: schedules.map((s, i) => ({
            dayNumber: s.dayNumber || i + 1,
            eventDate: s.eventDate,
            startTime: s.startTime,
            endTime: s.endTime,
            gatesOpenTime: s.gatesOpenTime || null,
            lastEntryTime: s.lastEntryTime || null,
          })),
        }),
      });
      setSuccess("Schedule saved.");
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to save schedule");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function saveVenue() {
    if (!token || !eventId) return;
    setIsSubmitting(true);
    try {
      await apiFetch(`/api/events/${eventId}/venue`, {
        method: "PUT",
        token,
        body: JSON.stringify({
          venueName,
          address,
          city,
          districtId,
          province: province || null,
          latitude: parseFloat(latitude),
          longitude: parseFloat(longitude),
          landmarkInstructions: landmarkInstructions || null,
        }),
      });
      setSuccess("Venue saved.");
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to save venue");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function savePolicies() {
    if (!token || !eventId) return;
    setIsSubmitting(true);
    try {
      await apiFetch(`/api/events/${eventId}`, {
        method: "PATCH",
        token,
        body: JSON.stringify({
          ageRestrictionId: ageRestrictionId || null,
          cancellationPolicyId: cancellationPolicyId || null,
          entryPolicy: entryPolicy || null,
          prohibitedItems: prohibitedItems || null,
          termsAndConditions: termsAndConditions || null,
        }),
      });
      setSuccess("Policies saved.");
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to save policies");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function saveProduction() {
    if (!token || !eventId) return;
    setIsSubmitting(true);
    try {
      await apiFetch(`/api/events/${eventId}`, {
        method: "PATCH",
        token,
        body: JSON.stringify({
          venueTypeId: venueTypeId || null,
          primaryGenreId: primaryGenreId || null,
          secondaryGenreId: secondaryGenreId || null,
          soundSystem: soundSystem || null,
        }),
      });
      await apiFetch(`/api/events/${eventId}/selections/Facility`, {
        method: "PUT",
        token,
        body: JSON.stringify({ lookupValueIds: facilityIds }),
      });
      await apiFetch(`/api/events/${eventId}/selections/ProductionFeature`, {
        method: "PUT",
        token,
        body: JSON.stringify({ lookupValueIds: productionFeatureIds }),
      });
      setSuccess("Production & facilities saved.");
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to save");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function saveVisibility() {
    if (!token || !eventId) return;
    setIsSubmitting(true);
    try {
      await apiFetch(`/api/events/${eventId}`, {
        method: "PATCH",
        token,
        body: JSON.stringify({
          visibilityTypeId: visibilityTypeId || null,
          inviteCode: inviteCode || null,
          requiresApproval,
        }),
      });
      setSuccess("Visibility saved.");
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to save visibility");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleMediaUpload(e: React.ChangeEvent<HTMLInputElement>, mediaType: string) {
    if (!token || !eventId || !e.target.files?.[0]) return;
    const formData = new FormData();
    formData.append("file", e.target.files[0]);
    formData.append("mediaType", mediaType);
    try {
      await apiUpload(`/api/events/${eventId}/media`, formData, token);
      setSuccess("Media uploaded.");
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Upload failed");
    }
  }

  async function publishEvent() {
    if (!token || !eventId) return;
    setIsSubmitting(true);
    try {
      await apiFetch(`/api/events/${eventId}/publish`, { method: "POST", token });
      setSuccess("Event published!");
      const readiness = await apiFetch<PublishReadiness>(`/api/events/${eventId}/publish-readiness`, { token });
      setPublishReadiness(readiness);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Publish failed");
    } finally {
      setIsSubmitting(false);
    }
  }

  function addScheduleDay() {
    setSchedules([
      ...schedules,
      {
        id: crypto.randomUUID(),
        dayNumber: schedules.length + 1,
        eventDate: new Date().toISOString().slice(0, 10),
        startTime: "20:00:00",
        endTime: "23:59:00",
        gatesOpenTime: null,
        lastEntryTime: null,
      },
    ]);
  }

  if (!isEdit) {
    return (
      <div className="mx-auto max-w-2xl space-y-6">
        <Link to="/events" className={cn(buttonVariants({ variant: "outline", size: "sm" }))}>
          <ArrowLeft className="mr-2 h-4 w-4" /> Back
        </Link>
        <Card>
          <CardHeader><CardTitle>Create event</CardTitle></CardHeader>
          <CardContent>
            <form className="grid gap-4" onSubmit={handleCreateBasic}>
              {admin && (
                <div className="space-y-2">
                  <label className="text-sm font-medium">Tenant</label>
                  <select className={selectClass} value={tenantId} onChange={(e) => setTenantId(e.target.value)} required>
                    <option value="">Select tenant...</option>
                    {tenants.map((t) => <option key={t.id} value={t.id}>{t.name}</option>)}
                  </select>
                </div>
              )}
              <Input placeholder="Event name *" value={title} onChange={(e) => setTitle(e.target.value)} required />
              <Input placeholder="Tagline" value={tagline} onChange={(e) => setTagline(e.target.value)} />
              <textarea className={cn(selectClass, "min-h-24 py-2")} placeholder="Description *" value={description} onChange={(e) => setDescription(e.target.value)} required />
              <LookupSelect typeCode="EventCategory" label="Category" value={eventCategoryId} onChange={setEventCategoryId} token={token} required />
              <Button type="submit" disabled={isSubmitting}>{isSubmitting ? "Creating..." : "Create & continue"}</Button>
              {error && <p className="text-sm text-destructive">{error}</p>}
            </form>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center gap-4">
        <Link to="/events" className={cn(buttonVariants({ variant: "outline", size: "sm" }))}>
          <ArrowLeft className="mr-2 h-4 w-4" /> Back
        </Link>
        {eventId && (
          <>
            <Link to={`/events/${eventId}/analytics`} className={cn(buttonVariants({ variant: "outline", size: "sm" }))}>
              Analytics
            </Link>
            <Link to={`/events/${eventId}/check-in`} className={cn(buttonVariants({ variant: "outline", size: "sm" }))}>
              Check-in
            </Link>
          </>
        )}
      </div>

      <div className="flex flex-wrap gap-2">
        {TABS.map((tab) => (
          <Button key={tab} size="sm" variant={activeTab === tab ? "default" : "outline"} onClick={() => { setActiveTab(tab); setSuccess(null); setError(null); }}>
            {tab.charAt(0).toUpperCase() + tab.slice(1)}
          </Button>
        ))}
      </div>

      {isLoading ? (
        <Skeleton className="h-64 w-full" />
      ) : (
        <Card>
          <CardHeader>
            <CardTitle>{title || "Event"}</CardTitle>
            <CardDescription>Manage all event details across tabs.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {activeTab === "basic" && (
              <div className="grid gap-4">
                <Input value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Event name" />
                <Input value={tagline} onChange={(e) => setTagline(e.target.value)} placeholder="Tagline" />
                <textarea className={cn(selectClass, "min-h-24 py-2")} value={description} onChange={(e) => setDescription(e.target.value)} placeholder="Description" />
                <LookupSelect typeCode="EventCategory" label="Category" value={eventCategoryId} onChange={setEventCategoryId} token={token} />
                <LookupSelect typeCode="EventStatus" label="Status" value={eventStatusId} onChange={setEventStatusId} token={token} />
                <Input value={theme} onChange={(e) => setTheme(e.target.value)} placeholder="Theme" />
                <Input value={organizerReference} onChange={(e) => setOrganizerReference(e.target.value)} placeholder="Organizer reference" />
                <Button onClick={() => void saveBasic()} disabled={isSubmitting}>Save basic info</Button>
              </div>
            )}

            {activeTab === "schedule" && (
              <div className="space-y-4">
                {schedules.map((s, idx) => (
                  <div key={s.id} className="grid gap-2 rounded border p-4 md:grid-cols-3">
                    <Input type="date" value={s.eventDate} onChange={(e) => { const n = [...schedules]; n[idx] = { ...s, eventDate: e.target.value }; setSchedules(n); }} />
                    <Input type="time" value={s.startTime.slice(0, 5)} onChange={(e) => { const n = [...schedules]; n[idx] = { ...s, startTime: e.target.value + ":00" }; setSchedules(n); }} />
                    <Input type="time" value={s.endTime.slice(0, 5)} onChange={(e) => { const n = [...schedules]; n[idx] = { ...s, endTime: e.target.value + ":00" }; setSchedules(n); }} />
                  </div>
                ))}
                <Button variant="outline" onClick={addScheduleDay}>Add day</Button>
                <Button onClick={() => void saveSchedules()} disabled={isSubmitting}>Save schedule</Button>
              </div>
            )}

            {activeTab === "venue" && (
              <div className="grid gap-4 md:grid-cols-2">
                <Input value={venueName} onChange={(e) => setVenueName(e.target.value)} placeholder="Venue name *" />
                <Input value={city} onChange={(e) => setCity(e.target.value)} placeholder="City *" />
                <Input className="md:col-span-2" value={address} onChange={(e) => setAddress(e.target.value)} placeholder="Address *" />
                <LookupSelect typeCode="District" label="District" value={districtId} onChange={setDistrictId} token={token} />
                <Input value={province} onChange={(e) => setProvince(e.target.value)} placeholder="Province" />
                <Input value={latitude} onChange={(e) => setLatitude(e.target.value)} placeholder="Latitude" type="number" step="any" />
                <Input value={longitude} onChange={(e) => setLongitude(e.target.value)} placeholder="Longitude" type="number" step="any" />
                <Input className="md:col-span-2" value={landmarkInstructions} onChange={(e) => setLandmarkInstructions(e.target.value)} placeholder="Landmark instructions" />
                <Button onClick={() => void saveVenue()} disabled={isSubmitting}>Save venue</Button>
              </div>
            )}

            {activeTab === "media" && (
              <div className="space-y-4">
                <div><label className="text-sm font-medium">Cover image</label><Input type="file" accept="image/*" onChange={(e) => void handleMediaUpload(e, "Cover")} /></div>
                <div><label className="text-sm font-medium">Banner</label><Input type="file" accept="image/*" onChange={(e) => void handleMediaUpload(e, "Banner")} /></div>
                <div><label className="text-sm font-medium">Gallery</label><Input type="file" accept="image/*" onChange={(e) => void handleMediaUpload(e, "Gallery")} /></div>
              </div>
            )}

            {activeTab === "lineup" && (
              <div className="space-y-4">
                <select className={selectClass} value={lineupArtistId} onChange={(e) => setLineupArtistId(e.target.value)}>
                  <option value="">Select artist...</option>
                  {artists.map((a) => <option key={a.id} value={a.id}>{a.stageName || a.name}</option>)}
                </select>
                <Button onClick={async () => {
                  if (!token || !eventId || !lineupArtistId) return;
                  await apiFetch(`/api/events/${eventId}/lineup`, {
                    method: "PUT",
                    token,
                    body: JSON.stringify({ artists: [{ artistId: lineupArtistId, stageNameOverride: null, primaryGenreId: null, setStart: null, setEnd: null, displayOrder: 1 }] }),
                  });
                  setSuccess("Lineup updated.");
                }}>Add to lineup</Button>
              </div>
            )}

            {activeTab === "tickets" && (
              <div className="space-y-4">
                <Button variant="outline" onClick={() => setTicketTypes([...ticketTypes, { id: "", name: "General Admission", description: null, price: 0, quantity: 100, quantitySold: 0, saleStart: null, saleEnd: null, maxPerUser: null, isActive: true, displayOrder: ticketTypes.length + 1 }])}>
                  Add ticket type
                </Button>
                {ticketTypes.map((t, idx) => (
                  <div key={idx} className="grid gap-2 rounded border p-4 md:grid-cols-3">
                    <Input value={t.name} onChange={(e) => { const n = [...ticketTypes]; n[idx] = { ...t, name: e.target.value }; setTicketTypes(n); }} placeholder="Name" />
                    <Input type="number" value={t.price} onChange={(e) => { const n = [...ticketTypes]; n[idx] = { ...t, price: parseFloat(e.target.value) }; setTicketTypes(n); }} placeholder="Price" />
                    <Input type="number" value={t.quantity} onChange={(e) => { const n = [...ticketTypes]; n[idx] = { ...t, quantity: parseInt(e.target.value) }; setTicketTypes(n); }} placeholder="Quantity" />
                  </div>
                ))}
                <Button onClick={async () => {
                  if (!token || !eventId) return;
                  await apiFetch(`/api/events/${eventId}/ticket-types`, {
                    method: "PUT",
                    token,
                    body: JSON.stringify({ ticketTypes: ticketTypes.map((t, i) => ({ id: t.id || null, name: t.name, description: t.description, price: t.price, quantity: t.quantity, saleStart: t.saleStart, saleEnd: t.saleEnd, maxPerUser: t.maxPerUser, isActive: t.isActive, defaultLookupValueId: null, displayOrder: i + 1 })) }),
                  });
                  setSuccess("Ticket types saved.");
                }}>Save ticket types</Button>
              </div>
            )}

            {activeTab === "promos" && (
              <div className="space-y-4">
                <Button variant="outline" onClick={() => setPromoCodes([...promoCodes, { id: "", code: "SAVE10", discountType: "Percent", discountValue: 10, expiresAt: null, usageLimit: 100, usageCount: 0, isActive: true }])}>
                  Add promo code
                </Button>
                {promoCodes.map((p, idx) => (
                  <div key={idx} className="grid gap-2 rounded border p-4 md:grid-cols-3">
                    <Input value={p.code} onChange={(e) => { const n = [...promoCodes]; n[idx] = { ...p, code: e.target.value }; setPromoCodes(n); }} />
                    <Input type="number" value={p.discountValue} onChange={(e) => { const n = [...promoCodes]; n[idx] = { ...p, discountValue: parseFloat(e.target.value) }; setPromoCodes(n); }} />
                  </div>
                ))}
                <Button onClick={async () => {
                  if (!token || !eventId) return;
                  await apiFetch(`/api/events/${eventId}/promo-codes`, {
                    method: "PUT",
                    token,
                    body: JSON.stringify({ promoCodes: promoCodes.map((p) => ({ id: p.id || null, code: p.code, discountType: p.discountType === "Percent" ? 1 : 2, discountValue: p.discountValue, expiresAt: p.expiresAt, usageLimit: p.usageLimit, isActive: p.isActive, appliesToTicketTypeIdsJson: null })) }),
                  });
                  setSuccess("Promo codes saved.");
                }}>Save promo codes</Button>
              </div>
            )}

            {activeTab === "policies" && (
              <div className="grid gap-4">
                <LookupSelect typeCode="AgeRestriction" label="Age restriction" value={ageRestrictionId} onChange={setAgeRestrictionId} token={token} />
                <LookupSelect typeCode="CancellationPolicy" label="Cancellation policy" value={cancellationPolicyId} onChange={setCancellationPolicyId} token={token} />
                <textarea className={cn(selectClass, "min-h-20 py-2")} value={entryPolicy} onChange={(e) => setEntryPolicy(e.target.value)} placeholder="Entry policy" />
                <textarea className={cn(selectClass, "min-h-20 py-2")} value={prohibitedItems} onChange={(e) => setProhibitedItems(e.target.value)} placeholder="Prohibited items" />
                <textarea className={cn(selectClass, "min-h-20 py-2")} value={termsAndConditions} onChange={(e) => setTermsAndConditions(e.target.value)} placeholder="Terms & conditions" />
                <Button onClick={() => void savePolicies()} disabled={isSubmitting}>Save policies</Button>
              </div>
            )}

            {activeTab === "facilities" && (
              <div className="space-y-4">
                <LookupMultiSelect typeCode="Facility" label="Facilities" selectedIds={facilityIds} onChange={setFacilityIds} token={token} />
                <Button onClick={async () => {
                  if (!token || !eventId) return;
                  await apiFetch(`/api/events/${eventId}/selections/Facility`, {
                    method: "PUT",
                    token,
                    body: JSON.stringify({ lookupValueIds: facilityIds }),
                  });
                  setSuccess("Facilities saved.");
                }}>Save facilities</Button>
              </div>
            )}

            {activeTab === "production" && (
              <div className="grid gap-4">
                <LookupSelect typeCode="VenueType" label="Venue type" value={venueTypeId} onChange={setVenueTypeId} token={token} />
                <LookupSelect typeCode="MusicGenre" label="Primary genre" value={primaryGenreId} onChange={setPrimaryGenreId} token={token} />
                <LookupSelect typeCode="MusicGenre" label="Secondary genre" value={secondaryGenreId} onChange={setSecondaryGenreId} token={token} />
                <Input value={soundSystem} onChange={(e) => setSoundSystem(e.target.value)} placeholder="Sound system" />
                <LookupMultiSelect typeCode="ProductionFeature" label="Production features" selectedIds={productionFeatureIds} onChange={setProductionFeatureIds} token={token} />
                <Button onClick={() => void saveProduction()} disabled={isSubmitting}>Save production</Button>
              </div>
            )}

            {activeTab === "visibility" && (
              <div className="grid gap-4">
                <LookupSelect typeCode="EventVisibility" label="Visibility" value={visibilityTypeId} onChange={setVisibilityTypeId} token={token} />
                <Input value={inviteCode} onChange={(e) => setInviteCode(e.target.value)} placeholder="Invite code" />
                <label className="flex items-center gap-2 text-sm">
                  <input type="checkbox" checked={requiresApproval} onChange={(e) => setRequiresApproval(e.target.checked)} />
                  Requires approval
                </label>
                <Button onClick={() => void saveVisibility()} disabled={isSubmitting}>Save visibility</Button>
              </div>
            )}

            {activeTab === "publish" && (
              <div className="space-y-4">
                {publishReadiness && (
                  <div>
                    <Badge variant={publishReadiness.isReady ? "secondary" : "outline"}>
                      {publishReadiness.isReady ? "Ready to publish" : "Not ready"}
                    </Badge>
                    {publishReadiness.errors.length > 0 && (
                      <ul className="mt-2 list-inside list-disc text-sm text-muted-foreground">
                        {publishReadiness.errors.map((err) => <li key={err}>{err}</li>)}
                      </ul>
                    )}
                  </div>
                )}
                <Button onClick={() => void publishEvent()} disabled={isSubmitting || publishReadiness?.isReady === false}>
                  Publish event
                </Button>
              </div>
            )}

            {success && <p className="text-sm text-green-600">{success}</p>}
            {error && <p className="text-sm text-destructive">{error}</p>}
          </CardContent>
        </Card>
      )}
    </div>
  );
}
