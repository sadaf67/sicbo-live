import axios from "axios";
import { useAuthStore } from "../state/authStore";

// In production the client is built into SicBoLive.WebApi/wwwroot and served from the same
// origin as the API (see vite.config.ts's build.outDir + Program.cs's UseStaticFiles), so a
// relative base works there. Local dev still runs the Vite dev server (port 5173) separately
// from the backend (port 5299), so it needs the explicit absolute URL.
export const API_BASE_URL = import.meta.env.PROD ? "" : "http://localhost:5299";

/** Read by AuthPage on mount to show a one-time message after a forced logout (e.g. session-kick). */
export const SESSION_FLASH_KEY = "sicbo.sessionFlash";

export const api = axios.create({ baseURL: API_BASE_URL });

api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().token;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Single-active-session enforcement (see ApplicationUser.CurrentSessionId / Program.cs's
// OnTokenValidated): once a second device logs into the same account, the server starts
// rejecting *this* device's still-unexpired token with 401 on every subsequent call. Without this
// interceptor that would just look like every button silently failing - each page's own try/catch
// would show its own generic "ناموفق بود" message with no explanation. This catches that specific
// case app-wide, logs the stale session out, and bounces to the login screen with a clear reason.
// Login/Register themselves also return 401 for a wrong password - that's a normal credentials
// error already handled locally by AuthPage, not a session-kick, so it's excluded here by only
// acting when we currently believe we're logged in (a token exists in the store).
api.interceptors.response.use(
  (res) => res,
  (error) => {
    if (error?.response?.status === 401 && useAuthStore.getState().token) {
      useAuthStore.getState().logout();
      sessionStorage.setItem(SESSION_FLASH_KEY, "این حساب از دستگاه یا مرورگر دیگری وارد شده است. لطفاً دوباره وارد شوید.");
      if (window.location.pathname !== "/") {
        window.location.href = "/";
      }
    }
    return Promise.reject(error);
  },
);
