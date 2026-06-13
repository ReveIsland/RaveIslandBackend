import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useAuth } from "react-oidc-context";
import { ArrowLeft } from "lucide-react";
import { apiFetch } from "../lib/api";
import { Button, buttonVariants } from "../components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "../components/ui/card";
import { Input } from "../components/ui/input";
import { cn } from "../lib/utils";

export function EventCheckInPage() {
  const { eventId } = useParams<{ eventId: string }>();
  const auth = useAuth();
  const token = auth.user?.access_token;
  const [qrToken, setQrToken] = useState("");
  const [gateId, setGateId] = useState("");
  const [result, setResult] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleCheckIn(e: React.FormEvent) {
    e.preventDefault();
    if (!token || !eventId) return;
    setIsSubmitting(true);
    setError(null);
    setResult(null);
    try {
      const data = await apiFetch<{ holderName?: string; checkedInAt: string }>(
        `/api/events/${eventId}/check-in`,
        {
          method: "POST",
          token,
          body: JSON.stringify({ qrToken, gateId: gateId || null }),
        },
      );
      setResult(`Checked in: ${data.holderName ?? "Guest"} at ${new Date(data.checkedInAt).toLocaleTimeString()}`);
      setQrToken("");
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Check-in failed");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="mx-auto max-w-lg space-y-6">
      <Link to={`/events/${eventId}/edit`} className={cn(buttonVariants({ variant: "outline", size: "sm" }))}>
        <ArrowLeft className="mr-2 h-4 w-4" /> Back to event
      </Link>

      <div>
        <h2 className="text-2xl font-bold tracking-tight">Attendee check-in</h2>
        <p className="text-muted-foreground">Scan or paste QR token to verify entry.</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Scan ticket</CardTitle>
          <CardDescription>Duplicate entries are rejected automatically.</CardDescription>
        </CardHeader>
        <CardContent>
          <form className="grid gap-4" onSubmit={handleCheckIn}>
            <Input
              value={qrToken}
              onChange={(e) => setQrToken(e.target.value)}
              placeholder="QR token"
              required
              autoFocus
            />
            <Input
              value={gateId}
              onChange={(e) => setGateId(e.target.value)}
              placeholder="Gate ID (optional)"
            />
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Scanning..." : "Check in"}
            </Button>
          </form>
          {result && <p className="mt-4 text-sm text-green-600">{result}</p>}
          {error && <p className="mt-4 text-sm text-destructive">{error}</p>}
        </CardContent>
      </Card>
    </div>
  );
}
