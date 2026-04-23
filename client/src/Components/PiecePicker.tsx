import { useState, useEffect } from "react";
import { PIECE_COMPONENTS, PIECE_LABELS, PIECE_ORDER } from "./MonopolyPieces";
import { gameService } from "../services/gameService";

interface Props {
  playerNames: string[];
  onAllPicked: () => void;
}

const PLAYER_COLORS_EXTENDED = [
  "#ef4444",
  "#3b82f6",
  "#eab308",
  "#22c55e",
  "#a855f7",
  "#ec4899",
  "#06b6d4",
  "#f97316",
];

export default function PiecePicker({ playerNames, onAllPicked }: Props) {
  const [available, setAvailable] = useState<Record<string, boolean>>({});
  const [selections, setSelections] = useState<Record<string, string>>({}); // playerName → pieceType
  const [currentIdx, setCurrentIdx] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    gameService.getAvailablePieces().then((pieces) => {
      const map: Record<string, boolean> = {};
      pieces.forEach((p) => (map[p.pieceType] = p.isAvailable));
      setAvailable(map);
    });
  }, []);

  const currentPlayer = playerNames[currentIdx];
  const allDone = currentIdx >= playerNames.length;

  const handlePick = async (pieceType: string) => {
    if (!available[pieceType]) return;
    setLoading(true);
    setError("");
    try {
      await gameService.selectPiece(currentPlayer, pieceType);

      setSelections((prev) => {
        const next = { ...prev, [currentPlayer]: pieceType };
        localStorage.setItem("playerPieces", JSON.stringify(next));
        return next;
      });
      setAvailable((prev) => ({ ...prev, [pieceType]: false }));

      if (currentIdx + 1 >= playerNames.length) {
        onAllPicked();
      } else {
        setCurrentIdx((i) => i + 1);
      }
    } catch (e: any) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  };

  if (allDone) return null;

  return (
    <div className="w-full">
      <div className="flex items-center gap-2 mb-5">
        <span
          className="w-7 h-7 rounded-lg flex items-center justify-center text-lg"
          style={{
            background:
              PLAYER_COLORS_EXTENDED[
                currentIdx % PLAYER_COLORS_EXTENDED.length
              ],
          }}
        >
          {currentIdx + 1}
        </span>
        <div>
          <p className="text-xs uppercase tracking-widest text-zinc-400 font-bold">
            Pilih Token
          </p>
          <p className="text-zinc-100 font-semibold text-sm">{currentPlayer}</p>
        </div>
      </div>

      {Object.keys(selections).length > 0 && (
        <div className="flex gap-2 mb-4 flex-wrap">
          {Object.entries(selections).map(([name, piece]) => {
            const idx = playerNames.indexOf(name);
            const SVG = PIECE_COMPONENTS[piece];
            return (
              <div
                key={name}
                className="flex items-center gap-1.5 bg-zinc-800 rounded-lg px-2.5 py-1.5"
              >
                <SVG
                  size={18}
                  color={
                    PLAYER_COLORS_EXTENDED[idx % PLAYER_COLORS_EXTENDED.length]
                  }
                />
                <span className="text-zinc-400 text-xs">{name}</span>
              </div>
            );
          })}
        </div>
      )}

      <div className="grid grid-cols-4 gap-2">
        {PIECE_ORDER.map((pieceType) => {
          const SVG = PIECE_COMPONENTS[pieceType];
          const isTaken = !available[pieceType];
          const isCurrentPlayerPick = selections[currentPlayer] === pieceType;

          return (
            <button
              key={pieceType}
              onClick={() => handlePick(pieceType)}
              disabled={isTaken || loading}
              className={`
                relative flex flex-col items-center gap-1.5 p-3 rounded-xl border transition-all duration-200
                ${
                  isTaken
                    ? "border-zinc-800 bg-zinc-900 opacity-30 cursor-not-allowed"
                    : `border-zinc-700 bg-zinc-800 hover:border-zinc-500 hover:bg-zinc-750
                       active:scale-95 cursor-pointer`
                }
              `}
              style={
                isCurrentPlayerPick
                  ? {
                      boxShadow: `0 0 0 2px ${PLAYER_COLORS_EXTENDED[currentIdx % PLAYER_COLORS_EXTENDED.length]} inset`,
                    }
                  : undefined
              }
            >
              <SVG
                size={36}
                color={
                  isTaken
                    ? "#555"
                    : PLAYER_COLORS_EXTENDED[
                        currentIdx % PLAYER_COLORS_EXTENDED.length
                      ]
                }
              />
              <span className="text-zinc-400 text-[10px] text-center leading-tight font-medium">
                {PIECE_LABELS[pieceType]}
              </span>
              {isTaken && (
                <div className="absolute inset-0 flex items-center justify-center rounded-xl">
                  <span className="text-zinc-600 text-xs">✕</span>
                </div>
              )}
            </button>
          );
        })}
      </div>

      {error && (
        <p className="mt-3 text-red-400 text-xs text-center">{error}</p>
      )}

      <div className="flex justify-center gap-2 mt-4">
        {playerNames.map((_, i) => (
          <div
            key={i}
            className={`w-2 h-2 rounded-full transition-all ${
              i < currentIdx
                ? "bg-green-500"
                : i === currentIdx
                  ? "bg-red-500 w-4"
                  : "bg-zinc-700"
            }`}
          />
        ))}
      </div>
    </div>
  );
}
