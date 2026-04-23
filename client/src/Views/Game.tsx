import { useEffect, useMemo, useState } from "react";
import type { GameState, RollResult, TileData } from "../Interfaces/Interface";
import { gameService } from "../services/gameService";
import { buildBoard } from "../hooks/buildBoard";
import { ShowSuccess, ShowError } from "../Constant/ToastUI";
import Tiles from "../Components/Tiles";
import DiceAnimation from "../Components/DiceAnimation";
import { usePieceAnimation } from "../Components/UsePieceAnimation";
import { PIECE_COMPONENTS } from "../Components/MonopolyPieces";
import PlayerPropertiesPanel from "../Components/PlayerPropertiesPanel";

const PLAYER_COLORS = [
  "bg-red-500",
  "bg-blue-500",
  "bg-yellow-400",
  "bg-emerald-500",
  "bg-purple-500",
  "bg-pink-500",
  "bg-cyan-500",
  "bg-orange-500",
];
const PLAYER_DOTS = ["●", "■", "▲", "♦", "★", "◆", "⬟", "⬢"];
const PLAYER_HEX = [
  "#ef4444",
  "#3b82f6",
  "#eab308",
  "#10b981",
  "#a855f7",
  "#ec4899",
  "#06b6d4",
  "#f97316",
];

const BOARD_SIZE = 11;
const TILE_SIZE = 54;
const TILE_GAP = 1;

type PendingMove = {
  playerName: string;
  from: number;
  to: number;
  result: RollResult;
};

