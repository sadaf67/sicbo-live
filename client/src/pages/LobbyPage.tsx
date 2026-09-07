import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "../api/client";
import { useAuthStore } from "../state/authStore";
import { CopyButton } from "../components/CopyButton";
import type { AuthResponse } from "../api/types";

interface MyGroupDto {
  groupId: string;
  name: string;
  inviteCode: string;
  isActive: boolean;
}

export function LobbyPage() {
  const navigate = useNavigate();
  const { displayName, role, logout, setAuth } = useAuthStore();
  const isDealer = role === 1;

  const [tableName, setTableName] = useState("میز من");
  const [minTokens, setMinTokens] = useState(1);
  const [maxTokens, setMaxTokens] = useState(100);
  const [bettingWindow, setBettingWindow] = useState(30);
  const [restrictBigSmallOddEven, setRestrictBigSmallOddEven] = useState(false);
  const [inviteCode, setInviteCode] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [switchingRole, setSwitchingRole] = useState(false);
  const [myGroups, setMyGroups] = useState<MyGroupDto[]>([]);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  useEffect(() => {
    if (isDealer) {
      api.get<MyGroupDto[]>("/api/groups/mine").then((res) => setMyGroups(res.data));
    }
  }, [isDealer]);

  async function handleCreate() {
    setBusy(true);
    setError(null);
    try {
      const res = await api.post<string>("/api/groups", {
        name: tableName,
        minBetTokens: minTokens,
        maxBetTokens: maxTokens,
        bettingWindowSeconds: bettingWindow,
        restrictBigSmallOddEvenBets: restrictBigSmallOddEven,
      });
      navigate(`/table/${res.data}`);
    } catch {
      setError("ساخت میز ناموفق بود.");
    } finally {
      setBusy(false);
    }
  }

  async function handleJoin() {
    setBusy(true);
    setError(null);
    try {
      const res = await api.post<string>("/api/groups/join", { inviteCode });
      navigate(`/table/${res.data}`);
    } catch (e: unknown) {
      // Surface the backend's actual reason (e.g. "your subscription must be activated by an
      // admin first") instead of always blaming the invite code itself - those are two different
      // problems with two different fixes from the user's point of view.
      const resp = (e as { response?: { data?: unknown } })?.response;
      setError(typeof resp?.data === "string" ? resp.data : "پیوستن به میز ناموفق بود.");
    } finally {
      setBusy(false);
    }
  }

  // Toggles the account between Dealer and Player at any time. The server issues a fresh JWT
  // with the new role claim (role-gated endpoints trust only the token, not the DB), and feeding
  // it into setAuth() re-renders this page and every other screen reading the store immediately -
  // no logout/login round-trip needed.
  async function handleSwitchRole() {
    setSwitchingRole(true);
    setError(null);
    try {
      const res = await api.post<AuthResponse>("/api/auth/switch-role");
      setAuth(res.data.token, res.data.userId, res.data.displayName, res.data.role);
    } catch {
      setError("تغییر نقش ناموفق بود.");
    } finally {
      setSwitchingRole(false);
    }
  }

  // Soft-deletes a table: server-side this flips Group.IsActive to false (see DeleteGroupCommand),
  // it never hard-deletes the row, so wallets/history for that table stay intact. Blocked by the
  // server with a 400 if a round is still open. We just drop it from local state on success instead
  // of refetching - the list is small and this avoids an extra round-trip.
  async function handleDelete(g: MyGroupDto) {
    if (!window.confirm(`آیا از حذف میز «${g.name}» مطمئن هستید؟`)) return;
    setDeletingId(g.groupId);
    setError(null);
    try {
      await api.delete(`/api/groups/${g.groupId}`);
      setMyGroups((prev) => prev.filter((x) => x.groupId !== g.groupId));
    } catch {
      setError("حذف میز ناموفق بود. شاید این میز دور فعال دارد.");
    } finally {
      setDeletingId(null);
    }
  }

  const activeGroups = myGroups.filter((g) => g.isActive);

  return (
    <div className="lobby-page">
      <header className="lobby-header">
        <span>
          خوش آمدی، {displayName} <span className="lobby-role-badge">({isDealer ? "دیلر" : "بازیکن"})</span>
        </span>
        <div className="lobby-header-actions">
          <button className="btn" disabled={switchingRole} onClick={handleSwitchRole}>
            {switchingRole ? "..." : isDealer ? "تبدیل به بازیکن" : "تبدیل به دیلر"}
          </button>
          <button
            className="btn"
            onClick={() => {
              logout();
              navigate("/");
            }}
          >
            خروج
          </button>
        </div>
      </header>

      {isDealer && activeGroups.length > 0 && (
        <div className="lobby-card">
          <h2>میزهای من</h2>
          <ul className="player-list">
            {activeGroups.map((g) => (
              <li key={g.groupId}>
                {g.name} — کد: {g.inviteCode} <CopyButton text={g.inviteCode} label="کپی کد دعوت" />{" "}
                <button className="btn" onClick={() => navigate(`/table/${g.groupId}`)}>
                  ورود
                </button>{" "}
                <button
                  className="btn btn-danger"
                  disabled={deletingId === g.groupId}
                  onClick={() => handleDelete(g)}
                >
                  {deletingId === g.groupId ? "..." : "حذف"}
                </button>
              </li>
            ))}
          </ul>
        </div>
      )}

      {isDealer ? (
        <div className="lobby-card">
          <h2>ساخت میز جدید</h2>
          <label>
            نام میز
            <input value={tableName} onChange={(e) => setTableName(e.target.value)} />
          </label>
          <div className="lobby-row">
            <label>
              حداقل مقدار ژتون
              <input
                type="number"
                value={minTokens}
                onChange={(e) => setMinTokens(Number(e.target.value))}
                onFocus={(e) => e.target.select()}
              />
            </label>
            <label>
              حداکثر مقدار ژتون
              <input
                type="number"
                value={maxTokens}
                onChange={(e) => setMaxTokens(Number(e.target.value))}
                onFocus={(e) => e.target.select()}
              />
            </label>
            <label>
              زمان شرط‌بندی (ثانیه)
              <input
                type="number"
                value={bettingWindow}
                onChange={(e) => setBettingWindow(Number(e.target.value))}
                onFocus={(e) => e.target.select()}
              />
            </label>
          </div>
          <label className="checkbox-label">
            <input
              type="checkbox"
              checked={restrictBigSmallOddEven}
              onChange={(e) => setRestrictBigSmallOddEven(e.target.checked)}
            />
            <span>
              محدود کردن شرط بیگ / اسمال / فرد / زوج به بازه ۲۰ تا ۱۰۰ برابر حداقل شرط این میز
            </span>
          </label>
          <button className="btn btn-primary" disabled={busy} onClick={handleCreate}>
            {busy ? "..." : "ساخت میز"}
          </button>
        </div>
      ) : (
        <div className="lobby-card">
          <h2>پیوستن به میز</h2>
          <label>
            کد دعوت
            <input value={inviteCode} onChange={(e) => setInviteCode(e.target.value.toUpperCase())} />
          </label>
          <button className="btn btn-primary" disabled={busy || !inviteCode} onClick={handleJoin}>
            {busy ? "..." : "پیوستن"}
          </button>
        </div>
      )}

      {error && <div className="auth-error">{error}</div>}
    </div>
  );
}
