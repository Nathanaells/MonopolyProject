import { useEffect, useMemo, useState } from "react";
import type { GameState, RollResult, TileData } from "../Interfaces/Interface";
import { gameService } from "../services/gameService";
import { buildBoard } from "../hooks/buildBoard";
import { ShowSuccess, ShowError } from "../Constant/ToastUI";
import Tiles from "../Components/Tiles";
import DiceAnimation from "../Components/DiceAnimation";
import { usePieceAnimation } from "../Components/UsePieceAnimation";
import { PieceComponents } from "../Components/MonopolyPieces";
import PlayerPropertiesPanel from "../Components/PlayerPropertiesPanel";
import GameOverPanel from "../Components/GameOverPanel";
import {
  PLAYER_COLORS,
  PLAYER_DOTS,
  PLAYER_HEX,
  BOARD_SIZE,
  TILE_SIZE,
  TILE_GAP,
} from "../Constant/PlayerAssets";

import woodTexture from "../assets/abstract-surface-wood-texture-background.jpg";
import type {PendingMove} from "../Interfaces/Interface"
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

      const normalized = fetchedTiles.map((t) => ({
        ...t,
        position: { x: t.position.x, y: BOARD_SIZE - 1 - t.position.y },
      }));
      setTiles(normalized);
      const builtBoard = buildBoard(normalized, 11);
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
      <div className="min-h-screen flex items-center justify-center" style={{ backgroundImage: `url(${woodTexture})`, backgroundSize: "cover" }}>
        <div className="flex flex-col items-center gap-3 bg-stone-800/80 backdrop-blur-sm p-8 rounded-2xl">
          <div className="w-8 h-8 border-2 border-red-500 border-t-transparent rounded-full animate-spin" />
          <p className="text-stone-300 text-sm tracking-widest uppercase">
            Loading board…
          </p>
        </div>
      </div>
    );
  }

  const playerIdx = (name: string) =>
    gameState.players.findIndex((p) => p.name === name);

  return (
    <div className="min-h-screen text-stone-800 flex overflow-hidden" style={{ backgroundImage: `url(${woodTexture})`, backgroundSize: "cover" }}>
      <aside className="w-48 flex-shrink-0 bg-[#f9e8cf]/80 backdrop-blur-sm border-r border-[#8f4a42]/50 flex flex-col p-3 gap-2 overflow-y-auto">
        <div className="bg-red-600 border-2 border-white text-center px-3 py-1 rotate-[-1deg] mb-1 self-center shadow-lg">
          <span className="text-base font-black text-white uppercase tracking-tight">
            Monopoly
          </span>
        </div>

        <p className="text-[9px] uppercase tracking-widest text-[#8f4a42] font-bold mt-1">
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
                  ? "bg-white/80 border-red-700 shadow-md"
                  : "bg-[#e4c0b2]/70 border-[#8f4a42]/60"
              } ${p.isBankrupt ? "opacity-40" : ""}`}
            >
              <div className="flex items-center gap-2 mb-1">
                <span
                  className={`w-5 h-5 rounded-md flex items-center justify-center text-[9px] ${PLAYER_COLORS[colorIdx % PLAYER_COLORS.length]} text-white font-bold flex-shrink-0`}
                >
                  {PLAYER_DOTS[colorIdx % PLAYER_DOTS.length]}
                </span>
                <span
                  className={`text-[11px] font-bold truncate flex-1 ${isCurrent ? "text-[#5a2f2a]" : "text-[#6a3832]"}`}
                >
                  {p.name}
                </span>
                {isCurrent && (
                  <span className="text-[7px] bg-red-700 text-white px-1.5 py-0.5 rounded font-bold flex-shrink-0">
                    TURN
                  </span>
                )}
              </div>
              <div className="text-[10px]">
                <span className="text-emerald-600 font-bold">${p.balance}</span>
                {p.isBankrupt && (
                  <span className="ml-1 text-red-600 text-[9px]">Bankrupt</span>
                )}
              </div>
              {p.properties.length > 0 && (
                <div className="text-[9px] text-[#8f4a42] mt-0.5">
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
          className="relative border-none  rounded-lg overflow-hidden"
          style={{
            display: "grid",

            gridTemplateColumns: `repeat(${BOARD_SIZE}, 1fr)`,
            gridTemplateRows: `repeat(${BOARD_SIZE}, 1fr)`,
            gap: "1px",
            background: "#f9e8cf", // White board color
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
                    className="w-[54px] h-[54px] bg-[#d47867] flex flex-col items-center justify-center gap-0.5"
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
                    className="w-[54px] h-[54px] bg-[#d47867] flex flex-col items-center justify-center gap-0.5"
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
                    className="w-[54px] h-[54px] bg-[#d47867] flex items-center justify-center"
                  >
                    <span className="text-[7px] font-black text-white uppercase tracking-widest text-center leading-tight">
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
                    className="w-[54px] h-[54px] bg-[#d47867] flex items-center justify-center"
                  />
                );
              }

              return (
                <div
                  key={`${x}-${y}`}
                  className="w-[54px] h-[54px] bg-white flex items-center justify-center"
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
                PieceComponents[pieceType] ?? PieceComponents.Tophat;

              return (
                <div
                  key={p.name}
                  className="absolute transition-all duration-150"
                  style={{
                    left: coord.x * (TILE_SIZE + TILE_GAP) + 8 + dx,
                    top: coord.y * (TILE_SIZE + TILE_GAP) + 8 + dy,
                  }}
                >
                  <div className="bg-stone-900/70 backdrop-blur-sm rounded-md p-0.5 border border-stone-600/80">
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

      <aside className="w-52 flex-shrink-0 bg-[#f9e8cf]/80 backdrop-blur-sm border-l border-[#8f4a42]/50 flex flex-col p-3 gap-3 overflow-y-auto">
        <div className="bg-white/80 rounded-xl p-3 border border-[#8f4a42]/60">
          <p className="text-[9px] uppercase tracking-widest text-[#8f4a42] font-bold mb-1">
            Current Turn
          </p>
          <p className="text-sm font-black text-[#5a2f2a]">
            {gameState.currentPlayer}
          </p>
          <p className="text-xs text-emerald-600 font-bold">
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
            className="w-full py-3 bg-[#a92e23] hover:bg-[#92261d] disabled:bg-[#7f4b41] disabled:text-[#e4c0b2] text-white font-black uppercase tracking-wider text-sm rounded-xl transition-colors shadow-lg"
          >
            {loading ? "Rolling…" : "🎲 Roll Dice"}
          </button>
        )}

        {!buyPrompt && !diceRoll && !gameState.isGameEnded && (
          <button
            onClick={() => setPropertiesPlayer(gameState.currentPlayer)}
            disabled={loading || isAnimating || isDiceRolling}
            className="w-full py-2.5 bg-[#6a3832]/80 hover:bg-[#5a2f2a]/80 border border-[#8f4a42]/80 disabled:bg-[#7f4b41] disabled:text-[#e4c0b2] text-stone-100 font-bold uppercase tracking-wider text-xs rounded-xl transition-colors"
          >
            Manage Properties
          </button>
        )}

        {diceRoll && (
          <div className="bg-white/80 rounded-xl p-3 border border-[#8f4a42]/60">
            <div className="mb-3">
              <DiceAnimation
                dice1={diceRoll.dice1}
                dice2={diceRoll.dice2}
                rolling={isDiceRolling}
                onDone={handleDiceAnimationDone}
              />
            </div>
            <div className="flex items-baseline gap-2 mb-2">
              <span className="text-3xl font-black text-[#5a2f2a]">
                {diceRoll.diceTotal}
              </span>
              <span className="text-[10px] text-[#8f4a42] uppercase tracking-wider">
                rolled
              </span>
            </div>
            <p className="text-xs text-[#6a3832]">
              Landed on{" "}
              <span className="font-bold text-[#5a2f2a]">
                {diceRoll.landedProperty || diceRoll.landedTileType}
              </span>
            </p>
            {diceRoll.drawnCardDescription && (
              <p className="text-xs text-amber-700 mt-2 italic">
                "{diceRoll.drawnCardDescription}"
              </p>
            )}

            {diceRoll.jailRollResult !== "None" && (
              <p className="text-[10px] text-[#8f4a42] mt-2 uppercase tracking-wider">
                Jail: {diceRoll.jailRollResult}
              </p>
            )}
          </div>
        )}

        {buyPrompt && diceRoll && (
          <div className="bg-white/80 rounded-xl p-3 border border-[#8f4a42]/60">
            <p className="text-[9px] uppercase tracking-widest text-[#8f4a42] font-bold mb-1">
              Purchase?
            </p>
            <p className="text-sm font-bold text-[#5a2f2a] mb-3">
              {diceRoll.landedProperty}
            </p>
            <div className="flex gap-2">
              <button
                onClick={() => handleBuyProperty(true)}
                disabled={loading}
                className="flex-1 py-2 bg-emerald-600 hover:bg-emerald-500 disabled:bg-stone-400 text-white text-xs font-bold rounded-lg transition-colors"
              >
                Buy
              </button>
              <button
                onClick={() => handleBuyProperty(false)}
                disabled={loading}
                className="flex-1 py-2 bg-[#6a3832] hover:bg-[#5a2f2a] text-stone-100 text-xs font-bold rounded-lg transition-colors"
              >
                Pass
              </button>
            </div>
          </div>
        )}

        {gameState.isGameEnded && (
          <div className="bg-white/80 rounded-xl p-3 border border-amber-600">
            <p className="text-[9px] uppercase tracking-widest text-amber-700 font-bold mb-1">
              Game Over
            </p>
            <p className="text-sm font-black text-[#5a2f2a]">
              🏆 {gameState.winner}
            </p>
          </div>
        )}

        {error && (
          <div className="bg-red-800/80 rounded-xl p-3 border border-red-700/80">
            <p className="text-xs text-red-100">{error}</p>
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

      {gameState.isGameEnded && gameState.winner && (
        <GameOverPanel
          winner={gameState.winner}
          players={gameState.players.map((p) => ({
            name: p.name,
            balance: p.balance,
            properties: p.properties,
            isBankrupt: p.isBankrupt,
          }))}
          onPlayAgain={() => {
            localStorage.removeItem("playerNames");
            localStorage.removeItem("playerPieces");
            window.location.href = "/"; // atau navigate ke halaman lobby
          }}
        />
      )}
    </div>
  );
}
