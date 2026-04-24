namespace Backend.Domain.Entities;

using Backend.Domain.DTOs;
using Backend.Domain.Enums;
using Backend.Domain.Interfaces;

public class Game
{
    private IBoard _board;
    private bool _gameEnded = false;
    private int _currentPlayerIndex = 0;
    private List<IPlayer> _players;

    private List<ICard> _cards;
    private List<IPiece> _pieces;
    private List<IMoney> _money;

    private Dictionary<IPlayer, IPiece> _playerPiece;
    private Dictionary<IPlayer, List<IMoney>> _playerData;
    private Dictionary<PieceType, IPlayer> _takenPieces;

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
        List<IMoney> money
    )
    {
        _board = board;
        _players = players;
        _money = money;
        _pieces = pieces;
        _cards = cards;

        _takenPieces = new Dictionary<PieceType, IPlayer>();
        _playerPiece = new Dictionary<IPlayer, IPiece>();
        _playerData = new Dictionary<IPlayer, List<IMoney>>();

        ITile startTile = GetTileByType(TileType.StartTile);
        for (int i = 0; i < _players.Count; i++)
        {
            IPlayer player = _players[i];
            IPiece piece = _pieces[i % _pieces.Count];

            _playerPiece[player] = piece;
            _playerData[player] = new List<IMoney>
            {
                new Money(MoneyValue.fiveHundred + MoneyValue.fiveHundred + MoneyValue.fiveHundred),
            };
            startTile.Pieces.Add(piece);
        }
    }

    public IBoard Board => _board;

    public bool IsPieceAvailable(PieceType pieceType)
    {
        return !_takenPieces.ContainsKey(pieceType);
    }

    public void AssignPieceToPlayer(IPlayer player, PieceType pieceType) // bool
    {
        if (player == null)
            throw new Exception("Player cannot be null.");
        if (!_players.Contains(player))
            throw new Exception("Player not found in this game.");

        if (
            _playerPiece.TryGetValue(player, out IPiece? currentPiece)
            && currentPiece.Type == pieceType
        )
        {
            return;
        }

        if (!IsPieceAvailable(pieceType))
            throw new Exception($"Piece {pieceType} sudah diambil oleh pemain lain.");

        IPiece newPiece =
            _pieces.FirstOrDefault(p => p.Type == pieceType)
            ?? throw new Exception($"Piece {pieceType} tidak ditemukan.");

        if (_playerPiece.TryGetValue(player, out IPiece? oldPiece))
        {
            ITile? currentTile = _board.Tiles.FirstOrDefault(t => t.Pieces.Contains(oldPiece));
            currentTile?.Pieces.Remove(oldPiece);

            var oldTakenEntry = _takenPieces.FirstOrDefault(kv => kv.Value.Equals(player));
            if (!oldTakenEntry.Equals(default(KeyValuePair<PieceType, IPlayer>)))
            {
                _takenPieces.Remove(oldTakenEntry.Key);
            }
        }

        _playerPiece[player] = newPiece;
        _takenPieces[newPiece.Type] = player;

        ITile startTile = GetTileByType(TileType.StartTile);
        if (!_board.Tiles.Any(t => t.Pieces.Contains(newPiece)))
            startTile.Pieces.Add(newPiece);
    }

    public IPiece GetPiece(IPlayer player)
    {
        if (player == null)
            throw new Exception("Player cannot be null.");
        if (_playerPiece.ContainsKey(player))
            return _playerPiece[player];
        throw new Exception("Player does not have a piece assigned.");
    }

    public void NextPlayer() // IPlayer
    {
        if (_gameEnded)
            return;

        do
        {
            _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;
        } while (
            _players[_currentPlayerIndex].IsBankrupt && _players.Count(p => !p.IsBankrupt) > 1
        );
    }

    public RollTurnResult RollTurn()
    {
        if (Phase != GamePhase.WaitingRoll)
            throw new Exception("Not the right phase to roll the dice.");

        IPlayer player = CurrentPlayer;

        if (player.IsInJail)
        {
            IDice jailDice1 = new Dice();
            IDice jailDice2 = new Dice();
            int firstDice = jailDice1.MaxRolled;
            int secondDice = jailDice2.MaxRolled;

            if (firstDice == secondDice)
            { //Namiong Convention
                ReleaseFromJail(player);
                MovePiece(player, firstDice + secondDice);
                ITile landedTileJail = GetCurrentTile(player);

                HandleTileEffectsAfterMove(
                    player,
                    landedTileJail,
                    out ICard? jailCard,
                    out bool requiresBuyJail
                );

                if (!requiresBuyJail)
                {
                    EndGame();
                    if (!GameEnded)
                        NextPlayer();
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
                        SubstractPlayerMoney(
                            player,
                            _money.First(m => m.Value == MoneyValue.fifty)
                        );
                    ReleaseFromJail(player);
                }

                EndGame();
                if (!GameEnded)
                    NextPlayer();
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
            if (!GameEnded)
                NextPlayer();
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

        IDice dice1 = new Dice();
        IDice dice2 = new Dice();
        int roll1 = dice1.MaxRolled;
        int roll2 = dice2.MaxRolled;
        int diceTotal = roll1 + roll2;

        if (roll1 == roll2)
            player.DoubleRoll++;
        if (player.DoubleRoll >= 3)
        {
            player.DoubleRoll = 0;
            SendPieceToJail(player);
            EndGame();
            if (!GameEnded)
                NextPlayer();
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

        ITile landedTile = GetCurrentTile(player);

        HandleTileEffectsAfterMove(
            player,
            landedTile,
            out ICard? card,
            out bool requiresBuyDecision
        );

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
        if (!GameEnded)
            NextPlayer();
        Phase = GamePhase.WaitingRoll;

        return new RollTurnResult(
            DiceTotal: diceTotal,
            Dice1: roll1,
            Dice2: roll2,
            LandedTileType: landedTile.Type.ToString(),
            LandedProperty: landedTile.Asset != null ? landedTile : null, // no logic
            LandedTile: landedTile,
            RequiresBuyDecision: false,
            DrawnCard: card,
            JailRollResult: JailRollResult.None
        );
    }

    public record HandleTileEffectsResult(
        IPlayer player,
        ICard DrawnCard,
        ITile Tile,
        bool RequiresBuyDecision
    );

    private void HandleTileEffectsAfterMove( //Record HandleTileEffectsResult
        IPlayer player,
        ITile tile,
        out ICard? drawnCard,
        out bool requiresBuyDecision
    )
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

    public void MovePiece(IPlayer player, int? step = null) //bool
    {
        if (player == null)
            throw new Exception("Player cannot be null.");

        if (player.DoubleRoll >= 3)
        {
            SendPieceToJail(player);
            return;
        }

        int move = step ?? HandleDiceRoll();
        ITile currentTile = GetCurrentTile(player);
        int currentIndex = Array.IndexOf(_board.Tiles, currentTile);
        int count = _board.Tiles.Length;
        int newIndex = ((currentIndex + move) % count + count) % count;

        //Money
        int money = _money.First(m => m.Value == MoneyValue.twoHundred).Value;

        if (move > 0 && newIndex < currentIndex)
            AddPlayerMoney(player, new Money(money));

        MovePieceToIndex(player, newIndex);
    }

    private void MovePieceTo(IPlayer player, ITile targetTile)
    {
        ITile currentTile = GetCurrentTile(player);
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

    private ITile GetTileByType(TileType type) => _board.Tiles.First(t => t.Type == type);

    public ITile GetCurrentTile(IPlayer player) // di controller di handle
    {
        if (player == null) // pakai curly braces
            return null;

        if (!_playerPiece.TryGetValue(player, out IPiece? piece))
            throw new Exception("Player does not have a piece assigned.");

        ITile? tile = _board.Tiles.FirstOrDefault(t => t.Pieces.Contains(piece));

        if (tile != null)
            return tile;

        ITile startTile = GetTileByType(TileType.StartTile);
        if (!startTile.Pieces.Contains(piece))
            startTile.Pieces.Add(piece);

        return startTile;
    }

    private ITile GetTileByPropertyCity(PropertyCity city) =>
        _board.Tiles.First(t => t.Asset != null && t.Asset.City.PropertyCity == city);

    public ITile GetTileByCity(PropertyCity city) => GetTileByPropertyCity(city);

    public void SendPieceToJail(IPlayer player) // bool
    {
        ITile currentTile = GetCurrentTile(player);
        currentTile.Pieces.Remove(_playerPiece[player]);

        ITile jailTile = GetTileByType(TileType.JailTile);
        jailTile.Pieces.Add(_playerPiece[player]);
        player.IsInJail = true;
        player.JailTurnsRemaining = 3;

        PlayerSentToJail?.Invoke(player, _playerPiece[player]);
    }

    public void ReleaseFromJail(IPlayer player) // bool
    {
        ITile jailTile = GetTileByType(TileType.JailTile);
        jailTile.Pieces.Remove(_playerPiece[player]);
        player.IsInJail = false;
        player.JailTurnsRemaining = 0;
    }

    public int HandleDiceRoll(IDice? die1 = null, IDice? die2 = null)
    { // first dice, second dice
        IDice dice1 = die1 ?? new Dice();
        IDice dice2 = die2 ?? new Dice();
        if (dice1.MaxRolled == dice2.MaxRolled)
            CurrentPlayer.DoubleRoll++;
        return dice1.MaxRolled + dice2.MaxRolled;
    }

    public bool CheckBankruptcy(IPlayer player) => player.IsBankrupt;

    public bool CheckPlayerJailStatus(IPlayer player)
    {
        if (player == null)
            throw new Exception("Player cannot be null.");
        return player.IsInJail;
    }

    private bool IsOnOwnedProperty(ITile tile) => tile.Owner != null;

    public void RemovePlayer(IPlayer player) //bool
    {
        if (player == null)
            throw new Exception("Player cannot be null.");
        _players.Remove(player);
        _playerPiece.Remove(player);
        _playerData.Remove(player);
    }

    public void SubstractPlayerMoney(IPlayer player, IMoney money) //money?
    {
        if (player == null)
            throw new Exception("Player cannot be null.");
        if (money == null)
            throw new Exception("Money cannot be null.");
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

    public void TransferPlayerMoney(IPlayer from, IPlayer to, IMoney money) // money?
    {
        if (from == null || to == null)
            throw new Exception("Players cannot be null.");
        if (money == null)
            throw new Exception("Money cannot be null.");
        if (!_playerData.ContainsKey(from) || !_playerData.ContainsKey(to))
            throw new Exception("Player data cannot be null.");

        int fromMoney = _playerData[from].Sum(m => m.Value);
        if (fromMoney < money.Value)
            throw new Exception("Not enough money to transfer.");

        _playerData[from].Add(new Money(-money.Value));
        _playerData[to].Add(new Money(money.Value));
    }

    public void AddPlayerMoney(IPlayer player, IMoney money) // money?
    {
        if (player == null)
            throw new Exception("Player cannot be null.");
        if (money == null)
            throw new Exception("Money cannot be null.");
        if (!_playerData.ContainsKey(player) || _playerData[player] == null)
            throw new Exception("Player data cannot be null.");

        _playerData[player].Add(new Money(money.Value));
    }

    public int GetPlayerBalance(IPlayer player)
    {
        if (!_playerData.ContainsKey(player))
            throw new Exception("Player data cannot be null.");
        return _playerData[player].Sum(m => m.Value);
    }

    public bool AttemptBuyCurrentProperty(IPlayer player, bool wantsToBuy)
    {
        if (!wantsToBuy)
            return false;
        ITile tile = GetCurrentTile(player);
        if (!isPropertyAvailable(tile))
            return false;
        int price = tile.Asset!.Price.Value;
        if (GetPlayerBalance(player) < price)
            return false;
        SubstractPlayerMoney(player, new Money(price));
        tile.Owner = player;
        return true;
    }

    public void HandleBuyDecision(bool wantsToBuy) //bool
    {
        if (Phase != GamePhase.WaitingBuyDecision)
            throw new Exception("Not the right phase to handle buy decision.");

        AttemptBuyCurrentProperty(CurrentPlayer, wantsToBuy);

        EndGame();
        if (!GameEnded)
            NextPlayer();
        Phase = GamePhase.WaitingRoll;
    }

    public void BuyBuilding(IPlayer player, PropertyCity city, bool buildHotel)
    {
        ITile tile = GetTileByCity(city);

        if (tile.Asset == null)
            throw new Exception("This tile has no asset.");

        if (tile.Owner == null || !tile.Owner.Equals(player))
            throw new Exception("You do not own this property.");

        Color? color = tile.Asset.Color;
        if (color == null)
        {
            throw new Exception("Property ini tidak memiliki warna.");
        }

        List<ITile> sameColorTiles = _board.Tiles.Where(t => t.Asset?.Color == color).ToList();

        bool hasMonopoly = sameColorTiles.All(t => t.Owner != null && t.Owner.Equals(player));
        if (!hasMonopoly)
        {
            throw new Exception(
                "Kamu harus memiliki semua properti warna yang sama terlebih dahulu."
            );
        }

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
        List<ITile> properties = GetPlayerProperties(player).ToList();
        int totalIncome = 0;

        foreach (ITile tile in properties)
        {
            if (tile.Asset == null)
                continue;

            int income = SellPropertyToBank(
                player,
                tile.Asset.City.PropertyCity,
                includeBuildings: true
            );

            totalIncome += income;
        }

        return totalIncome;
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

    public int SellBuildingsToBank(
        IPlayer owner,
        PropertyCity city,
        int housesToSell,
        bool sellHotel
    )
    {
        ITile tile = GetTileByCity(city);
        if (tile.Owner == null || !tile.Owner.Equals(owner))
            throw new Exception("Property ini tidak dimiliki oleh pemain.");
        if (tile.Asset == null)
            throw new Exception("Tile tidak memiliki aset yang bisa dijual.");

        int houseCount = tile.House ?? 0;
        bool hasHotel = tile.HasHotel ?? false;
        if (!hasHotel && houseCount == 0)
            return 0;

        int soldValue = 0;
        int houseSellPrice = GetHousePrice(tile.Asset) / 2;

        if (sellHotel && hasHotel)
        {
            tile.HasHotel = false;
            soldValue += houseSellPrice * 5;
        }

        if (housesToSell > houseCount)
            throw new Exception("Not enough houses to sell.");
        if (housesToSell > 0)
        {
            tile.House = houseCount - housesToSell;
            soldValue += houseSellPrice * housesToSell;
        }

        if (soldValue > 0)
            AddPlayerMoney(owner, new Money(soldValue));
        return soldValue;
    }

    public int SellPropertyToBank(IPlayer owner, PropertyCity city, bool includeBuildings = true)
    {
        ITile tile = GetTileByCity(city);
        if (tile.Owner == null || !tile.Owner.Equals(owner))
            throw new Exception("Property ini tidak dimiliki oleh pemain.");
        if (tile.Asset == null)
            throw new Exception("Property tidak valid.");

        int totalIncome = 0;
        int houses = tile.House ?? 0;
        bool hasHotel = tile.HasHotel ?? false;

        if ((houses > 0 || hasHotel) && !includeBuildings)
            throw new Exception("Property ini memiliki bangunan. Jual bangunan terlebih dahulu.");

        if (includeBuildings)
        {
            if (hasHotel)
                totalIncome += SellBuildingsToBank(owner, city, 0, true);
            houses = tile.House ?? 0;
            if (houses > 0)
                totalIncome += SellBuildingsToBank(owner, city, houses, false);
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
        if (tile == null)
            throw new Exception("Tile cannot be null.");
        return tile.Asset != null && tile.Owner == null;
    }

    public ICard DrawCard(TileType drawType)
    {
        List<ICard> candidates = new List<ICard>();

        foreach (ICard card in _cards)
        {
            if (drawType == TileType.DrawChance && card is ChanceCard)
            {
                candidates.Add(card);
            }
            else if (drawType == TileType.DrawCommunity && card is CommunityCard)
            {
                candidates.Add(card);
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        Random rand = new();
        ICard chosen = candidates[rand.Next(candidates.Count)];
        return chosen;
    }

    public void ExecuteCard(ICard card, IPlayer player)
    {
        if (card == null || player == null)
        {
            throw new Exception("Card or player cannot be null.");
        }

        if (card is CommunityCard)
        {
            ExecuteCommunityCard(card, player);
        }
        else if (card is ChanceCard)
        {
            ExecuteChanceCard(card, player);
        }
    }

    private int HandleStreetRepairs(IPlayer player)
    {
        int houseCost = 40;
        int hotelCost = 115;
        int totalCost = 0; // dinamis
        foreach (ITile tile in _board.Tiles)
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
                if (CheckPlayerJailStatus(player))
                    ReleaseFromJail(player);
                else
                    player.JailFreeCardCount++;
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
                foreach (IPlayer p in _players)
                    if (!p.Equals(player))
                        TransferPlayerMoney(p, player, new Money(MoneyValue.ten));
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
                AddPlayerMoney(player, new Money(MoneyValue.twoHundred));
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
                if (CheckPlayerJailStatus(player))
                    ReleaseFromJail(player);
                else
                    player.JailFreeCardCount++;
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
                foreach (IPlayer p in _players)
                    if (!p.Equals(player))
                        TransferPlayerMoney(player, p, new Money(MoneyValue.fifty));
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
        if (tile == null || player == null)
            throw new Exception("Tile or player cannot be null.");

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
                        if (doubleRent)
                            rent *= 2;
                        SubstractPlayerMoney(player, new Money(rent));
                        if (!player.IsBankrupt)
                            AddPlayerMoney(tile.Owner, new Money(rent));
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
                ICard chanceCard = DrawCard(tile.Type);
                ExecuteCard(chanceCard, player);
                return chanceCard;
            case TileType.DrawCommunity:
                ICard communityCard = DrawCard(tile.Type);
                ExecuteCard(communityCard, player);
                return communityCard;

            default:
                return null;
        }
    }

    public List<ITile> GetPlayerProperties(IPlayer player) =>
        _board.Tiles.Where(t => t.Owner != null && t.Owner.Equals(player)).ToList();

    public IPlayer? GetWinnerOrNull()
    {
        List<IPlayer> activePlayers = _players.Where(p => !p.IsBankrupt).ToList();

        return activePlayers.Count == 1 ? activePlayers[0] : null; // masukin identiies
    }

    public IPlayer? FindPlayerByName(string playerName) =>
        _players.FirstOrDefault(p => p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));

    public bool IsPropertyOwnedBy(ITile tile, IPlayer player) =>
        tile.Owner != null && tile.Owner.Equals(player);

    public bool CheckWinner()
    {
        IPlayer? winner = GetWinnerOrNull();
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
        if (isGameEnded)
            _gameEnded = true;
        return isGameEnded;
    }
}
