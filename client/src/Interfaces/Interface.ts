export interface GameState {
  isGameEnded: boolean;
  winner?: string;
  currentPlayer: string;
  players: PlayerState[];
  message?: string;
}

export interface PlayerState {
  name: string;
  balance: number;
  isInJail: boolean;
  isBankrupt: boolean;
  properties: string[];
  currentTileIndex?: number;
}

export interface TilePosition {
  gridColumn: number;
  gridRow: number;
  className: string;
}

export interface RollResult {
  diceTotal: number;
  dice1: number;
  dice2: number;
  landedTileType: string;
  landedProperty?: string;
  requiresBuyDecision: boolean;
  drawnCardDescription?: string;
  jailRollResult: "None" | "Released" | "StayedInJail";
  state: GameState;
}

export type TileData = {
  index: number;
  type: string;
  position: {
    x: number;
    y: number;
  };
  asset?: {
    price: number;
    color: string;
    city: string;
  };
  owner?: string;
  houses?: number;
  hasHotel?: boolean;
};

export interface PieceData {
  pieceType: string;
  isAvailable: boolean;
}

export interface SellResult {
  income: number;
  gameState: GameState;
  message?: string;
}


export interface Props {
  playerName: string;
  isCurrentPlayer: boolean;
  onUpdate: () => void;
  onClose: () => void;
}

export interface DiceProps {
  dice1: number;
  dice2: number;
  rolling: boolean;
  onDone?: () => void;
}

export interface PlayerProps {
  playerNames: string[];
  onAllPicked: () => void;
}

export interface PieceSVGProps {
  color: string;
  size?: number;
}

export type PendingMove = {
  playerName: string;
  from: number;
  to: number;
  result: RollResult;
};
