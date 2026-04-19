namespace Backend.Services;

using Backend.Domain.ValueObjects;
using Backend.Domain.Enums;


public class PieceFactory
{
    public static List<IPiece> CreateStandardPieces()
    {
        var pieces = new List<IPiece>();

        var pieceTypes = new[] { PieceType.Tophat, PieceType.Car, PieceType.ScottieDog, PieceType.Thimble, PieceType.Cannon, PieceType.Wheelbarrow, PieceType.Battleship, PieceType.Horse };

        foreach (var type in pieceTypes)
        {
            pieces.Add(new Piece(type, new Point(0, 0)));
        }
        return pieces;
    }
}