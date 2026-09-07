import { useEffect, useState } from "react";
import { api } from "../api/client";
import type { RoundHistoryItemDto } from "../api/types";

interface RoundHistoryPanelProps {
  groupId: string;
  /** Bump this whenever a round finishes so the list refetches (drawer content stays mounted
   * while closed, so it won't otherwise notice new rounds on its own). */
  refreshSignal: number;
}

/**
 * "دور‌های قبلی" list in the hamburger drawer - shows the dice results of past closed rounds for
 * this table, newest first, so a player can check what was rolled earlier without having watched
 * the DiceRollOverlay live (e.g. they joined mid-session or looked away).
 */
export function RoundHistoryPanel({ groupId, refreshSignal }: RoundHistoryPanelProps) {
  const [items, setItems] = useState<RoundHistoryItemDto[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!groupId) return;
    setLoading(true);
    api
      .get<RoundHistoryItemDto[]>(`/api/groups/${groupId}/rounds/history`)
      .then((res) => setItems(res.data))
      .catch(() => {
        // best-effort panel - keep showing the last known list rather than an error banner
      })
      .finally(() => setLoading(false));
  }, [groupId, refreshSignal]);

  return (
    <div className="round-history-panel">
      <h4>تاس‌های دورهای قبلی</h4>
      {loading && items.length === 0 && <p className="dealer-panel-empty">در حال بارگذاری...</p>}
      {!loading && items.length === 0 && <p className="dealer-panel-empty">هنوز دوری به پایان نرسیده است.</p>}
      <ul>
        {items.map((item) => (
          <li key={item.roundId}>
            دور #{item.roundNumber}:{" "}
            {item.dice
              ? `${item.dice.die1}-${item.dice.die2}-${item.dice.die3} (مجموع ${item.dice.total}${item.dice.isTriple ? " — سه‌تایی" : ""})`
              : "بدون نتیجه"}
          </li>
        ))}
      </ul>
    </div>
  );
}
