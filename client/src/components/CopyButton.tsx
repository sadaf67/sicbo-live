import { useState } from "react";

interface CopyButtonProps {
  text: string;
  label?: string;
}

/**
 * Small inline "copy to clipboard" icon button - used next to invite codes so a dealer can share
 * one without having to manually select/copy the text. Shows a brief "کپی شد!" confirmation
 * instead of a silent no-feedback copy, since clipboard writes have no other visible effect.
 */
export function CopyButton({ text, label = "کپی کردن" }: CopyButtonProps) {
  const [copied, setCopied] = useState(false);

  async function handleCopy() {
    try {
      await navigator.clipboard.writeText(text);
    } catch {
      // Fallback for browsers/contexts without Clipboard API access (e.g. older WebViews).
      const el = document.createElement("textarea");
      el.value = text;
      el.style.position = "fixed";
      el.style.opacity = "0";
      document.body.appendChild(el);
      el.select();
      try {
        document.execCommand("copy");
      } catch {
        // best-effort - nothing more we can do here
      }
      document.body.removeChild(el);
    }
    setCopied(true);
    setTimeout(() => setCopied(false), 1500);
  }

  return (
    <button type="button" className="copy-icon-btn" onClick={handleCopy} aria-label={label} title={label}>
      {copied ? "کپی شد!" : "📋"}
    </button>
  );
}
