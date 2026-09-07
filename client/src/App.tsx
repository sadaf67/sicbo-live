import { Navigate, Route, BrowserRouter as Router, Routes } from "react-router-dom";
import { AdminPage } from "./pages/AdminPage";
import { AuthPage } from "./pages/AuthPage";
import { InstallAppButton } from "./components/InstallAppButton";
import { LobbyPage } from "./pages/LobbyPage";
import { SiteFooter } from "./components/SiteFooter";
import { TablePage } from "./pages/TablePage";
import { useAuthStore } from "./state/authStore";

function RequireAuth({ children }: { children: React.ReactNode }) {
  const token = useAuthStore((s) => s.token);
  if (!token) return <Navigate to="/" replace />;
  return <>{children}</>;
}

function RequireAdmin({ children }: { children: React.ReactNode }) {
  const { token, role } = useAuthStore();
  if (!token) return <Navigate to="/" replace />;
  if (role !== 0) return <Navigate to="/lobby" replace />;
  return <>{children}</>;
}

function App() {
  return (
    <Router>
      <InstallAppButton />
      <Routes>
        <Route path="/" element={<AuthPage />} />
        <Route
          path="/lobby"
          element={
            <RequireAuth>
              <LobbyPage />
            </RequireAuth>
          }
        />
        <Route
          path="/table/:groupId"
          element={
            <RequireAuth>
              <TablePage />
            </RequireAuth>
          }
        />
        <Route
          path="/admin"
          element={
            <RequireAdmin>
              <AdminPage />
            </RequireAdmin>
          }
        />
      </Routes>
      <SiteFooter />
    </Router>
  );
}

export default App;
