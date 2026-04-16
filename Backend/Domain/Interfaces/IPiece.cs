using Backend.Domain.Enums;
using Backend.Domain.ValueObjects;

public interface IPiece
{
    public PieceType Type { get; set; }
    public Point Position { get; set; }
}
