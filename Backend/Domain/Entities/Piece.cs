using Backend.Domain.Enums;
using Backend.Domain.ValueObjects;

public class Piece : IPiece
{
    public PieceType Type { get; set; }

    public Piece(PieceType type, Point position)
    {
        Type = type;

    }
}
