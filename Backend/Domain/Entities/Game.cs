namespace Backend.Domain.GameEntity;

using Backend.Domain.Enums;
using Backend.Domain.Interfaces;
using Backend.Helpers;
class Game
{
    private IBoard _board;
    private bool _gameEnded = false;
    private int _currentPlayerIndex = 0;
    private List<IPlayer> _players;

    private List<ICard> _cards;
    private List<IPiece> _pieces;

    private List<IMoney> _money;

    private Dictionary<IPlayer, IPiece> _playerPiece;
    private Dictionary<IPlayer, List<IMoney?>> _playerData;

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
        _playerData = new Dictionary<IPlayer, List<IMoney?>>();
    }

    public IBoard Board => _board;

    public List<IMoney> GetMoney()
    {
        return _money;
    }

    public IPiece GetPiece(IPlayer player)
    {
        if (player == null)
        {
            throw new Exception("Player cannot be null.");
        }

        if (_playerPiece.ContainsKey(player))
        {
            return _playerPiece[player];
        }
        else
        {
            throw new Exception("Player does not have a piece assigned.");
        }
    }

    public void Playturn()
    {
        while (!_players[_currentPlayerIndex].IsBankrupt)
        {
            _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;
        }

    }

    public void MovePiece(IPlayer player, int? step = null)
    {

        if (player == null)
        {
            throw new Exception("Player cannot be null.");
        }

        if (step != null)
        {
            var currentTile = _board.Tiles.First(t => t.Pieces.Contains(_playerPiece[player]));

            int currentIndex = Array.IndexOf(_board.Tiles, currentTile);
            int newIndex = currentIndex - (int)step;

            _board.Tiles[newIndex].Pieces.Add(_playerPiece[player]);
            currentTile.Pieces.Remove(_playerPiece[player]);
        }
        else
        {
            int move = HandleDiceRoll(new Dice(), new Dice());
            var currentTile = _board.Tiles.First(t => t.Pieces.Contains(_playerPiece[player]));

            int currentIndex = Array.IndexOf(_board.Tiles, currentTile);
            int newIndex = currentIndex + move;

            _board.Tiles[newIndex].Pieces.Add(_playerPiece[player]);
            currentTile.Pieces.Remove(_playerPiece[player]);
        }

    }

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

    public int HandleDiceRoll(IDice dice1, IDice dice2)
    {
        // Logic for handling a dice roll, including checking for doubles and sending the player to jail if they roll three doubles in a row.
        int roll = dice1.Roll() + dice2.Roll();
        return roll;

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
        if (player.IsInJail)
        {
            // Logic for handling a player who is in jail, such as allowing them to pay a fine or use a "Get Out of Jail Free" card to get out of jail.
            return true;
        }
        else
        {
            return false;
        }
        // Logic for checking if the player is in jail and updating the player's jail status.
    }

    private bool IsOnOwnedProperty(ITile tile)
    {
        if (tile.Owner != null)
        {
            // Logic for checking if the tile is owned by another player and handling rent payment if necessary.
            return true;
        }
        else
        {
            return false;
        }
        // Logic for checking if the tile the player landed on is owned by another player and handling rent payment if necessary.
    }

    public void RemovePlayer(IPlayer player)
    {
        _players.Remove(player);
        _playerPiece.Remove(player);
        _playerData.Remove(player);
        // Logic for removing a player from the game if they are bankrupt.
    }

    public void SubstractPlayerMoney(IPlayer player, IMoney money)
    {
        if (_playerData[player] != null)
        {
            _playerData[player].Remove(money);
        }
        // Logic for subtracting money from a player's total and checking for bankruptcy.
    }

    public void TransferPlayerMoney(IPlayer from, IPlayer to, IMoney money)
    {
        _playerData[from].Remove(money);
        _playerData[to].Add(money);
        // Logic for transferring money from one player to another, such as when paying rent or buying a property.
    }

    public void AddPlayerMoney(IPlayer player, IMoney money)
    {
        _playerData[player].Add(money);
        // Logic for adding money to a player's total, such as when passing Go or collecting rent.
    }

    public IPiece GetPieceLocation(IPlayer player)
    {
        var PlayerData = _board.Tiles.First(t => t.Pieces.Contains(_playerPiece[player]));
        return PlayerData.Pieces.First(p => p == _playerPiece[player]);
    }

    // public void GetCurrentPlayer(IPlayer player)
    // {
    //     // Logic for determining the current player based on the turn order and updating the game state accordingly.
    // }

    public void BuyProperty(IPlayer player)
    {


        // Logic for allowing a player to buy a property if they have enough money and the property is not already owned.
        var tile = _board.Tiles.First(t => t.Pieces == _playerPiece[player]);

        if (_playerData[player] == null && tile.Asset?.Price != null)
        {
            int moneyData = _playerData[player].Sum(m => MoneyConverter.ConvertTonInt(m));
            int propertyPrice = MoneyConverter.ConvertTonInt(tile.Asset?.Price);

            if (moneyData < propertyPrice)
            {
                throw new Exception("Not enough money to buy the property.");
            }

            moneyData -= propertyPrice;





        }
        else
        {
            throw new Exception("Player data or property price is null.");
        }


        //logic for handling the purchase of a property, including deducting the purchase price from the player's money and updating the property's ownership status.
        _playerData[player].Remove(tile.Asset?.Price);
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

    public void ExecuteCard(ICard card, IPlayer player)
    {
        if (card.Behaviour == CardBehaviour.AdvanceToGo)
        {
            var goTile = GetTileByType(TileType.StartTile);

        }

    }

    public void ExecuteTile(ITile tile, IPlayer player)
    {
        // Logic for executing the action associated with the tile the player landed on, such as drawing a card, paying rent, or going to jail.
    }

    public bool CheckWinner()
    {
        if (_players.Count == 1)
        {
            IsGameEnded?.Invoke(_players[0]);
            return true;
        }

        else
        {
            return false;
        }
        // Logic for checking if there is a winner, which occurs when all other players are bankrupt.
    }

    public void EndGame()
    {

        bool isGameEnded = CheckWinner();

        if (isGameEnded)
        {
            _gameEnded = true;
        }
        // Logic for ending the game and declaring the winner.
    }
}
