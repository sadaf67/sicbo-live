import type { AllBetDto } from "../api/types";

interface AllBetsPanelProps {
  bets: AllBetDto[];
}

const OUTCOME_LABELS: Record<string, string> = {
  Pending: "در انتظار",
  Won: "برنده",
  Lost: "باخته",
};

// Optional visibility for dealer + players alike (see hamburger drawer): everyone's bets in the
// active round, grouped by bettor so it reads like a table of who bet what rather than a flat log.
// Fed from GroupStateDto.allBetsInActiveRound, which already refreshes live whenever anyone places
// or cancels a bet (WalletUpdated fires table-wide on both, and TablePage's onWalletUpdated handler
// already calls refreshState() for every connected client - no new SignalR event needed here).
export function AllBetsPanel({ bets }: AllBetsPanelProps) {
  if (bets.length === 0) {
    return (
      <div className="all-bets-panel">
        <h4>شرط‌های همه بازیکنان در این دور</h4>
        <p className="dealer-panel-empty">هنوز شرطی ثبت نشده است.</p>
      </div>
    );
  }

  const byPlayer = new Map<string, AllBetDto[]>();
  for (const bet of bets) {
    const list = byPlayer.get(bet.playerDisplayName) ?? [];
    list.push(bet);
    byPlayer.set(bet.playerDisplayName, list);
  }

  return (
    <div className="all-bets-panel">
      <h4>شرط‌های همه بازیکنان در این دور</h4>
      <ul>
        {[...byPlayer.entries()].map(([playerName, playerBets]) => (
          <li key={playerName}>
            <span className="all-bets-player-name">{playerName}</span>
            <ul className="all-bets-player-sublist">
              {playerBets.map((b) => (
                <li key={b.betId}>
                  {b.betTypeCode}: {b.amount.toLocaleString("fa-IR")}
                  {b.outcome !== "Pending" && ` — ${OUTCOME_LABELS[b.outcome] ?? b.outcome}`}
                  {b.winAmount ? ` (+${b.winAmount.toLocaleString("fa-IR")})` : ""}
                </li>
              ))}
            </ul>
          </li>
        ))}
      </ul>
    </div>
  );
}
