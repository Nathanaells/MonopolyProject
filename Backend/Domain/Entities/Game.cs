namespace Backend.Domain.Entities;

using Backend.Domain.DTOs;
using Backend.Domain.Enums;
using Backend.Domain.Interfaces;

public class Game
{
    private const int StartBonus = 200;
    private IBoard _board;
    private bool _gameEnded = false;
    private int _currentPlayerIndex = 0;
    private List<IPlayer> _players;

    private List<ICard> _cards;
    private List<IPiece> _pieces;
    private List<IMoney> _money;

    private Dictionary<IPlayer, IPiece> _playerPiece;
    private Dictionary<IPlayer, List<IMoney>> _playerData;
    private Dictionary<PieceType, IPlayer> _takenPieces = new();

    private readonly Random _random = new();

    public event Action<IPlayer, IPiece>? PlayerSentToJail;
    public event Action<IPlayer>? PlayerBankrupt;
    public event Action<IPlayer>? IsGameEnded;

    public IPlayer CurrentPlayer => _players[_currentPlayerIndex];
    public IReadOnlyList<IPlayer> Players => _players;
    public int CurrentPlayerIndex => _currentPlayerIndex;
    public List<ICard> Cards => _cards;
    public List<IPiece> Pieces => _pieces;
    public bool GameEnded => _gameEnded;
    public GamePhase Phase { get; private set; } = GamePhase.WaitingRoll;

    public Game(
        IBoard board,
        List<IPlayer> players,
        List<IPiece> pieces,
        List<ICard> cards,
        List<IMoney> money)
    {
        _board = board;
        _players = players;
        _money = money;
        _pieces = pieces;
        _cards = cards;

        _playerPiece = new Dictionary<IPlayer, IPiece>();
        _playerData = new Dictionary<IPlayer, List<IMoney>>();

        var startTile = GetTileByType(TileType.StartTile);
        for (int i = 0; i < _players.Count; i++)
        {
            var player = _players[i];
            var piece = _pieces[i % _pieces.Count];

            _playerPiece[player] = piece;
            _playerData[player] = new List<IMoney> { new Money(1500) };
            startTile.Pieces.Add(piece);
        }
    }

    public IBoard Board => _board;

    public List<IMoney> GetMoney() => _money;

    public bool IsPieceAvailable(PieceType pieceType)
    {
        return !_takenPieces.ContainsKey(pieceType);
    }

    public void AssignPieceToPlayer(IPlayer player, PieceType pieceType)
    {
        if (player == null) throw new Exception("Player cannot be null.");
        if (!_players.Contains(player)) throw new Exception("Player not found in this game.");

        if (_playerPiece.TryGetValue(player, out var currentPiece) && currentPiece.Type == pieceType)
        {
            return;
        }

        if (!IsPieceAvailable(pieceType))
            throw new Exception($"Piece {pieceType} sudah diambil oleh pemain lain.");

        var newPiece = _pieces.FirstOrDefault(p => p.Type == pieceType)
            ?? throw new Exception($"Piece {pieceType} tidak ditemukan.");

        if (_playerPiece.TryGetValue(player, out var oldPiece))
        {
            var currentTile = _board.Tiles.FirstOrDefault(t => t.Pieces.Contains(oldPiece));
            currentTile?.Pieces.Remove(oldPiece);

            var oldTakenEntry = _takenPieces.FirstOrDefault(kv => kv.Value.Equals(player));
            if (!oldTakenEntry.Equals(default(KeyValuePair<PieceType, IPlayer>)))
            {
                _takenPieces.Remove(oldTakenEntry.Key);
            }
        }

        _playerPiece[player] = newPiece;
        _takenPieces[newPiece.Type] = player;

        var startTile = GetTileByType(TileType.StartTile);
        if (!_board.Tiles.Any(t => t.Pieces.Contains(newPiece)))
            startTile.Pieces.Add(newPiece);
    }

    public IPiece GetPiece(IPlayer player)
    {
        if (player == null) throw new Exception("Player cannot be null.");
        if (_playerPiece.ContainsKey(player)) return _playerPiece[player];
        throw new Exception("Player does not have a piece assigned.");
    }

