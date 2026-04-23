import type { TileData } from "../Interfaces/Interface";

export default function Tiles({ type, asset, owner }: TileData) {
  const accentColor =
    type === "RentTile" && asset?.color ? asset.color : "#52525b";

  return (
    <div className="relative w-[52px] h-[52px] flex flex-col justify-between p-1 rounded border border-zinc-700 bg-zinc-900 overflow-hidden transition-transform duration-100 cursor-default hover:scale-110 hover:z-10">
      <div
        className="absolute top-0 left-0 right-0 h-1.5 rounded-t"
        style={{ backgroundColor: accentColor }}
      />
      <p className="mt-2 text-[6px] font-bold uppercase tracking-wider text-zinc-500 leading-tight truncate">
        {type.replace("Tile", "")}
      </p>
      <div className="flex flex-col gap-0.5">
        {asset && (
          <p className="text-[7px] font-semibold text-zinc-200 leading-tight truncate">
            {asset.city}
          </p>
        )}
        {owner && (
          <span className="inline-block text-[6px] font-bold text-white bg-emerald-600 rounded-sm px-1 leading-tight truncate max-w-full">
            {owner}
          </span>
        )}
      </div>
    </div>
  );
}
