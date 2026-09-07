const PIP_LAYOUTS: Record<number, [number, number][]> = {
  1: [[1, 1]],
  2: [[0, 0], [2, 2]],
  3: [[0, 0], [1, 1], [2, 2]],
  4: [[0, 0], [0, 2], [2, 0], [2, 2]],
  5: [[0, 0], [0, 2], [1, 1], [2, 0], [2, 2]],
  6: [[0, 0], [0, 2], [1, 0], [1, 2], [2, 0], [2, 2]],
};

export function DiceFace({ value, size = 28 }: { value: number; size?: number }) {
  const pips = PIP_LAYOUTS[value] ?? [];
  return (
    <div
      className="dice-face"
      style={{
        width: size,
        height: size,
        display: "grid",
        gridTemplateColumns: "repeat(3, 1fr)",
        gridTemplateRows: "repeat(3, 1fr)",
        background: "#fff",
        borderRadius: size * 0.18,
        border: "1px solid #999",
        padding: size * 0.08,
        boxShadow: "0 1px 2px rgba(0,0,0,0.4)",
      }}
    >
      {[0, 1, 2].map((row) =>
        [0, 1, 2].map((col) => {
          const active = pips.some(([r, c]) => r === row && c === col);
          return (
            <div key={`${row}-${col}`} style={{ display: "flex", alignItems: "center", justifyContent: "center" }}>
              {active && (
                <div
                  style={{
                    width: "60%",
                    height: "60%",
                    borderRadius: "50%",
                    background: "#c0141c",
                  }}
                />
              )}
            </div>
          );
        })
      )}
    </div>
  );
}
