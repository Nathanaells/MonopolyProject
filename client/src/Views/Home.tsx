import { useState } from "react";
import { useNavigate } from "react-router";
import { ShowSuccess } from "../Constant/ToastUI";
import { gameService } from "../services/gameService";
import PiecePicker from "../Components/PiecePicker";
import { MAX_PLAYERS, PLAYER_AVATARS, PLAYER_COLORS } from "../Constant/PlayerAssets";
import woodTexture from "../assets/abstract-surface-wood-texture-background.jpg";


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
    
    <div className="relative min-h-screen bg-[#d6a06b] flex items-center justify-center p-6 font-sans overflow-hidden">
      <img
        src={woodTexture}
        alt="Monopoly Board"
        className="absolute inset-0 w-full h-full object-cover opacity-65 pointer-events-none"
      />
      <div
        className="absolute inset-0 pointer-events-none"
        style={{
          background:
            "radial-gradient(circle at 20% 20%, rgba(255,255,255,0.24), transparent 42%), linear-gradient(160deg, rgba(116,63,42,0.26), rgba(255,255,255,0.06) 45%, rgba(65,33,24,0.2))",
        }}
      />

      <div className="relative w-full max-w-sm">
        <div className="text-center mb-8">
          <div className="inline-block bg-red-600 border-4 border-white px-6 py-2 mb-3 rotate-[-1deg] shadow-lg">
            <h1 className="text-5xl font-black text-white tracking-tight uppercase">
              Monopoly
            </h1>
          </div>
          <p className="text-white font-bold font text-sm tracking-widest uppercase mt-3">
            {step === "addPlayers" ? "Setup your game" : "Choose your token"}
          </p>
        </div>

        <div className="bg-gradient-to-br from-[#d47867] via-[#c9574d] to-[#ae4138] rounded-2xl overflow-hidden shadow-2xl border-2 border-[#5a2f2a]/60">
          <div className="flex bg-[#5a2f2a]">
            {Array.from({ length: MAX_PLAYERS }, (_, i) => i + 1).map((n) => (
              <div
                key={n}
                className={`flex-1 h-1.5 transition-all duration-300 ${
                  n <= playerNames.length ? "bg-[#f9e8cf]" : "bg-[#3f211d]/55"
                }`}
              />
            ))}
          </div>

          <div className="p-6">
            {step === "addPlayers" && (
              <>
                <div className="mb-5">
                  <div className="flex items-center justify-between mb-3">
                    <span className="text-xs font-bold uppercase tracking-widest text-[#fff6e6]">
                      Players
                    </span>
                    <span className="text-xs text-[#ffecd6]">
                      {playerNames.length} / {MAX_PLAYERS}
                    </span>
                  </div>

                  <div className="space-y-2 min-h-[48px]">
                    {playerNames.length === 0 && (
                      <p className="text-[#ffeede] text-sm text-center py-3 font-semibold">
                        Add at least 2 players to start
                      </p>
                    )}
                    {playerNames.map((name, idx) => (
                      <div
                        key={idx}
                        className="flex items-center gap-3 bg-[#6a3832]/92 rounded-xl px-3 py-2.5 group border border-[#8f4a42]"
                      >
                        <div
                          className={`w-7 h-7 rounded-lg ${PLAYER_COLORS[idx % PLAYER_COLORS.length]} flex items-center justify-center text-sm flex-shrink-0`}
                        >
                          {PLAYER_AVATARS[idx % PLAYER_AVATARS.length]}
                        </div>
                        <span className="flex-1 text-sm font-semibold text-[#fff5eb] truncate">
                          {name}
                        </span>
                        <button
                          onClick={() => handleRemovePlayer(idx)}
                          className="text-[#ffd9c8] hover:text-[#fff2de] transition-colors text-lg leading-none opacity-0 group-hover:opacity-100"
                        >
                          ×
                        </button>
                      </div>
                    ))}
                  </div>
                </div>

                {playerNames.length < MAX_PLAYERS && (
                  <div className="flex gap-2 mb-5 color-white">
                    <input
                      type="text"
                      value={playerInput}
                      onChange={(e) => setPlayerInput(e.target.value)}
                      onKeyDown={handleKeyDown}
                      placeholder={`Player ${playerNames.length + 1} name…`}
                      className="flex-1 bg-[#f6e9d2] border border-[#8d4a40] text-[#5a2f2a] placeholder-[#9d6a58] rounded-xl px-4 py-2.5 text-sm focus:outline-none focus:border-[#a92e23] transition-colors"
                    />
                    <button
                      onClick={handleAddPlayer}
                      disabled={!playerInput.trim()}
                      className="px-4 py-2.5 bg-[#a92e23] hover:bg-[#92261d] disabled:bg-[#7f4b41] disabled:text-[#e4c0b2] text-white rounded-xl text-sm font-bold transition-colors"
                    >
                      Add
                    </button>
                  </div>
                )}

                <button
                  onClick={handleContinueToPieces}
                  disabled={playerNames.length < 2 || starting}
                  className="w-full py-3 rounded-xl font-black text-base uppercase tracking-wider transition-all duration-200
                    bg-[#a92e23] hover:bg-[#92261d] text-white
                    disabled:bg-[#7f4b41] disabled:text-[#e4c0b2] disabled:cursor-not-allowed"
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

        <p className="text-center text-[#4a2818]/82 text-xs mt-4 font-semibold">
          {step === "addPlayers"
            ? "Press Enter to quickly add a player"
            : "Pick a token for each player"}
        </p>
      </div>
    </div>
  );
}
