import { useState, type ReactNode } from "react";
import { DiceFace } from "./DiceFace";

interface BetSpotProps {
  label?: ReactNode;
  payoutText?: ReactNode;
  /** Small colored strip rendered above the label, e.g. "BIG" / "ANY TRIPLE" headline banners on the felt. */
  banner?: ReactNode;
  /** Bar color for `banner` - gold (default, matches BIG/ANY TRIPLE) or blue (used for SMALL, echoing the existing blue-for-small/gold-for-big convention used elsewhere on the board). */
  bannerVariant?: "gold" | "blue";
  faces?: number[];
  myAmount?: number;
  isWinner?: boolean;
  isLoser?: boolean;
  disabled?: boolean;
  accent?: "red" | "black" | "gold" | "blue" | "split-blue-pink" | "split-gold-purple";
  /** Cell reads visually larger (used for the BIG/SMALL/ODD/EVEN headline tiles and the big single-die row). */
  size?: "normal" | "large";
  onClick?: () => void;
  /** Called when a chip dragged from the chip selector is dropped on this spot, with that chip's value. */
  onDropChip?: (amount: number) => void;
  /** Called when the player clicks their own placed-chip badge to take it back (full refund of this spot), or, once cancelling is blocked past the countdown, to start relocating it instead. */
  onRemoveBet?: () => void;
  /** This spot is the currently selected source of an in-progress bet move (see SicBoBoard.movingBet). */
  isMoveSource?: boolean;
}

export function BetSpot({
  label,
  payoutText,
  banner,
  bannerVariant = "gold",
  faces,
  myAmount,
  isWinner,
  isLoser,
  disabled,
  accent = "black",
  size = "normal",
  onClick,
  onDropChip,
  onRemoveBet,
  isMoveSource,
}: BetSpotProps) {
  const [dragOver, setDragOver] = useState(false);

  return (
    <div
      role="button"
      tabIndex={disabled ? -1 : 0}
      aria-disabled={disabled}
      onClick={() => {
        if (!disabled) onClick?.();
      }}
      onKeyDown={(e) => {
        if (disabled) return;
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault();
          onClick?.();
        }
      }}
      onDragOver={(e) => {
        if (disabled) return;
        e.preventDefault();
        e.dataTransfer.dropEffect = "copy";
        setDragOver(true);
      }}
      onDragLeave={() => setDragOver(false)}
      onDrop={(e) => {
        e.preventDefault();
        setDragOver(false);
        if (disabled) return;
        const amount = Number(e.dataTransfer.getData("text/plain"));
        if (amount > 0) onDropChip?.(amount);
      }}
      className={[
        "bet-spot",
        `accent-${accent}`,
        size === "large" ? "bet-spot-large" : "",
        isWinner ? "winner" : "",
        isLoser ? "loser" : "",
        disabled ? "disabled" : "",
        dragOver ? "drag-over" : "",
        isMoveSource ? "move-source" : "",
      ].join(" ")}
    >
      {banner ? (
        <div className={`bet-spot-banner ${bannerVariant === "blue" ? "bet-spot-banner-blue" : ""}`}>{banner}</div>
      ) : null}
      {faces && faces.length > 0 ? (
        <div className="bet-spot-faces">
          {faces.map((f, i) => (
            <DiceFace key={i} value={f} size={size === "large" ? 34 : 26} />
          ))}
        </div>
      ) : label ? (
        <div className="bet-spot-label">{label}</div>
      ) : null}
      {payoutText ? <div className="bet-spot-payout">{payoutText}</div> : null}
      {myAmount ? (
        <button
          type="button"
          className="bet-spot-chip"
          disabled={disabled}
          title="برای پس گرفتن این شرط کلیک کنید"
          onClick={(e) => {
            e.stopPropagation();
            if (!disabled) onRemoveBet?.();
          }}
        >
          {myAmount}
        </button>
      ) : null}
    </div>
  );
}
