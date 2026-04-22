import { useState, useEffect } from "react";
import type { GameState, RollResult } from "../Interfaces/Interface";
import { gameService } from "../services/gameService";
import { useBoardTiles } from "../hooks/useBoardTiles";
import "../Style/GameCSS.css";
import { ShowSuccess } from "../Constant/ToastUI";

export default function Game() {
  const playerNamesString = localStorage.getItem("playerNames");
  const playerNames = playerNamesString ? JSON.parse(playerNamesString) : [];

  const [gameState, setGameState] = useState<GameState | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [diceRoll, setDiceRoll] = useState<RollResult | null>(null);
  const [buyPrompt, setBuyPrompt] = useState(false);

  const { tiles, tilePositions, loading: tilesLoading } = useBoardTiles();

  useEffect(() => {
    const startGame = async () => {
      //   console.log("Starting game with players:", playerNames);
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

  useEffect(() => {
    if (!gameState) {
      console.log("Updated game state:", gameState);
    }
  }, [gameState]);
  const handleRoll = async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await gameService.rollTurn();
      setDiceRoll(result);
      setGameState(result.state);

      if (result.requiresBuyDecision) {
        setBuyPrompt(true);
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
      setError(err instanceof Error ? err.message : "Failed to buy property");
    } finally {
      setLoading(false);
    }
  };

  const handleEndTurn = async () => {
    setLoading(true);
    setError(null);
    try {
      const state = await gameService.endTurn();
      setGameState(state);
      setDiceRoll(null);
      setBuyPrompt(false);

      if (state.isGameEnded) {
        ShowSuccess(`Game Over! Winner: ${state.winner}`);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to end turn");
    } finally {
      setLoading(false);
    }
  };

  if (tilesLoading || !gameState) {
    return <div style={{ padding: "40px" }}>Loading board...</div>;
  }

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
              🎲 Roll Dice
            </button>
            <button
              onClick={handleEndTurn}
              disabled={loading}
              style={{
                width: "100%",
                padding: "12px",
                fontSize: "14px",
                cursor: loading ? "not-allowed" : "pointer",
                background: "#2196f3",
                color: "white",
                border: "none",
                borderRadius: "4px",
              }}
            >
              End Turn
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
            <button
              onClick={handleEndTurn}
              style={{
                width: "100%",
                padding: "8px",
                cursor: "pointer",
                background: "#2196f3",
                color: "white",
                border: "none",
                borderRadius: "4px",
              }}
            >
              Continue
            </button>
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

      <div className="table">
        <div className="board">
          <div className="center">
            <div className="community-chest-deck">
              <h2 className="label">Community Chest</h2>
              <div className="deck" />
            </div>
            <h1 className="title">MONOPOLY</h1>
            <div className="chance-deck">
              <h2 className="label">Chance</h2>
              <div className="deck" />
            </div>
          </div>
          {tilesLoading ? (
            <div
              style={{
                gridColumn: "1 / -1",
                textAlign: "center",
                padding: "40px",
              }}
            >
              Loading board...
            </div>
          ) : (
            tiles &&
            tiles.map((tile: any) => {
              const pos = tilePositions.get(tile.index);
              if (!pos) return null;

              const baseClass = `space ${
                tile.type === "StartTile"
                  ? "corner go"
                  : tile.type === "JailTile"
                    ? "corner jail"
                    : tile.type === "FreeParkingTile"
                      ? "corner free-parking"
                      : tile.type === "GoToJailTile"
                        ? "corner go-to-jail"
                        : tile.type === "RailroadTile"
                          ? "railroad"
                          : tile.type === "UtilityTile"
                            ? "utility"
                            : tile.type === "TaxTile"
                              ? "fee"
                              : tile.type === "DrawChance"
                                ? "chance"
                                : tile.type === "DrawCommunity"
                                  ? "community-chest"
                                  : "property"
              }`;

              const colorMap: Record<string, string> = {
                Brown: "dark-purple",
                LightBlue: "light-blue",
                Pink: "purple",
                Orange: "orange",
                Red: "red",
                Yellow: "yellow",
                Green: "green",
                DarkBlue: "dark-blue",
              };
              const colorClass = (tile.color && colorMap[tile.color]) || "";

              return (
                <div
                  key={tile.index}
                  className={`${baseClass} ${colorClass}`}
                  style={{
                    gridColumn: pos.gridColumn,
                    gridRow: pos.gridRow,
                  }}
                >
                  <div className="container">
                    {tile.color && <div className="color-bar" />}
                    <div className="name" style={{ fontSize: "10px" }}>
                      {tile.city || tile.type}
                    </div>
                    {tile.price && (
                      <div className="price">Price ${tile.price}</div>
                    )}
                    {tile.owner && (
                      <div
                        style={{
                          fontSize: "9px",
                          color: "#666",
                          marginTop: "2px",
                        }}
                      >
                        Owned: {tile.owner}
                      </div>
                    )}
                    {(tile.houses || tile.hasHotel) && (
                      <div style={{ fontSize: "9px", color: "#d32f2f" }}>
                        {tile.hasHotel ? "🏨" : `${tile.houses}🏠`}
                      </div>
                    )}
                  </div>
                </div>
              );
            })
          )}
        </div>
      </div>
    </>
  );
}
