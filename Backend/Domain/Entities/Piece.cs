using Backend.Domain.Enums;
using Backend.Domain.ValueObjects;

class Piece : IPiece
{
    public PieceType Type { get; set; }

    public Point Position { get; set; }

    public Piece(PieceType type, Point position)
    {
        Type = type;
        Position = position;
    }
}
