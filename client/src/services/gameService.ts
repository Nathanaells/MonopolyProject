import { API_BASE } from "../Constant/Url";
import type {
  TileData,
  GameState,
  RollResult,
  PieceData,
  SellResult,
} from "../Interfaces/Interface";
import { ShowError } from "../Constant/ToastUI";

// ─── Types ────────────────────────────────────────────────────────

const parseResponseBody = async <T = unknown>(
  res: Response,
): Promise<T | null> => {
  const text = await res.text();
  if (!text) return null;

  try {
    return JSON.parse(text) as T;
  } catch {
    return text as T;
  }
};

const getErrorMessage = (data: unknown, fallback: string): string => {
  if (!data) return fallback;
  if (typeof data === "string") return data;

  if (typeof data === "object") {
    const obj = data as Record<string, unknown>;

    if (obj.errors && typeof obj.errors === "object") {
      const errors = obj.errors as Record<string, unknown>;
      const firstErrorArray = Object.values(errors).find(
        (v) => Array.isArray(v) && v.length > 0,
      ) as unknown[] | undefined;

      if (firstErrorArray && typeof firstErrorArray[0] === "string") {
        return firstErrorArray[0];
      }
    }

    if (typeof obj.message === "string") return obj.message;
    if (typeof obj.title === "string") return obj.title;
    if (typeof obj.error === "string") return obj.error;
  }

  return fallback;
};

const normalizeTile = (tile: TileData): TileData => {
  const ownerValue = tile.owner as unknown;
  if (
    ownerValue &&
    typeof ownerValue === "object" &&
    "name" in (ownerValue as Record<string, unknown>)
  ) {
    const ownerName = (ownerValue as { name?: unknown }).name;
    return {
      ...tile,
      owner: typeof ownerName === "string" ? ownerName : undefined,
    };
  }

  return tile;
};