    public void NextPlayer()
    {
        if (_gameEnded) return;

        do
        {
            _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;
        } while (_players[_currentPlayerIndex].IsBankrupt && _players.Count(p => !p.IsBankrupt) > 1);
    }

    public RollTurnResult RollTurn()
    {
        if (Phase != GamePhase.WaitingRoll)
            throw new Exception("Not the right phase to roll the dice.");

        IPlayer player = CurrentPlayer;

        if (player.IsInJail)
        {
            var jailDice1 = new Dice();
            var jailDice2 = new Dice();
            int d1 = jailDice1.MaxRolled;
            int d2 = jailDice2.MaxRolled;

            if (d1 == d2)
            {
                ReleaseFromJail(player);
                MovePiece(player, d1 + d2);
                var landedTileJail = GetCurrentTile(player);

                HandleTileEffectsAfterMove(player, landedTileJail, out ICard? jailCard,
                    out bool requiresBuyJail);

                if (!requiresBuyJail)
                {
                    EndGame();
                    if (!GameEnded) NextPlayer();
                    Phase = GamePhase.WaitingRoll;
                }
                else
                {
                    Phase = GamePhase.WaitingBuyDecision;
                }

                return new RollTurnResult(
                    DiceTotal: d1 + d2,
                    Dice1: d1,
                    Dice2: d2,
                    LandedTileType: landedTileJail.Type.ToString(),
                    LandedProperty: landedTileJail.Asset != null ? landedTileJail : null,
                    LandedTile: landedTileJail,
                    RequiresBuyDecision: requiresBuyJail,
                    DrawnCard: jailCard,
                    JailRollResult: JailRollResult.Released
                );

            }
            else
            {
                player.JailTurnsRemaining--;

                if (player.JailTurnsRemaining <= 0)
                {
                    if (GetPlayerBalance(player) >= 50)
                        SubstractPlayerMoney(player, new Money(50));
                    ReleaseFromJail(player);
                }

                EndGame();
                if (!GameEnded) NextPlayer();
                Phase = GamePhase.WaitingRoll;

                return new RollTurnResult(
                    DiceTotal: d1 + d2,
                    Dice1: d1,
                    Dice2: d2,
                    LandedTileType: "None",
                    LandedProperty: null,
                    LandedTile: null,
                    RequiresBuyDecision: false,
                    DrawnCard: null,
                    JailRollResult: JailRollResult.StayedInJail
                );
            }
        }

        if (CheckBankruptcy(player))
        {
            RemovePlayer(player);
            if (!GameEnded) NextPlayer();
            return new RollTurnResult(
                DiceTotal: 0,
                Dice1: 0,
                Dice2: 0,
                LandedTileType: "None",
                LandedProperty: null,
                LandedTile: null,
                RequiresBuyDecision: false,
                DrawnCard: null,
                JailRollResult: JailRollResult.None
            );
        }

        var dice1 = new Dice();
        var dice2 = new Dice();
        int roll1 = dice1.MaxRolled;
        int roll2 = dice2.MaxRolled;
        int diceTotal = roll1 + roll2;

        if (roll1 == roll2) player.DoubleRoll++;
        if (player.DoubleRoll >= 3)
        {
            player.DoubleRoll = 0;
            SendPieceToJail(player);
            EndGame();
            if (!GameEnded) NextPlayer();
            Phase = GamePhase.WaitingRoll;
            return new RollTurnResult(
                DiceTotal: diceTotal,
                Dice1: roll1,
                Dice2: roll2,
                LandedTileType: "None",
                LandedProperty: null,
                LandedTile: null,
                RequiresBuyDecision: false,
                DrawnCard: null,
                JailRollResult: JailRollResult.None
            );
        }

        MovePiece(player, diceTotal);

        var landedTile = GetCurrentTile(player);

        HandleTileEffectsAfterMove(player, landedTile, out ICard? card, out bool requiresBuyDecision);

        if (requiresBuyDecision)
        {
            Phase = GamePhase.WaitingBuyDecision;
            return new RollTurnResult(
                DiceTotal: diceTotal,
                Dice1: roll1,
                Dice2: roll2,
                LandedTileType: landedTile.Type.ToString(),
                LandedProperty: landedTile.Asset != null ? landedTile : null,
                LandedTile: landedTile,
                RequiresBuyDecision: true,
                DrawnCard: null,
                JailRollResult: JailRollResult.None
            );
        }

        EndGame();
        if (!GameEnded) NextPlayer();
        Phase = GamePhase.WaitingRoll;

        return new RollTurnResult(
            DiceTotal: diceTotal,
            Dice1: roll1,
            Dice2: roll2,
            LandedTileType: landedTile.Type.ToString(),
            LandedProperty: landedTile.Asset != null ? landedTile : null,
            LandedTile: landedTile,
            RequiresBuyDecision: false,
            DrawnCard: card,
            JailRollResult: JailRollResult.None
        );
    }

