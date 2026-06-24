import type { ComponentType } from "react";
import {
  Building2,
  CalendarDays,
  Eye,
  Image,
  MapPin,
  Music,
  Rocket,
  Shield,
  Sliders,
  Sparkles,
  Tag,
  Ticket,
} from "lucide-react";
import { cn } from "../../lib/utils";

export const EVENT_TABS = [
  "basic",
  "schedule",
  "venue",
  "media",
  "lineup",
  "tickets",
  "promos",
  "policies",
  "facilities",
  "production",
  "visibility",
  "publish",
] as const;

export type EventTab = (typeof EVENT_TABS)[number];

type TabConfig = {
  id: EventTab;
  label: string;
  description: string;
  icon: ComponentType<{ className?: string }>;
};

type TabGroup = {
  label: string;
  tabs: TabConfig[];
};

export const TAB_GROUPS: TabGroup[] = [
  {
    label: "Overview",
    tabs: [
      {
        id: "basic",
        label: "Basic info",
        description: "Name, tagline & category",
        icon: Sparkles,
      },
    ],
  },
  {
    label: "When & Where",
    tabs: [
      {
        id: "schedule",
        label: "Schedule",
        description: "Dates & show times",
        icon: CalendarDays,
      },
      {
        id: "venue",
        label: "Venue",
        description: "Location details",
        icon: MapPin,
      },
    ],
  },
  {
    label: "Experience",
    tabs: [
      {
        id: "media",
        label: "Media",
        description: "Cover & gallery",
        icon: Image,
      },
      {
        id: "lineup",
        label: "Lineup",
        description: "Artists & sets",
        icon: Music,
      },
      {
        id: "production",
        label: "Production",
        description: "Sound & genre",
        icon: Sliders,
      },
      {
        id: "facilities",
        label: "Facilities",
        description: "On-site amenities",
        icon: Building2,
      },
    ],
  },
  {
    label: "Commerce",
    tabs: [
      {
        id: "tickets",
        label: "Tickets",
        description: "Pricing & tiers",
        icon: Ticket,
      },
      {
        id: "promos",
        label: "Promos",
        description: "Discount codes",
        icon: Tag,
      },
    ],
  },
  {
    label: "Rules & Access",
    tabs: [
      {
        id: "policies",
        label: "Policies",
        description: "Age & entry rules",
        icon: Shield,
      },
      {
        id: "visibility",
        label: "Visibility",
        description: "Who can see this",
        icon: Eye,
      },
    ],
  },
  {
    label: "Launch",
    tabs: [
      {
        id: "publish",
        label: "Publish",
        description: "Go-live checklist",
        icon: Rocket,
      },
    ],
  },
];

const ALL_TABS = TAB_GROUPS.flatMap((group) => group.tabs);

export function getTabConfig(tab: EventTab): TabConfig {
  return ALL_TABS.find((item) => item.id === tab) ?? ALL_TABS[0];
}

type EventFormNavProps = {
  activeTab: EventTab;
  onTabChange: (tab: EventTab) => void;
  className?: string;
};

export function EventFormNav({ activeTab, onTabChange, className }: EventFormNavProps) {
  return (
    <nav className={cn("space-y-6", className)} aria-label="Event sections">
      {TAB_GROUPS.map((group) => (
        <div key={group.label}>
          <p className="mb-2 px-3 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
            {group.label}
          </p>
          <ul className="space-y-0.5">
            {group.tabs.map((tab) => {
              const Icon = tab.icon;
              const isActive = activeTab === tab.id;
              return (
                <li key={tab.id}>
                  <button
                    type="button"
                    onClick={() => onTabChange(tab.id)}
                    className={cn(
                      "group flex w-full items-start gap-3 rounded-xl px-3 py-2.5 text-left transition-all",
                      isActive
                        ? "glass ring-1 ring-primary/30 text-foreground shadow-lg shadow-primary/10"
                        : "text-muted-foreground hover:bg-white/5 hover:text-foreground",
                    )}
                  >
                    <span
                      className={cn(
                        "mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-lg transition-colors",
                        isActive
                          ? "gradient-primary text-primary-foreground shadow-md shadow-primary/30"
                          : "glass-subtle group-hover:bg-accent/60",
                      )}
                    >
                      <Icon className="h-4 w-4" />
                    </span>
                    <span className="min-w-0 flex-1">
                      <span className="block text-sm font-medium leading-tight">{tab.label}</span>
                      <span className="mt-0.5 block text-xs leading-snug opacity-80">
                        {tab.description}
                      </span>
                    </span>
                  </button>
                </li>
              );
            })}
          </ul>
        </div>
      ))}
    </nav>
  );
}

export function EventFormNavMobile({ activeTab, onTabChange }: EventFormNavProps) {
  const activeConfig = getTabConfig(activeTab);

  return (
    <div className="space-y-3 lg:hidden">
      <div className="flex gap-2 overflow-x-auto pb-1 [-ms-overflow-style:none] [scrollbar-width:none] [&::-webkit-scrollbar]:hidden">
        {TAB_GROUPS.map((group) => {
          const hasActive = group.tabs.some((tab) => tab.id === activeTab);
          const firstTab = group.tabs[0];
          return (
            <button
              key={group.label}
              type="button"
              onClick={() => onTabChange(firstTab.id)}
              className={cn(
                "shrink-0 rounded-full px-3.5 py-1.5 text-xs font-medium transition-colors",
                hasActive
                  ? "gradient-primary text-primary-foreground shadow-md shadow-primary/25"
                  : "glass-subtle text-muted-foreground hover:text-foreground",
              )}
            >
              {group.label}
            </button>
          );
        })}
      </div>

      <div className="flex gap-2 overflow-x-auto pb-1 [-ms-overflow-style:none] [scrollbar-width:none] [&::-webkit-scrollbar]:hidden">
        {TAB_GROUPS.flatMap((group) => group.tabs).map((tab) => {
          const Icon = tab.icon;
          const isActive = activeTab === tab.id;
          return (
            <button
              key={tab.id}
              type="button"
              onClick={() => onTabChange(tab.id)}
              className={cn(
                "flex shrink-0 items-center gap-1.5 rounded-lg border px-3 py-2 text-xs font-medium transition-colors",
                isActive
                  ? "glass ring-1 ring-primary/30 text-foreground"
                  : "glass-subtle text-muted-foreground",
              )}
            >
              <Icon className="h-3.5 w-3.5" />
              {tab.label}
            </button>
          );
        })}
      </div>

      <p className="text-xs text-muted-foreground">{activeConfig.description}</p>
    </div>
  );
}
