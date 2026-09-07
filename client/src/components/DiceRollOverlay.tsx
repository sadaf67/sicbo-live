import { useEffect, useState } from "react";
import { DiceFace } from "./DiceFace";

interface DiceRollOverlayProps {
  visible: boolean;
  durationSeconds: number;
}

/**
 * Full-screen "dice shaker" overlay shown to every connected user (dealer + players) while a round's
 * dice are settling. The backend rolls and settles a round synchronously in one request (see
 * RollDiceCommand), so DiceRolled and RoundSettled arrive back-to-back with no real suspense gap.
 * To give the table a proper "dice are being shaken" moment, TablePage buffers the RoundSettled
 * payload and only reveals it once this overlay's countdown finishes — this component only
 * renders the animation + countdown, it has no knowledge of the real result.
 *
 * Modeled after a physical dice shaker the user photographed: a black cylindrical base topped with
 * a clear acrylic dome, green felt visible on the floor inside, three dice rattling around on that
 * felt (not falling through a tower - see the dice-dome-* rules in index.css for the shake keyframes).
 * Replaces the earlier "acrylic dice tower" design (dice dropping in from the top and cascading down
 * zigzag baffles) per the user's follow-up request to match this specific device instead.
 */
export function DiceRollOverlay({ visible, durationSeconds }: DiceRollOverlayProps) {
  const [secondsLeft, setSecondsLeft] = useState(durationSeconds);
  const [faces, setFaces] = useState<[number, number, number]>([1, 1, 1]);

  useEffect(() => {
    if (!visible) return;
    setSecondsLeft(durationSeconds);
    const countdownInterval = setInterval(() => {
      setSecondsLeft((s) => Math.max(0, s - 1));
    }, 1000);
    return () => clearInterval(countdownInterval);
  }, [visible, durationSeconds]);

  useEffect(() => {
    if (!visible) return;
    const spinInterval = setInterval(() => {
      setFaces([
        1 + Math.floor(Math.random() * 6),
        1 + Math.floor(Math.random() * 6),
        1 + Math.floor(Math.random() * 6),
      ]);
    }, 120);
    return () => clearInterval(spinInterval);
  }, [visible]);

  if (!visible) return null;

  return (
    <div className="dice-roll-overlay" role="alert">
      {/* Dice shaker: a soft ambient glow behind the whole device, a clear acrylic dome (glass
          highlight sweep for a curved-surface look) with the green felt floor visible at the
          bottom, three dice rattling on that felt, a metal trim ring where the dome meets the
          base, and a black base plaque with two bolt heads - matching the reference photo's
          "black round base + clear dome + green felt + shaking dice" device. */}
      <div className="dice-dome">
        <div className="dice-dome-glow" />
        <div className="dice-dome-glass">
          <span className="dice-dome-highlight" />
          <div className="dice-dome-felt">
            {[0, 1, 2].map((i) => (
              <div key={i} className={`dice-dome-die dice-dome-die-${i}`}>
                <DiceFace value={faces[i]} size={30} />
              </div>
            ))}
          </div>
        </div>
        <div className="dice-dome-ring" />
        <div className="dice-dome-base">
          <span className="dice-dome-bolt dice-dome-bolt-l" />
          <span className="dice-dome-bolt dice-dome-bolt-r" />
        </div>
      </div>
      <div className="dice-roll-overlay-text">دیلر تاس را انداخت — تاس‌ها در حال تکان خوردن...</div>
      <div className="dice-roll-overlay-countdown">{secondsLeft} ثانیه</div>
    </div>
  );
}
