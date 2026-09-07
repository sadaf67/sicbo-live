export function WalletBadge({ balance, label }: { balance: number; label?: string }) {
  return (
    <div className="wallet-badge">
      <span className="wallet-badge-label">{label ?? "موجودی ژتون"}</span>
      <span className="wallet-badge-amount">{balance.toLocaleString("fa-IR")}</span>
    </div>
  );
}
