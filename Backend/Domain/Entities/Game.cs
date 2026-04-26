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

    public GameResultDTO<bool> AssignPieceToPlayer(IPlayer player, PieceType pieceType)
    {
        if (player == null)
        {
            return GameResultDTO<bool>.Failure("Player tidak boleh null.");
        }

        if (!_players.Contains(player))
        {
            return GameResultDTO<bool>.Failure("Player tidak ditemukan dalam game ini.");
        }

        if (
            _playerPiece.TryGetValue(player, out IPiece? currentPiece)
            && currentPiece.Type == pieceType
        )
        {
            return GameResultDTO<bool>.Success(true);
        }

        if (!IsPieceAvailable(pieceType))
        {
            return GameResultDTO<bool>.Failure($"Piece {pieceType} sudah diambil oleh pemain lain.");
        }

        IPiece? newPiece = _pieces.FirstOrDefault(p => p.Type == pieceType);

        if (newPiece == null)
        {
            return GameResultDTO<bool>.Failure($"Piece {pieceType} tidak ditemukan.");
        }

        if (_playerPiece.TryGetValue(player, out IPiece? oldPiece))
        {
            ITile? currentTile = _board.Tiles.FirstOrDefault(t => t.Pieces.Contains(oldPiece));
            if (currentTile != null)
            {
                currentTile.Pieces.Remove(oldPiece);
            }

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
        {
            startTile.Pieces.Add(newPiece);
        }

        return GameResultDTO<bool>.Success(true);
    }

    public GameResultDTO<IPiece> GetPiece(IPlayer player)
    {
        if (player == null)
        {
            return GameResultDTO<IPiece>.Failure("Player tidak boleh null.");
        }

        if (_playerPiece.ContainsKey(player))
        {
            return GameResultDTO<IPiece>.Success(_playerPiece[player]);
        }

        return GameResultDTO<IPiece>.Failure("Player tidak memiliki piece yang di-assign.");
    }

    public void NextPlayer()
    {
        if (_gameEnded)
        {
            return;
        }

        do
        {
            _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;
        } while (
            _players[_currentPlayerIndex].IsBankrupt && _players.Count(p => !p.IsBankrupt) > 1
        );
    }

    public GameResultDTO<RollTurnResult> RollTurn()
    {
        if (Phase != GamePhase.WaitingRoll)
        {
            return GameResultDTO<RollTurnResult>.Failure("Bukan fase yang tepat untuk melempar dadu.");
        }

        IPlayer player = CurrentPlayer;

        if (player.IsInJail)
        {
            int firstDice = new Dice().MaxRolled;
            int secondDice = new Dice().MaxRolled;

            if (firstDice == secondDice)
            {
                ReleaseFromJail(player);
                MovePiece(player, firstDice + secondDice);

                GameResultDTO<ITile?> jailTileResult = GetCurrentTile(player);
                if (!jailTileResult.IsSuccess || jailTileResult.Data == null)
                {
                    return GameResultDTO<RollTurnResult>.Failure("Gagal mendapatkan tile saat ini setelah keluar penjara.");
                }

                ITile landedTileJail = jailTileResult.Data;

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
                    {
                        NextPlayer();
                    }
                    Phase = GamePhase.WaitingRoll;
                }
                else
                {
                    Phase = GamePhase.WaitingBuyDecision;
                }

                return GameResultDTO<RollTurnResult>.Success(new RollTurnResult(
                    DiceTotal: firstDice + secondDice,
                    Dice1: firstDice,
                    Dice2: secondDice,
                    LandedTileType: landedTileJail.Type.ToString(),
                    LandedProperty: landedTileJail.Asset != null ? landedTileJail : null,
                    LandedTile: landedTileJail,
                    RequiresBuyDecision: requiresBuyJail,
                    DrawnCard: jailCard,
                    JailRollResult: JailRollResult.Released
                ));
            }
            else
            {
                player.JailTurnsRemaining--;

                if (player.JailTurnsRemaining <= 0)
                {
                    IMoney? fiftyMoney = _money.FirstOrDefault(m => m.Value == MoneyValue.fifty);
                    GameResultDTO<int> balanceResult = GetPlayerBalance(player);

                    if (fiftyMoney != null && balanceResult.IsSuccess && balanceResult.Data >= MoneyValue.fifty)
                    {
                        SubstractPlayerMoney(player, fiftyMoney);
                    }

                    ReleaseFromJail(player);
                }

                EndGame();
                if (!GameEnded)
                {
                    NextPlayer();
                }
                Phase = GamePhase.WaitingRoll;

                return GameResultDTO<RollTurnResult>.Success(new RollTurnResult(
                    DiceTotal: firstDice + secondDice,
                    Dice1: firstDice,
                    Dice2: secondDice,
                    LandedTileType: "None",
                    LandedProperty: null,
                    LandedTile: null,
                    RequiresBuyDecision: false,
                    DrawnCard: null,
                    JailRollResult: JailRollResult.StayedInJail
                ));
            }
        }

        if (CheckBankruptcy(player))
        {
            RemovePlayer(player);
            EndGame();
            if (!GameEnded)
            {
                NextPlayer();
            }

            return GameResultDTO<RollTurnResult>.Success(new RollTurnResult(
                DiceTotal: 0,
                Dice1: 0,
                Dice2: 0,
                LandedTileType: "None",
                LandedProperty: null,
                LandedTile: null,
                RequiresBuyDecision: false,
                DrawnCard: null,
                JailRollResult: JailRollResult.None
            ));
        }

        int firstRoll = new Dice().MaxRolled;
        int secondRoll = new Dice().MaxRolled;
        int diceTotal = firstRoll + secondRoll;

        if (firstRoll == secondRoll)
        {
            player.DoubleRoll++;
        }

        if (player.DoubleRoll >= 3)
        {
            player.DoubleRoll = 0;
            SendPieceToJail(player);
            EndGame();
            if (!GameEnded)
            {
                NextPlayer();
            }
            Phase = GamePhase.WaitingRoll;

            return GameResultDTO<RollTurnResult>.Success(new RollTurnResult(
                DiceTotal: diceTotal,
                Dice1: firstRoll,
                Dice2: secondRoll,
                LandedTileType: "SentToJail",
                LandedProperty: null,
                LandedTile: null,
                RequiresBuyDecision: false,
                DrawnCard: null,
                JailRollResult: JailRollResult.None
            ));
        }

        GameResultDTO<bool> moveResult = MovePiece(player, diceTotal);
        if (!moveResult.IsSuccess)
        {
            return GameResultDTO<RollTurnResult>.Failure(moveResult.Error!);
        }

        GameResultDTO<ITile?> currentTileResult = GetCurrentTile(player);
        if (!currentTileResult.IsSuccess || currentTileResult.Data == null)
        {
            return GameResultDTO<RollTurnResult>.Failure("Gagal mendapatkan tile saat ini setelah bergerak.");
        }

        ITile landedTile = currentTileResult.Data;

        HandleTileEffectsAfterMove(
            player,
            landedTile,
            out ICard? card,
            out bool requiresBuyDecision
        );

        if (requiresBuyDecision)
        {
            Phase = GamePhase.WaitingBuyDecision;

            return GameResultDTO<RollTurnResult>.Success(new RollTurnResult(
                DiceTotal: diceTotal,
                Dice1: firstRoll,
                Dice2: secondRoll,
                LandedTileType: landedTile.Type.ToString(),
                LandedProperty: landedTile.Asset != null ? landedTile : null,
                LandedTile: landedTile,
                RequiresBuyDecision: true,
                DrawnCard: null,
                JailRollResult: JailRollResult.None
            ));
        }

        EndGame();
        if (!GameEnded)
        {
            NextPlayer();
        }
        Phase = GamePhase.WaitingRoll;

        return GameResultDTO<RollTurnResult>.Success(new RollTurnResult(
            DiceTotal: diceTotal,
            Dice1: firstRoll,
            Dice2: secondRoll,
            LandedTileType: landedTile.Type.ToString(),
            LandedProperty: landedTile.Asset != null ? landedTile : null,
            LandedTile: landedTile,
            RequiresBuyDecision: false,
            DrawnCard: card,
            JailRollResult: JailRollResult.None
        ));
    }

    private void HandleTileEffectsAfterMove(
        IPlayer player,
        ITile tile,
        out ICard? drawnCard,
        out bool requiresBuyDecision
    )
    {
        drawnCard = null;
        requiresBuyDecision = false;

        if (IsPropertyAvailable(tile))
        {
            int price = tile.Asset?.Price.Value ?? 0;
            GameResultDTO<int> balanceResult = GetPlayerBalance(player);

            if (balanceResult.IsSuccess && balanceResult.Data >= price)
            {
                requiresBuyDecision = true;
                return;
            }
        }

        GameResultDTO<ICard?> tileResult = ExecuteTile(tile, player);
        if (tileResult.IsSuccess)
        {
            drawnCard = tileResult.Data;
        }
    }

    public GameResultDTO<bool> MovePiece(IPlayer player, int? step = null)
    {
        if (player == null)
        {
            return GameResultDTO<bool>.Failure("Player tidak boleh null.");
        }

        if (player.DoubleRoll >= 3)
        {
            SendPieceToJail(player);
            return GameResultDTO<bool>.Success(true);
        }

        GameResultDTO<int> diceResult = HandleDiceRoll();
        int move = step ?? diceResult.Data;

        GameResultDTO<ITile?> currentTileResult = GetCurrentTile(player);
        if (!currentTileResult.IsSuccess || currentTileResult.Data == null)
        {
            return GameResultDTO<bool>.Failure("Gagal mendapatkan tile saat ini.");
        }

        ITile currentTile = currentTileResult.Data;
        int currentIndex = Array.IndexOf(_board.Tiles, currentTile);
        int count = _board.Tiles.Length;
        int newIndex = ((currentIndex + move) % count + count) % count;

        if (move > 0 && newIndex < currentIndex)
        {
            IMoney? twoHundredMoney = _money.FirstOrDefault(m => m.Value == MoneyValue.twoHundred);
            if (twoHundredMoney != null)
            {
                AddPlayerMoney(player, new Money(twoHundredMoney.Value));
            }
        }

        MovePieceToIndex(player, newIndex);
        return GameResultDTO<bool>.Success(true);
    }

    private void MovePieceTo(IPlayer player, ITile targetTile)
    {
        GameResultDTO<ITile?> currentTileResult = GetCurrentTile(player);
        if (!currentTileResult.IsSuccess || currentTileResult.Data == null)
        {
            return;
        }

        ITile currentTile = currentTileResult.Data;
        currentTile.Pieces.Remove(_playerPiece[player]);
        targetTile.Pieces.Add(_playerPiece[player]);
    }

    private void MovePieceToIndex(IPlayer player, int targetIndex)
    {
        MovePieceTo(player, _board.Tiles[targetIndex]);
    }

    private void MoveToNearestUtility(IPlayer player)
    {
        GameResultDTO<ITile?> currentTileResult = GetCurrentTile(player);
        if (!currentTileResult.IsSuccess || currentTileResult.Data == null)
        {
            return;
        }

        int currentIndex = Array.IndexOf(_board.Tiles, currentTileResult.Data);
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
        GameResultDTO<ITile?> currentTileResult = GetCurrentTile(player);
        if (!currentTileResult.IsSuccess || currentTileResult.Data == null)
        {
            return;
        }

        int currentIndex = Array.IndexOf(_board.Tiles, currentTileResult.Data);
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

    private ITile GetTileByType(TileType type)
    {
        return _board.Tiles.First(t => t.Type == type);
    }

    public GameResultDTO<ITile?> GetCurrentTile(IPlayer player)
    {
        if (player == null)
        {
            return GameResultDTO<ITile?>.Failure("Player tidak boleh null.");
        }

        if (!_playerPiece.TryGetValue(player, out IPiece? piece))
        {
            return GameResultDTO<ITile?>.Failure("Player tidak memiliki piece yang di-assign.");
        }

        ITile? tile = _board.Tiles.FirstOrDefault(t => t.Pieces.Contains(piece));

        if (tile != null)
        {
            return GameResultDTO<ITile?>.Success(tile);
        }

        ITile startTile = GetTileByType(TileType.StartTile);

        if (!startTile.Pieces.Contains(piece))
        {
            startTile.Pieces.Add(piece);
        }

        return GameResultDTO<ITile?>.Success(startTile);
    }

    private ITile GetTileByPropertyCity(PropertyCity city)
    {
        return _board.Tiles.First(t => t.Asset != null && t.Asset.City.PropertyCity == city);
    }

    public ITile GetTileByCity(PropertyCity city)
    {
        return GetTileByPropertyCity(city);
    }

    public GameResultDTO<bool> SendPieceToJail(IPlayer player)
    {
        if (player == null)
        {
            return GameResultDTO<bool>.Failure("Player tidak boleh null.");
        }

        GameResultDTO<ITile?> currentTileResult = GetCurrentTile(player);
        if (!currentTileResult.IsSuccess || currentTileResult.Data == null)
        {
            return GameResultDTO<bool>.Failure("Gagal mendapatkan tile saat ini.");
        }

        ITile currentTile = currentTileResult.Data;
        currentTile.Pieces.Remove(_playerPiece[player]);

        ITile jailTile = GetTileByType(TileType.JailTile);
        jailTile.Pieces.Add(_playerPiece[player]);
        player.IsInJail = true;
        player.JailTurnsRemaining = 3;

        PlayerSentToJail?.Invoke(player, _playerPiece[player]);
        return GameResultDTO<bool>.Success(true);
    }

    public GameResultDTO<bool> ReleaseFromJail(IPlayer player)
    {
        if (player == null)
        {
            return GameResultDTO<bool>.Failure("Player tidak boleh null.");
        }

        ITile jailTile = GetTileByType(TileType.JailTile);
        jailTile.Pieces.Remove(_playerPiece[player]);
        player.IsInJail = false;
        player.JailTurnsRemaining = 0;
        return GameResultDTO<bool>.Success(true);
    }

    public GameResultDTO<int> HandleDiceRoll(IDice? firstDice = null, IDice? secondDice = null)
    {
        IDice dice1 = firstDice ?? new Dice();
        IDice dice2 = secondDice ?? new Dice();

        if (dice1.MaxRolled == dice2.MaxRolled)
        {
            CurrentPlayer.DoubleRoll++;
        }

        return GameResultDTO<int>.Success(dice1.MaxRolled + dice2.MaxRolled);
    }

    public bool CheckBankruptcy(IPlayer player)
    {
        return player.IsBankrupt;
    }

    public bool CheckPlayerJailStatus(IPlayer player)
    {
        if (player == null)
        {
            return false;
        }

        return player.IsInJail;
    }

    private bool IsOnOwnedProperty(ITile tile)
    {
        return tile.Owner != null;
    }

    public GameResultDTO<bool> RemovePlayer(IPlayer player)
    {
        if (player == null)
        {
            return GameResultDTO<bool>.Failure("Player tidak boleh null.");
        }

        _players.Remove(player);
        _playerPiece.Remove(player);
        _playerData.Remove(player);
        return GameResultDTO<bool>.Success(true);
    }

    public GameResultDTO<bool> SubstractPlayerMoney(IPlayer player, IMoney money)
    {
        if (player == null)
        {
            return GameResultDTO<bool>.Failure("Player tidak boleh null.");
        }

        if (money == null)
        {
            return GameResultDTO<bool>.Failure("Money tidak boleh null.");
        }

        if (!_playerData.ContainsKey(player) || _playerData[player] == null)
        {
            return GameResultDTO<bool>.Failure("Data player tidak ditemukan.");
        }

        int currentMoney = _playerData[player].Sum(m => m.Value);
        if (currentMoney < money.Value)
        {
            player.IsBankrupt = true;
            PlayerBankrupt?.Invoke(player);
            return GameResultDTO<bool>.Failure($"Uang tidak cukup. Saldo saat ini: {currentMoney}, dibutuhkan: {money.Value}.");
        }

        _playerData[player].Add(new Money(-money.Value));
        return GameResultDTO<bool>.Success(true);
    }

    public GameResultDTO<bool> TransferPlayerMoney(IPlayer from, IPlayer to, IMoney money)
    {
        if (from == null || to == null)
        {
            return GameResultDTO<bool>.Failure("Player tidak boleh null.");
        }

        if (money == null)
        {
            return GameResultDTO<bool>.Failure("Money tidak boleh null.");
        }

        if (!_playerData.ContainsKey(from) || !_playerData.ContainsKey(to))
        {
            return GameResultDTO<bool>.Failure("Data player tidak ditemukan.");
        }

        int fromMoney = _playerData[from].Sum(m => m.Value);
        if (fromMoney < money.Value)
        {
            return GameResultDTO<bool>.Failure($"Uang tidak cukup untuk transfer. Saldo {from.Name}: {fromMoney}, dibutuhkan: {money.Value}.");
        }

        _playerData[from].Add(new Money(-money.Value));
        _playerData[to].Add(new Money(money.Value));
        return GameResultDTO<bool>.Success(true);
    }

    public GameResultDTO<bool> AddPlayerMoney(IPlayer player, IMoney money)
    {
        if (player == null)
        {
            return GameResultDTO<bool>.Failure("Player tidak boleh null.");
        }

        if (money == null)
        {
            return GameResultDTO<bool>.Failure("Money tidak boleh null.");
        }

        if (!_playerData.ContainsKey(player) || _playerData[player] == null)
        {
            return GameResultDTO<bool>.Failure("Data player tidak ditemukan.");
        }

        _playerData[player].Add(new Money(money.Value));
        return GameResultDTO<bool>.Success(true);
    }

    public GameResultDTO<int> GetPlayerBalance(IPlayer player)
    {
        if (player == null)
        {
            return GameResultDTO<int>.Failure("Player tidak boleh null.");
        }

        if (!_playerData.ContainsKey(player))
        {
            return GameResultDTO<int>.Failure("Data player tidak ditemukan.");
        }

        return GameResultDTO<int>.Success(_playerData[player].Sum(m => m.Value));
    }

    public GameResultDTO<bool> AttemptBuyCurrentProperty(IPlayer player, bool wantsToBuy)
    {
        if (!wantsToBuy)
        {
            return GameResultDTO<bool>.Success(false);
        }

        GameResultDTO<ITile?> tileResult = GetCurrentTile(player);
        if (!tileResult.IsSuccess || tileResult.Data == null)
        {
            return GameResultDTO<bool>.Failure("Gagal mendapatkan tile saat ini.");
        }

        ITile tile = tileResult.Data;

        if (!IsPropertyAvailable(tile))
        {
            return GameResultDTO<bool>.Failure("Properti ini tidak tersedia untuk dibeli.");
        }

        int price = tile.Asset!.Price.Value;

        GameResultDTO<int> balanceResult = GetPlayerBalance(player);
        if (!balanceResult.IsSuccess)
        {
            return GameResultDTO<bool>.Failure(balanceResult.Error!);
        }

        if (balanceResult.Data < price)
        {
            return GameResultDTO<bool>.Failure($"Uang tidak cukup untuk membeli properti. Saldo: {balanceResult.Data}, harga: {price}.");
        }

        GameResultDTO<bool> subtractResult = SubstractPlayerMoney(player, new Money(price));
        if (!subtractResult.IsSuccess)
        {
            return GameResultDTO<bool>.Failure(subtractResult.Error!);
        }

        tile.Owner = player;
        return GameResultDTO<bool>.Success(true);
    }

    public GameResultDTO<bool> HandleBuyDecision(bool wantsToBuy)
    {
        if (Phase != GamePhase.WaitingBuyDecision)
        {
            return GameResultDTO<bool>.Failure("Bukan fase yang tepat untuk keputusan beli properti.");
        }

        AttemptBuyCurrentProperty(CurrentPlayer, wantsToBuy);

        EndGame();

        if (!GameEnded)
        {
            NextPlayer();
        }

        Phase = GamePhase.WaitingRoll;
        return GameResultDTO<bool>.Success(true);
    }

    public GameResultDTO<bool> BuyBuilding(IPlayer player, PropertyCity city, bool buildHotel)
    {
        if (player == null)
        {
            return GameResultDTO<bool>.Failure("Player tidak boleh null.");
        }

        ITile tile = GetTileByCity(city);

        if (tile.Asset == null)
        {
            return GameResultDTO<bool>.Failure("Tile ini tidak memiliki aset.");
        }

        if (tile.Owner == null || !tile.Owner.Equals(player))
        {
            return GameResultDTO<bool>.Failure($"Properti {city} bukan milik {player.Name}.");
        }

        Color? color = tile.Asset.Color;

        if (color == null)
        {
            return GameResultDTO<bool>.Failure("Properti ini tidak memiliki warna (tidak bisa dibangun).");
        }

        List<ITile> sameColorTiles = _board.Tiles.Where(t => t.Asset?.Color == color).ToList();
        bool hasMonopoly = sameColorTiles.All(t => t.Owner != null && t.Owner.Equals(player));

        if (!hasMonopoly)
        {
            return GameResultDTO<bool>.Failure($"Pemain harus memiliki monopoli warna {color} untuk membangun.");
        }

        int housePrice = GetHousePrice(tile.Asset);
        int currentHouses = tile.House ?? 0;
        bool hasHotel = tile.HasHotel ?? false;

        if (hasHotel)
        {
            return GameResultDTO<bool>.Failure("Tile ini sudah memiliki hotel, tidak bisa dibangun lagi.");
        }

        if (buildHotel)
        {
            if (currentHouses < 3)
            {
                return GameResultDTO<bool>.Failure($"Dibutuhkan minimal 3 rumah sebelum membangun hotel. Saat ini: {currentHouses}.");
            }

            int hotelPrice = housePrice * 5;
            GameResultDTO<bool> subtractResult = SubstractPlayerMoney(player, new Money(hotelPrice));
            if (!subtractResult.IsSuccess)
            {
                return GameResultDTO<bool>.Failure(subtractResult.Error!);
            }

            tile.House = 0;
            tile.HasHotel = true;
        }
        else
        {
            if (currentHouses >= 3)
            {
                return GameResultDTO<bool>.Failure("Sudah mencapai maksimum 3 rumah. Bangun hotel sebagai gantinya.");
            }

            GameResultDTO<bool> subtractResult = SubstractPlayerMoney(player, new Money(housePrice));
            if (!subtractResult.IsSuccess)
            {
                return GameResultDTO<bool>.Failure(subtractResult.Error!);
            }

            tile.House = currentHouses + 1;
        }

        return GameResultDTO<bool>.Success(true);
    }

    public GameResultDTO<int> SellAllAssetsToBank(IPlayer player)
    {
        if (player == null)
        {
            return GameResultDTO<int>.Failure("Player tidak boleh null.");
        }

        List<ITile> properties = GetPlayerProperties(player).ToList();
        int totalIncome = 0;

        foreach (ITile tile in properties)
        {
            if (tile.Asset == null)
            {
                continue;
            }

            GameResultDTO<int> sellResult = SellPropertyToBank(
                player,
                tile.Asset.City.PropertyCity,
                includeBuildings: true
            );

            if (sellResult.IsSuccess)
            {
                totalIncome += sellResult.Data;
            }
        }

        return GameResultDTO<int>.Success(totalIncome);
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

    public GameResultDTO<int> SellBuildingsToBank(SendBuildingToBankResult sellInfo)
    {
        if (sellInfo == null)
        {
            return GameResultDTO<int>.Failure("Info penjualan bangunan tidak boleh null.");
        }

        ITile tile = GetTileByCity(sellInfo.City);

        if (tile.Owner == null || !tile.Owner.Equals(sellInfo.Player))
        {
            return GameResultDTO<int>.Failure($"Properti {sellInfo.City} bukan milik {sellInfo.Player?.Name}.");
        }

        if (tile.Asset == null)
        {
            return GameResultDTO<int>.Failure("Tile ini tidak memiliki aset.");
        }

        int houseCount = tile.House ?? 0;
        bool hasHotel = tile.HasHotel ?? false;

        if (!hasHotel && houseCount == 0)
        {
            return GameResultDTO<int>.Failure("Tidak ada bangunan untuk dijual di properti ini.");
        }

        int soldValue = 0;
        int houseSellPrice = GetHousePrice(tile.Asset) / 2;

        if (sellInfo.SellHotel && hasHotel)
        {
            tile.HasHotel = false;
            soldValue += houseSellPrice * 5;
        }

        if (sellInfo.HousesToSell > houseCount)
        {
            return GameResultDTO<int>.Failure($"Jumlah rumah yang dijual ({sellInfo.HousesToSell}) melebihi yang tersedia ({houseCount}).");
        }

        if (sellInfo.HousesToSell > 0)
        {
            tile.House = houseCount - sellInfo.HousesToSell;
            soldValue += houseSellPrice * sellInfo.HousesToSell;
        }

        if (soldValue > 0)
        {
            AddPlayerMoney(sellInfo.Player, new Money(soldValue));
        }

        return GameResultDTO<int>.Success(soldValue);
    }

    public GameResultDTO<int> SellPropertyToBank(IPlayer owner, PropertyCity city, bool includeBuildings = true)
    {
        if (owner == null)
        {
            return GameResultDTO<int>.Failure("Owner tidak boleh null.");
        }

        ITile tile = GetTileByCity(city);

        if (tile.Owner == null || !tile.Owner.Equals(owner))
        {
            return GameResultDTO<int>.Failure($"Properti {city} bukan milik {owner.Name}.");
        }

        if (tile.Asset == null)
        {
            return GameResultDTO<int>.Failure("Tile ini tidak memiliki aset.");
        }

        int totalIncome = 0;
        int houses = tile.House ?? 0;
        bool hasHotel = tile.HasHotel ?? false;

        if ((houses > 0 || hasHotel) && !includeBuildings)
        {
            return GameResultDTO<int>.Failure("Properti masih memiliki bangunan. Jual bangunan terlebih dahulu atau gunakan includeBuildings=true.");
        }

        if (includeBuildings)
        {
            if (hasHotel)
            {
                GameResultDTO<int> hotelResult = SellBuildingsToBank(
                    new SendBuildingToBankResult(owner, city, 0, true)
                );
                if (hotelResult.IsSuccess)
                {
                    totalIncome += hotelResult.Data;
                }
            }

            houses = tile.House ?? 0;

            if (houses > 0)
            {
                GameResultDTO<int> houseResult = SellBuildingsToBank(
                    new SendBuildingToBankResult(owner, city, houses, false)
                );
                if (houseResult.IsSuccess)
                {
                    totalIncome += houseResult.Data;
                }
            }
        }

        int propertySellValue = tile.Asset.Price.Value / 2;

        tile.Owner = null;
        tile.House = 0;
        tile.HasHotel = false;

        AddPlayerMoney(owner, new Money(propertySellValue));
        totalIncome += propertySellValue;

        return GameResultDTO<int>.Success(totalIncome);
    }

    public bool IsPropertyAvailable(ITile tile)
    {
        if (tile == null)
        {
            return false;
        }

        return tile.Asset != null && tile.Owner == null;
    }

    public ICard? DrawCard(TileType drawType)
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
        ICard chosenCard = candidates[rand.Next(candidates.Count)];
        return chosenCard;
    }

    public GameResultDTO<bool> ExecuteCard(ICard card, IPlayer player)
    {
        if (card == null)
        {
            return GameResultDTO<bool>.Failure("Card tidak boleh null.");
        }

        if (player == null)
        {
            return GameResultDTO<bool>.Failure("Player tidak boleh null.");
        }

        if (card is CommunityCard)
        {
            return ExecuteCommunityCard(card, player);
        }
        else if (card is ChanceCard)
        {
            return ExecuteChanceCard(card, player);
        }

        return GameResultDTO<bool>.Failure("Tipe card tidak dikenali.");
    }

    private int HandleStreetRepairs(IPlayer player)
    {
        if (player == null)
        {
            return 0;
        }

        int totalCost = 0;

        foreach (ITile tile in _board.Tiles)
        {
            if (tile.Owner != null && tile.Owner.Equals(player) && tile.Asset != null)
            {
                totalCost += (tile.House ?? 0) * BuildingValue.HOUSE_COST;
                totalCost = tile.HasHotel == true ? totalCost + BuildingValue.HOTEL_COST : totalCost;
            }
        }

        return totalCost;
    }

    private GameResultDTO<bool> ExecuteCommunityCard(ICard card, IPlayer player)
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
                {
                    ReleaseFromJail(player);
                }
                else
                {
                    player.JailFreeCardCount++;
                }
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
                {
                    if (!p.Equals(player))
                    {
                        TransferPlayerMoney(p, player, new Money(MoneyValue.ten));
                    }
                }
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
                return GameResultDTO<bool>.Failure($"CardBehaviour {card.Behaviour} tidak dikenali untuk Community Card.");
        }

        return GameResultDTO<bool>.Success(true);
    }

    private GameResultDTO<bool> ExecuteChanceCard(ICard card, IPlayer player)
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
                GameResultDTO<ITile?> nearestRailTile = GetCurrentTile(player);
                if (nearestRailTile.IsSuccess && nearestRailTile.Data != null)
                {
                    ExecuteTile(nearestRailTile.Data, player, true);
                }
                break;
            case CardBehaviour.BankPaysDividend:
                AddPlayerMoney(player, new Money(MoneyValue.fifty));
                break;
            case CardBehaviour.GetOutOfJailFree:
                if (CheckPlayerJailStatus(player))
                {
                    ReleaseFromJail(player);
                }
                else
                {
                    player.JailFreeCardCount++;
                }
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
                {
                    if (!p.Equals(player))
                    {
                        TransferPlayerMoney(player, p, new Money(MoneyValue.fifty));
                    }
                }
                break;
            case CardBehaviour.YourBuildingLoanMatures:
                AddPlayerMoney(player, new Money(MoneyValue.hundred + MoneyValue.fifty));
                break;
            default:
                return GameResultDTO<bool>.Failure($"CardBehaviour {card.Behaviour} tidak dikenali untuk Chance Card.");
        }

        return GameResultDTO<bool>.Success(true);
    }

    public GameResultDTO<ICard?> ExecuteTile(ITile tile, IPlayer player, bool doubleRent = false)
    {
        if (tile == null)
        {
            return GameResultDTO<ICard?>.Failure("Tile tidak boleh null.");
        }

        if (player == null)
        {
            return GameResultDTO<ICard?>.Failure("Player tidak boleh null.");
        }

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
                        {
                            rent *= 2;
                        }

                        GameResultDTO<bool> subtractResult = SubstractPlayerMoney(player, new Money(rent));
                        if (subtractResult.IsSuccess && !player.IsBankrupt)
                        {
                            AddPlayerMoney(tile.Owner, new Money(rent));
                        }
                    }
                }
                return GameResultDTO<ICard?>.Success(null);

            case TileType.TaxTile:
            case TileType.PayTaxTile:
                SubstractPlayerMoney(player, new Money(MoneyValue.hundred));
                return GameResultDTO<ICard?>.Success(null);

            case TileType.GoToJailTile:
                SendPieceToJail(player);
                return GameResultDTO<ICard?>.Success(null);

            case TileType.DrawChance:
                ICard? chanceCard = DrawCard(tile.Type);
                if (chanceCard != null)
                {
                    ExecuteCard(chanceCard, player);
                }
                return GameResultDTO<ICard?>.Success(chanceCard);

            case TileType.DrawCommunity:
                ICard? communityCard = DrawCard(tile.Type);
                if (communityCard != null)
                {
                    ExecuteCard(communityCard, player);
                }
                return GameResultDTO<ICard?>.Success(communityCard);

            default:
                return GameResultDTO<ICard?>.Failure($"Tipe tile '{tile.Type}' tidak memiliki efek yang dapat dieksekusi.");
        }
    }

    public List<ITile> GetPlayerProperties(IPlayer player)
    {
        return _board.Tiles.Where(t => t.Owner != null && t.Owner.Equals(player)).ToList();
    }

    public GameResultDTO<IPlayer?> GetWinnerOrNull()
    {
        List<IPlayer> activePlayers = _players.Where(p => !p.IsBankrupt).ToList();

        if (activePlayers.Count == 1)
        {
            return GameResultDTO<IPlayer?>.Success(activePlayers[0]);
        }

        return GameResultDTO<IPlayer?>.Failure("Belum ada pemenang.");
    }

    public IPlayer? FindPlayerByName(string playerName)
    {
        return _players.FirstOrDefault(p => p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsPropertyOwnedBy(ITile tile, IPlayer player)
    {
        return tile.Owner != null && tile.Owner.Equals(player);
    }

    public bool CheckWinner()
    {
        GameResultDTO<IPlayer?> winnerResult = GetWinnerOrNull();
        if (winnerResult.IsSuccess && winnerResult.Data != null)
        {
            IsGameEnded?.Invoke(winnerResult.Data);
            return true;
        }

        return false;
    }

    public bool EndGame()
    {
        bool isGameEnded = CheckWinner();
        if (isGameEnded)
        {
            _gameEnded = true;
        }

        return isGameEnded;
    }
}