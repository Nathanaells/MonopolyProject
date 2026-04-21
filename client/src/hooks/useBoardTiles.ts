import { useEffect, useState } from "react";
import type { TileData } from "../services/gameService";
import { gameService } from "../services/gameService";

interface TilePosition {
  gridColumn: string;
  gridRow: string;
  className: string;
}

export const useBoardTiles = () => {
  const [tiles, setTiles] = useState<TileData[]>([]);
  const [tilePositions, setTilePositions] = useState<Map<number, TilePosition>>(
    new Map(),
  );
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadTiles = async () => {
      try {
        const tilesData = await gameService.getBoardTiles();
        setTiles(tilesData);

        // Map tiles ke posisi di grid berdasarkan index dan Point
        const positions = new Map<number, TilePosition>();

        tilesData.forEach((tile) => {
          const { index } = tile;

          // Konversi Point (x, y) ke grid position
          // Bottom row (index 0-9): row 11, columns vary
          // Left column (index 10-19): col 1, rows vary
          // Top row (index 20-29): row 1, columns vary
          // Right column (index 30-39): col 11, rows vary

          let gridCol = "1";
          let gridRow = "1";
          let className = "space";

          if (index === 0) {
            // GO corner
            gridCol = "11";
            gridRow = "11";
            className = "space corner go";
          } else if (index < 10) {
            // Bottom row
            gridCol = String(12 - index);
            gridRow = "11";
            className = "space";
          } else if (index < 20) {
            // Left column
            gridCol = "1";
            gridRow = String(20 - index);
            className = "space";
          } else if (index < 30) {
            // Top row
            gridCol = String(index - 19);
            gridRow = "1";
            className = "space";
          } else {
            // Right column
            gridCol = "11";
            gridRow = String(index - 29);
            className = "space";
          }

          positions.set(index, {
            gridColumn: gridCol,
            gridRow: gridRow,
            className,
          });
        });

        setTilePositions(positions);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load tiles");
      } finally {
        setLoading(false);
      }
    };

    loadTiles();
  }, []);

  return { tiles, tilePositions, loading, error };
};
