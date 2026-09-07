import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "../api/client";
import { useAuthStore } from "../state/authStore";
import type { AdminGroupDto, PlayerAdminDto, SubscriptionPlanDto } from "../api/types";

export function AdminPage() {
  const navigate = useNavigate();
  const { displayName, logout } = useAuthStore();

  const [plans, setPlans] = useState<SubscriptionPlanDto[]>([]);
  const [groups, setGroups] = useState<AdminGroupDto[]>([]);
  const [players, setPlayers] = useState<PlayerAdminDto[]>([]);
  const [selectedPlanByUser, setSelectedPlanByUser] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [photoModalUrl, setPhotoModalUrl] = useState<string | null>(null);

  const [newPlayerName, setNewPlayerName] = useState("");
  const [createdCredentials, setCreatedCredentials] = useState<{ username: string; password: string; displayName: string } | null>(null);

  const [planName, setPlanName] = useState("پلن استاندارد");
  const [entryAmount, setEntryAmount] = useState(100);
  const [durationMinutes, setDurationMinutes] = useState(60);
  const [discountPercent, setDiscountPercent] = useState(0);

  function loadPlans() {
    api.get<SubscriptionPlanDto[]>("/api/admin/plans").then((res) => setPlans(res.data));
  }

  function loadGroups() {
    api.get<AdminGroupDto[]>("/api/admin/groups").then((res) => setGroups(res.data));
  }

  function loadPlayers() {
    api.get<PlayerAdminDto[]>("/api/admin/players").then((res) => setPlayers(res.data));
  }

  useEffect(() => {
    loadPlans();
    loadGroups();
    loadPlayers();
  }, []);

  async function handleCreatePlan() {
    setBusy(true);
    setError(null);
    try {
      await api.post("/api/admin/plans", {
        name: planName,
        entryTokenAmount: entryAmount,
        gameDurationMinutes: durationMinutes,
        discountPercent,
      });
      loadPlans();
    } catch {
      setError("ساخت پلن ناموفق بود.");
    } finally {
      setBusy(false);
    }
  }

  async function handleUpdatePlan(plan: SubscriptionPlanDto, patch: Partial<SubscriptionPlanDto>) {
    setBusy(true);
    setError(null);
    try {
      await api.put(`/api/admin/plans/${plan.id}`, {
        entryTokenAmount: patch.entryTokenAmount ?? plan.entryTokenAmount,
        gameDurationMinutes: patch.gameDurationMinutes ?? plan.gameDurationMinutes,
        discountPercent: patch.discountPercent ?? plan.discountPercent,
      });
      loadPlans();
    } catch {
      setError("ویرایش پلن ناموفق بود.");
    } finally {
      setBusy(false);
    }
  }

  async function handleDeactivatePlan(planId: string) {
    setBusy(true);
    setError(null);
    try {
      await api.delete(`/api/admin/plans/${planId}`);
      loadPlans();
    } catch {
      setError("غیرفعال‌سازی پلن ناموفق بود.");
    } finally {
      setBusy(false);
    }
  }

  function extractErrorMessage(err: unknown, fallback: string): string {
    const data = (err as { response?: { data?: unknown } })?.response?.data;
    return typeof data === "string" && data.trim() ? data : fallback;
  }

  async function handleActivateSubscription(userId: string) {
    const planId = selectedPlanByUser[userId] ?? plans.find((p) => p.isActive)?.id;
    if (!planId) {
      setError("ابتدا یک پلن اشتراک فعال بسازید.");
      return;
    }
    setBusy(true);
    setError(null);
    try {
      await api.post(`/api/admin/players/${userId}/subscription`, { planId });
      loadPlayers();
    } catch (err) {
      setError(extractErrorMessage(err, "فعال‌سازی اشتراک ناموفق بود."));
    } finally {
      setBusy(false);
    }
  }

  async function handleRevokeSubscription(userId: string) {
    setBusy(true);
    setError(null);
    try {
      await api.delete(`/api/admin/players/${userId}/subscription`);
      loadPlayers();
    } catch (err) {
      setError(extractErrorMessage(err, "لغو اشتراک ناموفق بود."));
    } finally {
      setBusy(false);
    }
  }

  // Fetches the player's mandatory registration-time selfie (see ApplicationUser.VerificationPhoto)
  // as a blob - not a plain <img src=".../photo">, since that endpoint requires the admin's bearer
  // token and a bare <img> tag can't attach an Authorization header. Object URL is revoked on modal
  // close to avoid leaking memory across repeated views.
  async function handleViewPhoto(userId: string) {
    setError(null);
    try {
      const res = await api.get(`/api/admin/players/${userId}/photo`, { responseType: "blob" });
      setPhotoModalUrl(URL.createObjectURL(res.data as Blob));
    } catch {
      setError("عکس احراز هویت برای این کاربر یافت نشد.");
    }
  }

  function closePhotoModal() {
    if (photoModalUrl) URL.revokeObjectURL(photoModalUrl);
    setPhotoModalUrl(null);
  }

  // Lets the admin hand out a login without going through the Telegram bot at all (e.g. in
  // person). Mirrors the bot's own credential-minting on the backend (see CredentialGenerator).
  // The password only ever appears once, in the confirmation dialog right after creation - it is
  // not stored anywhere retrievable, same as the bot's own flow.
  async function handleCreatePlayer() {
    const displayName = newPlayerName.trim();
    if (!displayName) {
      setError("نام نمایشی الزامی است.");
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const res = await api.post<{ userId: string; username: string; password: string; displayName: string }>("/api/admin/players", { displayName });
      setCreatedCredentials({ username: res.data.username, password: res.data.password, displayName: res.data.displayName });
      setNewPlayerName("");
      loadPlayers();
    } catch (err) {
      setError(extractErrorMessage(err, "ساخت حساب کاربری ناموفق بود."));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="lobby-page">
      <header className="lobby-header">
        <span>پنل مدیریت — {displayName}</span>
        <button
          className="btn"
          onClick={() => {
            logout();
            navigate("/");
          }}
        >
          خروج
        </button>
      </header>

      <div className="lobby-card">
        <h2>پلن‌های اشتراک</h2>
        <div className="lobby-row">
          <label>
            نام پلن
            <input value={planName} onChange={(e) => setPlanName(e.target.value)} />
          </label>
          <label>
            مبلغ ورودی (ژتون)
            <input
              type="number"
              value={entryAmount}
              onChange={(e) => setEntryAmount(Number(e.target.value))}
              onFocus={(e) => e.target.select()}
            />
          </label>
          <label>
            زمان بازی (دقیقه)
            <input
              type="number"
              value={durationMinutes}
              onChange={(e) => setDurationMinutes(Number(e.target.value))}
              onFocus={(e) => e.target.select()}
            />
          </label>
          <label>
            درصد تخفیف
            <input
              type="number"
              value={discountPercent}
              onChange={(e) => setDiscountPercent(Number(e.target.value))}
              onFocus={(e) => e.target.select()}
            />
          </label>
        </div>
        <button className="btn btn-primary" disabled={busy || !planName} onClick={handleCreatePlan}>
          {busy ? "..." : "ساخت پلن"}
        </button>

        <ul className="player-list">
          {plans.map((p) => (
            <li key={p.id}>
              {p.name} — ورودی: {p.entryTokenAmount} ژتون، مدت: {p.gameDurationMinutes} دقیقه، تخفیف: {p.discountPercent}% (مؤثر:{" "}
              {p.effectiveEntryAmount}) {!p.isActive && "— غیرفعال"}{" "}
              {p.isActive && (
                <>
                  <button className="btn" disabled={busy} onClick={() => handleUpdatePlan(p, { discountPercent: p.discountPercent + 5 })}>
                    +۵٪ تخفیف
                  </button>{" "}
                  <button className="btn btn-danger" disabled={busy} onClick={() => handleDeactivatePlan(p.id)}>
                    غیرفعال کردن
                  </button>
                </>
              )}
            </li>
          ))}
        </ul>
      </div>

      <div className="lobby-card">
        <h2>ساخت حساب کاربری جدید (بدون ربات تلگرام)</h2>
        <p>
          معمولاً حساب‌ها از طریق ربات تلگرام <strong>@game2026bot</strong> ساخته می‌شوند، اما در صورت نیاز می‌توانید
          از همین‌جا هم یک حساب بازیکن با نام کاربری و رمز عبور تصادفی بسازید.
        </p>
        <div className="lobby-row">
          <label>
            نام نمایشی
            <input value={newPlayerName} onChange={(e) => setNewPlayerName(e.target.value)} placeholder="مثلاً: علی" />
          </label>
        </div>
        <button className="btn btn-primary" disabled={busy || !newPlayerName.trim()} onClick={handleCreatePlayer}>
          {busy ? "..." : "ساخت حساب کاربری"}
        </button>
      </div>

      <div className="lobby-card">
        <h2>کاربران — دسترسی به شرط‌بندی</h2>
        <p>
          بازیکنان بعد از ثبت‌نام می‌توانند میز را تماشا کنند اما تا زمانی که اشتراک آن‌ها فعال نشود، اجازه شرط‌بندی ندارند.
          پس از دریافت حق عضویت (خارج از سایت)، اشتراک را از همین‌جا برای کاربر فعال کنید.
        </p>
        <ul className="player-list">
          {players.map((p) => (
            <li key={p.userId}>
              {p.displayName} ({p.email}) —{" "}
              {p.hasVerificationPhoto ? (
                <button className="btn" onClick={() => handleViewPhoto(p.userId)}>
                  مشاهده عکس احراز هویت
                </button>
              ) : (
                <span className="auth-error" style={{ display: "inline-block" }}>
                  بدون عکس احراز هویت (ثبت‌نام قبل از این قابلیت)
                </span>
              )}{" "}
              {p.hasActiveSubscription ? (
                <>
                  اشتراک فعال تا {p.activeSubscriptionExpiresAt ? new Date(p.activeSubscriptionExpiresAt).toLocaleString("fa-IR") : "-"}{" "}
                  <button className="btn btn-danger" disabled={busy} onClick={() => handleRevokeSubscription(p.userId)}>
                    لغو اشتراک
                  </button>
                </>
              ) : (
                <>
                  بدون اشتراک فعال{" "}
                  <select
                    value={selectedPlanByUser[p.userId] ?? plans.find((pl) => pl.isActive)?.id ?? ""}
                    onChange={(e) => setSelectedPlanByUser((prev) => ({ ...prev, [p.userId]: e.target.value }))}
                  >
                    {plans
                      .filter((pl) => pl.isActive)
                      .map((pl) => (
                        <option key={pl.id} value={pl.id}>
                          {pl.name}
                        </option>
                      ))}
                  </select>{" "}
                  <button className="btn btn-primary" disabled={busy || plans.filter((pl) => pl.isActive).length === 0} onClick={() => handleActivateSubscription(p.userId)}>
                    فعال‌سازی اشتراک
                  </button>
                </>
              )}
            </li>
          ))}
        </ul>
      </div>

      <div className="lobby-card">
        <h2>همه میزها</h2>
        <ul className="player-list">
          {groups.map((g) => (
            <li key={g.groupId}>
              {g.name} — دیلر: {g.dealerDisplayName} — کد: {g.inviteCode} — حد شرط: {g.minBetTokens} تا {g.maxBetTokens}{" "}
              {!g.isActive && "— بسته"}
            </li>
          ))}
        </ul>
      </div>

      {error && <div className="auth-error">{error}</div>}

      {photoModalUrl && (
        <div className="modal-backdrop" onClick={closePhotoModal}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <img src={photoModalUrl} alt="عکس احراز هویت" style={{ maxWidth: "100%", borderRadius: "8px" }} />
            <button className="btn btn-primary" onClick={closePhotoModal}>
              بستن
            </button>
          </div>
        </div>
      )}

      {createdCredentials && (
        <div className="modal-backdrop" onClick={() => setCreatedCredentials(null)}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <h3>حساب کاربری «{createdCredentials.displayName}» ساخته شد</h3>
            <p>این اطلاعات فقط همین یک بار نمایش داده می‌شود — همین حالا برای کاربر ارسال یا یادداشت کنید:</p>
            <p>
              نام کاربری: <code>{createdCredentials.username}</code>
              <br />
              رمز عبور: <code>{createdCredentials.password}</code>
            </p>
            <button className="btn btn-primary" onClick={() => setCreatedCredentials(null)}>
              بستن
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
