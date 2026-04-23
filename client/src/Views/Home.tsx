import { useState } from "react";
import { useNavigate } from "react-router";
import { ShowSuccess } from "../Constant/ToastUI";
import { gameService } from "../services/gameService";
import PiecePicker from "../Components/PiecePicker";

const MAX_PLAYERS = 8;

const PLAYER_COLORS = [
  "bg-red-500",
  "bg-blue-500",
  "bg-yellow-400",
  "bg-green-500",
  "bg-purple-500",
  "bg-pink-500",
  "bg-cyan-500",
  "bg-orange-500",
];
const PLAYER_AVATARS = ["♟", "♜", "♝", "♛", "♞", "♚", "◆", "★"];

type Step = "addPlayers" | "pickPieces";

export default function Home() {
  const [step, setStep] = useState<Step>("addPlayers");
  const [playerNames, setPlayerNames] = useState<string[]>([]);
  const [playerInput, setPlayerInput] = useState("");
  const [starting, setStarting] = useState(false);
  const navigate = useNavigate();

  const handleAddPlayer = () => {
    if (playerInput.trim() && playerNames.length < MAX_PLAYERS) {
      setPlayerNames([...playerNames, playerInput.trim()]);
      setPlayerInput("");
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter") handleAddPlayer();
  };

  const handleRemovePlayer = (idx: number) => {
    setPlayerNames(playerNames.filter((_, i) => i !== idx));
  };

  const handleContinueToPieces = async () => {
    if (playerNames.length < 2) return;
    setStarting(true);
    try {
      await gameService.startGame(playerNames);
      localStorage.setItem("playerNames", JSON.stringify(playerNames));
      setStep("pickPieces");
    } catch {
    } finally {
      setStarting(false);
    }
  };

  const handleAllPiecesPicked = () => {
    ShowSuccess("Game started!");
    navigate("/game");
  };

  return (
    <div className="min-h-screen bg-zinc-950 flex items-center justify-center p-6 font-sans">
      <div
        className="absolute inset-0 opacity-5 pointer-events-none"
        style={{
          backgroundImage:
            "repeating-conic-gradient(#fff 0% 25%, transparent 0% 50%)",
          backgroundSize: "40px 40px",
        }}
      />

      <div className="relative w-full max-w-sm">
        <div className="text-center mb-8">
          <div className="inline-block bg-red-600 border-4 border-white px-6 py-2 mb-3 rotate-[-1deg] shadow-lg">
            <h1 className="text-5xl font-black text-white tracking-tight uppercase">
              Monopoly
            </h1>
          </div>
          <p className="text-zinc-400 text-sm tracking-widest uppercase mt-3">
            {step === "addPlayers" ? "Setup your game" : "Choose your token"}
          </p>
        </div>

        <div className="bg-zinc-900 border border-zinc-700 rounded-2xl overflow-hidden shadow-2xl">
          <div className="flex border-b border-zinc-700">
            {Array.from({ length: MAX_PLAYERS }, (_, i) => i + 1).map((n) => (
              <div
                key={n}
                className={`flex-1 h-1.5 transition-all duration-300 ${
                  n <= playerNames.length ? "bg-red-500" : "bg-zinc-700"
                }`}
              />
            ))}
          </div>

          <div className="p-6">
            {step === "addPlayers" && (
              <>
                <div className="mb-5">
                  <div className="flex items-center justify-between mb-3">
                    <span className="text-xs font-bold uppercase tracking-widest text-zinc-400">
                      Players
                    </span>
                    <span className="text-xs text-zinc-500">
                      {playerNames.length} / {MAX_PLAYERS}
                    </span>
                  </div>

                  <div className="space-y-2 min-h-[48px]">
                    {playerNames.length === 0 && (
                      <p className="text-zinc-600 text-sm text-center py-3">
                        Add at least 2 players to start
                      </p>
                    )}
                    {playerNames.map((name, idx) => (
                      <div
                        key={idx}
                        className="flex items-center gap-3 bg-zinc-800 rounded-xl px-3 py-2.5 group"
                      >
                        <div
                          className={`w-7 h-7 rounded-lg ${PLAYER_COLORS[idx % PLAYER_COLORS.length]} flex items-center justify-center text-sm flex-shrink-0`}
                        >
                          {PLAYER_AVATARS[idx % PLAYER_AVATARS.length]}
                        </div>
                        <span className="flex-1 text-sm font-semibold text-zinc-100 truncate">
                          {name}
                        </span>
                        <button
                          onClick={() => handleRemovePlayer(idx)}
                          className="text-zinc-600 hover:text-red-400 transition-colors text-lg leading-none opacity-0 group-hover:opacity-100"
                        >
                          ×
                        </button>
                      </div>
                    ))}
                  </div>
                </div>

                {playerNames.length < MAX_PLAYERS && (
                  <div className="flex gap-2 mb-5">
                    <input
                      type="text"
                      value={playerInput}
                      onChange={(e) => setPlayerInput(e.target.value)}
                      onKeyDown={handleKeyDown}
                      placeholder={`Player ${playerNames.length + 1} name…`}
                      className="flex-1 bg-zinc-800 border border-zinc-700 text-zinc-100 placeholder-zinc-600 rounded-xl px-4 py-2.5 text-sm focus:outline-none focus:border-red-500 transition-colors"
                    />
                    <button
                      onClick={handleAddPlayer}
                      disabled={!playerInput.trim()}
                      className="px-4 py-2.5 bg-red-600 hover:bg-red-500 disabled:bg-zinc-700 disabled:text-zinc-500 text-white rounded-xl text-sm font-bold transition-colors"
                    >
                      Add
                    </button>
                  </div>
                )}

                <button
                  onClick={handleContinueToPieces}
                  disabled={playerNames.length < 2 || starting}
                  className="w-full py-3 rounded-xl font-black text-base uppercase tracking-wider transition-all duration-200
                    bg-red-600 hover:bg-red-500 text-white
                    disabled:bg-zinc-800 disabled:text-zinc-600 disabled:cursor-not-allowed"
                >
                  {starting
                    ? "Starting…"
                    : playerNames.length < 2
                      ? `Need ${2 - playerNames.length} more player${2 - playerNames.length > 1 ? "s" : ""}`
                      : "Choose Tokens →"}
                </button>
              </>
            )}

            {step === "pickPieces" && (
              <PiecePicker
                playerNames={playerNames}
                onAllPicked={handleAllPiecesPicked}
              />
            )}
          </div>
        </div>

        <p className="text-center text-zinc-600 text-xs mt-4">
          {step === "addPlayers"
            ? "Press Enter to quickly add a player"
            : "Pick a token for each player"}
        </p>
      </div>
    </div>
  );
}
