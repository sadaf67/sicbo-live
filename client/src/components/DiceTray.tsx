import { DiceFace } from "./DiceFace";
import type { DiceResultDto } from "../api/types";

export function DiceTray({ dice, rolling }: { dice: DiceResultDto | null; rolling: boolean }) {
  return (
    <div className="dice-tray">
      <div className={`dice-tray-dice ${rolling ? "rolling" : ""}`}>
        <DiceFace value={dice?.die1 ?? 1} size={48} />
        <DiceFace value={dice?.die2 ?? 1} size={48} />
        <DiceFace value={dice?.die3 ?? 1} size={48} />
      </div>
      {dice && (
        <div className="dice-tray-total">
          مجموع: {dice.total} {dice.isTriple ? "— سه‌تایی!" : ""}
        </div>
      )}
    </div>
  );
}
