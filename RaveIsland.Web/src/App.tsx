import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AdminLayout } from "./components/layout/AdminLayout";
import { MarketingLayout } from "./components/layout/MarketingLayout";
import { ProtectedRoute } from "./components/ProtectedRoute";
import { ThemeProvider } from "./components/theme/ThemeProvider";
import { RaveAuthProvider } from "./auth/RaveAuthProvider";
import { AdminPage } from "./pages/AdminPage";
import { DashboardPage } from "./pages/DashboardPage";
import { LandingPage } from "./pages/LandingPage";
import { ProfileSettingsPage } from "./pages/ProfileSettingsPage";

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
            <Route element={<ProtectedRoute />}>
              <Route element={<AdminLayout />}>
                <Route path="/dashboard" element={<DashboardPage />} />
                <Route path="/profile" element={<ProfileSettingsPage />} />
                <Route path="/admin" element={<AdminPage />} />
              </Route>
            </Route>
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </BrowserRouter>
      </RaveAuthProvider>
    </ThemeProvider>
  );
}
