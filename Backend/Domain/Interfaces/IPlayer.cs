using Backend.Domain.Enums;

public interface IPlayer
{
    public string Name { get; set; }
    public int Position { get; set; }
    public PieceType Piece { get; set; }
    public bool IsBankrupt { get; set; }
}
