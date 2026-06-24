import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useAuth } from "react-oidc-context";
import { ArrowLeft, CheckCircle2, Plus, AlertCircle } from "lucide-react";
import { useCurrentUser } from "../auth/CurrentUserContext";
import { LookupSelect } from "../components/LookupSelect";
import { LookupMultiSelect } from "../components/LookupMultiSelect";
import { EventEditorHeader } from "../components/events/EventEditorHeader";
import {
  EventFormNav,
  EventFormNavMobile,
  getTabConfig,
  type EventTab,
} from "../components/events/EventFormNav";
import { FormField, FormSection } from "../components/layout/FormField";
import { PageHeader } from "../components/layout/PageHeader";
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
import { useLookupValues } from "../hooks/useLookupValues";
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

const selectClass =
  "glass-subtle flex h-11 w-full rounded-xl px-4 py-2 text-sm transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/50";

const textareaClass = cn(selectClass, "min-h-28 resize-y py-2.5 leading-relaxed");

export function EventFormPage() {
  const { eventId } = useParams<{ eventId: string }>();
  const isEdit = Boolean(eventId);
  const navigate = useNavigate();
  const auth = useAuth();
  const { profile } = useCurrentUser();
  const token = auth.user?.access_token;
  const roles = profile?.roles ?? [];
  const admin = isPlatformAdmin(roles);

  const [activeTab, setActiveTab] = useState<EventTab>("basic");
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

  const { values: eventStatuses } = useLookupValues("EventStatus", token);
  const statusLabel = useMemo(
    () => eventStatuses.find((s) => s.id === eventStatusId)?.name,
    [eventStatuses, eventStatusId],
  );
  const activeTabConfig = getTabConfig(activeTab);

  function switchTab(tab: EventTab) {
    setActiveTab(tab);
    setSuccess(null);
    setError(null);
  }

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
        <PageHeader
          title="Create event"
          description="Start with the essentials — you can add schedule, lineup, and tickets after."
          actions={
            <Link to="/events" className={cn(buttonVariants({ variant: "outline", size: "sm" }))}>
              <ArrowLeft className="mr-2 h-4 w-4" /> Back
            </Link>
          }
        />
        <Card className="event-editor-panel overflow-hidden">
          <CardHeader>
            <CardTitle>New event</CardTitle>
            <CardDescription>
              Give your party a name and category to get started.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form className="grid gap-5" onSubmit={handleCreateBasic}>
              {admin && (
                <FormField label="Tenant" description="Which organizer owns this event?" required>
                  <select
                    className={selectClass}
                    value={tenantId}
                    onChange={(e) => setTenantId(e.target.value)}
                    required
                  >
                    <option value="">Select tenant...</option>
                    {tenants.map((t) => (
                      <option key={t.id} value={t.id}>
                        {t.name}
                      </option>
                    ))}
                  </select>
                </FormField>
              )}
              <FormField label="Event name" required>
                <Input
                  placeholder="e.g. Sunset Sessions Vol. 3"
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  required
                />
              </FormField>
              <FormField label="Tagline" description="A short hook for listings and social">
                <Input
                  placeholder="Where the night meets the horizon"
                  value={tagline}
                  onChange={(e) => setTagline(e.target.value)}
                />
              </FormField>
              <FormField label="Description" required>
                <textarea
                  className={textareaClass}
                  placeholder="Describe the vibe, music, and what guests can expect..."
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  required
                />
              </FormField>
              <LookupSelect
                typeCode="EventCategory"
                label="Category"
                value={eventCategoryId}
                onChange={setEventCategoryId}
                token={token}
                required
              />
              <Button type="submit" disabled={isSubmitting} className="w-full sm:w-auto">
                {isSubmitting ? "Creating..." : "Create & continue"}
              </Button>
              {error && (
                <p className="flex items-center gap-2 text-sm text-destructive">
                  <AlertCircle className="h-4 w-4 shrink-0" />
                  {error}
                </p>
              )}
            </form>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {eventId && (
        <EventEditorHeader
          eventId={eventId}
          title={title}
          tagline={tagline}
          statusLabel={statusLabel}
          theme={theme}
        />
      )}

      {(success || error) && (
        <div
          className={cn(
            "flex items-start gap-3 rounded-lg border px-4 py-3 text-sm",
            success
              ? "border-emerald-500/30 bg-emerald-500/10 text-emerald-800 dark:text-emerald-300"
              : "border-destructive/30 bg-destructive/10 text-destructive",
          )}
        >
          {success ? (
            <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0" />
          ) : (
            <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
          )}
          <span>{success ?? error}</span>
        </div>
      )}

      {isLoading ? (
        <div className="grid gap-6 lg:grid-cols-[240px_1fr]">
          <Skeleton className="hidden h-96 lg:block" />
          <Skeleton className="h-96 w-full" />
        </div>
      ) : (
        <div className="grid gap-6 lg:grid-cols-[240px_1fr]">
          <aside className="hidden lg:block">
            <div className="glass-strong sticky top-24 rounded-2xl p-3">
              <EventFormNav activeTab={activeTab} onTabChange={switchTab} />
            </div>
          </aside>

          <div className="min-w-0 space-y-4">
            <EventFormNavMobile activeTab={activeTab} onTabChange={switchTab} />

            <Card className="event-editor-panel overflow-hidden border-border/80 shadow-sm">
              <CardContent className="p-5 sm:p-6 md:p-8">
                {activeTab === "basic" && (
                  <FormSection
                    title={activeTabConfig.label}
                    description="The identity of your event — what guests see first."
                    footer={
                      <div className="flex justify-end border-t border-border/80 pt-6">
                        <Button onClick={() => void saveBasic()} disabled={isSubmitting}>
                          {isSubmitting ? "Saving..." : "Save basic info"}
                        </Button>
                      </div>
                    }
                  >
                    <FormField label="Event name" required>
                      <Input
                        value={title}
                        onChange={(e) => setTitle(e.target.value)}
                        placeholder="e.g. Sunset Sessions Vol. 3"
                      />
                    </FormField>
                    <FormField
                      label="Tagline"
                      description="One line that captures the energy of the night."
                    >
                      <Input
                        value={tagline}
                        onChange={(e) => setTagline(e.target.value)}
                        placeholder="Where the night meets the horizon"
                      />
                    </FormField>
                    <FormField label="Description" description="Tell the full story — vibe, music, dress code, highlights.">
                      <textarea
                        className={textareaClass}
                        value={description}
                        onChange={(e) => setDescription(e.target.value)}
                        placeholder="Describe what makes this event unmissable..."
                      />
                    </FormField>
                    <div className="grid gap-5 sm:grid-cols-2">
                      <LookupSelect
                        typeCode="EventCategory"
                        label="Category"
                        value={eventCategoryId}
                        onChange={setEventCategoryId}
                        token={token}
                      />
                      <LookupSelect
                        typeCode="EventStatus"
                        label="Status"
                        value={eventStatusId}
                        onChange={setEventStatusId}
                        token={token}
                      />
                      <FormField label="Theme" description="Visual or creative direction for branding.">
                        <Input
                          value={theme}
                          onChange={(e) => setTheme(e.target.value)}
                          placeholder="e.g. Neon Oasis, Tropical House"
                        />
                      </FormField>
                      <FormField label="Organizer reference" description="Internal ID or contract reference.">
                        <Input
                          value={organizerReference}
                          onChange={(e) => setOrganizerReference(e.target.value)}
                          placeholder="Optional reference code"
                        />
                      </FormField>
                    </div>
                  </FormSection>
                )}

                {activeTab === "schedule" && (
                  <FormSection
                    title={activeTabConfig.label}
                    description="Set dates, doors, and set times for each day."
                    footer={
                      <div className="flex flex-wrap items-center justify-between gap-3 border-t border-border/80 pt-6">
                        <Button variant="outline" onClick={addScheduleDay}>
                          <Plus className="mr-2 h-4 w-4" />
                          Add day
                        </Button>
                        <Button onClick={() => void saveSchedules()} disabled={isSubmitting}>
                          {isSubmitting ? "Saving..." : "Save schedule"}
                        </Button>
                      </div>
                    }
                  >
                    {schedules.length === 0 ? (
                      <p className="rounded-lg border border-dashed border-border/80 bg-muted/20 px-4 py-8 text-center text-sm text-muted-foreground">
                        No schedule days yet. Add your first date to get started.
                      </p>
                    ) : (
                      schedules.map((s, idx) => (
                        <div key={s.id} className="form-item-card space-y-4">
                          <p className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                            Day {s.dayNumber || idx + 1}
                          </p>
                          <div className="grid gap-4 sm:grid-cols-3">
                            <FormField label="Date">
                              <Input
                                type="date"
                                value={s.eventDate}
                                onChange={(e) => {
                                  const n = [...schedules];
                                  n[idx] = { ...s, eventDate: e.target.value };
                                  setSchedules(n);
                                }}
                              />
                            </FormField>
                            <FormField label="Start">
                              <Input
                                type="time"
                                value={s.startTime.slice(0, 5)}
                                onChange={(e) => {
                                  const n = [...schedules];
                                  n[idx] = { ...s, startTime: e.target.value + ":00" };
                                  setSchedules(n);
                                }}
                              />
                            </FormField>
                            <FormField label="End">
                              <Input
                                type="time"
                                value={s.endTime.slice(0, 5)}
                                onChange={(e) => {
                                  const n = [...schedules];
                                  n[idx] = { ...s, endTime: e.target.value + ":00" };
                                  setSchedules(n);
                                }}
                              />
                            </FormField>
                          </div>
                        </div>
                      ))
                    )}
                  </FormSection>
                )}

                {activeTab === "venue" && (
                  <FormSection
                    title={activeTabConfig.label}
                    description="Where the party happens — help guests find you."
                    footer={
                      <div className="flex justify-end border-t border-border/80 pt-6">
                        <Button onClick={() => void saveVenue()} disabled={isSubmitting}>
                          {isSubmitting ? "Saving..." : "Save venue"}
                        </Button>
                      </div>
                    }
                  >
                    <div className="grid gap-5 sm:grid-cols-2">
                      <FormField label="Venue name" required>
                        <Input
                          value={venueName}
                          onChange={(e) => setVenueName(e.target.value)}
                          placeholder="e.g. The Warehouse"
                        />
                      </FormField>
                      <FormField label="City" required>
                        <Input
                          value={city}
                          onChange={(e) => setCity(e.target.value)}
                          placeholder="City"
                        />
                      </FormField>
                      <FormField label="Address" required className="sm:col-span-2">
                        <Input
                          value={address}
                          onChange={(e) => setAddress(e.target.value)}
                          placeholder="Street address"
                        />
                      </FormField>
                      <LookupSelect
                        typeCode="District"
                        label="District"
                        value={districtId}
                        onChange={setDistrictId}
                        token={token}
                      />
                      <FormField label="Province">
                        <Input
                          value={province}
                          onChange={(e) => setProvince(e.target.value)}
                          placeholder="Province or region"
                        />
                      </FormField>
                      <FormField label="Latitude">
                        <Input
                          value={latitude}
                          onChange={(e) => setLatitude(e.target.value)}
                          placeholder="0.000000"
                          type="number"
                          step="any"
                        />
                      </FormField>
                      <FormField label="Longitude">
                        <Input
                          value={longitude}
                          onChange={(e) => setLongitude(e.target.value)}
                          placeholder="0.000000"
                          type="number"
                          step="any"
                        />
                      </FormField>
                      <FormField
                        label="Landmark instructions"
                        description="Help guests navigate the last mile."
                        className="sm:col-span-2"
                      >
                        <Input
                          value={landmarkInstructions}
                          onChange={(e) => setLandmarkInstructions(e.target.value)}
                          placeholder="e.g. Enter through the blue gate behind the parking lot"
                        />
                      </FormField>
                    </div>
                  </FormSection>
                )}

                {activeTab === "media" && (
                  <FormSection
                    title={activeTabConfig.label}
                    description="Visual assets that sell the experience."
                  >
                    <div className="grid gap-5 sm:grid-cols-3">
                      {[
                        { label: "Cover image", type: "Cover", hint: "Square or portrait, used in listings" },
                        { label: "Banner", type: "Banner", hint: "Wide hero image for the event page" },
                        { label: "Gallery", type: "Gallery", hint: "Additional photos from past editions" },
                      ].map((item) => (
                        <div key={item.type} className="form-item-card space-y-3">
                          <FormField label={item.label} description={item.hint}>
                            <Input
                              type="file"
                              accept="image/*"
                              className="cursor-pointer file:mr-3 file:rounded-md file:border-0 file:bg-primary/10 file:px-3 file:py-1.5 file:text-xs file:font-medium file:text-primary"
                              onChange={(e) => void handleMediaUpload(e, item.type)}
                            />
                          </FormField>
                        </div>
                      ))}
                    </div>
                  </FormSection>
                )}

                {activeTab === "lineup" && (
                  <FormSection
                    title={activeTabConfig.label}
                    description="Build the bill — artists and set order."
                    footer={
                      <div className="flex justify-end border-t border-border/80 pt-6">
                        <Button
                          onClick={async () => {
                            if (!token || !eventId || !lineupArtistId) return;
                            await apiFetch(`/api/events/${eventId}/lineup`, {
                              method: "PUT",
                              token,
                              body: JSON.stringify({
                                artists: [
                                  {
                                    artistId: lineupArtistId,
                                    stageNameOverride: null,
                                    primaryGenreId: null,
                                    setStart: null,
                                    setEnd: null,
                                    displayOrder: 1,
                                  },
                                ],
                              }),
                            });
                            setSuccess("Lineup updated.");
                          }}
                          disabled={!lineupArtistId}
                        >
                          Add to lineup
                        </Button>
                      </div>
                    }
                  >
                    <FormField label="Artist" description="Select an artist to add to the lineup.">
                      <select
                        className={selectClass}
                        value={lineupArtistId}
                        onChange={(e) => setLineupArtistId(e.target.value)}
                      >
                        <option value="">Select artist...</option>
                        {artists.map((a) => (
                          <option key={a.id} value={a.id}>
                            {a.stageName || a.name}
                          </option>
                        ))}
                      </select>
                    </FormField>
                  </FormSection>
                )}

                {activeTab === "tickets" && (
                  <FormSection
                    title={activeTabConfig.label}
                    description="Define tiers, pricing, and capacity."
                    footer={
                      <div className="flex flex-wrap items-center justify-between gap-3 border-t border-border/80 pt-6">
                        <Button
                          variant="outline"
                          onClick={() =>
                            setTicketTypes([
                              ...ticketTypes,
                              {
                                id: "",
                                name: "General Admission",
                                description: null,
                                price: 0,
                                quantity: 100,
                                quantitySold: 0,
                                saleStart: null,
                                saleEnd: null,
                                maxPerUser: null,
                                isActive: true,
                                displayOrder: ticketTypes.length + 1,
                              },
                            ])
                          }
                        >
                          <Plus className="mr-2 h-4 w-4" />
                          Add ticket type
                        </Button>
                        <Button
                          onClick={async () => {
                            if (!token || !eventId) return;
                            await apiFetch(`/api/events/${eventId}/ticket-types`, {
                              method: "PUT",
                              token,
                              body: JSON.stringify({
                                ticketTypes: ticketTypes.map((t, i) => ({
                                  id: t.id || null,
                                  name: t.name,
                                  description: t.description,
                                  price: t.price,
                                  quantity: t.quantity,
                                  saleStart: t.saleStart,
                                  saleEnd: t.saleEnd,
                                  maxPerUser: t.maxPerUser,
                                  isActive: t.isActive,
                                  defaultLookupValueId: null,
                                  displayOrder: i + 1,
                                })),
                              }),
                            });
                            setSuccess("Ticket types saved.");
                          }}
                        >
                          Save ticket types
                        </Button>
                      </div>
                    }
                  >
                    {ticketTypes.length === 0 ? (
                      <p className="rounded-lg border border-dashed border-border/80 bg-muted/20 px-4 py-8 text-center text-sm text-muted-foreground">
                        No ticket tiers yet. Add your first tier to start selling.
                      </p>
                    ) : (
                      ticketTypes.map((t, idx) => (
                        <div key={idx} className="form-item-card">
                          <div className="grid gap-4 sm:grid-cols-3">
                            <FormField label="Tier name">
                              <Input
                                value={t.name}
                                onChange={(e) => {
                                  const n = [...ticketTypes];
                                  n[idx] = { ...t, name: e.target.value };
                                  setTicketTypes(n);
                                }}
                                placeholder="Early Bird"
                              />
                            </FormField>
                            <FormField label="Price">
                              <Input
                                type="number"
                                value={t.price}
                                onChange={(e) => {
                                  const n = [...ticketTypes];
                                  n[idx] = { ...t, price: parseFloat(e.target.value) };
                                  setTicketTypes(n);
                                }}
                                placeholder="0.00"
                              />
                            </FormField>
                            <FormField label="Quantity">
                              <Input
                                type="number"
                                value={t.quantity}
                                onChange={(e) => {
                                  const n = [...ticketTypes];
                                  n[idx] = { ...t, quantity: parseInt(e.target.value) };
                                  setTicketTypes(n);
                                }}
                                placeholder="100"
                              />
                            </FormField>
                          </div>
                        </div>
                      ))
                    )}
                  </FormSection>
                )}

                {activeTab === "promos" && (
                  <FormSection
                    title={activeTabConfig.label}
                    description="Discount codes for early birds, partners, and VIPs."
                    footer={
                      <div className="flex flex-wrap items-center justify-between gap-3 border-t border-border/80 pt-6">
                        <Button
                          variant="outline"
                          onClick={() =>
                            setPromoCodes([
                              ...promoCodes,
                              {
                                id: "",
                                code: "SAVE10",
                                discountType: "Percent",
                                discountValue: 10,
                                expiresAt: null,
                                usageLimit: 100,
                                usageCount: 0,
                                isActive: true,
                              },
                            ])
                          }
                        >
                          <Plus className="mr-2 h-4 w-4" />
                          Add promo code
                        </Button>
                        <Button
                          onClick={async () => {
                            if (!token || !eventId) return;
                            await apiFetch(`/api/events/${eventId}/promo-codes`, {
                              method: "PUT",
                              token,
                              body: JSON.stringify({
                                promoCodes: promoCodes.map((p) => ({
                                  id: p.id || null,
                                  code: p.code,
                                  discountType: p.discountType === "Percent" ? 1 : 2,
                                  discountValue: p.discountValue,
                                  expiresAt: p.expiresAt,
                                  usageLimit: p.usageLimit,
                                  isActive: p.isActive,
                                  appliesToTicketTypeIdsJson: null,
                                })),
                              }),
                            });
                            setSuccess("Promo codes saved.");
                          }}
                        >
                          Save promo codes
                        </Button>
                      </div>
                    }
                  >
                    {promoCodes.length === 0 ? (
                      <p className="rounded-lg border border-dashed border-border/80 bg-muted/20 px-4 py-8 text-center text-sm text-muted-foreground">
                        No promo codes yet. Create one to reward your community.
                      </p>
                    ) : (
                      promoCodes.map((p, idx) => (
                        <div key={idx} className="form-item-card">
                          <div className="grid gap-4 sm:grid-cols-2">
                            <FormField label="Code">
                              <Input
                                value={p.code}
                                onChange={(e) => {
                                  const n = [...promoCodes];
                                  n[idx] = { ...p, code: e.target.value };
                                  setPromoCodes(n);
                                }}
                                placeholder="EARLYBIRD"
                                className="font-mono uppercase"
                              />
                            </FormField>
                            <FormField label="Discount value">
                              <Input
                                type="number"
                                value={p.discountValue}
                                onChange={(e) => {
                                  const n = [...promoCodes];
                                  n[idx] = { ...p, discountValue: parseFloat(e.target.value) };
                                  setPromoCodes(n);
                                }}
                              />
                            </FormField>
                          </div>
                        </div>
                      ))
                    )}
                  </FormSection>
                )}

                {activeTab === "policies" && (
                  <FormSection
                    title={activeTabConfig.label}
                    description="Set expectations so everyone has a great — and safe — time."
                    footer={
                      <div className="flex justify-end border-t border-border/80 pt-6">
                        <Button onClick={() => void savePolicies()} disabled={isSubmitting}>
                          {isSubmitting ? "Saving..." : "Save policies"}
                        </Button>
                      </div>
                    }
                  >
                    <div className="grid gap-5 sm:grid-cols-2">
                      <LookupSelect
                        typeCode="AgeRestriction"
                        label="Age restriction"
                        value={ageRestrictionId}
                        onChange={setAgeRestrictionId}
                        token={token}
                      />
                      <LookupSelect
                        typeCode="CancellationPolicy"
                        label="Cancellation policy"
                        value={cancellationPolicyId}
                        onChange={setCancellationPolicyId}
                        token={token}
                      />
                    </div>
                    <FormField label="Entry policy">
                      <textarea
                        className={textareaClass}
                        value={entryPolicy}
                        onChange={(e) => setEntryPolicy(e.target.value)}
                        placeholder="ID requirements, re-entry rules, last call..."
                      />
                    </FormField>
                    <FormField label="Prohibited items">
                      <textarea
                        className={textareaClass}
                        value={prohibitedItems}
                        onChange={(e) => setProhibitedItems(e.target.value)}
                        placeholder="Outside food, professional cameras, weapons..."
                      />
                    </FormField>
                    <FormField label="Terms & conditions">
                      <textarea
                        className={textareaClass}
                        value={termsAndConditions}
                        onChange={(e) => setTermsAndConditions(e.target.value)}
                        placeholder="Legal terms guests agree to when purchasing..."
                      />
                    </FormField>
                  </FormSection>
                )}

                {activeTab === "facilities" && (
                  <FormSection
                    title={activeTabConfig.label}
                    description="Amenities on site — bars, restrooms, accessibility, and more."
                    footer={
                      <div className="flex justify-end border-t border-border/80 pt-6">
                        <Button
                          onClick={async () => {
                            if (!token || !eventId) return;
                            await apiFetch(`/api/events/${eventId}/selections/Facility`, {
                              method: "PUT",
                              token,
                              body: JSON.stringify({ lookupValueIds: facilityIds }),
                            });
                            setSuccess("Facilities saved.");
                          }}
                        >
                          Save facilities
                        </Button>
                      </div>
                    }
                  >
                    <LookupMultiSelect
                      typeCode="Facility"
                      label="Facilities"
                      selectedIds={facilityIds}
                      onChange={setFacilityIds}
                      token={token}
                    />
                  </FormSection>
                )}

                {activeTab === "production" && (
                  <FormSection
                    title={activeTabConfig.label}
                    description="Sound, genre, and production details that define the experience."
                    footer={
                      <div className="flex justify-end border-t border-border/80 pt-6">
                        <Button onClick={() => void saveProduction()} disabled={isSubmitting}>
                          {isSubmitting ? "Saving..." : "Save production"}
                        </Button>
                      </div>
                    }
                  >
                    <div className="grid gap-5 sm:grid-cols-2">
                      <LookupSelect
                        typeCode="VenueType"
                        label="Venue type"
                        value={venueTypeId}
                        onChange={setVenueTypeId}
                        token={token}
                      />
                      <FormField label="Sound system">
                        <Input
                          value={soundSystem}
                          onChange={(e) => setSoundSystem(e.target.value)}
                          placeholder="e.g. Funktion-One, L-Acoustics"
                        />
                      </FormField>
                      <LookupSelect
                        typeCode="MusicGenre"
                        label="Primary genre"
                        value={primaryGenreId}
                        onChange={setPrimaryGenreId}
                        token={token}
                      />
                      <LookupSelect
                        typeCode="MusicGenre"
                        label="Secondary genre"
                        value={secondaryGenreId}
                        onChange={setSecondaryGenreId}
                        token={token}
                      />
                    </div>
                    <LookupMultiSelect
                      typeCode="ProductionFeature"
                      label="Production features"
                      selectedIds={productionFeatureIds}
                      onChange={setProductionFeatureIds}
                      token={token}
                    />
                  </FormSection>
                )}

                {activeTab === "visibility" && (
                  <FormSection
                    title={activeTabConfig.label}
                    description="Control who can discover and access your event."
                    footer={
                      <div className="flex justify-end border-t border-border/80 pt-6">
                        <Button onClick={() => void saveVisibility()} disabled={isSubmitting}>
                          {isSubmitting ? "Saving..." : "Save visibility"}
                        </Button>
                      </div>
                    }
                  >
                    <div className="grid gap-5 sm:grid-cols-2">
                      <LookupSelect
                        typeCode="EventVisibility"
                        label="Visibility"
                        value={visibilityTypeId}
                        onChange={setVisibilityTypeId}
                        token={token}
                      />
                      <FormField label="Invite code" description="Required for private or invite-only events.">
                        <Input
                          value={inviteCode}
                          onChange={(e) => setInviteCode(e.target.value)}
                          placeholder="Optional access code"
                          className="font-mono"
                        />
                      </FormField>
                    </div>
                    <label className="flex cursor-pointer items-center gap-3 rounded-lg border border-border/80 bg-muted/20 px-4 py-3">
                      <input
                        type="checkbox"
                        className="h-4 w-4 rounded border-input accent-primary"
                        checked={requiresApproval}
                        onChange={(e) => setRequiresApproval(e.target.checked)}
                      />
                      <div>
                        <p className="text-sm font-medium">Requires approval</p>
                        <p className="text-xs text-muted-foreground">
                          Guests must be approved before they can purchase tickets.
                        </p>
                      </div>
                    </label>
                  </FormSection>
                )}

                {activeTab === "publish" && (
                  <FormSection
                    title={activeTabConfig.label}
                    description="Review readiness and take your event live."
                    footer={
                      <div className="flex justify-end border-t border-border/80 pt-6">
                        <Button
                          size="lg"
                          onClick={() => void publishEvent()}
                          disabled={isSubmitting || publishReadiness?.isReady === false}
                        >
                          {isSubmitting ? "Publishing..." : "Publish event"}
                        </Button>
                      </div>
                    }
                  >
                    {publishReadiness ? (
                      <div className="space-y-4">
                        <div
                          className={cn(
                            "rounded-lg border px-5 py-4",
                            publishReadiness.isReady
                              ? "border-emerald-500/30 bg-emerald-500/10"
                              : "border-amber-500/30 bg-amber-500/10",
                          )}
                        >
                          <Badge variant={publishReadiness.isReady ? "success" : "warning"}>
                            {publishReadiness.isReady ? "Ready to publish" : "Not ready yet"}
                          </Badge>
                          <p className="mt-2 text-sm text-muted-foreground">
                            {publishReadiness.isReady
                              ? "All required fields are complete. You're good to go live."
                              : "Complete the items below before publishing."}
                          </p>
                        </div>
                        {publishReadiness.errors.length > 0 && (
                          <ul className="space-y-2">
                            {publishReadiness.errors.map((err) => (
                              <li
                                key={err}
                                className="flex items-start gap-2 rounded-md border border-border/80 bg-muted/20 px-3 py-2 text-sm text-muted-foreground"
                              >
                                <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-amber-600 dark:text-amber-400" />
                                {err}
                              </li>
                            ))}
                          </ul>
                        )}
                      </div>
                    ) : (
                      <p className="text-sm text-muted-foreground">Checking publish readiness...</p>
                    )}
                  </FormSection>
                )}
              </CardContent>
            </Card>
          </div>
        </div>
      )}
    </div>
  );
}
