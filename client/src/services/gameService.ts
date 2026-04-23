import { API_BASE } from "../Constant/Url";
import type { TileData, GameState } from "../Interfaces/Interface";
import { ShowError } from "../Constant/ToastUI";

export const gameService = {
  async startGame(playerNames: string[]): Promise<GameState> {
    const res = await fetch(`${API_BASE}/start`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ playerNames }),
    });

    const data = await res.json();

    if (!res.ok) {
      ShowError(data.message || "Failed to start game");
      throw new Error(data.message || "Failed to start game");
    }
    return data;
  },

  async getGameState(): Promise<GameState> {
    const res = await fetch(`${API_BASE}/state`);

    if(!res.ok){
      ShowError(res.statusText || "Failed to fetch game state");
    }
    const data = await res.json();
    return data;
  },

  async getBoardTiles(): Promise<TileData[]> {
    const res = await fetch(`${API_BASE}/board/tiles`);
    const data: TileData[] = await res.json();
    if (!res.ok) {
      ShowError(res.statusText || "Failed to load board tiles");
    }
  
    console.log("Fetched tiles:", data);
    return data;
  },

  async rollTurn(): Promise<any> {
    const res = await fetch(`${API_BASE}/turn/roll`, { method: "POST" });

    if(!res.ok){
      ShowError(res.statusText || "Failed to roll dice");
    }
    const data = await res.json();
    console.log("Roll turn response:", data);
  
    return data;
  },

  async buyProperty(buy: boolean): Promise<GameState> {
    const res = await fetch(`${API_BASE}/turn/buy-property`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ buy }),
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  
};
