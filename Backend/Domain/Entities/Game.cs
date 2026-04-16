using Backend.Domain.Interfaces;

class Game
{
    private IBoard _board;
    private bool _gameEnded;
    private List<IPlayer> _players;
    private List<IPiece> _pieces;
}