    private void HandleTileEffectsAfterMove(
        IPlayer player,
        ITile tile,
        out ICard? drawnCard,
        out bool requiresBuyDecision)
    {
        drawnCard = null;
        requiresBuyDecision = false;

        if (isPropertyAvailable(tile))
        {
            int price = tile.Asset?.Price.Value ?? 0;
            if (GetPlayerBalance(player) >= price)
            {
                requiresBuyDecision = true;
                return;
            }
        }

        drawnCard = ExecuteTile(tile, player);
    }

    public void MovePiece(IPlayer player, int? step = null, int doubleRollCount = 0)
    {
        if (player == null) throw new Exception("Player cannot be null.");

        if (doubleRollCount >= 3)
        {
            SendPieceToJail(player);
            return;
        }

        int move = step ?? HandleDiceRoll();
        var currentTile = GetCurrentTile(player);
        int currentIndex = Array.IndexOf(_board.Tiles, currentTile);
        int count = _board.Tiles.Length;
        int newIndex = ((currentIndex + move) % count + count) % count;

        if (move > 0 && newIndex < currentIndex)
            AddPlayerMoney(player, new Money(StartBonus));

        MovePieceToIndex(player, newIndex);
    }

    private void MovePieceTo(IPlayer player, ITile targetTile)
    {
        var currentTile = GetCurrentTile(player);
        currentTile.Pieces.Remove(_playerPiece[player]);
        targetTile.Pieces.Add(_playerPiece[player]);
    }

    private void MovePieceToIndex(IPlayer player, int targetIndex)
    {
        MovePieceTo(player, _board.Tiles[targetIndex]);
    }

    private void MoveToNearestUtility(IPlayer player)
    {
        int currentIndex = Array.IndexOf(_board.Tiles, GetCurrentTile(player));
        int count = _board.Tiles.Length;
        for (int i = 1; i <= count; i++)
        {
            int index = (currentIndex + i) % count;
            if (_board.Tiles[index] is ITile tile && tile.Type == TileType.UtilityTile)
            {
                MovePieceTo(player, _board.Tiles[index]);
                break;
            }
        }
    }

    private void MoveToNearestRailroad(IPlayer player)
    {
        int currentIndex = Array.IndexOf(_board.Tiles, GetCurrentTile(player));
        int count = _board.Tiles.Length;
        for (int i = 1; i <= count; i++)
        {
            int index = (currentIndex + i) % count;
            if (_board.Tiles[index] is ITile tile && tile.Type == TileType.RailroadTile)
            {
                MovePieceTo(player, _board.Tiles[index]);
                break;
            }
        }
    }

    private ITile GetTileByType(TileType type) =>
        _board.Tiles.First(t => t.Type == type);

    public ITile GetCurrentTile(IPlayer player)
    {
        if (player == null) throw new Exception("Player cannot be null.");
        if (!_playerPiece.TryGetValue(player, out var piece))
            throw new Exception("Player does not have a piece assigned.");

        var tile = _board.Tiles.FirstOrDefault(t => t.Pieces.Contains(piece));
        if (tile != null) return tile;

        var startTile = GetTileByType(TileType.StartTile);
        if (!startTile.Pieces.Contains(piece))
            startTile.Pieces.Add(piece);

        return startTile;
    }

