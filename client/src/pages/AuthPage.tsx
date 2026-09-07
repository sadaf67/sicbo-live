import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { api, SESSION_FLASH_KEY } from "../api/client";
import { useAuthStore } from "../state/authStore";
import type { AuthResponse } from "../api/types";

/** One-time read: if the api client's response interceptor just force-logged-out this device
 * (e.g. someone else logged into the same account elsewhere), show why instead of landing here
 * with no explanation. sessionStorage (not state) because the redirect that got us here is a full
 * page navigation (window.location.href), so any in-memory state from before is already gone. */
function takeSessionFlash(): string | null {
  const msg = sessionStorage.getItem(SESSION_FLASH_KEY);
  if (msg) sessionStorage.removeItem(SESSION_FLASH_KEY);
  return msg;
}

// There is no registration form here anymore - accounts are created by the shared Telegram bot
// (@game2652bot, displayed as "game2026bot" - the same bot Poker uses, see
// D:\SikboLive\telegram-bot\bot.js and the backend's InternalController.cs), which hands the
// player a ready-to-use username and password after admin approval. This page only ever logs an
// existing account in.
export function AuthPage() {
  const navigate = useNavigate();
  const setAuth = useAuthStore((s) => s.setAuth);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(takeSessionFlash);
  const [busy, setBusy] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const res = await api.post<AuthResponse>("/api/auth/login", { email, password });
      setAuth(res.data.token, res.data.userId, res.data.displayName, res.data.role);
      navigate(res.data.role === 0 ? "/admin" : "/lobby");
    } catch (err: unknown) {
      const data = (err as { response?: { data?: unknown } })?.response?.data;
      let message: string | null = null;
      if (typeof data === "string" && data.trim()) {
        message = data;
      } else if (Array.isArray(data) && data.length > 0) {
        message = data.join(" ");
      } else if (data && typeof data === "object" && "errors" in data) {
        // ASP.NET model-binding validation error shape: { errors: { field: [messages] } }
        const errors = (data as { errors?: Record<string, string[]> }).errors;
        if (errors) message = Object.values(errors).flat().join(" ");
      }
      setError(message ?? "نام کاربری یا رمز عبور اشتباه است.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="auth-page">
      <div className="auth-card">
        <h1>SicBo Live</h1>
        <form onSubmit={handleSubmit}>
          <label>
            نام کاربری
            <input value={email} onChange={(e) => setEmail(e.target.value)} required />
          </label>
          <label>
            رمز عبور
            <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required minLength={6} />
          </label>
          {error && <div className="auth-error">{error}</div>}
          <button type="submit" disabled={busy} className="btn btn-primary">
            {busy ? "..." : "ورود"}
          </button>
        </form>
        <p className="auth-hint">
          حساب کاربری ندارید؟ ثبت‌نام فقط از طریق ربات تلگرام{" "}
          <a href="https://t.me/game2652bot" target="_blank" rel="noopener noreferrer">
            @game2026bot
          </a>{" "}
          انجام می‌شود: عکس رسید پرداخت را برای ربات بفرستید، و پس از تایید توسط ادمین، نام کاربری و رمز
          عبور همان‌جا برایتان ارسال می‌شود.
        </p>
      </div>
      <footer className="site-footer">
        تهیه شده توسط{" "}
        <a href="https://sadafkohan.ir" target="_blank" rel="noopener noreferrer" className="site-footer-link">
          sadafkohan.ir
        </a>
      </footer>
    </div>
  );
}
