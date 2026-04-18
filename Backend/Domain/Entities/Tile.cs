using Backend.Domain.Enums;
using Backend.Domain.Interfaces;
using Backend.Domain.ValueObjects;

class Tile : ITile
{
    public TileType Type { get; set; }
    public Point Point { get; set; }
    public List<IPiece> Pieces { get; set; }

    public IAsset? Asset { get; set; }

    public IPlayer? Owner { get; set; }

    public int? House { get; set; }

    public bool? HasHotel { get; set; }

    public Tile(TileType type, Point point)
    {
        Type = type;
        Point = point;
        Pieces = new List<IPiece>();
        Asset = null;
        Owner = null;
        House = null;
        HasHotel = null;
    }
}