    private ITile GetTileByPropertyCity(PropertyCity city) =>
        _board.Tiles.First(t => t.Asset != null && t.Asset.City.PropertyCity == city);

    public ITile GetTileByCity(PropertyCity city) =>
        GetTileByPropertyCity(city);

    public void SendPieceToJail(IPlayer player)
    {
        var currentTile = GetCurrentTile(player);
        currentTile.Pieces.Remove(_playerPiece[player]);

        var jailTile = GetTileByType(TileType.JailTile);
        jailTile.Pieces.Add(_playerPiece[player]);
        player.IsInJail = true;
        player.JailTurnsRemaining = 3;

        PlayerSentToJail?.Invoke(player, _playerPiece[player]);
    }

    public void ReleaseFromJail(IPlayer player)
    {
        var jailTile = GetTileByType(TileType.JailTile);
        jailTile.Pieces.Remove(_playerPiece[player]);
        player.IsInJail = false;
        player.JailTurnsRemaining = 0;
    }

    public int HandleDiceRoll(IDice? die1 = null, IDice? die2 = null)
    {
        IDice dice1 = die1 ?? new Dice();
        IDice dice2 = die2 ?? new Dice();
        if (dice1.MaxRolled == dice2.MaxRolled) CurrentPlayer.DoubleRoll++;
        return dice1.MaxRolled + dice2.MaxRolled;
    }

    public bool CheckBankruptcy(IPlayer player) => player.IsBankrupt;

    public bool CheckPlayerJailStatus(IPlayer player)
    {
        if (player == null) throw new Exception("Player cannot be null.");
        return player.IsInJail;
    }

    private bool IsOnOwnedProperty(ITile tile) => tile.Owner != null;

    public void RemovePlayer(IPlayer player)
    {
        if (player == null) throw new Exception("Player cannot be null.");
        _players.Remove(player);
        _playerPiece.Remove(player);
        _playerData.Remove(player);
    }

    public void SubstractPlayerMoney(IPlayer player, IMoney money)
    {
        if (player == null) throw new Exception("Player cannot be null.");
        if (money == null) throw new Exception("Money cannot be null.");
        if (!_playerData.ContainsKey(player) || _playerData[player] == null)
            throw new Exception("Player data cannot be null.");

        int currentMoney = _playerData[player].Sum(m => m.Value);
        if (currentMoney < money.Value)
        {
            player.IsBankrupt = true;
            PlayerBankrupt?.Invoke(player);
            throw new Exception("Not enough money.");
        }
        _playerData[player].Add(new Money(-money.Value));
    }

    public void TransferPlayerMoney(IPlayer from, IPlayer to, IMoney money)
    {
        if (from == null || to == null) throw new Exception("Players cannot be null.");
        if (money == null) throw new Exception("Money cannot be null.");
        if (!_playerData.ContainsKey(from) || !_playerData.ContainsKey(to))
            throw new Exception("Player data cannot be null.");

        int fromMoney = _playerData[from].Sum(m => m.Value);
        if (fromMoney < money.Value) throw new Exception("Not enough money to transfer.");

        _playerData[from].Add(new Money(-money.Value));
        _playerData[to].Add(new Money(money.Value));
    }

    public void AddPlayerMoney(IPlayer player, IMoney money)
    {
        if (player == null) throw new Exception("Player cannot be null.");
        if (money == null) throw new Exception("Money cannot be null.");
        if (!_playerData.ContainsKey(player) || _playerData[player] == null)
            throw new Exception("Player data cannot be null.");

        _playerData[player].Add(new Money(money.Value));
    }

    public int GetPlayerBalance(IPlayer player)
    {
        if (!_playerData.ContainsKey(player)) throw new Exception("Player data cannot be null.");
        return _playerData[player].Sum(m => m.Value);
    }

    public IPiece GetPieceLocation(IPlayer player)
    {
        var playerData = GetCurrentTile(player);
        return playerData.Pieces.First(p => p == _playerPiece[player]);
    }

