interface LeaveConfirmDialogProps {
  balance: number;
  busy: boolean;
  error: string | null;
  onReturnAndLeave: () => void;
  onLeaveWithoutReturning: () => void;
  onCancel: () => void;
}

/**
 * Shown when a player with a non-zero balance clicks "بازگشت" (leave the table). Gives them an
 * explicit choice to hand their remaining balance back to the dealer before leaving, rather than
 * silently walking away with tokens still sitting in a wallet no one is looking at. Dealers and
 * zero-balance players skip this entirely (see TablePage's handleBackClick) and just navigate away
 * as before.
 */
export function LeaveConfirmDialog({
  balance,
  busy,
  error,
  onReturnAndLeave,
  onLeaveWithoutReturning,
  onCancel,
}: LeaveConfirmDialogProps) {
  return (
    <div className="leave-confirm-overlay" role="dialog" aria-modal="true">
      <div className="leave-confirm-card">
        <h3>خروج از میز</h3>
        <p>
          شما <strong>{balance.toLocaleString("fa-IR")}</strong> ژتون باقی‌مانده دارید. می‌خواهید قبل
          از خروج آن را به دیلر تحویل دهید؟
        </p>
        {error && <div className="auth-error">{error}</div>}
        <div className="leave-confirm-actions">
          <button className="btn btn-primary" disabled={busy} onClick={onReturnAndLeave}>
            {busy ? "..." : "تحویل بده و خارج شو"}
          </button>
          <button className="btn btn-danger" disabled={busy} onClick={onLeaveWithoutReturning}>
            بدون تحویل خارج شو
          </button>
          <button className="btn" disabled={busy} onClick={onCancel}>
            انصراف
          </button>
        </div>
      </div>
    </div>
  );
}
