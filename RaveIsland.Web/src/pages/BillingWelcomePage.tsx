import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "../components/ui/card";
import { buttonVariants } from "../components/ui/button";
import { cn } from "../lib/utils";
import { apiFetch } from "../lib/api";

export function BillingWelcomePage() {
  const [searchParams] = useSearchParams();
  const sessionId = searchParams.get("session_id");
  const [error, setError] = useState<string | null>(null);
  const [isConfirming, setIsConfirming] = useState(Boolean(sessionId));

  useEffect(() => {
    if (!sessionId) {
      return;
    }

    apiFetch("/api/billing/confirm-checkout", {
      method: "POST",
      body: JSON.stringify({ sessionId }),
    })
      .catch((err: unknown) =>
        setError(err instanceof Error ? err.message : "Could not confirm billing setup"),
      )
      .finally(() => setIsConfirming(false));
  }, [sessionId]);

  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle>Billing setup complete</CardTitle>
          <CardDescription>
            Your organization subscription is active. Sign in to start managing events.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {isConfirming && (
            <p className="text-sm text-muted-foreground">Confirming your subscription...</p>
          )}
          {error && <p className="text-sm text-destructive">{error}</p>}
          <p className="text-sm text-muted-foreground">
            You can change your plan or payment method anytime from billing settings.
          </p>
          <Link to="/" className={cn(buttonVariants(), "w-full")}>
            Sign in to Rave Island
          </Link>
        </CardContent>
      </Card>
    </div>
  );
}