    public void BuyProperty(IPlayer player)
    {
        var tile = GetCurrentTile(player);
        if (_playerData.ContainsKey(player) && _playerData[player] != null && tile.Asset?.Price != null)
        {
            int moneyData = _playerData[player].Sum(m => m.Value);
            int propertyPrice = tile.Asset.Price.Value;
            if (moneyData < propertyPrice) throw new Exception("Not enough money to buy the property.");

            _playerData[player].Add(new Money(-propertyPrice));
            tile.Owner = player;
        }
        else
        {
            throw new Exception("Player data or property price is null.");
        }
    }

    public bool AttemptBuyCurrentProperty(IPlayer player, bool wantsToBuy)
    {
        if (!wantsToBuy) return false;
        var tile = GetCurrentTile(player);
        if (!isPropertyAvailable(tile)) return false;
        int price = tile.Asset!.Price.Value;
        if (GetPlayerBalance(player) < price) return false;
        SubstractPlayerMoney(player, new Money(price));
        tile.Owner = player;
        return true;
    }

    public void HandleBuyDecision(bool wantsToBuy)
    {
        if (Phase != GamePhase.WaitingBuyDecision)
            throw new Exception("Not the right phase to handle buy decision.");

        AttemptBuyCurrentProperty(CurrentPlayer, wantsToBuy);

        EndGame();
        if (!GameEnded) NextPlayer();
        Phase = GamePhase.WaitingRoll;
    }

    public void BuyBuilding(IPlayer player, PropertyCity city, bool buildHotel)
    {
        var tile = GetTileByCity(city);

        if (tile.Asset == null)
            throw new Exception("This tile has no asset.");

        if (tile.Owner == null || !tile.Owner.Equals(player))
            throw new Exception("You do not own this property.");

        var color = tile.Asset.Color;
        if (color == null)
            throw new Exception("This property cannot have buildings (no color).");

        var sameColorTiles = _board.Tiles
            .Where(t => t.Asset?.Color == color)
            .ToList();

        bool hasMonopoly = sameColorTiles.All(t => t.Owner != null && t.Owner.Equals(player));
        if (!hasMonopoly)
            throw new Exception("Kamu harus memiliki semua properti warna yang sama terlebih dahulu.");

        int housePrice = GetHousePrice(tile.Asset);
        int currentHouses = tile.House ?? 0;
        bool hasHotel = tile.HasHotel ?? false;

        if (hasHotel)
            throw new Exception("Tile sudah memiliki hotel.");

        if (buildHotel)
        {
            if (currentHouses < 3)
                throw new Exception("Butuh minimal 3 rumah sebelum bisa bangun hotel.");

            int hotelPrice = housePrice * 5;
            SubstractPlayerMoney(player, new Money(hotelPrice));
            tile.House = 0;
            tile.HasHotel = true;
        }
        else
        {
            if (currentHouses >= 3)
                throw new Exception("Sudah 3 rumah. Bangun hotel sekarang.");

            SubstractPlayerMoney(player, new Money(housePrice));
            tile.House = currentHouses + 1;
        }
    }

    public int SellAllAssetsToBank(IPlayer player)
    {
        var properties = GetPlayerProperties(player).ToList();
        int totalIncome = 0;

        foreach (var tile in properties)
        {
            if (tile.Asset == null) continue;

            int income = SellPropertyToBank(
                player,
                tile.Asset.City.PropertyCity,
                includeBuildings: true);

            totalIncome += income;
        }

        return totalIncome;
    }

    public void SellProperty(IPlayer owner, List<ITile> properties)
    {
        if (owner == null) throw new Exception("Player cannot be null.");
        if (!_playerData.ContainsKey(owner)) throw new Exception("Player data not found.");

        var playerProperties = GetPlayerProperties(owner);
        int sellPrice = 0;

        foreach (var property in properties)
        {
            if (!playerProperties.Contains(property))
                throw new Exception("Property does not belong to player.");
            if (property.Asset == null) throw new Exception("Invalid property.");
            if ((property.House ?? 0) > 0 || property.HasHotel == true)
                throw new Exception("Sell buildings first.");

            sellPrice += property.Asset.Price.Value / 2;
            property.Owner = null;
        }

        AddPlayerMoney(owner, new Money(sellPrice));
    }

