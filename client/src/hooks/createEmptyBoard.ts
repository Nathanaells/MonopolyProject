import type { TileData } from "../Interfaces/Interface";

export const createEmptyBoard = (size: number): (TileData | null)[][] => {
  return Array.from({ length: size }, () => Array.from({ length: size }, () => null));
};