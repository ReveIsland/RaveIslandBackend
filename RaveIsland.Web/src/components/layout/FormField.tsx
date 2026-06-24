import type { ReactNode } from "react";
import { cn } from "../../lib/utils";

type FormFieldProps = {
  label: string;
  description?: string;
  children: ReactNode;
  className?: string;
  required?: boolean;
};

export function FormField({ label, description, children, className, required }: FormFieldProps) {
  return (
    <div className={cn("space-y-2", className)}>
      <div>
        <label className="text-sm font-medium leading-none">
          {label}
          {required && <span className="text-destructive"> *</span>}
        </label>
        {description && (
          <p className="mt-1.5 text-xs leading-relaxed text-muted-foreground">{description}</p>
        )}
      </div>
      {children}
    </div>
  );
}

type FormSectionProps = {
  title: string;
  description?: string;
  children: ReactNode;
  footer?: ReactNode;
};

export function FormSection({ title, description, children, footer }: FormSectionProps) {
  return (
    <div className="space-y-6">
      <div className="border-b border-border/80 pb-5">
        <h3 className="text-lg font-semibold tracking-tight">{title}</h3>
        {description && (
          <p className="mt-1.5 max-w-2xl text-sm text-muted-foreground">{description}</p>
        )}
      </div>
      <div className="space-y-5">{children}</div>
      {footer}
    </div>
  );
}