    private int GetHousePrice(IAsset asset)
    {
        return asset.Color switch
        {
            Color.Brown => 50,
            Color.LightBlue => 50,
            Color.Pink => 100,
            Color.Orange => 100,
            Color.Red => 150,
            Color.Yellow => 150,
            Color.Green => 200,
            Color.DarkBlue => 200,
            _ => 100,
        };
    }

    public int SellBuildingsToBank(IPlayer owner, PropertyCity city, int housesToSell, bool sellHotel)
    {
        var tile = GetTileByCity(city);
        if (tile.Owner == null || !tile.Owner.Equals(owner))
            throw new Exception("Property does not belong to player.");
        if (tile.Asset == null) throw new Exception("Tile does not have sellable asset.");

        int houseCount = tile.House ?? 0;
        bool hasHotel = tile.HasHotel ?? false;
        if (!hasHotel && houseCount == 0) return 0;

        int soldValue = 0;
        int houseSellPrice = GetHousePrice(tile.Asset) / 2;

        if (sellHotel && hasHotel)
        {
            tile.HasHotel = false;
            soldValue += houseSellPrice * 5;
        }

        if (housesToSell > houseCount) throw new Exception("Not enough houses to sell.");
        if (housesToSell > 0)
        {
            tile.House = houseCount - housesToSell;
            soldValue += houseSellPrice * housesToSell;
        }

        if (soldValue > 0) AddPlayerMoney(owner, new Money(soldValue));
        return soldValue;
    }

    public int SellPropertyToBank(IPlayer owner, PropertyCity city, bool includeBuildings = true)
    {
        var tile = GetTileByCity(city);
        if (tile.Owner == null || !tile.Owner.Equals(owner))
            throw new Exception("Property does not belong to player.");
        if (tile.Asset == null) throw new Exception("Invalid property.");

        int totalIncome = 0;
        int houses = tile.House ?? 0;
        bool hasHotel = tile.HasHotel ?? false;

        if ((houses > 0 || hasHotel) && !includeBuildings)
            throw new Exception("Property has buildings. Sell buildings first.");

        if (includeBuildings)
        {
            if (hasHotel) totalIncome += SellBuildingsToBank(owner, city, 0, true);
            houses = tile.House ?? 0;
            if (houses > 0) totalIncome += SellBuildingsToBank(owner, city, houses, false);
        }

        int propertySellValue = tile.Asset.Price.Value / 2;
        tile.Owner = null;
        tile.House = 0;
        tile.HasHotel = false;
        AddPlayerMoney(owner, new Money(propertySellValue));
        totalIncome += propertySellValue;
        return totalIncome;
    }

    public bool isPropertyAvailable(ITile tile)
    {
        if (tile == null) throw new Exception("Tile cannot be null.");
        return tile.Asset != null && tile.Owner == null;
    }

    public bool IsPropertyAvailableForCurrentPlayer() =>
        isPropertyAvailable(GetCurrentTile(CurrentPlayer));

    public ICard DrawCard(TileType drawType)
    {
        IEnumerable<ICard> candidateCards = drawType switch
        {
            TileType.DrawChance => _cards.Where(c => c is ChanceCard),
            TileType.DrawCommunity => _cards.Where(c => c is CommunityCard),
            _ => throw new Exception("Tile type is not a card tile."),
        };

        var shuffled = candidateCards.OrderBy(_ => _random.Next()).ToList();
        if (!shuffled.Any()) throw new Exception("No cards available.");
        return shuffled.First();
    }

    public void ExecuteCard(ICard card, IPlayer player)
    {
        if (card == null || player == null) throw new Exception("Card or player cannot be null.");

        if (card is CommunityCard) ExecuteCommunityCard(card, player);
        else if (card is ChanceCard) ExecuteChanceCard(card, player);
    }

