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

export interface TilePosition {
  gridColumn: string;
  gridRow: string;
  className: string;
}

export interface RollResult {
  diceTotal: number;
  landedTileType: string;
  landedProperty?: string;
  requiresBuyDecision: boolean;
  drawnCardDescription?: string;
  state: GameState;
}
