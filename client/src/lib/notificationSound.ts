// Synthesizes a short two-tone alert "ding" via the Web Audio API instead of shipping/loading an
// audio asset file - keeps the bundle asset-free and sidesteps any question about the provenance
// of a real sound file. Used to alert the dealer when a player submits a token request (see
// TablePage's onTokenRequested handler), since that SignalR event is broadcast to the whole table
// group and would otherwise be silent/easy to miss if the dealer isn't looking at the drawer.
let sharedCtx: AudioContext | null = null;

function getContext(): AudioContext | null {
  try {
    const Ctor = window.AudioContext ?? (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!Ctor) return null;
    if (!sharedCtx) sharedCtx = new Ctor();
    return sharedCtx;
  } catch {
    return null;
  }
}

function tone(ctx: AudioContext, freq: number, startAt: number, duration: number) {
  const osc = ctx.createOscillator();
  const gain = ctx.createGain();
  osc.type = "sine";
  osc.frequency.value = freq;
  // Quick fade in/out avoids an audible click at the start/end of each tone.
  gain.gain.setValueAtTime(0, startAt);
  gain.gain.linearRampToValueAtTime(0.3, startAt + 0.02);
  gain.gain.linearRampToValueAtTime(0, startAt + duration);
  osc.connect(gain);
  gain.connect(ctx.destination);
  osc.start(startAt);
  osc.stop(startAt + duration + 0.02);
}

/**
 * Plays a short "ding-dong" alert. Browsers suspend a freshly-created AudioContext until a user
 * gesture happens on the page (autoplay policy) - we opportunistically try to resume it, but if
 * that fails (e.g. the very first alert right after page load, before any click) this silently
 * no-ops rather than throwing, since a missed alert sound is much better than a crashed handler.
 */
export function playTokenRequestAlarm() {
  const ctx = getContext();
  if (!ctx) return;
  const run = () => {
    const now = ctx.currentTime;
    tone(ctx, 880, now, 0.18);
    tone(ctx, 660, now + 0.2, 0.22);
  };
  if (ctx.state === "suspended") {
    ctx.resume().then(run).catch(() => {});
  } else {
    run();
  }
}
