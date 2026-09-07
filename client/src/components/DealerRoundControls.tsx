import type { GroupStateDto } from "../api/types";
import { RoundTimer } from "./RoundTimer";
import { CopyButton } from "./CopyButton";

interface DealerRoundControlsProps {
  groupState: GroupStateDto;
  /** Seconds left in the betting window, or null when there's no active betting countdown. */
  secondsLeft: number | null;
  onStartRound: () => void;
  onRollDice: () => void;
  startingRound: boolean;
  rollingDice: boolean;
}

/**
 * The dealer's always-visible round controls (start round / roll dice), split out of the full
 * DealerPanel so they stay on the main screen even though the rest of the dealer panel (limits,
 * denominations, token requests, player list) moved into the side drawer for a more compact page.
 */
export function DealerRoundControls({
  groupState,
  secondsLeft,
  onStartRound,
  onRollDice,
  startingRound,
  rollingDice,
}: DealerRoundControlsProps) {
  const canRoll = groupState.activeRound && groupState.activeRound.status === "Betting";
  const canStart = !groupState.activeRound || groupState.activeRound.status === "Closed";
  // Once the countdown hits 0 and rolling is actually possible, glow the roll button so the
  // dealer's eye is drawn straight to it instead of having to notice the timer text separately.
  const rollReady = !!canRoll && secondsLeft !== null && secondsLeft <= 0;

  return (
    <div className="dealer-round-controls">
      <div className="dealer-panel-invite">
        کد دعوت گروه: <strong>{groupState.inviteCode}</strong>
        <CopyButton text={groupState.inviteCode} label="کپی کد دعوت" />
      </div>
      {canRoll && secondsLeft !== null && <RoundTimer secondsLeft={secondsLeft} variant="dealer" />}
      <div className="dealer-panel-actions">
        <button disabled={!canStart || startingRound} onClick={onStartRound} className="btn btn-primary">
          {startingRound ? "..." : "شروع دور جدید"}
        </button>
        <button
          disabled={!canRoll || rollingDice}
          onClick={onRollDice}
          className={["btn", "btn-danger", rollReady ? "roll-ready" : ""].join(" ")}
        >
          {rollingDice ? "در حال پرتاب..." : "پرتاب تاس"}
        </button>
      </div>
    </div>
  );
}
