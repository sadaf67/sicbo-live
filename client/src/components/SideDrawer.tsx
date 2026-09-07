import type { ReactNode } from "react";

interface SideDrawerProps {
  open: boolean;
  onClose: () => void;
  children: ReactNode;
}

/**
 * Slide-in panel (hamburger menu) that holds the secondary table-page sections — video, chat,
 * dealer settings, "my bets" list — so the main screen only shows the board + dice + chips and
 * stays compact. Closes on backdrop click or the ✕ button; content keeps running (chat/video stay
 * mounted) while closed, it's purely a CSS transform off-screen, not an unmount.
 */
export function SideDrawer({ open, onClose, children }: SideDrawerProps) {
  return (
    <>
      <div className={`drawer-backdrop ${open ? "open" : ""}`} onClick={onClose} aria-hidden={!open} />
      <aside className={`side-drawer ${open ? "open" : ""}`} aria-hidden={!open}>
        <div className="side-drawer-header">
          <button type="button" className="btn side-drawer-close" onClick={onClose}>
            ✕ بستن
          </button>
        </div>
        <div className="side-drawer-content">{children}</div>
      </aside>
    </>
  );
}
