import { useEffect, useState } from "react";

interface BeforeInstallPromptEvent extends Event {
  prompt: () => Promise<void>;
  userChoice: Promise<{ outcome: "accepted" | "dismissed" }>;
}

function isStandalone() {
  return (
    window.matchMedia("(display-mode: standalone)").matches ||
    // iOS Safari's own non-standard flag for "already added to home screen"
    (navigator as Navigator & { standalone?: boolean }).standalone === true
  );
}

function isIos() {
  return /iphone|ipad|ipod/i.test(navigator.userAgent);
}

/**
 * A small floating "install app" affordance shown on every page. Chrome/Edge/Android fire
 * `beforeinstallprompt` when the site meets installability criteria (manifest + service worker,
 * see public/manifest.webmanifest and public/sw.js) - we capture that event and trigger it from
 * our own styled button instead of relying on the browser's address-bar icon, since most users
 * never notice that icon. iOS Safari never fires this event (Apple doesn't support the API), so
 * there we show a one-line hint pointing at the native Share > "Add to Home Screen" action instead.
 */
export function InstallAppButton() {
  const [deferredPrompt, setDeferredPrompt] = useState<BeforeInstallPromptEvent | null>(null);
  const [installed, setInstalled] = useState(isStandalone());
  const [dismissed, setDismissed] = useState(false);
  const [showIosHint, setShowIosHint] = useState(false);

  useEffect(() => {
    function onBeforeInstallPrompt(e: Event) {
      e.preventDefault();
      setDeferredPrompt(e as BeforeInstallPromptEvent);
    }
    function onInstalled() {
      setInstalled(true);
      setDeferredPrompt(null);
    }
    window.addEventListener("beforeinstallprompt", onBeforeInstallPrompt);
    window.addEventListener("appinstalled", onInstalled);
    return () => {
      window.removeEventListener("beforeinstallprompt", onBeforeInstallPrompt);
      window.removeEventListener("appinstalled", onInstalled);
    };
  }, []);

  if (installed || dismissed) return null;
  if (!deferredPrompt && !isIos()) return null;

  async function handleClick() {
    if (deferredPrompt) {
      await deferredPrompt.prompt();
      const { outcome } = await deferredPrompt.userChoice;
      if (outcome === "accepted") setInstalled(true);
      setDeferredPrompt(null);
      return;
    }
    // iOS has no programmatic install prompt - walk the user through the manual step instead.
    setShowIosHint((v) => !v);
  }

  return (
    <div className="install-app-banner">
      <button className="btn btn-primary install-app-btn" onClick={handleClick}>
        نصب اپلیکیشن
      </button>
      <button className="install-app-dismiss" onClick={() => setDismissed(true)} aria-label="بستن">
        ×
      </button>
      {showIosHint && (
        <div className="install-app-ios-hint">
          برای نصب: دکمه اشتراک‌گذاری Safari را بزنید و «Add to Home Screen» را انتخاب کنید.
        </div>
      )}
    </div>
  );
}
