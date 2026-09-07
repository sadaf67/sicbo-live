import { useEffect, useState } from "react";
import { api } from "../api/client";
import type { GroupPlayerDto, GroupStateDto, TokenRequestDto } from "../api/types";

interface DealerPanelProps {
  groupId: string;
  groupState: GroupStateDto;
  onRefresh: () => void;
  pendingTokenRequests: TokenRequestDto[];
  onTokenRequestsChanged: () => void;
  /** Player list + balances, now owned by TablePage (shared with the always-visible
   * DealerPlayerBalances panel) rather than fetched independently here. */
  players: GroupPlayerDto[];
  onPlayersChanged: () => void;
}

export function DealerPanel({
  groupId,
  groupState,
  onRefresh,
  pendingTokenRequests,
  onTokenRequestsChanged,
  players,
  onPlayersChanged,
}: DealerPanelProps) {
  const [minTokens, setMinTokens] = useState(groupState.minBetTokens);
  const [maxTokens, setMaxTokens] = useState(groupState.maxBetTokens);
  const [bettingWindowSeconds, setBettingWindowSeconds] = useState(groupState.bettingWindowSeconds);
  const [issuePlayerId, setIssuePlayerId] = useState("");
  const [issueAmount, setIssueAmount] = useState(100);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [respondingId, setRespondingId] = useState<string | null>(null);

  useEffect(() => {
    if (!issuePlayerId && players.length > 0) setIssuePlayerId(players[0].userId);
  }, [players, issuePlayerId]);

  async function handleRespondTokenRequest(requestId: string, approve: boolean) {
    setRespondingId(requestId);
    setMessage(null);
    try {
      await api.post(`/api/groups/token-requests/${requestId}/respond`, { approve });
      setMessage(approve ? "ژتون واریز شد." : "درخواست رد شد.");
      onTokenRequestsChanged();
      onPlayersChanged();
      onRefresh();
    } catch (e) {
      setMessage(errorMessage(e));
    } finally {
      setRespondingId(null);
    }
  }

  async function handleSetLimits() {
    setBusy(true);
    setMessage(null);
    try {
      await api.put(`/api/groups/${groupId}/token-limits`, {
        minBetTokens: minTokens,
        maxBetTokens: maxTokens,
      });
      setMessage("محدودیت شرط به‌روزرسانی شد.");
      onRefresh();
    } catch (e) {
      setMessage(errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  async function handleSetBettingWindow() {
    setBusy(true);
    setMessage(null);
    try {
      await api.put(`/api/groups/${groupId}/betting-window`, { bettingWindowSeconds });
      setMessage("مدت زمان شرط‌بندی به‌روزرسانی شد.");
      onRefresh();
    } catch (e) {
      setMessage(errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  async function handleIssueTokens() {
    if (!issuePlayerId) return;
    setBusy(true);
    setMessage(null);
    try {
      await api.post(`/api/groups/${groupId}/players/${issuePlayerId}/tokens`, { amount: issueAmount });
      setMessage("ژتون با موفقیت واریز شد.");
      onPlayersChanged();
      onRefresh();
    } catch (e) {
      setMessage(errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="dealer-panel">
      <h3>تنظیمات دیلر</h3>

      <div className="dealer-panel-section">
        <h4>حداقل / حداکثر شرط</h4>
        <div className="dealer-panel-row">
          <input
            type="number"
            value={minTokens}
            onChange={(e) => setMinTokens(Number(e.target.value))}
            onFocus={(e) => e.target.select()}
          />
          <input
            type="number"
            value={maxTokens}
            onChange={(e) => setMaxTokens(Number(e.target.value))}
            onFocus={(e) => e.target.select()}
          />
          <button disabled={busy} onClick={handleSetLimits} className="btn">
            ذخیره
          </button>
        </div>
      </div>

      <div className="dealer-panel-section">
        <h4>مدت زمان شرط‌بندی (ثانیه)</h4>
        <div className="dealer-panel-row">
          <input
            type="number"
            value={bettingWindowSeconds}
            onChange={(e) => setBettingWindowSeconds(Number(e.target.value))}
            onFocus={(e) => e.target.select()}
          />
          <button disabled={busy} onClick={handleSetBettingWindow} className="btn">
            ذخیره
          </button>
        </div>
      </div>

      <div className="dealer-panel-section">
        <h4>درخواست‌های ژتون در انتظار</h4>
        {pendingTokenRequests.length === 0 ? (
          <p className="dealer-panel-empty">درخواستی در انتظار نیست.</p>
        ) : (
          <ul className="player-list">
            {pendingTokenRequests.map((r) => (
              <li key={r.requestId} className="dealer-panel-row">
                <span>
                  {r.playerDisplayName} — {r.amount.toLocaleString("fa-IR")} ژتون
                </span>
                <button
                  className="btn btn-primary"
                  disabled={respondingId === r.requestId}
                  onClick={() => handleRespondTokenRequest(r.requestId, true)}
                >
                  تأیید
                </button>
                <button
                  className="btn btn-danger"
                  disabled={respondingId === r.requestId}
                  onClick={() => handleRespondTokenRequest(r.requestId, false)}
                >
                  رد
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>

      <div className="dealer-panel-section">
        <h4>تحویل ژتون به بازیکن</h4>
        <div className="dealer-panel-row">
          <select value={issuePlayerId} onChange={(e) => setIssuePlayerId(e.target.value)}>
            {players.map((p) => (
              <option key={p.userId} value={p.userId}>
                {p.displayName} ({p.balance})
              </option>
            ))}
          </select>
          <input
            type="number"
            value={issueAmount}
            onChange={(e) => setIssueAmount(Number(e.target.value))}
            onFocus={(e) => e.target.select()}
          />
          <button disabled={busy || !issuePlayerId} onClick={handleIssueTokens} className="btn">
            واریز
          </button>
        </div>
      </div>

      <div className="dealer-panel-section">
        <h4>بازیکنان گروه</h4>
        <ul className="player-list">
          {players.map((p) => (
            <li key={p.userId}>
              {p.displayName} — {p.balance.toLocaleString("fa-IR")} ژتون
            </li>
          ))}
        </ul>
      </div>

      {message && <div className="dealer-panel-message">{message}</div>}
    </div>
  );
}

function errorMessage(e: unknown): string {
  if (e && typeof e === "object" && "response" in e) {
    const resp = (e as { response?: { data?: unknown } }).response;
    if (resp?.data) return typeof resp.data === "string" ? resp.data : JSON.stringify(resp.data);
  }
  return "خطایی رخ داد.";
}
