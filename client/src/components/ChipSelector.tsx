const PRESET_CHIPS = [1, 5, 10, 25, 50, 100, 200, 500, 1000, 5000];

interface ChipSelectorProps {
  selected: number;
  onSelect: (value: number) => void;
  balance: number;
}

export function ChipSelector({ selected, onSelect, balance }: ChipSelectorProps) {
  return (
    <div className="chip-selector">
      <span className="chip-selector-label">مقدار شرط: (برای شرط‌بندی کلیک کنید یا ژتون را روی محل شرط بکشید)</span>
      {PRESET_CHIPS.map((value) => {
        const disabled = value > balance;
        return (
          <button
            key={value}
            type="button"
            disabled={disabled}
            draggable={!disabled}
            className={`chip ${selected === value ? "chip-active" : ""}`}
            onClick={() => onSelect(value)}
            onDragStart={(e) => {
              e.dataTransfer.setData("text/plain", String(value));
              e.dataTransfer.effectAllowed = "copy";
            }}
          >
            {value}
          </button>
        );
      })}
    </div>
  );
}