    private int HandleStreetRepairs(IPlayer player)
    {
        int houseCost = 40;
        int hotelCost = 115;
        int totalCost = 0;
        foreach (var tile in _board.Tiles)
        {
            if (tile.Owner != null && tile.Owner.Equals(player) && tile.Asset != null)
            {
                totalCost += (tile.House ?? 0) * houseCost;
                totalCost = tile.HasHotel == true ? totalCost + hotelCost : totalCost;
            }
        }
        return totalCost;
    }

    private void ExecuteCommunityCard(ICard card, IPlayer player)
    {
        switch (card.Behaviour)
        {
            case CardBehaviour.AdvanceToGo:
                MovePieceTo(player, GetTileByType(TileType.StartTile));
                break;
            case CardBehaviour.BankError:
                AddPlayerMoney(player, new Money(MoneyValue.fifty));
                break;
            case CardBehaviour.DoctorFees:
                SubstractPlayerMoney(player, new Money(MoneyValue.fifty));
                break;
            case CardBehaviour.FromSaleOfStock:
                AddPlayerMoney(player, new Money(MoneyValue.fifty));
                break;
            case CardBehaviour.GetOutOfJailFree:
                if (CheckPlayerJailStatus(player)) ReleaseFromJail(player);
                else player.JailFreeCardCount++;
                break;
            case CardBehaviour.GoToJail:
                SendPieceToJail(player);
                break;
            case CardBehaviour.HolidayFundMatures:
                AddPlayerMoney(player, new Money(MoneyValue.hundred));
                break;
            case CardBehaviour.IncomeTaxRefund:
                AddPlayerMoney(player, new Money(MoneyValue.hundred));
                break;
            case CardBehaviour.Birthday:
                foreach (var p in _players)
                    if (!p.Equals(player)) TransferPlayerMoney(p, player, new Money(MoneyValue.ten));
                break;
            case CardBehaviour.LifeInsuranceMatures:
                AddPlayerMoney(player, new Money(MoneyValue.hundred));
                break;
            case CardBehaviour.PayHospitalFees:
                SubstractPlayerMoney(player, new Money(MoneyValue.hundred));
                break;
            case CardBehaviour.PaySchoolFees:
                SubstractPlayerMoney(player, new Money(MoneyValue.fifty));
                break;
            case CardBehaviour.ConsultancyFee:
                AddPlayerMoney(player, new Money(MoneyValue.twenty + MoneyValue.five));
                break;
            case CardBehaviour.StreetRepairs:
                int totalCost = HandleStreetRepairs(player);
                SubstractPlayerMoney(player, new Money(totalCost));
                break;
            case CardBehaviour.BeautyContestPrize:
                AddPlayerMoney(player, new Money(MoneyValue.hundred));
                break;
            case CardBehaviour.InheritMoney:
                AddPlayerMoney(player, new Money(MoneyValue.hundred));
                break;
            default:
                throw new Exception("Card behaviour not implemented.");
        }
    }

