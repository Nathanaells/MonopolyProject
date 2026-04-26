import { useEffect, useState } from "react";

interface PlayerResult {
  name: string;
  balance: number;
  properties: string[];
  isBankrupt: boolean;
}

interface GameOverProps {
  winner: string;
  players: PlayerResult[];
  onPlayAgain: () => void;
}

const CONFETTI_COLORS = [
  "#ef4444", "#3b82f6", "#eab308",
  "#10b981", "#a855f7", "#ec4899",
  "#06b6d4", "#f97316",
];

function ConfettiPiece({ index }: { index: number }) {
  const color = CONFETTI_COLORS[index % CONFETTI_COLORS.length];
  const left = `${(index * 7.3 + 5) % 100}%`;
  const delay = `${(index * 0.17) % 2}s`;
  const duration = `${2.5 + (index % 4) * 0.4}s`;
  const size = index % 3 === 0 ? 8 : 5;
  const isRect = index % 2 === 0;

  return (
    <div
      className="absolute top-0 pointer-events-none animate-bounce"
      style={{
        left,
        animationDelay: delay,
        animationDuration: duration,
        zIndex: 0,
      }}
    >
      <div
        style={{
          width: size,
          height: isRect ? size * 1.6 : size,
          backgroundColor: color,
          borderRadius: isRect ? 1 : "50%",
          opacity: 0.85,
          transform: `rotate(${index * 37}deg)`,
        }}
      />
    </div>
  );
}

export default function GameOver({ winner, players, onPlayAgain }: GameOverProps) {
  const [visible, setVisible] = useState(false);
  const [showPlayers, setShowPlayers] = useState(false);

  useEffect(() => {
    const t1 = setTimeout(() => setVisible(true), 80);
    const t2 = setTimeout(() => setShowPlayers(true), 600);
    return () => { clearTimeout(t1); clearTimeout(t2); };
  }, []);

  const sorted = [...players].sort((a, b) => b.balance - a.balance);

  return (
    <div
      className={`fixed inset-0 z-50 flex items-center justify-center transition-all duration-500 ${
        visible ? "opacity-100" : "opacity-0"
      }`}
      style={{ background: "rgba(9,9,11,0.92)" }}
    >
      <div className="absolute inset-0 overflow-hidden pointer-events-none">
        {Array.from({ length: 28 }).map((_, i) => (
          <ConfettiPiece key={i} index={i} />
        ))}
      </div>

      <div
        className={`relative z-10 w-full max-w-md mx-4 transition-all duration-500 ${
          visible ? "scale-100 translate-y-0" : "scale-90 translate-y-8"
        }`}
      >
        <div className="bg-zinc-900 border border-zinc-700 rounded-2xl overflow-hidden shadow-2xl">

          <div className="relative bg-zinc-950 px-6 pt-8 pb-6 text-center border-b border-zinc-800">
            <div className="text-5xl mb-3 select-none">🏆</div>
            <p className="text-[10px] uppercase tracking-[0.25em] text-zinc-500 font-bold mb-1">
              Game Over
            </p>
            <h1 className="text-2xl font-black text-white tracking-tight">
              {winner}
            </h1>
            <p className="text-sm text-emerald-400 font-bold mt-1">
              wins the game!
            </p>

            <div
              className="absolute top-0 left-0 right-0 h-0.5"
              style={{
                background:
                  "linear-gradient(90deg, transparent, #ef4444, #eab308, #10b981, transparent)",
              }}
            />
          </div>

          <div className="px-4 py-4 space-y-2">
            <p className="text-[9px] uppercase tracking-widest text-zinc-600 font-bold mb-3 px-1">
              Final Standings
            </p>
            {sorted.map((p, i) => {
              const isWinner = p.name === winner;
              const medal = i === 0 ? "🥇" : i === 1 ? "🥈" : i === 2 ? "🥉" : null;

              return (
                <div
                  key={p.name}
                  className={`flex items-center gap-3 rounded-xl px-3 py-2.5 transition-all duration-300 ${
                    showPlayers
                      ? "opacity-100 translate-x-0"
                      : "opacity-0 -translate-x-4"
                  } ${
                    isWinner
                      ? "bg-amber-950/40 border border-amber-700/50"
                      : "bg-zinc-800/50 border border-zinc-700/30"
                  }`}
                  style={{ transitionDelay: `${i * 100}ms` }}
                >
                  <span className="text-base w-6 text-center select-none">
                    {medal ?? <span className="text-zinc-600 text-xs font-bold">{i + 1}</span>}
                  </span>

                  <span
                    className={`flex-1 text-sm font-bold truncate ${
                      isWinner ? "text-amber-300" : p.isBankrupt ? "text-zinc-600" : "text-zinc-200"
                    }`}
                  >
                    {p.name}
                    {p.isBankrupt && (
                      <span className="ml-2 text-[9px] text-red-500 font-normal uppercase tracking-wider">
                        bankrupt
                      </span>
                    )}
                  </span>

                  <div className="text-right">
                    <span
                      className={`text-sm font-black ${
                        isWinner ? "text-emerald-400" : p.isBankrupt ? "text-zinc-600" : "text-zinc-400"
                      }`}
                    >
                      ${p.balance.toLocaleString()}
                    </span>
                    {p.properties.length > 0 && (
                      <p className="text-[9px] text-zinc-600 mt-0.5">
                        {p.properties.length} propert{p.properties.length > 1 ? "ies" : "y"}
                      </p>
                    )}
                  </div>
                </div>
              );
            })}
          </div>

          <div className="px-4 pb-5 pt-2">
            <button
              onClick={onPlayAgain}
              className="w-full py-3 bg-red-600 hover:bg-red-500 active:scale-[0.98] text-white font-black uppercase tracking-wider text-sm rounded-xl transition-all duration-150"
            >
              Play Again
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}