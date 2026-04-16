namespace Backend.Domain.GameEntity;

using Backend.Domain.Enums;
using Backend.Domain.Interfaces;

class Game
{
    private IBoard _board;
    private bool _gameEnded = false;
    private int _currentPlayerIndex = 0;
    private List<IPlayer> _players;
    private List<IPiece> _pieces;

    private List<ICard> _cards;

    private List<IMoney> _money;

    private Dictionary<IPlayer, IPiece> _playerPiece;
    private Dictionary<IPlayer, List<IMoney>> _playerData;

    public event Action<IPlayer, IPiece> PlayerSentToJail;

    public event Action<IPlayer> PlayerBankrupt;
    public event Action<IPlayer> IsGameEnded;

    public IPlayer CurrentPlayer => _players[_currentPlayerIndex];

    public Game(
        IBoard board,
        List<IPlayer> players,
        List<IPiece> pieces,
        List<ICard> cards,
        List<IMoney> money
    )
    {
        _board = board;
        _players = players;
        _money = money;
        _pieces = pieces;
        _cards = cards;

        _playerPiece = new Dictionary<IPlayer, IPiece>();
        _playerData = new Dictionary<IPlayer, List<IMoney>>();
    }

    public IBoard Board => _board;

    public List<IMoney> GetMoney()
    {
        return _money;
    }

    public IPiece GetPiece(IPlayer player)
    {
        return _playerPiece[player];
    }

    public void Playturn(IPlayer player)
    {
        while (!_players[_currentPlayerIndex].IsBankrupt)
        {
            _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;
        }
    }

    public void MovePiece(IPlayer player, int steps) { }

    private ITile GetTileByType(TileType type)
    {
        return _board.Tiles.First(t => t.Type == type);
    }

    public void SendPieceToJail(IPlayer player)
    {
        var jailTile = GetTileByType(TileType.JailTile);
        jailTile.Pieces.Add(_playerPiece[player]);
        player.IsInJail = true;
    }

    public void PlayerAtTile(IPlayer player, ITile tile) { }

    public void HandleDiceRoll(IDice dice1, IDice dice2)
    {
        // Logic for handling a dice roll, including checking for doubles and sending the player to jail if they roll three doubles in a row.
    }

    private bool CheckDoubleDice(IPlayer player)
    {
        if (player.DoubleRoll >= 3)
        {
            PlayerSentToJail?.Invoke(player, _playerPiece[player]);
            return true;
        }
        return false;
    }

    private bool CheckBankruptcy(IPlayer player)
    {
        if (player.IsBankrupt)
        {
            PlayerBankrupt?.Invoke(player);
            return true;
        }

        return false;
        // Logic for checking if the player is bankrupt and updating the player's bankruptcy status.
    }

    private bool CheckPlayerJailStatus(IPlayer player)
    {
        return true;
        // Logic for checking if the player is in jail and updating the player's jail status.
    }

    private bool IsOnOwnedProperty(ITile tile)
    {
        return true;
        // Logic for checking if the tile the player landed on is owned by another player and handling rent payment if necessary.
    }

    public void RemovePlayer(IPlayer player)
    {
        // Logic for removing a player from the game if they are bankrupt.
    }

    public void SubstractPlayerMoney(IPlayer player, IMoney money)
    {
        // Logic for subtracting money from a player's total and checking for bankruptcy.
    }

    public void TransferPlayerMoney(IPlayer from, IPlayer to, IMoney money)
    {
        // Logic for transferring money from one player to another, such as when paying rent or buying a property.
    }

    public void AddPlayerMoney(IPlayer player, IMoney money)
    {
        // Logic for adding money to a player's total, such as when passing Go or collecting rent.
    }

    public void GetPieceLocation(IPlayer player, IBoard board) { }

    public void GetCurrentPlayer(IPlayer player)
    {
        // Logic for determining the current player based on the turn order and updating the game state accordingly.
    }

    public void BuyProperty(IPlayer player, ITile tile)
    {
        // Logic for allowing a player to buy a property if they have enough money and the property is not already owned.
    }

    public void SellProperty(IPlayer player, ITile tile)
    {
        // Logic for allowing a player to sell a property they own back to the bank or to another player.
    }

    public bool isPropertyAvailable(ITile tile)
    {
        return true;
        // Logic for checking if a property is available for purchase, meaning it is not currently owned by another player.
    }

    public void ExecuteCard(ICard card, IPlayer player) { }

    public void ExecuteTile(ITile tile, IPlayer player)
    {
        // Logic for executing the action associated with the tile the player landed on, such as drawing a card, paying rent, or going to jail.
    }

    public bool CheckWinner()
    {
        return true;
        // Logic for checking if there is a winner, which occurs when all other players are bankrupt.
    }

    public void EndGame()
    {
        // Logic for ending the game and declaring the winner.
    }
}
