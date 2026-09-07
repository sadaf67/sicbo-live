interface RoundTimerProps {
  secondsLeft: number;
  /** Dealer sees "get ready to roll" framing once time hits 0; player sees "betting closed". */
  variant: "player" | "dealer";
}

/**
 * Prominent big-number countdown for the active round's betting window. Replaces the old plain
 * sentence ("... - N ثانیه باقی‌مانده") buried in the status row - a dealer running a live table
 * needs to know at a glance, without reading a sentence, exactly when betting time is over and
 * it's time to roll; a player needs the same at-a-glance read of how long they still have to bet.
 */
export function RoundTimer({ secondsLeft, variant }: RoundTimerProps) {
  const urgent = secondsLeft > 0 && secondsLeft <= 5;
  const timeUp = secondsLeft <= 0;

  return (
    <div className={["round-timer", urgent ? "round-timer-urgent" : "", timeUp ? "round-timer-done" : ""].join(" ")}>
      <div className="round-timer-value">{Math.max(0, secondsLeft)}</div>
      <div className="round-timer-label">
        {variant === "dealer"
          ? timeUp
            ? "زمان تمام شد — پرتاب تاس!"
            : "زمان شرط‌بندی بازیکنان"
          : timeUp
            ? "زمان شرط‌بندی تمام شد"
            : "زمان باقی‌مانده برای شرط‌بندی"}
      </div>
    </div>
  );
}
