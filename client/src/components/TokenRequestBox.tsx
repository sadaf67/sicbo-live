import { useState } from "react";
import { api } from "../api/client";
import type { TokenRequestDto } from "../api/types";

interface TokenRequestBoxProps {
  groupId: string;
  pendingRequest: TokenRequestDto | null;
  onRequested: () => void;
}

export function TokenRequestBox({ groupId, pendingRequest, onRequested }: TokenRequestBoxProps) {
  const [amount, setAmount] = useState(50);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleRequest() {
    setBusy(true);
    setError(null);
    try {
      await api.post(`/api/groups/${groupId}/token-requests`, { amount });
      onRequested();
    } catch (e: unknown) {
      const resp = (e as { response?: { data?: unknown } })?.response;
      setError(typeof resp?.data === "string" ? resp.data : "درخواست ژتون ناموفق بود.");
    } finally {
      setBusy(false);
    }
  }

  if (pendingRequest) {
    return (
      <div className="token-request-box">
        درخواست {pendingRequest.amount.toLocaleString("fa-IR")} ژتون شما در انتظار تأیید دیلر است.
      </div>
    );
  }

  return (
    <div className="token-request-box">
      <input
        type="number"
        min={1}
        value={amount}
        onChange={(e) => setAmount(Number(e.target.value))}
        onFocus={(e) => e.target.select()}
      />
      <button className="btn" disabled={busy || amount <= 0} onClick={handleRequest}>
        {busy ? "..." : "درخواست ژتون"}
      </button>
      {error && <span className="auth-error">{error}</span>}
    </div>
  );
}
