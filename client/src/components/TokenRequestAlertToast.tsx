import type { TokenRequestDto } from "../api/types";

interface TokenRequestAlertToastProps {
  request: TokenRequestDto;
  onView: () => void;
  onDismiss: () => void;
}

/**
 * Dealer-only floating alert shown on top of the main table screen when a player submits a token
 * request - the SignalR TokenRequested event this reacts to is broadcast to the whole table group,
 * so without this a dealer not currently looking at the (buried-in-drawer) pending requests list
 * could easily miss it. Auto-dismissed by TablePage after a few seconds; "مشاهده" opens the drawer
 * so the dealer can approve/reject right away.
 */
export function TokenRequestAlertToast({ request, onView, onDismiss }: TokenRequestAlertToastProps) {
  return (
    <div className="token-request-toast" role="alert">
      <span className="token-request-toast-text">
        درخواست ژتون جدید: <strong>{request.playerDisplayName}</strong> — {request.amount.toLocaleString("fa-IR")} ژتون
      </span>
      <div className="token-request-toast-actions">
        <button className="btn btn-primary" onClick={onView}>
          مشاهده
        </button>
        <button className="btn" onClick={onDismiss} aria-label="بستن">
          ✕
        </button>
      </div>
    </div>
  );
}
