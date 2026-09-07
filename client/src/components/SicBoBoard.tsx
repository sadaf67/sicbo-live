import { useMemo, type ReactNode } from "react";
import type { BetTypeDto, DiceResultDto, MyBetDto } from "../api/types";
import { BetSpot } from "./BetSpot";
import { isWinningBet } from "../lib/payoutRules";

interface SicBoBoardProps {
  betTypes: BetTypeDto[];
  myBets: MyBetDto[];
  lastDice: DiceResultDto | null;
  disabled: boolean;
  /** amount is provided when the bet came from dragging a chip; omitted for a plain click (caller uses the currently selected chip amount). */
  onBet: (bet: BetTypeDto, amount?: number) => void;
  /** Player changed their mind and wants to take back everything they've placed on this spot this round (or, once the betting countdown is over, starts a move instead - see TablePage.handleRemoveBet). */
  onRemoveBet: (bet: BetTypeDto) => void;
  /** Set while the player has a bet selected to relocate (grace period only, after cancelling is
   * blocked but moving still isn't - see GameRound.EnsureBetMovable). While set, clicking or
   * dropping a chip on any other spot picks it as the move's destination instead of placing/removing
   * a bet there. */
  movingBet?: BetTypeDto | null;
  /** Confirms the destination spot for a bet move already in progress (movingBet set). */
  onMoveTarget?: (bet: BetTypeDto) => void;
}

/**
 * Small (sum < 11) spots are colored blue, big (sum >= 11) spots gold - except the two exact sums
 * that sit right on the Small/Big boundary (10 and 11), which get a distinct split-color treatment
 * so they visually stand out from the rest of the left-column grids: sum 10 -> half blue/half pink,
 * sum 11 -> half gold/half dark purple.
 */
function bigSmallAccent(bet: BetTypeDto): "blue" | "gold" | "split-blue-pink" | "split-gold-purple" {
  const sum = bet.faces.reduce((s, f) => s + f, 0);
  if (sum === 10) return "split-blue-pink";
  if (sum === 11) return "split-gold-purple";
  return sum < 11 ? "blue" : "gold";
}

/** Same small/big convention applied to the Total (4-17) row: 4-10 blue like SMALL, 11-17 gold like BIG. */
function totalAccent(bet: BetTypeDto): "blue" | "gold" {
  return (bet.requiredTotal ?? 0) < 11 ? "blue" : "gold";
}

/** If every bet type in the group pays the exact same multiplier, return it so we can show one big banner ("1 WINS X", like the physical felt). Mixed-payout groups (Totals) return undefined and keep their own per-spot text instead. */
function uniformMultiplier(bts: BetTypeDto[] | undefined): number | undefined {
  if (!bts || bts.length === 0) return undefined;
  const first = bts[0].multiplier;
  return bts.every((b) => b.multiplier === first) ? first : undefined;
}

/** Bold "1 WINS X" banner, the header bar printed above every payout group on the reference felt. */
function GroupBanner({ multiplier }: { multiplier: number }) {
  return (
    <div className="group-banner">
      1 WINS <span className="group-banner-num">{multiplier}</span>
    </div>
  );
}

