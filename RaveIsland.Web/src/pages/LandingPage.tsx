import { useAuth } from "react-oidc-context";
import { Navigate } from "react-router-dom";
import {
  ArrowRight,
  BarChart3,
  CalendarDays,
  ShieldCheck,
  Ticket,
  Users,
  Waves,
} from "lucide-react";
import { Button } from "../components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../components/ui/card";
import type { AuthRedirectState } from "../auth/authRedirect";

const features = [
  {
    icon: CalendarDays,
    title: "Event lifecycle",
    description: "Create, publish, and manage events from draft to live with full tenant isolation.",
    gradient: "from-violet-600/80 to-indigo-700/80",
  },
  {
    icon: Ticket,
    title: "Tickets & check-in",
    description: "Configure ticket types, track sales, and run on-site check-in from one place.",
    gradient: "from-fuchsia-600/80 to-pink-600/80",
  },
  {
    icon: Users,
    title: "Team management",
    description: "Invite organizers, assign roles, and control who can manage your organization.",
    gradient: "from-purple-600/80 to-violet-700/80",
  },
  {
    icon: BarChart3,
    title: "Analytics",
    description: "Monitor page views, attendance, and platform metrics with real-time dashboards.",
    gradient: "from-indigo-600/80 to-blue-700/80",
  },
];

export function LandingPage() {
  const auth = useAuth();

  if (auth.isAuthenticated) {
    return <Navigate to="/dashboard" replace />;
  }

  const oauthError = new URLSearchParams(window.location.search).get("error_description");

  function signIn() {
    void auth.signinRedirect({
      state: { returnTo: "/dashboard" } satisfies AuthRedirectState,
    });
  }

  return (
    <div className="space-y-12">
      <section className="mx-auto max-w-md space-y-8 pt-4 md:max-w-lg md:pt-8">
        <div className="space-y-3 text-center">
          <div className="mx-auto flex h-16 w-16 items-center justify-center">
            <span className="gradient-primary flex h-16 w-16 items-center justify-center rounded-2xl shadow-xl shadow-primary/40">
              <Waves className="h-8 w-8 text-primary-foreground" />
            </span>
          </div>
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.2em] text-muted-foreground">
              Rave Island
            </p>
            <h1 className="mt-2 text-3xl font-bold tracking-tight md:text-4xl">Welcome back</h1>
            <p className="mt-2 text-sm text-muted-foreground">
              Sign in to manage your events, tickets, and teams.
            </p>
          </div>
        </div>

        <Card className="glass-strong overflow-hidden shadow-2xl shadow-black/20">
          <CardContent className="space-y-5 p-6 md:p-8">
            {(auth.error || oauthError) && (
              <p className="rounded-xl border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
                Sign-in failed: {auth.error?.message ?? oauthError}
              </p>
            )}

            <Button size="lg" className="h-12 w-full text-base" onClick={signIn}>
              Sign in
            </Button>

            <div className="relative">
              <div className="absolute inset-0 flex items-center">
                <span className="w-full border-t border-border/80" />
              </div>
              <div className="relative flex justify-center text-xs uppercase">
                <span className="glass-subtle rounded-full px-3 py-1 text-muted-foreground">
                  Secure platform access
                </span>
              </div>
            </div>

            <p className="flex items-center justify-center gap-2 text-center text-xs text-muted-foreground">
              <ShieldCheck className="h-3.5 w-3.5 text-primary" />
              Powered by Keycloak — enterprise-grade authentication
            </p>
          </CardContent>
        </Card>
      </section>

      <section className="space-y-4">
        <div>
          <h2 className="text-xl font-bold tracking-tight">Built for organizers</h2>
          <p className="text-sm text-muted-foreground">Everything you need to run unforgettable events.</p>
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          {features.map(({ icon: Icon, title, description, gradient }) => (
            <Card
              key={title}
              className="group overflow-hidden transition-all duration-300 hover:border-white/20 hover:shadow-xl hover:shadow-primary/10"
            >
              <CardHeader>
                <div
                  className={`mb-3 flex h-11 w-11 items-center justify-center rounded-xl bg-gradient-to-br ${gradient} shadow-lg`}
                >
                  <Icon className="h-5 w-5 text-white" />
                </div>
                <CardTitle className="text-base">{title}</CardTitle>
                <CardDescription>{description}</CardDescription>
              </CardHeader>
            </Card>
          ))}
        </div>
      </section>

      <Card className="glass overflow-hidden border-primary/20">
        <CardHeader>
          <CardTitle>Ready to manage your next event?</CardTitle>
          <CardDescription>
            Sign in to access your dashboard, create events, and collaborate with your team.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Button onClick={signIn}>
            Get started
            <ArrowRight className="h-4 w-4" />
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}
