import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { apiFetch, type InvitationPreview } from "../lib/api";
import { Button } from "../components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "../components/ui/card";
import { Input } from "../components/ui/input";

export function InviteAcceptPage() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get("token") ?? "";
  const [invitation, setInvitation] = useState<InvitationPreview | null>(null);
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (!token) {
      setError("Missing invitation token.");
      setIsLoading(false);
      return;
    }

    apiFetch<InvitationPreview>(`/api/invitations/${encodeURIComponent(token)}`)
      .then(setInvitation)
      .catch((err: unknown) =>
        setError(err instanceof Error ? err.message : "Invalid invitation"),
      )
      .finally(() => setIsLoading(false));
  }, [token]);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      await apiFetch(`/api/invitations/${encodeURIComponent(token)}/accept`, {
        method: "POST",
        body: JSON.stringify({ password, confirmPassword }),
      });
      setSuccess(true);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Registration failed");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle>Accept invitation</CardTitle>
          <CardDescription>
            Complete your registration to join Rave Island.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading && <p className="text-sm text-muted-foreground">Loading invitation...</p>}

          {!isLoading && success && (
            <div className="space-y-4">
              <p className="text-sm text-muted-foreground">
                Registration complete. You can now sign in with your email and password.
              </p>
              <Link
                to="/"
                className="inline-flex h-10 w-full items-center justify-center rounded-lg bg-primary px-4 text-sm font-medium text-primary-foreground hover:opacity-90"
              >
                Go to sign in
              </Link>
            </div>
          )}

          {!isLoading && !success && invitation && (
            <form className="space-y-4" onSubmit={handleSubmit}>
              <div className="rounded-md border border-border bg-muted/40 p-3 text-sm">
                <p>
                  <span className="font-medium">{invitation.firstName} {invitation.lastName}</span>
                </p>
                <p className="text-muted-foreground">{invitation.email}</p>
                <p className="mt-2 text-muted-foreground">
                  Join <span className="font-medium text-foreground">{invitation.tenantName}</span> as{" "}
                  <span className="font-medium text-foreground">{invitation.intendedRole}</span>
                </p>
              </div>

              <div className="space-y-2">
                <label className="text-sm font-medium" htmlFor="password">
                  Password
                </label>
                <Input
                  id="password"
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                  minLength={8}
                />
              </div>

              <div className="space-y-2">
                <label className="text-sm font-medium" htmlFor="confirmPassword">
                  Confirm password
                </label>
                <Input
                  id="confirmPassword"
                  type="password"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  required
                  minLength={8}
                />
              </div>

              {error && <p className="text-sm text-destructive">{error}</p>}

              <Button type="submit" className="w-full" disabled={isSubmitting}>
                {isSubmitting ? "Creating account..." : "Complete registration"}
              </Button>
            </form>
          )}

          {!isLoading && !success && !invitation && error && (
            <p className="text-sm text-destructive">{error}</p>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