export function SicBoBoard({
  betTypes,
  myBets,
  lastDice,
  disabled,
  onBet,
  onRemoveBet,
  movingBet,
  onMoveTarget,
}: SicBoBoardProps) {
  const byCategory = useMemo(() => {
    const map: Record<string, BetTypeDto[]> = {};
    for (const bt of betTypes) {
      (map[bt.category] ??= []).push(bt);
    }
    // Descending (17 -> 4): the board is forced left-to-right (see .sicbo-board direction: ltr in
    // index.css) to physically match the reference felt photo regardless of the app's Persian RTL
    // layout, so the first array item lands on the left, same as "17" being leftmost in the photo.
    map.Total?.sort((a, b) => (b.requiredTotal ?? 0) - (a.requiredTotal ?? 0));
    for (const key of ["SingleNumber", "DoubleNumber", "SpecificTriple"]) {
      map[key]?.sort((a, b) => a.faces[0] - b.faces[0]);
    }
    map.Combination?.sort((a, b) => a.faces[0] - b.faces[0] || a.faces[1] - b.faces[1]);
    map.SpecificDouble?.sort((a, b) => a.faces[0] - b.faces[0] || a.faces[2] - b.faces[2]);
    map.ThreeNumberCombo?.sort((a, b) => a.faces[0] - b.faces[0] || a.faces[1] - b.faces[1] || a.faces[2] - b.faces[2]);
    return map;
  }, [betTypes]);

  const myAmountByCode = useMemo(() => {
    const map: Record<string, number> = {};
    for (const b of myBets) map[b.betTypeCode] = (map[b.betTypeCode] ?? 0) + b.amount;
    return map;
  }, [myBets]);

  function renderSpot(
    bt: BetTypeDto,
    opts: {
      accent?: "red" | "black" | "gold" | "blue" | "split-blue-pink" | "split-gold-purple";
      faces?: number[] | null;
      size?: "normal" | "large";
      banner?: ReactNode;
      bannerVariant?: "gold" | "blue";
      payout?: ReactNode;
      label?: ReactNode;
    } = {},
  ) {
    const { accent = "black", faces, size = "normal", banner, bannerVariant, payout, label } = opts;
    const won = lastDice ? isWinningBet(bt, lastDice) : false;
    return (
      <BetSpot
        key={bt.code}
        label={label !== undefined ? label : bt.displayLabel}
        payoutText={payout}
        banner={banner}
        bannerVariant={bannerVariant}
        faces={faces === null ? [] : faces ?? bt.faces}
        size={size}
        myAmount={myAmountByCode[bt.code]}
        isWinner={!!lastDice && won}
        isLoser={!!lastDice && !won && !!myAmountByCode[bt.code]}
        disabled={disabled}
        accent={accent}
        isMoveSource={movingBet?.code === bt.code}
        onClick={() => (movingBet ? onMoveTarget?.(bt) : onBet(bt))}
        onDropChip={(amount) => (movingBet ? onMoveTarget?.(bt) : onBet(bt, amount))}
        onRemoveBet={() => onRemoveBet(bt)}
      />
    );
  }

  const big = betTypes.find((b) => b.code === "BIG");
  const small = betTypes.find((b) => b.code === "SMALL");
  const odd = betTypes.find((b) => b.code === "ODD");
  const even = betTypes.find((b) => b.code === "EVEN");
  const anyTriple = betTypes.find((b) => b.category === "AnyTriple");

  const specDblMultiplier = uniformMultiplier(byCategory.SpecificDouble);
  const combo3Multiplier = uniformMultiplier(byCategory.ThreeNumberCombo);
  const doubleMultiplier = uniformMultiplier(byCategory.DoubleNumber);
  const tripleMultiplier = uniformMultiplier(byCategory.SpecificTriple);
  const comboMultiplier = uniformMultiplier(byCategory.Combination);

  return (
    <div className="sicbo-board">
      <div className="board-layout">
        {/* Left column: the two big plain-number grids, exactly like the felt's left-side panels. */}
        <div className="board-layout-left">
          <div className="board-panel">
            {specDblMultiplier !== undefined && <GroupBanner multiplier={specDblMultiplier} />}
            <div className="board-row specdbl-row">
              {map(byCategory.SpecificDouble, (bt) => renderSpot(bt, { accent: bigSmallAccent(bt), faces: null }))}
            </div>
          </div>

          <div className="board-panel">
            {combo3Multiplier !== undefined && <GroupBanner multiplier={combo3Multiplier} />}
            <div className="board-row combo3-row">
              {map(byCategory.ThreeNumberCombo, (bt) => renderSpot(bt, { accent: bigSmallAccent(bt), faces: null }))}
            </div>
          </div>
        </div>

        <div className="board-layout-right">
          <div className="board-row top-triband">
            <div className="side-col">
              {even && renderSpot(even, { accent: "black", size: "large" })}
              {big &&
                renderSpot(big, {
                  accent: "red",
                  size: "large",
                  banner: "BIG",
                  label: null,
                  payout: (
                    <>
                      <div>11-17</div>
                      <div>ONE WINS ONE</div>
                    </>
                  ),
                })}
            </div>

            <div className="mid-col">
              <div className="board-panel">
                {doubleMultiplier !== undefined && <GroupBanner multiplier={doubleMultiplier} />}
                <div className="board-row doubles-row">
                  {map(byCategory.DoubleNumber, (bt) => renderSpot(bt, { faces: [bt.faces[0], bt.faces[0]] }))}
                </div>
              </div>

              <div className="board-panel">
                {tripleMultiplier !== undefined && <GroupBanner multiplier={tripleMultiplier} />}
                <div className="board-row triples-row">
                  {anyTriple &&
                    renderSpot(anyTriple, {
                      accent: "red",
                      banner: "ANY TRIPLE",
                      label: null,
                      payout: anyTriple.multiplier !== undefined ? `1 WINS ${anyTriple.multiplier}` : undefined,
                    })}
                  {map(byCategory.SpecificTriple, (bt) =>
                    renderSpot(bt, { accent: "red", faces: [bt.faces[0], bt.faces[0], bt.faces[0]] }),
                  )}
                </div>
              </div>
            </div>

            <div className="side-col">
              {odd && renderSpot(odd, { accent: "black", size: "large" })}
              {small &&
                renderSpot(small, {
                  accent: "red",
                  size: "large",
                  banner: "SMALL",
                  bannerVariant: "blue",
                  label: null,
                  payout: (
                    <>
                      <div>4-10</div>
                      <div>ONE WINS ONE</div>
                    </>
                  ),
                })}
            </div>
          </div>

          <div className="board-panel">
            <div className="board-row totals-row">
              {map(byCategory.Total, (bt) => renderSpot(bt, { accent: totalAccent(bt), payout: `1 WINS ${bt.multiplier}` }))}
            </div>
          </div>

          <div className="board-panel">
            {comboMultiplier !== undefined && <GroupBanner multiplier={comboMultiplier} />}
            <div className="board-row combos-row">{map(byCategory.Combination, (bt) => renderSpot(bt))}</div>
          </div>

          <div className="board-panel">
            <div className="board-row singles-row">
              {map(byCategory.SingleNumber, (bt) => renderSpot(bt, { size: "large" }))}
            </div>
            <div className="single-caption">1 TO 1 ON ONE DIE . . . 2 TO 1 ON TWO DICE . . . 3 TO 1 ON THREE DICE</div>
          </div>
        </div>
      </div>
    </div>
  );
}

function map<T>(arr: T[] | undefined, fn: (item: T) => ReactNode) {
  return (arr ?? []).map(fn);
}
