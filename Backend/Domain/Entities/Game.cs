namespace Backend.Domain.Entities;

using Backend.Domain.Enums;
using Backend.Domain.Interfaces;
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

    public event Action<IPlayer, IPiece>? PlayerSentToJail;

    public event Action<IPlayer>? PlayerBankrupt;
    public event Action<IPlayer>? IsGameEnded;

    public IPlayer CurrentPlayer => _players[_currentPlayerIndex];
    public List<ICard> Cards => _cards;
    public List<IPiece> Pieces => _pieces;
    public bool GameEnded => _gameEnded;

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

    }

    private bool CheckPlayerJailStatus(IPlayer player)
    {

        if (player == null)
        {
            throw new Exception("Player cannot be null.");
        }


        if (player.IsInJail)
        {

            return true;
        }
        else
        {
            return false;
        }

    }

    private bool IsOnOwnedProperty(ITile tile)
    {
        if (tile.Owner != null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void RemovePlayer(IPlayer player)
    {
        if (player == null)
        {
            throw new Exception("Player cannot be null.");
        }

        _players.Remove(player);
        _playerPiece.Remove(player);
        _playerData.Remove(player);
    }

    public void SubstractPlayerMoney(IPlayer player, IMoney money)
    {
        if (player == null)
        {
            throw new Exception("Player cannot be null.");
        }

        if (money == null)
        {
            throw new Exception("Money cannot be null.");
        }

        if (!_playerData.ContainsKey(player) || _playerData[player] == null)
        {
            throw new Exception("Player data cannot be null.");
        }

        int currentMoney = _playerData[player].Where(m => m != null).Sum(m => m!.Value);
        if (currentMoney < money.Value)
        {
            throw new Exception("Not enough money.");
        }

        _playerData[player].Add(new Money(-money.Value));
    }

    public void TransferPlayerMoney(IPlayer from, IPlayer to, IMoney money)
    {
        if (from == null || to == null)
        {
            throw new Exception("Players cannot be null.");
        }

        if (money == null)
        {
            throw new Exception("Money cannot be null.");
        }

        if (!_playerData.ContainsKey(from) || !_playerData.ContainsKey(to) || _playerData[from] == null || _playerData[to] == null)
        {
            throw new Exception("Player data cannot be null.");
        }

        int fromMoney = _playerData[from].Where(m => m != null).Sum(m => m!.Value);
        if (fromMoney < money.Value)
        {
            throw new Exception("Not enough money to transfer.");
        }

        _playerData[from].Add(new Money(-money.Value));
        _playerData[to].Add(new Money(money.Value));
    }

    public void AddPlayerMoney(IPlayer player, IMoney money)
    {
        if (player == null)
        {
            throw new Exception("Player cannot be null.");
        }

        if (money == null)
        {
            throw new Exception("Money cannot be null.");
        }

        if (!_playerData.ContainsKey(player) || _playerData[player] == null)
        {
            throw new Exception("Player data cannot be null.");
        }

        _playerData[player].Add(new Money(money.Value));
    }

    public IPiece GetPieceLocation(IPlayer player)
    {
        var PlayerData = _board.Tiles.First(t => t.Pieces.Contains(_playerPiece[player]));
        return PlayerData.Pieces.First(p => p == _playerPiece[player]);
    }

    public void BuyProperty(IPlayer player)
    {
        var tile = _board.Tiles.First(t => t.Pieces == _playerPiece[player]);

        if (_playerData.ContainsKey(player) && _playerData[player] != null && tile.Asset?.Price != null)
        {
            int moneyData = _playerData[player].Where(m => m != null).Sum(m => m!.Value);
            int propertyPrice = tile.Asset.Price.Value;

            if (moneyData < propertyPrice)
            {
                throw new Exception("Not enough money to buy the property.");
            }

            _playerData[player].Add(new Money(-propertyPrice));
            tile.Owner = player;
        }
        else
        {
            throw new Exception("Player data or property price is null.");
        }
    }

    public void SellProperty(IPlayer owner)
    {
        if (owner == null)
        {
            throw new Exception("Player cannot be null.");
        }


        _board.Tiles.Where(t => t.Owner == owner).ToList().ForEach(t =>
        {
            if (t.Asset?.Price != null)
            {
                int salePrice = t.Asset.Price.Value / 2;
                _playerData[owner].Add(new Money(salePrice));
                t.Owner = null;
            }
        });
    }

    public bool isPropertyAvailable(ITile tile)
    {
        if (tile == null)
        {
            throw new Exception("Tile cannot be null.");
        }

        if (tile.Asset != null && tile.Owner == null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void ExecuteCard(ICard card, IPlayer player)
    {

        if (card == null)
        {
            throw new Exception("Card cannot be null.");
        }

        if (player == null)
        {
            throw new Exception("Player cannot be null.");
        }


        if (card is CommunityCard)
        {
            switch (card.Behaviour)
            {
                case CardBehaviour.BankError:
                    _playerData[player].Add(new Money(MoneyValue.fifty));
                    break;
                case CardBehaviour.DoctorFees:
                    _playerData[player].Add(new Money(-MoneyValue.fifty));
                    break;
                case CardBehaviour.FromSaleOfStock:
                    _playerData[player].Add(new Money(MoneyValue.fifty));
                    break;
                case CardBehaviour.HolidayFundMatures:
                    _playerData[player].Add(new Money(MoneyValue.hundred));
                    break;
                case CardBehaviour.IncomeTaxRefund:
                    _playerData[player].Add(new Money(MoneyValue.twenty));
                    break;
                case CardBehaviour.Birthday:
                    _playerData[player].Add(new Money(MoneyValue.ten));
                    break;
                case CardBehaviour.LifeInsuranceMatures:
                    _playerData[player].Add(new Money(MoneyValue.hundred));
                    break;
                case CardBehaviour.PayHospitalFees:
                    _playerData[player].Add(new Money(-MoneyValue.hundred));
                    break;
                case CardBehaviour.PaySchoolFees:
                    _playerData[player].Add(new Money(-MoneyValue.fifty));
                    break;
                case CardBehaviour.ConsultancyFee:
                    _playerData[player].Add(new Money(MoneyValue.twenty + MoneyValue.five));
                    break;
                case CardBehaviour.StreetRepairs:
                    _playerData[player].Add(new Money(-MoneyValue.twenty + MoneyValue.twenty));
                    break;
                case CardBehaviour.BeautyContestPrize:
                    _playerData[player].Add(new Money(MoneyValue.ten));
                    break;
                case CardBehaviour.InheritMoney:
                    _playerData[player].Add(new Money(MoneyValue.twoHundred));
                    break;
                case CardBehaviour.GetOutOfJailFree:
                    player.JailFreeCardCount += 1;
                    break;
            }
        }

        if (card is ChanceCard)
        {
            switch (card.Behaviour)
            {
                case CardBehaviour.AdvanceToIllinois:
                    var currentTile = _board.Tiles.First(t => t.Pieces.Contains(_playerPiece[player]));
                    currentTile.Pieces.Remove(_playerPiece[player]);

                    var illinoisTile = GetTileByType(TileType.RentTile);
                    illinoisTile.Pieces.Add(_playerPiece[player]);



                    break;
            }
        }

    }

    public void ExecuteTile(ITile tile, IPlayer player)
    {

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
