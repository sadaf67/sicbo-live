import type { GroupPlayerDto } from "../api/types";

interface DealerPlayerBalancesProps {
  players: GroupPlayerDto[];
}

/**
 * "موجودی ژتون بازیکنان" - a compact, always-visible glance list of every player's current
 * wallet balance at this table. Split out of DealerPanel's existing (drawer-only) player list for
 * the same reason DealerRoundControls was split out earlier: a dealer needs this at a glance
 * during play, not buried behind the hamburger menu's settings drawer. DealerPanel's own player
 * list (in the drawer, next to the "issue tokens" form) is left in place too - that one still
 * matters for picking a recipient while issuing tokens; this one is the fast read-only check.
 */
export function DealerPlayerBalances({ players }: DealerPlayerBalancesProps) {
  if (players.length === 0) return null;

  return (
    <div className="dealer-player-balances">
      <h4>موجودی ژتون بازیکنان</h4>
      <ul>
        {players.map((p) => (
          <li key={p.userId}>
            <span>{p.displayName}</span>
            <span className="dealer-player-balances-amount">{p.balance.toLocaleString("fa-IR")}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}
