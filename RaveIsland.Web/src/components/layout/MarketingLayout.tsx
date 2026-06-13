import { Link, useLocation } from "react-router-dom";
import { useAuth } from "react-oidc-context";
import { LogIn, LogOut, Waves } from "lucide-react";
import { Button } from "../ui/button";
import { ThemeToggle } from "../theme/ThemeToggle";
import type { AuthRedirectState } from "../../auth/authRedirect";

export function MarketingLayout({ children }: { children: React.ReactNode }) {
  const auth = useAuth();
  const location = useLocation();

  function signIn() {
    const returnTo =
      location.pathname === "/" ? "/dashboard" : `${location.pathname}${location.search}`;
    void auth.signinRedirect({ state: { returnTo } satisfies AuthRedirectState });
  }

  return (
    <div className="min-h-screen bg-background">
      <header className="border-b border-border bg-card/80 backdrop-blur">
        <div className="mx-auto flex max-w-5xl items-center justify-between gap-4 px-4 py-4">
          <Link to="/" className="flex items-center gap-2 font-semibold">
            <Waves className="h-5 w-5 text-primary" />
            Rave Island
          </Link>
          <div className="flex items-center gap-2">
            <ThemeToggle />
            {auth.isAuthenticated ? (
              <>
                <Link to="/dashboard">
                  <Button variant="outline" size="sm">
                    Dashboard
                  </Button>
                </Link>
                <Button variant="ghost" size="sm" onClick={() => void auth.signoutRedirect()}>
                  <LogOut className="h-4 w-4" /> Log out
                </Button>
              </>
            ) : (
              <Button size="sm" onClick={signIn}>
                <LogIn className="h-4 w-4" /> Log in
              </Button>
            )}
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-5xl px-4 py-10">{children}</main>
    </div>
  );
}
