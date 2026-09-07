import type { TokenLimitsUpdatedDto } from "../api/types";

interface BetLimitsToastProps {
  limits: TokenLimitsUpdatedDto;
  onDismiss: () => void;
}

/** Player-facing announcement shown when the dealer changes the table's min/max bet limits (see
 * SignalRGameNotifier.TokenLimitsUpdated). Structurally mirrors TokenRequestAlertToast, minus the
 * "مشاهده" action since there's nothing to navigate to - just an announcement + dismiss. */
export function BetLimitsToast({ limits, onDismiss }: BetLimitsToastProps) {
  return (
    <div className="token-request-toast" role="alert">
      <span className="token-request-toast-text">
        محدودیت شرط این میز تغییر کرد: از <strong>{limits.minBetTokens.toLocaleString("fa-IR")}</strong> تا{" "}
        <strong>{limits.maxBetTokens.toLocaleString("fa-IR")}</strong> ژتون
      </span>
      <div className="token-request-toast-actions">
        <button className="btn" onClick={onDismiss} aria-label="بستن">✕</button>
      </div>
    </div>
  );
}
