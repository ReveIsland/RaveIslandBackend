import type { ComponentType } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "../ui/card";
import { cn } from "../../lib/utils";

type StatCardProps = {
  title: string;
  value: string;
  description: string;
  icon: ComponentType<{ className?: string }>;
  accent?: "primary" | "violet" | "emerald" | "amber" | "rose";
};

const accentStyles = {
  primary: "bg-primary/10 text-primary",
  violet: "bg-violet-500/10 text-violet-600 dark:text-violet-400",
  emerald: "bg-emerald-500/10 text-emerald-600 dark:text-emerald-400",
  amber: "bg-amber-500/10 text-amber-600 dark:text-amber-400",
  rose: "bg-rose-500/10 text-rose-600 dark:text-rose-400",
};

export function StatCard({
  title,
  value,
  description,
  icon: Icon,
  accent = "primary",
}: StatCardProps) {
  return (
    <Card className="transition-all hover:border-white/20 hover:shadow-xl hover:shadow-primary/10">
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
        <div className={cn("flex h-9 w-9 items-center justify-center rounded-xl glass-subtle", accentStyles[accent])}>
          <Icon className="h-4 w-4" />
        </div>
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold tracking-tight">{value}</div>
        <p className="mt-1 text-xs text-muted-foreground">{description}</p>
      </CardContent>
    </Card>
  );
}