    private void ExecuteChanceCard(ICard card, IPlayer player)
    {
        switch (card.Behaviour)
        {
            case CardBehaviour.AdvanceToGo:
                MovePieceTo(player, GetTileByType(TileType.StartTile));
                AddPlayerMoney(player, new Money(StartBonus));
                break;
            case CardBehaviour.AdvanceToIllinois:
                MovePieceTo(player, GetTileByPropertyCity(PropertyCity.IllinoisAvenue));
                break;
            case CardBehaviour.AdvanceToStCharles:
                MovePieceTo(player, GetTileByPropertyCity(PropertyCity.StCharlesPlace));
                break;
            case CardBehaviour.AdvanceNearestUtility:
                MoveToNearestUtility(player);
                break;
            case CardBehaviour.AdvanceNearestRailroad:
                MoveToNearestRailroad(player);
                break;
            case CardBehaviour.AdvanceNearestRailroadPayDouble:
                MoveToNearestRailroad(player);
                ExecuteTile(GetCurrentTile(player), player, true);
                break;
            case CardBehaviour.BankPaysDividend:
                AddPlayerMoney(player, new Money(MoneyValue.fifty));
                break;
            case CardBehaviour.GetOutOfJailFree:
                if (CheckPlayerJailStatus(player)) ReleaseFromJail(player);
                else player.JailFreeCardCount++;
                break;
            case CardBehaviour.GoBackThreeSpaces:
                MovePiece(player, -3);
                break;
            case CardBehaviour.GoToJail:
                SendPieceToJail(player);
                break;
            case CardBehaviour.MakeGeneralRepairs:
                int totalCost = HandleStreetRepairs(player);
                SubstractPlayerMoney(player, new Money(totalCost));
                break;
            case CardBehaviour.PayPoorTax:
                SubstractPlayerMoney(player, new Money(MoneyValue.ten + MoneyValue.five));
                break;
            case CardBehaviour.TakeTripToReadingRailroad:
                MovePieceTo(player, GetTileByPropertyCity(PropertyCity.ReadingRailroad));
                break;
            case CardBehaviour.AdvanceToBoardwalk:
                MovePieceTo(player, GetTileByPropertyCity(PropertyCity.Boardwalk));
                break;
            case CardBehaviour.ChairmanOfTheBoard:
                foreach (var p in _players)
                    if (!p.Equals(player)) TransferPlayerMoney(player, p, new Money(MoneyValue.fifty));
                break;
            case CardBehaviour.YourBuildingLoanMatures:
                AddPlayerMoney(player, new Money(MoneyValue.hundred + MoneyValue.fifty));
                break;
            default:
                throw new Exception("Card behaviour not implemented.");
        }
    }

    public ICard? ExecuteTile(ITile tile, IPlayer player, bool doubleRent = false)
    {
        if (tile == null || player == null) throw new Exception("Tile or player cannot be null.");

        switch (tile.Type)
        {
            case TileType.RentTile:
            case TileType.UtilityTile:
            case TileType.RailroadTile:
                if (IsOnOwnedProperty(tile))
                {
                    if (!tile.Owner!.Equals(player))
                    {
                        int rent = Math.Max(10, tile.Asset!.Price.Value / 10);
                        if (doubleRent) rent *= 2;
                        SubstractPlayerMoney(player, new Money(rent));
                        if (!player.IsBankrupt) AddPlayerMoney(tile.Owner, new Money(rent));
                    }
                }
                return null;
            case TileType.TaxTile:
            case TileType.PayTaxTile:
                SubstractPlayerMoney(player, new Money(MoneyValue.hundred));
                return null;
            case TileType.GoToJailTile:
                SendPieceToJail(player);
                return null;
            case TileType.DrawChance:
            case TileType.DrawCommunity:
                var card = DrawCard(tile.Type);
                ExecuteCard(card, player);
                return card;
            default:
                return null;
        }
    }

    public List<ITile> GetPlayerProperties(IPlayer player) =>
        _board.Tiles.Where(t => t.Owner != null && t.Owner.Equals(player)).ToList();

    public IPlayer? GetWinnerOrNull()
    {
        var activePlayers = _players.Where(p => !p.IsBankrupt).ToList();
        return activePlayers.Count == 1 ? activePlayers[0] : null;
    }

    public IPlayer? FindPlayerByName(string playerName) =>
        _players.FirstOrDefault(p => p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));

    public bool IsPropertyOwnedBy(ITile tile, IPlayer player) =>
        tile.Owner != null && tile.Owner.Equals(player);

    public bool TryExecuteLandingForCurrentPlayer(out ICard? drawnCard)
    {
        drawnCard = null;
        var player = CurrentPlayer;
        var tile = GetCurrentTile(player);

        if (tile.Type == TileType.DrawChance || tile.Type == TileType.DrawCommunity)
        {
            drawnCard = DrawCard(tile.Type);
            ExecuteCard(drawnCard, player);
            NextPlayer();
            return true;
        }

        ExecuteTile(tile, player);
        return false;
    }

    public bool CheckWinner()
    {
        var winner = GetWinnerOrNull();
        if (winner != null)
        {
            IsGameEnded?.Invoke(winner);
            return true;
        }
        return false;
    }

    public bool EndGame()
    {
        bool isGameEnded = CheckWinner();
        if (isGameEnded) _gameEnded = true;
        return isGameEnded;
    }
}