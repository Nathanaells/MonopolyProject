using Backend.Domain.Interfaces;

public class Board : IBoard
{
    public ITile[] Tiles { get; set; }

    public Board(ITile[] tiles)
    {
        Tiles = tiles;
    }
}
