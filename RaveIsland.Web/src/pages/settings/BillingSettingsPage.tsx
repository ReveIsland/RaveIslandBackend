import { useCallback, useEffect, useState } from "react";
import { useAuth } from "react-oidc-context";
import { useSearchParams } from "react-router-dom";
import { useCurrentUser } from "../../auth/CurrentUserContext";
import { apiFetch, type BillingPlan, type BillingStatus } from "../../lib/api";
import { Button } from "../../components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "../../components/ui/card";
import { Badge } from "../../components/ui/badge";

export function BillingSettingsPage() {
  const auth = useAuth();
  const { refresh: refreshProfile } = useCurrentUser();
  const token = auth.user?.access_token;
  const [searchParams] = useSearchParams();
  const [status, setStatus] = useState<BillingStatus | null>(null);
  const [plans, setPlans] = useState<BillingPlan[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isBusy, setIsBusy] = useState(false);

  const checkoutResult = searchParams.get("checkout");
  const sessionId = searchParams.get("session_id");

  const load = useCallback(async () => {
    if (!token) return;
    setIsLoading(true);
    setError(null);
    try {
      const [billingStatus, billingPlans] = await Promise.all([
        apiFetch<BillingStatus>("/api/billing/status", { token }),
        apiFetch<BillingPlan[]>("/api/billing/plans"),
      ]);
      setStatus(billingStatus);
      setPlans(billingPlans);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to load billing");
    } finally {
      setIsLoading(false);
    }
  }, [token]);

  useEffect(() => {
    if (checkoutResult !== "success" || !sessionId) {
      void load();
      return;
    }

    apiFetch("/api/billing/confirm-checkout", {
      method: "POST",
      body: JSON.stringify({ sessionId }),
    })
      .finally(() => {
        refreshProfile();
        void load();
      });
  }, [checkoutResult, sessionId, load, refreshProfile]);

  function resolvePlanLabel(status: BillingStatus, billingPlans: BillingPlan[]) {
    if (status.planName) {
      return status.planName;
    }

    const matchedPlan = billingPlans.find((plan) => plan.priceId === status.priceId);
    if (matchedPlan?.name) {
      return matchedPlan.name;
    }

    if (status.isSubscribed) {
      return "Subscribed";
    }

    return "Not subscribed";
  }

  const planLabel = status ? resolvePlanLabel(status, plans) : null;

  async function openPortal() {
    if (!token) return;
    setIsBusy(true);
    setError(null);
    try {
      const { portalUrl } = await apiFetch<{ portalUrl: string }>(
        "/api/billing/portal-session",
        { method: "POST", token },
      );
      window.location.href = portalUrl;
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to open billing portal");
      setIsBusy(false);
    }
  }

  async function upgrade(priceId: string) {
    if (!token) return;
    setIsBusy(true);
    setError(null);
    try {
      const { checkoutUrl } = await apiFetch<{ checkoutUrl: string }>(
        "/api/billing/checkout-session",
        {
          method: "POST",
          token,
          body: JSON.stringify({ priceId }),
        },
      );
      window.location.href = checkoutUrl;
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to start checkout");
      setIsBusy(false);
    }
  }

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Billing</h1>
        <p className="text-sm text-muted-foreground">
          Manage your organization subscription and payment methods.
        </p>
      </div>

      {checkoutResult === "success" && (
        <p className="rounded-md border border-border bg-muted/40 p-3 text-sm">
          Checkout completed. Your subscription will update shortly.
        </p>
      )}

      {isLoading && <p className="text-sm text-muted-foreground">Loading billing...</p>}
      {error && <p className="text-sm text-destructive">{error}</p>}

      {status && (
        <Card>
          <CardHeader>
            <CardTitle>Current plan</CardTitle>
            <CardDescription>Subscription status for your organization</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="flex flex-wrap items-center gap-2">
              <span className="font-medium">{planLabel}</span>
              {status.subscriptionStatus && (
                <Badge variant="secondary">{status.subscriptionStatus}</Badge>
              )}
            </div>
            {!status.isSubscribed && !status.billingSetupCompleted && (
              <p className="text-sm text-muted-foreground">
                Billing setup is incomplete. Complete checkout to publish events.
              </p>
            )}
            {status.hasUnlimitedPublishes ? (
              <p className="text-sm text-muted-foreground">Unlimited event publishing</p>
            ) : (
              <p className="text-sm text-muted-foreground">
                Publish credits available: {status.availablePublishCredits ?? 0}
              </p>
            )}
            {status.activeFeatures.length > 0 && (
              <div className="flex flex-wrap gap-2">
                {status.activeFeatures.map((feature: string) => (
                  <Badge key={feature} variant="outline">
                    {feature}
                  </Badge>
                ))}
              </div>
            )}
            <Button onClick={openPortal} disabled={isBusy || !status.isConfigured}>
              Manage billing in Stripe
            </Button>
          </CardContent>
        </Card>
      )}

      {plans.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Available plans</CardTitle>
            <CardDescription>Upgrade or change your subscription</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4 sm:grid-cols-2">
            {plans.map((plan) => (
              <div key={plan.priceId} className="rounded-lg border border-border p-4">
                <p className="font-medium">{plan.name}</p>
                {plan.description && (
                  <p className="mt-1 text-sm text-muted-foreground">{plan.description}</p>
                )}
                <p className="mt-2 text-sm">
                  {plan.unitAmount === 0 || plan.unitAmount === null
                    ? "Free"
                    : `$${((plan.unitAmount ?? 0) / 100).toFixed(2)}/${plan.interval ?? "month"}`}
                </p>
                {plan.priceId === status?.priceId ? (
                  <Badge className="mt-3" variant="secondary">
                    Current plan
                  </Badge>
                ) : (
                  <Button
                    className="mt-3 w-full"
                    variant="outline"
                    disabled={isBusy}
                    onClick={() => upgrade(plan.priceId)}
                  >
                    Select plan
                  </Button>
                )}
              </div>
            ))}
          </CardContent>
        </Card>
      )}
    </div>
  );
}