export default function Game() {
  const playerNamesString = localStorage.getItem("playerNames");
  const playerNames = playerNamesString ? JSON.parse(playerNamesString) : [];

  const [gameState, setGameState] = useState<GameState | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [diceRoll, setDiceRoll] = useState<RollResult | null>(null);
  const [isDiceRolling, setIsDiceRolling] = useState(false);
  const [pendingMove, setPendingMove] = useState<PendingMove | null>(null);
  const [buyPrompt, setBuyPrompt] = useState(false);
  const [board, setBoard] = useState<(TileData | null)[][]>([]);
  const [tiles, setTiles] = useState<TileData[]>([]);
  const [propertiesPlayer, setPropertiesPlayer] = useState<string | null>(null);

  const [playerPieces] = useState<Record<string, string>>(() => {
    try {
      const raw = localStorage.getItem("playerPieces");
      return raw ? JSON.parse(raw) : {};
    } catch {
      return {};
    }
  });

  const { positions, isAnimating, animateMove, setAllPositions } =
    usePieceAnimation({});

  const startTileIndex = useMemo(
    () => tiles.find((t) => t.type === "StartTile")?.index ?? 0,
    [tiles],
  );

  const totalTiles = useMemo(() => Math.max(tiles.length, 1), [tiles.length]);

  const indexToCoord = useMemo(() => {
    const map = new Map<number, { x: number; y: number }>();
    tiles.forEach((t) => map.set(t.index, t.position));
    return map;
  }, [tiles]);

  const playersByTile = useMemo(() => {
    const grouped: Record<number, string[]> = {};
    if (!gameState) return grouped;

    gameState.players.forEach((p) => {
      const idx = positions[p.name] ?? startTileIndex;
      if (!grouped[idx]) grouped[idx] = [];
      grouped[idx].push(p.name);
    });

    return grouped;
  }, [gameState, positions, startTileIndex]);

  useEffect(() => {
    const ensureGameState = async () => {
      setLoading(true);
      setError(null);
      try {
        try {
          const state = await gameService.getGameState();
          setGameState(state);
        } catch {
          const state = await gameService.startGame(playerNames);
          setGameState(state);
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to start game");
      } finally {
        setLoading(false);
      }
    };
    if (playerNames.length >= 2) ensureGameState();
  }, []);

  useEffect(() => {
    if (!propertiesPlayer || !gameState) return;
    if (propertiesPlayer !== gameState.currentPlayer) {
      setPropertiesPlayer(null);
    }
  }, [propertiesPlayer, gameState]);

  async function loadBoardTiles() {
    try {
      const fetchedTiles = await gameService.getBoardTiles();
      setTiles(fetchedTiles);
      const builtBoard = buildBoard(fetchedTiles, 11);
      setBoard(builtBoard);
    } catch {
      ShowError("Failed to load board tiles");
    }
  }

  async function refreshStateAndBoard() {
    try {
      const state = await gameService.getGameState();
      setGameState(state);
      await loadBoardTiles();
    } catch {
      ShowError("Failed to refresh game state");
    }
  }

  useEffect(() => {
    loadBoardTiles();
  }, []);

  useEffect(() => {
    if (!gameState || tiles.length === 0) return;

    const nextPositions: Record<string, number> = {};
    gameState.players.forEach((p) => {
      nextPositions[p.name] = p.currentTileIndex ?? startTileIndex;
    });

    if (Object.keys(nextPositions).length > 0) {
      setAllPositions(nextPositions);
    }
  }, [gameState, tiles.length, setAllPositions, startTileIndex]);

  const handleRoll = async () => {
    if (!gameState || isAnimating || isDiceRolling) return;

    setLoading(true);
    setError(null);
    try {
      const currentPlayerName = gameState.currentPlayer;
      const result = await gameService.rollTurn();
      setDiceRoll(result);
      setGameState(result.state);

      const fromIndex = positions[currentPlayerName] ?? startTileIndex;
      const rolledPlayer = result.state.players.find(
        (p) => p.name === currentPlayerName,
      );
      const toIndex = rolledPlayer?.currentTileIndex ?? fromIndex;

      setPendingMove({
        playerName: currentPlayerName,
        from: fromIndex,
        to: toIndex,
        result,
      });
      setIsDiceRolling(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to roll dice");
    } finally {
      setLoading(false);
    }
  };

  const handleDiceAnimationDone = async () => {
    if (!pendingMove) return;

    setIsDiceRolling(false);

    await animateMove(
      pendingMove.playerName,
      pendingMove.from,
      pendingMove.to,
      totalTiles,
    );

    const result = pendingMove.result;
    ShowSuccess(
      `You rolled a ${result.diceTotal} and landed on ${result.landedProperty || result.landedTileType}!`,
    );

    if (result.requiresBuyDecision) {
      setBuyPrompt(true);
    } else {
      setBuyPrompt(false);
      setTimeout(() => setDiceRoll(null), 700);
    }

    setPendingMove(null);
  };

  const handleBuyProperty = async (buy: boolean) => {
    setLoading(true);
    setError(null);
    try {
      const state = await gameService.buyProperty(buy);
      setGameState(state);
      await loadBoardTiles();
      setBuyPrompt(false);
      setDiceRoll(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to buy property");
    } finally {
      setLoading(false);
    }
  };

  if (!gameState || board.length === 0) {
    return (
      <div className="min-h-screen bg-zinc-950 flex items-center justify-center">
        <div className="flex flex-col items-center gap-3">
          <div className="w-8 h-8 border-2 border-red-500 border-t-transparent rounded-full animate-spin" />
          <p className="text-zinc-400 text-sm tracking-widest uppercase">
            Loading board…
          </p>
        </div>
      </div>
    );
  }

  const playerIdx = (name: string) =>
    gameState.players.findIndex((p) => p.name === name);

  return (
    <div className="min-h-screen bg-zinc-950 text-zinc-100 flex overflow-hidden">
      <aside className="w-48 flex-shrink-0 bg-zinc-900 border-r border-zinc-800 flex flex-col p-3 gap-2 overflow-y-auto">
        <div className="bg-red-600 border-2 border-white text-center px-3 py-1 rotate-[-1deg] mb-1 self-center">
          <span className="text-base font-black text-white uppercase tracking-tight">
            Monopoly
          </span>
        </div>

        <p className="text-[9px] uppercase tracking-widest text-zinc-500 font-bold mt-1">
          Players
        </p>

        {gameState.players.map((p) => {
          const isCurrent = p.name === gameState.currentPlayer;
          const colorIdx = playerIdx(p.name);
          return (
            <div
              key={p.name}
              className={`rounded-xl p-2.5 border transition-all ${
                isCurrent
                  ? "bg-zinc-800 border-zinc-600"
                  : "bg-zinc-900 border-zinc-800"
              } ${p.isBankrupt ? "opacity-40" : ""}`}
            >
              <div className="flex items-center gap-2 mb-1">
                <span
                  className={`w-5 h-5 rounded-md flex items-center justify-center text-[9px] ${PLAYER_COLORS[colorIdx % PLAYER_COLORS.length]} text-white font-bold flex-shrink-0`}
                >
                  {PLAYER_DOTS[colorIdx % PLAYER_DOTS.length]}
                </span>
                <span
                  className={`text-[11px] font-bold truncate flex-1 ${isCurrent ? "text-white" : "text-zinc-400"}`}
                >
                  {p.name}
                </span>
                {isCurrent && (
                  <span className="text-[7px] bg-red-600 text-white px-1.5 py-0.5 rounded font-bold flex-shrink-0">
                    TURN
                  </span>
                )}
              </div>
              <div className="text-[10px]">
                <span className="text-emerald-400 font-bold">${p.balance}</span>
                {p.isBankrupt && (
                  <span className="ml-1 text-red-400 text-[9px]">Bankrupt</span>
                )}
              </div>
              {p.properties.length > 0 && (
                <div className="text-[9px] text-zinc-600 mt-0.5">
                  {p.properties.length} propert
                  {p.properties.length > 1 ? "ies" : "y"}
                </div>
              )}
            </div>
          );
        })}
      </aside>

      <main className="flex-1 flex items-center justify-center p-2 overflow-hidden">
        <div
          className="relative border border-zinc-700 rounded-lg overflow-hidden"
          style={{
            display: "grid",
            gridTemplateColumns: `repeat(${BOARD_SIZE}, 1fr)`,
            gridTemplateRows: `repeat(${BOARD_SIZE}, 1fr)`,
            gap: "1px",
            background: "#27272a",
            width: "fit-content",
          }}
        >
          {board.map((row, y) =>
            row.map((tile, x) => {
              const isEdge =
                x === 0 ||
                x === BOARD_SIZE - 1 ||
                y === 0 ||
                y === BOARD_SIZE - 1;
              const cx = Math.floor(BOARD_SIZE / 2);
              const cy = Math.floor(BOARD_SIZE / 2);
              const isChance = !isEdge && x === cx && y === cy - 1;
              const isCommunity = !isEdge && x === cx && y === cy + 1;
              const isCenter = !isEdge && x === cx && y === cy;

              if (!isEdge && isChance) {
                return (
                  <div
                    key={`${x}-${y}`}
                    className="w-[54px] h-[54px] bg-zinc-950 flex flex-col items-center justify-center gap-0.5"
                  >
                    <div className="relative w-7 h-9 flex items-center justify-center">
                      <div className="absolute w-6 h-8 rounded bg-amber-700 border border-amber-500 rotate-6" />
                      <div className="absolute w-6 h-8 rounded bg-amber-600 border border-amber-400 rotate-3" />
                      <div className="absolute w-6 h-8 rounded bg-amber-500 border border-amber-300 flex items-center justify-center text-amber-900 font-black text-[10px]">
                        ?
                      </div>
                    </div>
                    <span className="text-[5px] font-bold text-amber-500 uppercase tracking-wider">
                      Chance
                    </span>
                  </div>
                );
              }

              if (!isEdge && isCommunity) {
                return (
                  <div
                    key={`${x}-${y}`}
                    className="w-[54px] h-[54px] bg-zinc-950 flex flex-col items-center justify-center gap-0.5"
                  >
                    <div className="relative w-7 h-9 flex items-center justify-center">
                      <div className="absolute w-6 h-8 rounded bg-sky-900 border border-sky-700 rotate-6" />
                      <div className="absolute w-6 h-8 rounded bg-sky-800 border border-sky-600 rotate-3" />
                      <div className="absolute w-6 h-8 rounded bg-sky-600 border border-sky-400 flex items-center justify-center text-sky-100 font-black text-[8px]">
                        CC
                      </div>
                    </div>
                    <span className="text-[5px] font-bold text-sky-400 uppercase tracking-wider text-center leading-tight">
                      Comm.
                      <br />
                      Chest
                    </span>
                  </div>
                );
              }

              if (!isEdge && isCenter) {
                return (
                  <div
                    key={`${x}-${y}`}
                    className="w-[54px] h-[54px] bg-zinc-950 flex items-center justify-center"
                  >
                    <span className="text-[7px] font-black text-zinc-700 uppercase tracking-widest text-center leading-tight">
                      Mono
                      <br />
                      poly
                    </span>
                  </div>
                );
              }

              if (!isEdge) {
                return (
                  <div
                    key={`${x}-${y}`}
                    className="w-[54px] h-[54px] bg-zinc-950"
                  />
                );
              }

              return (
                <div
                  key={`${x}-${y}`}
                  className="w-[54px] h-[54px] bg-zinc-950 flex items-center justify-center"
                >
                  {tile ? <Tiles {...tile} /> : null}
                </div>
              );
            }),
          )}

          <div className="absolute inset-0 pointer-events-none">
            {gameState.players.map((p, idx) => {
              const tileIndex = positions[p.name] ?? startTileIndex;
              const coord = indexToCoord.get(tileIndex);
              if (!coord) return null;

              const stack = playersByTile[tileIndex] ?? [];
              const stackPos = stack.indexOf(p.name);
              const dx = (stackPos % 2) * 18;
              const dy = Math.floor(stackPos / 2) * 18;

              const pieceType = playerPieces[p.name] ?? "Tophat";
              const PieceComponent =
                PIECE_COMPONENTS[pieceType] ?? PIECE_COMPONENTS.Tophat;

              return (
                <div
                  key={p.name}
                  className="absolute transition-all duration-150"
                  style={{
                    left: coord.x * (TILE_SIZE + TILE_GAP) + 8 + dx,
                    top: coord.y * (TILE_SIZE + TILE_GAP) + 8 + dy,
                  }}
                >
                  <div className="bg-zinc-950/70 rounded-md p-0.5 border border-zinc-700">
                    <PieceComponent
                      size={18}
                      color={PLAYER_HEX[idx % PLAYER_HEX.length]}
                    />
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </main>

      <aside className="w-52 flex-shrink-0 bg-zinc-900 border-l border-zinc-800 flex flex-col p-3 gap-3 overflow-y-auto">
        <div className="bg-zinc-800 rounded-xl p-3 border border-zinc-700">
          <p className="text-[9px] uppercase tracking-widest text-zinc-500 font-bold mb-1">
            Current Turn
          </p>
          <p className="text-sm font-black text-white">
            {gameState.currentPlayer}
          </p>
          <p className="text-xs text-emerald-400 font-bold">
            $
            {
              gameState.players.find((p) => p.name === gameState.currentPlayer)
                ?.balance
            }
          </p>
        </div>

        {!buyPrompt && !diceRoll && !gameState.isGameEnded && (
          <button
            onClick={handleRoll}
            disabled={loading || isAnimating || isDiceRolling}
            className="w-full py-3 bg-red-600 hover:bg-red-500 disabled:bg-zinc-700 disabled:text-zinc-500 text-white font-black uppercase tracking-wider text-sm rounded-xl transition-colors"
          >
            {loading ? "Rolling…" : "🎲 Roll Dice"}
          </button>
        )}

        {!buyPrompt && !diceRoll && !gameState.isGameEnded && (
          <button
            onClick={() => setPropertiesPlayer(gameState.currentPlayer)}
            disabled={loading || isAnimating || isDiceRolling}
            className="w-full py-2.5 bg-zinc-800 hover:bg-zinc-700 border border-zinc-700 disabled:bg-zinc-900 disabled:text-zinc-600 text-zinc-200 font-bold uppercase tracking-wider text-xs rounded-xl transition-colors"
          >
            Manage Properties
          </button>
        )}

        {diceRoll && (
          <div className="bg-zinc-800 rounded-xl p-3 border border-zinc-700">
            <div className="mb-3">
              <DiceAnimation
                die1={diceRoll.dice1}
                die2={diceRoll.dice2}
                rolling={isDiceRolling}
                onDone={handleDiceAnimationDone}
              />
            </div>
            <div className="flex items-baseline gap-2 mb-2">
              <span className="text-3xl font-black text-white">
                {diceRoll.diceTotal}
              </span>
              <span className="text-[10px] text-zinc-400 uppercase tracking-wider">
                rolled
              </span>
            </div>
            <p className="text-xs text-zinc-300">
              Landed on{" "}
              <span className="font-bold text-white">
                {diceRoll.landedProperty || diceRoll.landedTileType}
              </span>
            </p>
            {diceRoll.drawnCardDescription && (
              <p className="text-xs text-amber-400 mt-2 italic">
                "{diceRoll.drawnCardDescription}"
              </p>
            )}

            {diceRoll.jailRollResult !== "None" && (
              <p className="text-[10px] text-zinc-400 mt-2 uppercase tracking-wider">
                Jail: {diceRoll.jailRollResult}
              </p>
            )}
          </div>
        )}

        {buyPrompt && diceRoll && (
          <div className="bg-zinc-800 rounded-xl p-3 border border-zinc-700">
            <p className="text-[9px] uppercase tracking-widest text-zinc-500 font-bold mb-1">
              Purchase?
            </p>
            <p className="text-sm font-bold text-white mb-3">
              {diceRoll.landedProperty}
            </p>
            <div className="flex gap-2">
              <button
                onClick={() => handleBuyProperty(true)}
                disabled={loading}
                className="flex-1 py-2 bg-emerald-600 hover:bg-emerald-500 disabled:bg-zinc-700 text-white text-xs font-bold rounded-lg transition-colors"
              >
                Buy
              </button>
              <button
                onClick={() => handleBuyProperty(false)}
                disabled={loading}
                className="flex-1 py-2 bg-zinc-700 hover:bg-zinc-600 text-zinc-200 text-xs font-bold rounded-lg transition-colors"
              >
                Pass
              </button>
            </div>
          </div>
        )}

        {gameState.isGameEnded && (
          <div className="bg-zinc-800 rounded-xl p-3 border border-amber-700">
            <p className="text-[9px] uppercase tracking-widest text-amber-500 font-bold mb-1">
              Game Over
            </p>
            <p className="text-sm font-black text-white">
              🏆 {gameState.winner}
            </p>
          </div>
        )}

        {error && (
          <div className="bg-red-950 rounded-xl p-3 border border-red-800">
            <p className="text-xs text-red-400">{error}</p>
          </div>
        )}
      </aside>

      {propertiesPlayer && gameState && (
        <PlayerPropertiesPanel
          playerName={propertiesPlayer}
          isCurrentPlayer
          onUpdate={refreshStateAndBoard}
          onClose={() => setPropertiesPlayer(null)}
        />
      )}
    </div>
  );
}
