import { baseURL } from "../Constant/Url";
import type {
  TileData,
  GameState,
  RollResult,
  PieceData,
  SellResult,
} from "../Interfaces/Interface";
import { ShowError } from "../Constant/ToastUI";

export const gameService = {
  async startGame(playerNames: string[]): Promise<GameState> {
    const res = await fetch(`${baseURL}/start`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ playerNames }),
    });
    const data: GameState = await res.json();

    if (!res.ok) {
      ShowError("Failed to start game");
    }
    return data;
  },

  async getGameState(): Promise<GameState> {
    const res = await fetch(`${baseURL}/state`);
    const data: GameState = await res.json();
    if (!res.ok) {
      ShowError("Failed to fetch game state");
    }
    return data;
  },

  async getBoardTiles(): Promise<TileData[]> {
    const res = await fetch(`${baseURL}/board/tiles`);
    const data: TileData[] = await res.json();
    if (!res.ok) {
      ShowError("Failed to load board tiles");
    }
    return data;
  },

  async getAvailablePieces(): Promise<PieceData[]> {
    const res = await fetch(`${baseURL}/pieces`);
    const data: PieceData[] = await res.json();
    if (!res.ok) {
      ShowError("Failed to load pieces");
      throw new Error("Failed to load pieces");
    }
    return data as PieceData[];
  },

  async selectPiece(playerName: string, pieceType: string): Promise<GameState> {
    const res = await fetch(`${baseURL}/select-piece`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ playerName, pieceType }),
    });
    const data: GameState = await res.json();
    if (!res.ok) {
      ShowError("Failed to select piece");
    }
    return data as GameState;
  },

  async rollTurn(): Promise<RollResult> {
    const res = await fetch(`${baseURL}/turn/roll`, { method: "POST" });
    const data: RollResult = await res.json();
    if (!res.ok) {
      ShowError("Failed to roll dice");
    }
    return data;
  },

  async buyProperty(buy: boolean): Promise<GameState> {
    const res = await fetch(`${baseURL}/turn/buy-property`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ buy }),
    });
    const data: GameState = await res.json();
    if (!res.ok) {
      ShowError("Failed to buy property");
      throw new Error("Failed to buy property");
    }
    return data as GameState;
  },

  async getPlayerProperties(playerName: string): Promise<TileData[]> {
    const res = await fetch(
      `${baseURL}/player-properties?playerName=${encodeURIComponent(playerName)}`,
    );
    const data: TileData[] = await res.json();
    if (!res.ok) {
      ShowError("Failed to fetch player properties");
      throw new Error("Failed to fetch player properties");
    }
    return data;
  },

  async buyBuilding(
    playerName: string,
    city: string,
    buildHotel: boolean,
  ): Promise<GameState> {
    const res = await fetch(`${baseURL}/buy-building`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ playerName, city, buildHotel }),
    });
    const data: GameState = await res.json();
    if (!res.ok) {
      ShowError("Failed to buy building");
      throw new Error("Failed to buy building");
    }
    return data as GameState;
  },

  async sellProperty(
    playerName: string,
    city: string,
    includeBuildings: boolean,
  ): Promise<SellResult> {
    const res = await fetch(`${baseURL}/sell-property`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ playerName, city, includeBuildings }),
    });
    const data: SellResult = await res.json();
    if (!res.ok) {
      ShowError("Failed to sell property");
      throw new Error("Failed to sell property");
    }
    return data;
  },

  async sellBuildings(
    playerName: string,
    city: string,
    housesToSell: number,
    sellHotel: boolean,
  ): Promise<SellResult> {
    const res = await fetch(`${baseURL}/sell-buildings`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ playerName, city, housesToSell, sellHotel }),
    });
    const data: SellResult = await res.json();
    if (!res.ok) {
      ShowError("Failed to sell buildings");
      throw new Error("Failed to sell buildings");
    }
    return data;
  },

  async sellAllAssets(playerName: string): Promise<SellResult> {
    const res = await fetch(`${baseURL}/sell-all-assets`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ playerName }),
    });
    const data: SellResult = await res.json();
    if (!res.ok) {
      ShowError("Failed to sell all assets");
      throw new Error("Failed to sell all assets");
    }
    return data;
  },

  async executeCard(
    cardType: string,
    description: string,
    behaviour: number,
  ): Promise<GameState> {
    const res = await fetch(`${baseURL}/execute-card`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ cardType, description, behaviour }),
    });
    const data: GameState = await res.json();

    // console.log("Card execution result:", data);
    if (!res.ok) {
      ShowError("Failed to execute card");
      throw new Error("Failed to execute card");
    }
    return data;
  },
};
