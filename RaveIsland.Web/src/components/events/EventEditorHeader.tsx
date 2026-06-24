import { Link } from "react-router-dom";
import { ArrowLeft, BarChart3, QrCode } from "lucide-react";
import { Badge } from "../ui/badge";
import { buttonVariants } from "../ui/button";
import { cn } from "../../lib/utils";

type EventEditorHeaderProps = {
  eventId: string;
  title: string;
  tagline?: string;
  statusLabel?: string;
  theme?: string;
};

function statusVariant(
  status?: string,
): "success" | "warning" | "secondary" | "destructive" | "outline" {
  const normalized = status?.toLowerCase() ?? "";
  if (normalized.includes("publish") || normalized.includes("live")) return "success";
  if (normalized.includes("draft")) return "warning";
  if (normalized.includes("cancel")) return "destructive";
  return "secondary";
}

export function EventEditorHeader({
  eventId,
  title,
  tagline,
  statusLabel,
  theme,
}: EventEditorHeaderProps) {
  return (
    <div className="event-hero relative overflow-hidden rounded-xl border border-border/80">
      <div className="relative flex flex-col gap-5 p-5 sm:p-6 md:flex-row md:items-start md:justify-between">
        <div className="min-w-0 space-y-3">
          <div className="flex flex-wrap items-center gap-2">
            <Link
              to="/events"
              className={cn(
                buttonVariants({ variant: "ghost", size: "sm" }),
                "h-8 px-2 text-muted-foreground hover:text-foreground",
              )}
            >
              <ArrowLeft className="mr-1.5 h-4 w-4" />
              Events
            </Link>
            {statusLabel && (
              <Badge variant={statusVariant(statusLabel)}>{statusLabel}</Badge>
            )}
            {theme && (
              <Badge variant="outline" className="font-normal">
                {theme}
              </Badge>
            )}
          </div>

          <div>
            <h2 className="text-2xl font-bold tracking-tight sm:text-3xl">
              {title || "Untitled event"}
            </h2>
            {tagline ? (
              <p className="mt-1.5 max-w-2xl text-sm text-muted-foreground sm:text-base">
                {tagline}
              </p>
            ) : (
              <p className="mt-1.5 text-sm text-muted-foreground">
                Build your event experience section by section.
              </p>
            )}
          </div>
        </div>

        <div className="flex shrink-0 flex-wrap gap-2">
          <Link
            to={`/events/${eventId}/analytics`}
            className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
          >
            <BarChart3 className="mr-2 h-4 w-4" />
            Analytics
          </Link>
          <Link
            to={`/events/${eventId}/check-in`}
            className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
          >
            <QrCode className="mr-2 h-4 w-4" />
            Check-in
          </Link>
        </div>
      </div>
    </div>
  );
}
