import type { BetTypeDto, DiceResultDto } from "../api/types";

function countOf(dice: DiceResultDto, face: number): number {
  return [dice.die1, dice.die2, dice.die3].filter((d) => d === face).length;
}

function contains(dice: DiceResultDto, face: number): boolean {
  return dice.die1 === face || dice.die2 === face || dice.die3 === face;
}

/** Mirrors SicBoLive.Domain.Services.SicBoPayoutEngine — used only for board highlighting. */
export function isWinningBet(bet: BetTypeDto, dice: DiceResultDto): boolean {
  switch (bet.category) {
    case "BigSmall": {
      if (dice.isTriple) return false;
      return bet.code === "BIG" ? dice.total >= 11 && dice.total <= 17 : dice.total >= 4 && dice.total <= 10;
    }
    case "OddEven": {
      if (dice.isTriple) return false;
      return bet.code === "ODD" ? dice.total % 2 === 1 : dice.total % 2 === 0;
    }
    case "Total":
      if (dice.isTriple) return false;
      return bet.requiredTotal != null && dice.total === bet.requiredTotal;
    case "SingleNumber":
      return countOf(dice, bet.faces[0]) > 0;
    case "DoubleNumber":
      return countOf(dice, bet.faces[0]) >= 2;
    case "SpecificTriple":
      return dice.isTriple && dice.die1 === bet.faces[0];
    case "AnyTriple":
      return dice.isTriple;
    case "Combination":
      return contains(dice, bet.faces[0]) && contains(dice, bet.faces[1]);
    case "SpecificDouble":
      return countOf(dice, bet.faces[0]) === 2 && countOf(dice, bet.faces[2]) === 1;
    case "ThreeNumberCombo":
      return contains(dice, bet.faces[0]) && contains(dice, bet.faces[1]) && contains(dice, bet.faces[2]);
    default:
      return false;
  }
}