export const gameService = {
  async startGame(playerNames: string[]): Promise<GameState> {
    const res = await fetch(`${API_BASE}/start`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ playerNames }),
    });
    const data = await parseResponseBody<GameState | { message?: string }>(res);
    if (!res.ok) {
      const message = getErrorMessage(data, "Failed to start game");
      ShowError(message);
      throw new Error(message);
    }
    return data as GameState;
  },

  async getGameState(): Promise<GameState> {
    const res = await fetch(`${API_BASE}/state`);
    const data = await parseResponseBody<GameState | unknown>(res);
    if (!res.ok) {
      const message = getErrorMessage(data, "Failed to fetch game state");
      ShowError(message);
      throw new Error(message);
    }
    return data as GameState;
  },

  // ── Board ───────────────────────────────────────────────────────
  async getBoardTiles(): Promise<TileData[]> {
    const res = await fetch(`${API_BASE}/board/tiles`);
    const data = await parseResponseBody<TileData[] | unknown>(res);
    if (!res.ok) {
      const message = getErrorMessage(data, "Failed to load board tiles");
      ShowError(message);
      throw new Error(message);
    }
    return (data as TileData[]).map(normalizeTile);
  },

  async getAvailablePieces(): Promise<PieceData[]> {
    const res = await fetch(`${API_BASE}/pieces`);
    const data = await parseResponseBody<PieceData[] | unknown>(res);
    if (!res.ok) {
      const message = getErrorMessage(data, "Failed to load pieces");
      ShowError(message);
      throw new Error(message);
    }
    return data as PieceData[];
  },

  async selectPiece(playerName: string, pieceType: string): Promise<GameState> {
    const res = await fetch(`${API_BASE}/select-piece`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ playerName, pieceType }),
    });
    const data = await parseResponseBody<GameState | unknown>(res);
    if (!res.ok) {
      const message = getErrorMessage(data, "Failed to select piece");
      ShowError(message);
      throw new Error(message);
    }
    return data as GameState;
  },

  // ── Roll ────────────────────────────────────────────────────────
  async rollTurn(): Promise<RollResult> {
    const res = await fetch(`${API_BASE}/turn/roll`, { method: "POST" });
    const data = await parseResponseBody<RollResult | unknown>(res);
    if (!res.ok) {
      const message = getErrorMessage(data, "Failed to roll dice");
      ShowError(message);
      throw new Error(message);
    }
    return data as RollResult;
  },

  // ── Buy property ────────────────────────────────────────────────
  async buyProperty(buy: boolean): Promise<GameState> {
    const res = await fetch(`${API_BASE}/turn/buy-property`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ buy }),
    });
    const data = await parseResponseBody<GameState | unknown>(res);
    if (!res.ok) {
      const message = getErrorMessage(data, "Failed to buy property");
      ShowError(message);
      throw new Error(message);
    }
    return data as GameState;
  },

  // ── Player properties ────────────────────────────────────────────
  async getPlayerProperties(playerName: string): Promise<TileData[]> {
    const res = await fetch(
      `${API_BASE}/player-properties?playerName=${encodeURIComponent(playerName)}`,
    );
    const data = await parseResponseBody<TileData[] | unknown>(res);
    if (!res.ok) {
      const message = getErrorMessage(data, "Failed to load player properties");
      ShowError(message);
      throw new Error(message);
    }
    return (data as TileData[]).map(normalizeTile);
  },

  // ── Buy building ─────────────────────────────────────────────────
  async buyBuilding(
    playerName: string,
    city: string,
    buildHotel: boolean,
  ): Promise<GameState> {
    const res = await fetch(`${API_BASE}/buy-building`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ playerName, city, buildHotel }),
    });
    const data = await parseResponseBody<GameState | unknown>(res);
    if (!res.ok) {
      const message = getErrorMessage(data, "Failed to buy building");
      ShowError(message);
      throw new Error(message);
    }
    return data as GameState;
  },

  // ── Sell property ────────────────────────────────────────────────
  async sellProperty(
    playerName: string,
    city: string,
    includeBuildings: boolean,
  ): Promise<SellResult> {
    const res = await fetch(`${API_BASE}/sell-property`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ playerName, city, includeBuildings }),
    });
    const data = await parseResponseBody<SellResult | unknown>(res);
    if (!res.ok) {
      const message = getErrorMessage(data, "Failed to sell property");
      ShowError(message);
      throw new Error(message);
    }
    return data as SellResult;
  },

  // ── Sell buildings ───────────────────────────────────────────────
  async sellBuildings(
    playerName: string,
    city: string,
    housesToSell: number,
    sellHotel: boolean,
  ): Promise<SellResult> {
    const res = await fetch(`${API_BASE}/sell-buildings`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ playerName, city, housesToSell, sellHotel }),
    });
    const data = await parseResponseBody<SellResult | unknown>(res);
    if (!res.ok) {
      const message = getErrorMessage(data, "Failed to sell buildings");
      ShowError(message);
      throw new Error(message);
    }
    return data as SellResult;
  },

  // ── Sell ALL assets ──────────────────────────────────────────────
  async sellAllAssets(playerName: string): Promise<SellResult> {
    const res = await fetch(`${API_BASE}/sell-all-assets`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ playerName }),
    });
    const data = await parseResponseBody<SellResult | unknown>(res);
    if (!res.ok) {
      const message = getErrorMessage(data, "Failed to sell all assets");
      ShowError(message);
      throw new Error(message);
    }
    return data as SellResult;
  },

  // ── Execute card ─────────────────────────────────────────────────
  async executeCard(
    cardType: string,
    description: string,
    behaviour: number,
  ): Promise<GameState> {
    const res = await fetch(`${API_BASE}/execute-card`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ cardType, description, behaviour }),
    });
    const data = await parseResponseBody<GameState | unknown>(res);
    if (!res.ok) {
      const message = getErrorMessage(data, "Failed to execute card");
      ShowError(message);
      throw new Error(message);
    }
    return data as GameState;
  },
};
