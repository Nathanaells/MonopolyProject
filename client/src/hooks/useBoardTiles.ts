import { useEffect, useState } from "react";
import type { TileData, TilePosition } from "../Interfaces/Interface";
import { gameService } from "../services/gameService";

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

        const mappedTiles: TileData[] = tilesData.map((tile: any) => ({
          index: tile.index,
          type: tile.type,
          pointX: tile.position.x,
          pointY: tile.position.y,
          city: tile.asset?.city,
          price: tile.asset?.price,
          color: tile.asset?.color,
          owner: tile.owner,
          houses: tile.houses,
          hasHotel: tile.hasHotel,
        }));

        setTiles(mappedTiles);

        const positions = new Map<number, TilePosition>();

        mappedTiles.forEach((tile) => {
          const { index } = tile;

          let gridCol = "1";
          let gridRow = "1";
          let className = "space";

          if (index === 0) {
            gridCol = "11";
            gridRow = "11";
            className = "space corner go";
          } else if (index < 10) {
            gridCol = String(12 - index);
            gridRow = "11";
            className = "space";
          } else if (index < 20) {
            gridCol = "1";
            gridRow = String(20 - index);
            className = "space";
          } else if (index < 30) {
            // Top row
            gridCol = String(index - 19);
            gridRow = "1";
            className = "space";
          } else {
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
