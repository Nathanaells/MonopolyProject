const API_BASE = "http://localhost:5126/api/game";

export interface TileData {
  index: number;
  type: string;
  pointX: number;
  pointY: number;
  city?: string;
  price?: number;
  color?: string;
  owner?: string;
  houses?: number;
  hasHotel?: boolean;
}

export interface GameState {
  isGameEnded: boolean;
  winner?: string;
  currentPlayer: string;
  players: PlayerState[];
}

export interface PlayerState {
  name: string;
  balance: number;
  isInJail: boolean;
  isBankrupt: boolean;
  tileType: string;
  tileProperty?: string;
  properties: string[];
}

export const gameService = {
  async startGame(playerNames: string[]): Promise<GameState> {
    const res = await fetch(`${API_BASE}/start`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ playerNames }),
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  async getGameState(): Promise<GameState> {
    const res = await fetch(`${API_BASE}/state`);
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  async getBoardTiles(): Promise<TileData[]> {
    const res = await fetch(`${API_BASE}/board/tiles`);
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  async rollTurn(): Promise<any> {
    const res = await fetch(`${API_BASE}/turn/roll`, { method: "POST" });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
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

  async endTurn(): Promise<GameState> {
    const res = await fetch(`${API_BASE}/turn/end`, { method: "POST" });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },
};
