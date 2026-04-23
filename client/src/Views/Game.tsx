import { useState, useEffect } from "react";
import type { GameState, RollResult, TileData } from "../Interfaces/Interface";
import { gameService } from "../services/gameService";
import { buildBoard } from "../hooks/buildBoard";

import { ShowSuccess, ShowError} from "../Constant/ToastUI";
import Tiles from "../Components/Tiles";

export default function Game() {
  const playerNamesString = localStorage.getItem("playerNames");
  const playerNames = playerNamesString ? JSON.parse(playerNamesString) : [];

  const [gameState, setGameState] = useState<GameState | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [diceRoll, setDiceRoll] = useState<RollResult | null>(null);
  const [buyPrompt, setBuyPrompt] = useState(false);
  const [board, setBoard] = useState<(TileData | null)[][]>([]);


  useEffect(() => {
    const startGame = async () => {
      setLoading(true);
      setError(null);
      try {
        const state = await gameService.startGame(playerNames);
        console.log("Game state after starting:", state);
        setGameState(state);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to start game");
      } finally {
        setLoading(false);
      }
    };
    if (playerNames.length >= 2) {
      startGame();
    }
  }, []);


  async function loadBoardTiles() {
    try {
      const fetchedTiles = await gameService.getBoardTiles();
      const boardSize = 11; 
      const builtBoard = buildBoard(fetchedTiles, boardSize);
      setBoard(builtBoard);

      console.log("Board after building:", builtBoard);
    } catch (err) {
      ShowError("Failed to load board tiles");
    }
  }      

  useEffect(() => {
    loadBoardTiles();
  }, []);


  const handleRoll = async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await gameService.rollTurn();
      setDiceRoll(result);
      setGameState(result.state);
      ShowSuccess(`You rolled a ${result.diceTotal} and landed on ${result.landedProperty || result.landedTileType}!`);
      if (result.requiresBuyDecision) {
        setBuyPrompt(true);
      }else{
        setBuyPrompt(false);
         setDiceRoll(null);
         
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to roll dice");
    } finally {
      setLoading(false);
    }
  };

  const handleBuyProperty = async (buy: boolean) => {
    setLoading(true);
    setError(null);
    try {
      const state = await gameService.buyProperty(buy);

      
      setGameState(state);
      setBuyPrompt(false);
      setDiceRoll(null);
    } catch (err) {
      console.error("Error buying property:", err);
      setError(err instanceof Error ? err.message : "Failed to buy property");
    } finally {
      setLoading(false);
    }
  };


  if (!gameState || board.length === 0) {
    return <div style={{ padding: "40px" }}>Loading board...</div>;
  }

  const boardSize = 11;

  return (
    <>
      <div
        style={{
          position: "absolute",
          top: "10px",
          left: "10px",
          background: "#f9f9f9",
          padding: "16px",
          borderRadius: "8px",
          fontSize: "12px",
          maxWidth: "300px",
          zIndex: 10,
        }}
      >
        <div style={{ fontWeight: "bold", marginBottom: "8px" }}>
          Current:{" "}
          <span style={{ color: "#e53935" }}>{gameState.currentPlayer}</span>
        </div>
        {gameState.players.map((p) => (
          <div key={p.name} style={{ padding: "4px 0", fontSize: "11px" }}>
            <span
              style={{
                fontWeight:
                  p.name === gameState.currentPlayer ? "bold" : "normal",
              }}
            >
              {p.name}
            </span>
            : ${p.balance} {p.isBankrupt && "💔"}
          </div>
        ))}
      </div>

      <div
        style={{
          position: "absolute",
          top: "10px",
          right: "10px",
          background: "#f9f9f9",
          padding: "16px",
          borderRadius: "8px",
          minWidth: "280px",
          zIndex: 10,
        }}
      >
        {!buyPrompt && !diceRoll && (
          <>
            <button
              onClick={handleRoll}
              disabled={loading || gameState.isGameEnded}
              style={{
                width: "100%",
                padding: "12px",
                fontSize: "16px",
                marginBottom: "8px",
                cursor: loading ? "not-allowed" : "pointer",
                background: "#4caf50",
                color: "white",
                border: "none",
                borderRadius: "4px",
              }}
            >
              Roll Dice
            </button>
          </>
        )}

        {diceRoll && !buyPrompt && (
          <div
            style={{
              background: "#fff3cd",
              padding: "12px",
              borderRadius: "4px",
            }}
          >
            <div style={{ marginBottom: "8px" }}>
              <strong>Rolled: {diceRoll.diceTotal}</strong>
            </div>
            <div style={{ fontSize: "12px", marginBottom: "8px" }}>
              Landed on:{" "}
              <strong>
                {diceRoll.landedProperty || diceRoll.landedTileType}
              </strong>
            </div>
            {diceRoll.drawnCardDescription && (
              <div
                style={{
                  fontSize: "12px",
                  marginBottom: "8px",
                  color: "#d32f2f",
                }}
              >
                Card: {diceRoll.drawnCardDescription}
              </div>
            )}

          </div>
        )}

        {buyPrompt && diceRoll && (
          <div
            style={{
              background: "#e8f5e9",
              padding: "12px",
              borderRadius: "4px",
            }}
          >
            <div style={{ marginBottom: "8px", fontWeight: "bold" }}>
              Want to buy {diceRoll.landedProperty}?
            </div>
            <div style={{ display: "flex", gap: "8px" }}>
              <button
                onClick={() => handleBuyProperty(true)}
                disabled={loading}
                style={{
                  flex: 1,
                  padding: "8px",
                  background: "#4caf50",
                  color: "white",
                  cursor: loading ? "not-allowed" : "pointer",
                  border: "none",
                  borderRadius: "4px",
                }}
              >
                Buy
              </button>
              <button
                onClick={() => handleBuyProperty(false)}
                disabled={loading}
                style={{
                  flex: 1,
                  padding: "8px",
                  background: "#f44336",
                  color: "white",
                  cursor: loading ? "not-allowed" : "pointer",
                  border: "none",
                  borderRadius: "4px",
                }}
              >
                Pass
              </button>
            </div>
          </div>
        )}

        {gameState.isGameEnded && (
          <div
            style={{
              background: "#fff3cd",
              padding: "12px",
              borderRadius: "4px",
              color: "#e53935",
            }}
          >
            <strong>Game Over!</strong>
            <div>Winner: {gameState.winner}</div>
          </div>
        )}
        {error && (
          <div style={{ color: "red", marginTop: "12px", fontSize: "12px" }}>
            {error}
          </div>
        )}
      </div>

   <div
  style={{
    display: "grid",
    position: "absolute",
    top: "55%",
    left: "40%",
    transform: "translate(-50%, -50%)",
    gridTemplateColumns: `repeat(${boardSize}, 1fr)`,
    gridTemplateRows: `repeat(${boardSize}, 1fr)`,
    width: "400x",
    height: "400px",
  }}
>
  {board.map((row, y) =>
    row.map((tile, x) => (
      <div
        key={`${x}-${y}`}
        style={{
          border: "1px solid #ffffff",
          fontSize: "12px",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
      >
        {tile ? (
          <Tiles {...tile} />
        ) : (
          <div style={{ opacity: 0.2 }}>.</div>
        )}
      </div>
    ))
  )}
</div>
    </>
  );
}
