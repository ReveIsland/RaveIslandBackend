import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AdminLayout } from "./components/layout/AdminLayout";
import { MarketingLayout } from "./components/layout/MarketingLayout";
import { ProtectedRoute } from "./components/ProtectedRoute";
import { RoleRoute } from "./components/RoleRoute";
import { ThemeProvider } from "./components/theme/ThemeProvider";
import { RaveAuthProvider } from "./auth/RaveAuthProvider";
import { AdminPage } from "./pages/AdminPage";
import { DashboardPage } from "./pages/DashboardPage";
import { EventsPage } from "./pages/EventsPage";
import { EventFormPage } from "./pages/EventFormPage";
import { InviteAcceptPage } from "./pages/InviteAcceptPage";
import { LandingPage } from "./pages/LandingPage";
import { ProfileSettingsPage } from "./pages/ProfileSettingsPage";
import { TenantsPage } from "./pages/admin/TenantsPage";
import { UsersPage } from "./pages/admin/UsersPage";
import { LookupTypesPage } from "./pages/admin/LookupTypesPage";
import { LookupValuesPage } from "./pages/admin/LookupValuesPage";
import { EventAnalyticsPage } from "./pages/EventAnalyticsPage";
import { EventCheckInPage } from "./pages/EventCheckInPage";

export default function App() {
  return (
    <ThemeProvider>
      <RaveAuthProvider>
        <BrowserRouter>
          <Routes>
            <Route
              path="/"
              element={
                <MarketingLayout>
                  <LandingPage />
                </MarketingLayout>
              }
            />
            <Route path="/invite/accept" element={<InviteAcceptPage />} />
            <Route element={<ProtectedRoute />}>
              <Route element={<AdminLayout />}>
                <Route path="/dashboard" element={<DashboardPage />} />
                <Route path="/profile" element={<ProfileSettingsPage />} />
                <Route path="/events" element={<EventsPage />} />
                <Route path="/events/new" element={<EventFormPage />} />
                <Route path="/events/:eventId/edit" element={<EventFormPage />} />
                <Route path="/events/:eventId/analytics" element={<EventAnalyticsPage />} />
                <Route path="/events/:eventId/check-in" element={<EventCheckInPage />} />
                <Route element={<RoleRoute anyOf={["admin"]} />}>
                  <Route path="/admin" element={<AdminPage />} />
                  <Route path="/admin/tenants" element={<TenantsPage />} />
                  <Route path="/admin/lookups" element={<LookupTypesPage />} />
                  <Route path="/admin/lookups/:typeCode" element={<LookupValuesPage />} />
                </Route>
                <Route element={<RoleRoute anyOf={["admin", "tenant-admin"]} />}>
                  <Route path="/admin/users" element={<UsersPage />} />
                </Route>
              </Route>
            </Route>
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </BrowserRouter>
      </RaveAuthProvider>
    </ThemeProvider>
  );
}
