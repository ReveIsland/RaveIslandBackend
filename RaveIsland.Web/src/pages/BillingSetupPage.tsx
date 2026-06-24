import { Link } from "react-router-dom";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "../components/ui/card";
import { buttonVariants } from "../components/ui/button";
import { cn } from "../lib/utils";

export function BillingSetupPage() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle>Billing setup required</CardTitle>
          <CardDescription>
            Complete billing setup to activate your organization subscription.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <p className="text-sm text-muted-foreground">
            Your account was created, but billing setup was not finished. Sign in and open
            billing settings to continue, or contact your platform administrator.
          </p>
          <Link to="/" className={cn(buttonVariants(), "w-full")}>
            Sign in
          </Link>
        </CardContent>
      </Card>
    </div>
  );
}
