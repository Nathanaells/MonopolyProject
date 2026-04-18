using Backend.Domain.Enums;
using Backend.Domain.Interfaces;
using Backend.Domain.ValueObjects;

public interface ITile
{
    public TileType Type { get; set; }
    public Point Point { get; set; }
    public List<IPiece> Pieces { get; set; }

    public IAsset? Asset { get; set; }

    public IPlayer? Owner { get; set; }

    public int? House { get; set; }

    public bool? HasHotel { get; set; }
}
