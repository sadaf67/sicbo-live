/**
 * Small credit line shown at the bottom of every page: "تهیه شده توسط sadafkohan.ir".
 * Links out to the portfolio site in a new tab. Purely presentational, no logic.
 */
export function SiteFooter() {
  return (
    <footer className="site-footer">
      تهیه شده توسط{" "}
      <a
        href="https://sadafkohan.ir"
        target="_blank"
        rel="noopener noreferrer"
        className="site-footer-link"
      >
        sadafkohan.ir
      </a>
    </footer>
  );
}
