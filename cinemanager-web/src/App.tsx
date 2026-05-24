import { Navigate, Route, Routes } from "react-router-dom";
import { RootLayout } from "./components/layout/RootLayout";
import { useAuth } from "./hooks/useAuth";
import { BookingPage } from "./pages/BookingPage";
import { HomePage } from "./pages/HomePage";
import { MovieDetailPage } from "./pages/MovieDetailPage";
import { MoviesPage } from "./pages/MoviesPage";
import { NotFoundPage } from "./pages/NotFoundPage";
import { ProfilePage } from "./pages/ProfilePage";
import { AuthPage } from "./pages/AuthPage";

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { customer } = useAuth();
  return customer ? children : <Navigate to="/auth" replace />;
}

function GuestRoute({ children }: { children: React.ReactNode }) {
  const { customer } = useAuth();
  return customer ? <Navigate to="/profile" replace /> : children;
}

import { AnimatePresence } from "framer-motion";
import { useLocation } from "react-router-dom";
import { AboutPage } from "./pages/AboutPage";

function AnimatedRoutes() {
  const location = useLocation();
  return (
    <AnimatePresence mode="wait">
      <Routes location={location} key={location.pathname}>
        <Route element={<RootLayout />}>
          <Route index element={<HomePage />} />
          <Route path="movies" element={<MoviesPage />} />
          <Route path="movies/:id" element={<MovieDetailPage />} />
          <Route path="about" element={<AboutPage />} />
          <Route path="auth" element={<GuestRoute><AuthPage /></GuestRoute>} />
          <Route
            path="booking/:showtimeId"
            element={
              <ProtectedRoute>
                <BookingPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="profile"
            element={
              <ProtectedRoute>
                <ProfilePage />
              </ProtectedRoute>
            }
          />
          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Routes>
    </AnimatePresence>
  );
}

export default function App() {
  return <AnimatedRoutes />;
}
