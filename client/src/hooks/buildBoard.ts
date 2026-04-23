import type { TileData } from "../Interfaces/Interface";
import { createEmptyBoard } from "./createEmptyBoard";

export const buildBoard = (tiles: TileData[], size: number): (TileData | null)[][] => {
  const board = createEmptyBoard(size);

  tiles.forEach((tile) => {
    const { x, y } = tile.position;

    if (board[y] && board[y][x] !== undefined) {
      board[y][x] = tile;
    }
  });

  return board;
};