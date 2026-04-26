import { useState, useEffect } from "react";
import { gameService } from "../services/gameService";
import type { TileData, Props } from "../Interfaces/Interface";
import { colorHex  } from "../Constant/ColorHex";




function HouseIcon({ count }: { count: number }) {
  return (
    <div className="flex gap-0.5">
      {Array.from({ length: count }).map((_, i) => (
        <div key={i} className="w-3 h-3">
          <svg viewBox="0 0 12 12" fill="none">
            <path d="M1 6 L6 1 L11 6 V11 H8 V8 H4 V11 H1 Z" fill="#22c55e" />
          </svg>
        </div>
      ))}
    </div>
  );
}

function HotelIcon() {
  return (
    <div className="w-4 h-4">
      <svg viewBox="0 0 16 16" fill="none">
        <rect x="1" y="6" width="14" height="9" rx="1" fill="#ef4444" />
        <path d="M2 6 L8 1 L14 6 Z" fill="#dc2626" />
        <rect
          x="6"
          y="9"
          width="4"
          height="6"
          rx="0.5"
          fill="white"
          opacity="0.5"
        />
      </svg>
    </div>
  );
}

export default function PlayerPropertiesPanel({
  playerName,
  isCurrentPlayer,
  onUpdate,
  onClose,
}: Props) {
  const [properties, setProperties] = useState<TileData[]>([]);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [confirmSellAll, setConfirmSellAll] = useState(false);
  const [message, setMessage] = useState("");

  const fetchProperties = async () => {
    setLoading(true);
    try {
      const data = await gameService.getPlayerProperties(playerName);
      setProperties(data);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchProperties();
  }, [playerName]);

  const flash = (msg: string) => {
    setMessage(msg);
    setTimeout(() => setMessage(""), 3000);
  };

  const handleBuyBuilding = async (city: string, buildHotel: boolean) => {
    setActionLoading(city + (buildHotel ? "_hotel" : "_house"));
    try {
      await gameService.buyBuilding(playerName, city, buildHotel);
      flash(buildHotel ? "Hotel dibangun!" : "Rumah dibangun!");
      fetchProperties();
      onUpdate();
    } catch (e: any) {
      flash(`${e.message}`);
    } finally {
      setActionLoading(null);
    }
  };

  const handleSellProperty = async (city: string) => {
    setActionLoading(city + "_sell");
    try {
      await gameService.sellProperty(playerName, city, true);
      flash("Properti dijual!");
      fetchProperties();
      onUpdate();
    } catch (e: any) {
      flash(`${e.message}`);
    } finally {
      setActionLoading(null);
    }
  };

  const handleSellAll = async () => {
    if (!confirmSellAll) {
      setConfirmSellAll(true);
      setTimeout(() => setConfirmSellAll(false), 4000);
      return;
    }
    setActionLoading("all");
    try {
      const result = await gameService.sellAllAssets(playerName);
      flash(`💰 Semua aset dijual! +$${result.income}`);
      fetchProperties();
      onUpdate();
      setConfirmSellAll(false);
    } catch (e: any) {
      flash(`❌ ${e.message}`);
    } finally {
      setActionLoading(null);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-end justify-center sm:items-center">
      <div
        className="absolute inset-0 bg-black/60 backdrop-blur-sm"
        onClick={onClose}
      />

      <div className="relative z-10 w-full max-w-sm bg-zinc-900 border border-zinc-700 rounded-t-2xl sm:rounded-2xl overflow-hidden shadow-2xl animate-slide-up">
        <div className="flex items-center justify-between px-5 py-4 border-b border-zinc-800">
          <div>
            <h2 className="text-zinc-100 font-bold text-base">Properti</h2>
            <p className="text-zinc-400 text-xs">{playerName}</p>
          </div>
          <button
            onClick={onClose}
            className="text-zinc-500 hover:text-zinc-300 text-xl leading-none"
          >
            ✕
          </button>
        </div>

        {message && (
          <div className="mx-5 mt-3 bg-zinc-800 border border-zinc-600 rounded-lg px-3 py-2 text-sm text-zinc-200">
            {message}
          </div>
        )}

        <div className="overflow-y-auto max-h-[60vh] p-5 space-y-3">
          {loading ? (
            <div className="flex justify-center py-8">
              <div className="w-6 h-6 border-2 border-zinc-600 border-t-zinc-300 rounded-full animate-spin" />
            </div>
          ) : properties.length === 0 ? (
            <p className="text-zinc-500 text-sm text-center py-8">
              Belum punya properti
            </p>
          ) : (
            properties.map((tile) => {
              const city = tile.asset?.city ?? "";
              const color = tile.asset?.color ?? "";
              const price = tile.asset?.price ?? 0;
              const houses = tile.houses ?? 0;
              const hasHotel = tile.hasHotel ?? false;
              const isKey = city + "_house";
              const isKeyH = city + "_hotel";
              const isSellKey = city + "_sell";

              const canBuyHouse = isCurrentPlayer && !hasHotel && houses < 3;
              const canBuyHotel = isCurrentPlayer && !hasHotel && houses === 3;

              return (
                <div
                  key={city}
                  className="bg-zinc-800 rounded-xl overflow-hidden border border-zinc-700"
                >
                  <div
                    className="h-1.5 w-full"
                    style={{ background: colorHex[color] ?? "#888" }}
                  />

                  <div className="p-3">
                    <div className="flex items-start justify-between gap-2">
                      <div className="flex-1 min-w-0">
                        <p className="text-zinc-100 text-sm font-semibold truncate">
                          {city.replace(/([A-Z])/g, " $1").trim()}
                        </p>
                        <p className="text-zinc-500 text-xs">${price}</p>

                        <div className="flex items-center gap-1.5 mt-1.5">
                          {hasHotel ? (
                            <>
                              <HotelIcon />
                              <span className="text-xs text-red-400 font-medium">
                                Hotel
                              </span>
                            </>
                          ) : houses > 0 ? (
                            <>
                              <HouseIcon count={houses} />
                              <span className="text-xs text-green-400 font-medium">
                                {houses} rumah
                              </span>
                            </>
                          ) : (
                            <span className="text-xs text-zinc-600">
                              Kosong
                            </span>
                          )}
                        </div>
                      </div>

                      {isCurrentPlayer && (
                        <div className="flex flex-col gap-1.5 flex-shrink-0">
                          {canBuyHouse && (
                            <button
                              onClick={() => handleBuyBuilding(city, false)}
                              disabled={actionLoading === isKey}
                              className="px-2.5 py-1 bg-green-600 hover:bg-green-500 text-white text-xs rounded-lg font-semibold disabled:opacity-50 transition-colors"
                            >
                              {actionLoading === isKey ? "..." : "+🏠"}
                            </button>
                          )}
                          {canBuyHotel && (
                            <button
                              onClick={() => handleBuyBuilding(city, true)}
                              disabled={actionLoading === isKeyH}
                              className="px-2.5 py-1 bg-red-600 hover:bg-red-500 text-white text-xs rounded-lg font-semibold disabled:opacity-50 transition-colors"
                            >
                              {actionLoading === isKeyH ? "..." : "+🏨"}
                            </button>
                          )}
                          <button
                            onClick={() => handleSellProperty(city)}
                            disabled={actionLoading === isSellKey}
                            className="px-2.5 py-1 bg-zinc-700 hover:bg-zinc-600 text-zinc-300 text-xs rounded-lg font-semibold disabled:opacity-50 transition-colors"
                          >
                            {actionLoading === isSellKey ? "..." : "Jual"}
                          </button>
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              );
            })
          )}
        </div>

        {isCurrentPlayer && properties.length > 0 && (
          <div className="px-5 pb-5 pt-3 border-t border-zinc-800">
            <button
              onClick={handleSellAll}
              disabled={actionLoading === "all"}
              className={`
                w-full py-3 rounded-xl font-bold text-sm transition-all
                ${
                  confirmSellAll
                    ? "bg-red-600 hover:bg-red-500 text-white animate-pulse"
                    : "bg-zinc-800 hover:bg-zinc-700 text-zinc-300 border border-zinc-700"
                }
                disabled:opacity-50
              `}
            >
              {actionLoading === "all"
                ? "Menjual semua..."
                : confirmSellAll
                  ? "⚠️ Yakin? Klik lagi untuk konfirmasi"
                  : "💸 Jual Semua Aset"}
            </button>
          </div>
        )}
      </div>

      <style>{`
        @keyframes slideUp {
          from { transform: translateY(100%); opacity: 0; }
          to   { transform: translateY(0);    opacity: 1; }
        }
        .animate-slide-up {
          animation: slideUp 0.28s cubic-bezier(0.34, 1.56, 0.64, 1);
        }
      `}</style>
    </div>
  );
}
