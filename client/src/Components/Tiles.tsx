import type { TileData } from "../Interfaces/Interface";

export default function Tiles(props: TileData) {
    
    const { type, position, asset, owner } = props;

  return (
    <div
      style={{
        backgroundColor: type === "RentTile" ? asset?.color: "#ffffff",
        color: "#000000",
        fontWeight: "bold",
        fontFamily: "Arial, sans-serif",
        width: "80px",
        height: "80px",
        border: "1px solid #150000",

        fontSize: "12px",
        display: "flex",
        flexDirection: "column",
        justifyContent: "space-between",
        padding: "4px",
      }}
    >
      <div>{type}</div>

      {asset && (
        <div style={{ fontSize: "9px", color: "#000000" }}>
          {asset.city}
        </div>
      )}

      {owner && (
        <div style={{ fontSize: "9px", color: "green" }}>
          {owner}
        </div>
      )}
    </div>
  );
}