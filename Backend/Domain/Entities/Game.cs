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

    public IPlayer CurrentPlayer => _players[_currentPlayerIndex];
    public List<IPlayer> Players => _players;
    public int CurrentPlayerIndex => _currentPlayerIndex;
    public List<ICard> Cards => _cards;
    public List<IPiece> Pieces => _pieces;
    public bool GameEnded => _gameEnded;

    public bool IsPieceAvailable(PieceType pieceType)
    {
        bool isAvailable = !_takenPieces.ContainsKey(pieceType);
        return isAvailable;
    }

    public GameResultDTO<bool> AssignPieceToPlayer(IPlayer player, PieceType pieceType)
    {
        if (player == null)
        {
            GameResultDTO<bool> nullPlayerResult = GameResultDTO<bool>.Failure(
                "Player tidak boleh null."
            );
            return nullPlayerResult;
        }

        if (!_players.Contains(player))
        {
            GameResultDTO<bool> notFoundResult = GameResultDTO<bool>.Failure(
                "Player tidak ditemukan dalam game ini."
            );
            return notFoundResult;
        }

        if (
            _playerPiece.TryGetValue(player, out IPiece? currentPiece)
            && currentPiece.Type == pieceType
        )
        {
            GameResultDTO<bool> alreadyAssignedResult = GameResultDTO<bool>.Success(true);
            return alreadyAssignedResult;
        }

        if (!IsPieceAvailable(pieceType))
        {
            GameResultDTO<bool> takenResult = GameResultDTO<bool>.Failure(
                $"Piece {pieceType} sudah diambil oleh pemain lain."
            );
            return takenResult;
        }

        IPiece? newPiece = _pieces.FirstOrDefault(p => p.Type == pieceType);
        if (newPiece == null)
        {
            GameResultDTO<bool> pieceNotFoundResult = GameResultDTO<bool>.Failure(
                $"Piece {pieceType} tidak ditemukan."
            );
            return pieceNotFoundResult;
        }

        if (_playerPiece.TryGetValue(player, out IPiece? oldPiece))
        {
            ITile? currentTile = _board.Tiles.FirstOrDefault(t => t.Pieces.Contains(oldPiece));
            currentTile?.Pieces.Remove(oldPiece);

            KeyValuePair<PieceType, IPlayer> oldTakenEntry = _takenPieces.FirstOrDefault(kv =>
                kv.Value.Equals(player)
            );

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

        GameResultDTO<bool> successResult = GameResultDTO<bool>.Success(true);
        return successResult;
    }

    public GameResultDTO<IPiece> GetPiece(IPlayer player)
    {
        if (player == null)
        {
            GameResultDTO<IPiece> nullResult = GameResultDTO<IPiece>.Failure(
                "Player tidak boleh null."
            );
            return nullResult;
        }

        if (_playerPiece.ContainsKey(player))
        {
            GameResultDTO<IPiece> pieceResult = GameResultDTO<IPiece>.Success(_playerPiece[player]);
            return pieceResult;
        }

        GameResultDTO<IPiece> notFoundResult = GameResultDTO<IPiece>.Failure(
            "Player tidak memiliki piece yang di-assign."
        );
        return notFoundResult;
    }

    public void NextPlayer()
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

    public GameResultDTO<RollTurnResult> RollTurn()
    {
        if (Phase != GamePhase.WaitingRoll)
        {
            return GameResultDTO<RollTurnResult>.Failure(
                "Bukan fase yang tepat untuk melempar dadu."
            );
        }

        IPlayer player = CurrentPlayer;

        int firstRoll = new Dice().MaxRolled;
        int secondRoll = new Dice().MaxRolled;
        int total = firstRoll + secondRoll;

        bool isDouble = firstRoll == secondRoll;

        if (player.IsInJail)
        {
            if (isDouble)
            {
                ReleaseFromJail(player);

                GameResultDTO<bool> moveResult = MovePiece(player, total);
                if (!moveResult.IsSuccess)
                {
                    return GameResultDTO<RollTurnResult>.Failure(moveResult.Error!);
                }

                GameResultDTO<ITile?> tileResult = GetCurrentTile(player);
                if (!tileResult.IsSuccess || tileResult.Data == null)
                {
                    return GameResultDTO<RollTurnResult>.Failure(tileResult.Error!);
                }

                ITile tile = tileResult.Data;

                HandleTileResultDTO handleTileResult1 = HandleTileEffectsAfterMove(player, tile);

                ICard? card = handleTileResult1.DrawnCard;
                bool requiresBuy1 = handleTileResult1.RequiresBuyDecision;
                EndTurn(false);

                return GameResultDTO<RollTurnResult>.Success(
                    new RollTurnResult(
                        total,
                        firstRoll,
                        secondRoll,
                        tile.Type.ToString(),
                        tile.Asset != null ? tile : null,
                        tile,
                        requiresBuy1,
                        card,
                        JailRollResult.Released
                    )
                );
            }
            else
            {
                player.JailTurnsRemaining--;

                if (player.JailTurnsRemaining <= 0)
                {
                    ReleaseFromJail(player);

                    GameResultDTO<bool> moveResult = MovePiece(player, total);
                    if (!moveResult.IsSuccess)
                    {
                        return GameResultDTO<RollTurnResult>.Failure(moveResult.Error!);
                    }

                    GameResultDTO<ITile?> tileResult = GetCurrentTile(player);
                    if (!tileResult.IsSuccess || tileResult.Data == null)
                    {
                        return GameResultDTO<RollTurnResult>.Failure(tileResult.Error!);
                    }

                    ITile tile = tileResult.Data;

                    HandleTileResultDTO handleTileResult2 = HandleTileEffectsAfterMove(
                        player,
                        tile
                    );

                    ICard? card = handleTileResult2.DrawnCard;
                    bool requiresBuy2 = handleTileResult2.RequiresBuyDecision;

                    EndTurn(false);

                    return GameResultDTO<RollTurnResult>.Success(
                        new RollTurnResult(
                            total,
                            firstRoll,
                            secondRoll,
                            tile.Type.ToString(),
                            tile.Asset != null ? tile : null,
                            tile,
                            requiresBuy2,
                            card,
                            JailRollResult.Released
                        )
                    );
                }
                else
                {
                    EndTurn(false);

                    return GameResultDTO<RollTurnResult>.Success(
                        new RollTurnResult(
                            total,
                            firstRoll,
                            secondRoll,
                            "Jail",
                            null,
                            null,
                            false,
                            null,
                            JailRollResult.StayedInJail
                        )
                    );
                }
            }
        }

        //Check Bankruptcy

        if (CheckBankruptcy(player))
        {
            RemovePlayer(player);
            EndTurn(false);

            return GameResultDTO<RollTurnResult>.Success(
                new RollTurnResult(0, 0, 0, "None", null, null, false, null, JailRollResult.None)
            );
        }

        if (isDouble)
        {
            player.DoubleRoll++;
        }
        else
        {
            player.DoubleRoll = 0;
        }

        if (player.DoubleRoll >= 3)
        {
            player.DoubleRoll = 0;

            GameResultDTO<bool> jailResult = SendPieceToJail(player);
            if (!jailResult.IsSuccess)
            {
                return GameResultDTO<RollTurnResult>.Failure(jailResult.Error!);
            }

            EndTurn(false);

            return GameResultDTO<RollTurnResult>.Success(
                new RollTurnResult(
                    total,
                    firstRoll,
                    secondRoll,
                    "JailTile",
                    null,
                    null,
                    false,
                    null,
                    JailRollResult.None
                )
            );
        }

        //! Move
        GameResultDTO<bool> moveResultNormal = MovePiece(player, total);
        if (!moveResultNormal.IsSuccess)
        {
            return GameResultDTO<RollTurnResult>.Failure(moveResultNormal.Error!);
        }

        GameResultDTO<ITile?> tileResultAfterMove = GetCurrentTile(player);
        if (!tileResultAfterMove.IsSuccess || tileResultAfterMove.Data == null)
        {
            return GameResultDTO<RollTurnResult>.Failure(tileResultAfterMove.Error!);
        }

        ITile landedTile = tileResultAfterMove.Data;

        HandleTileResultDTO handleTileResult = HandleTileEffectsAfterMove(player, landedTile);

        ICard? drawnCard = handleTileResult.DrawnCard;
        bool requiresBuy = handleTileResult.RequiresBuyDecision;

        if (requiresBuy)
        {
            Phase = GamePhase.WaitingBuyDecision;

            return GameResultDTO<RollTurnResult>.Success(
                new RollTurnResult(
                    total,
                    firstRoll,
                    secondRoll,
                    landedTile.Type.ToString(),
                    landedTile.Asset != null ? landedTile : null,
                    landedTile,
                    true,
                    null,
                    JailRollResult.None
                )
            );
        }

        //* END TURN LOGIC

        bool repeatTurn = isDouble;

        EndTurn(repeatTurn);

        return GameResultDTO<RollTurnResult>.Success(
            new RollTurnResult(
                total,
                firstRoll,
                secondRoll,
                landedTile.Type.ToString(),
                landedTile.Asset != null ? landedTile : null,
                landedTile,
                false,
                drawnCard,
                JailRollResult.None
            )
        );
    }

    private void EndTurn(bool repeatTurn)
    {
        EndGame();

        if (!GameEnded && !repeatTurn)
        {
            NextPlayer();
        }

        Phase = GamePhase.WaitingRoll;
    }

    private HandleTileResultDTO HandleTileEffectsAfterMove(IPlayer player, ITile tile)
    {
        if (IsPropertyAvailable(tile))
        {
            int price = tile.Asset?.Price.Value ?? 0;
            var balanceResult = GetPlayerBalance(player);

            if (balanceResult.IsSuccess && balanceResult.Data >= price)
            {
                return new HandleTileResultDTO { RequiresBuyDecision = true, DrawnCard = null };
            }
        }

        var tileResult = ExecuteTile(tile, player);

        return new HandleTileResultDTO
        {
            DrawnCard = tileResult.IsSuccess ? tileResult.Data : null,
            RequiresBuyDecision = false,
        };
    }

    public GameResultDTO<bool> MovePiece(IPlayer player, int? step = null)
    {
        if (player == null)
        {
            return GameResultDTO<bool>.Failure("Player tidak boleh null.");
        }

        if (player.DoubleRoll >= 3)
        {
            GameResultDTO<bool> jail = SendPieceToJail(player);
            if (!jail.IsSuccess)
            {
                return GameResultDTO<bool>.Failure(jail.Error!);
            }

            return GameResultDTO<bool>.Success(true);
        }

        int move;

        if (step.HasValue)
        {
            move = step.Value;
        }
        else
        {
            GameResultDTO<int> diceResult = HandleDiceRoll();
            move = diceResult.Data;
        }

        GameResultDTO<ITile?> currentTileResult = GetCurrentTile(player);
        if (!currentTileResult.IsSuccess || currentTileResult.Data == null)
        {
            return GameResultDTO<bool>.Failure(currentTileResult.Error!);
        }

        ITile currentTile = currentTileResult.Data;

        int currentIndex = Array.IndexOf(_board.Tiles, currentTile);
        int count = _board.Tiles.Length;
        int newIndex = ((currentIndex + move) % count + count) % count;

        if (move > 0 && newIndex < currentIndex)
        {
            IMoney? twoHundred = _money.FirstOrDefault(m => m.Value == MoneyValue.TWO_HUNDRED);
            if (twoHundred != null)
            {
                AddPlayerMoney(player, new Money(twoHundred.Value));
            }
        }

        GameResultDTO<bool> moveToResult = MovePieceTo(player, _board.Tiles[newIndex]);
        if (!moveToResult.IsSuccess)
        {
            return GameResultDTO<bool>.Failure(moveToResult.Error!);
        }

        GameResultDTO<ITile?> verify = GetCurrentTile(player);

        if (!verify.IsSuccess || verify.Data == null)
        {
            return GameResultDTO<bool>.Failure("Posisi piece tidak valid setelah move.");
        }

        return GameResultDTO<bool>.Success(true);
    }

    private GameResultDTO<bool> MovePieceTo(IPlayer player, ITile targetTile)
    {
        GameResultDTO<ITile?> currentTileResult = GetCurrentTile(player);
        if (!currentTileResult.IsSuccess || currentTileResult.Data == null)
        {
            return GameResultDTO<bool>.Failure(currentTileResult.Error!);
        }

        if (!_playerPiece.ContainsKey(player))
        {
            return GameResultDTO<bool>.Failure("Piece player tidak ditemukan.");
        }

        IPiece piece = _playerPiece[player];
        ITile currentTile = currentTileResult.Data;

        if (!currentTile.Pieces.Contains(piece))
        {
            return GameResultDTO<bool>.Failure("Piece tidak ada di tile asal.");
        }

        currentTile.Pieces.Remove(piece);
        targetTile.Pieces.Add(piece);

        return GameResultDTO<bool>.Success(true);
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
            if (_board.Tiles[index].Type == TileType.UtilityTile)
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
            if (_board.Tiles[index].Type == TileType.RailroadTile)
            {
                MovePieceTo(player, _board.Tiles[index]);
                break;
            }
        }
    }

    private ITile GetTileByType(TileType type)
    {
        ITile tile = _board.Tiles.First(t => t.Type == type);
        return tile;
    }

    public GameResultDTO<ITile?> GetCurrentTile(IPlayer player)
    {
        if (player == null)
        {
            return GameResultDTO<ITile?>.Failure("Player tidak boleh null.");
        }

        if (!_playerPiece.TryGetValue(player, out IPiece? piece))
        {
            return GameResultDTO<ITile?>.Failure("Piece tidak ditemukan.");
        }

        ITile? tile = _board.Tiles.FirstOrDefault(t => t.Pieces.Contains(piece));

        if (tile == null)
        {
            return GameResultDTO<ITile?>.Failure("Piece tidak berada di tile manapun.");
        }

        GameResultDTO<ITile?> successResult = GameResultDTO<ITile?>.Success(tile);

        return successResult;
    }

    private ITile GetTileByPropertyCity(PropertyCity city)
    {
        ITile tile = _board.Tiles.First(t => t.Asset != null && t.Asset.City.PropertyCity == city);
        return tile;
    }

    public ITile GetTileByCity(PropertyCity city)
    {
        ITile tile = GetTileByPropertyCity(city);
        return tile;
    }

    public GameResultDTO<bool> SendPieceToJail(IPlayer player)
    {
        if (player == null)
        {
            GameResultDTO<bool> nullResult = GameResultDTO<bool>.Failure(
                "Player tidak boleh null."
            );
            return nullResult;
        }

        GameResultDTO<ITile?> currentTileResult = GetCurrentTile(player);
        if (!currentTileResult.IsSuccess || currentTileResult.Data == null)
        {
            GameResultDTO<bool> tileFailResult = GameResultDTO<bool>.Failure(
                "Gagal mendapatkan tile saat ini."
            );
            return tileFailResult;
        }

        currentTileResult.Data.Pieces.Remove(_playerPiece[player]);

        ITile jailTile = GetTileByType(TileType.JailTile);
        jailTile.Pieces.Add(_playerPiece[player]);
        player.IsInJail = true;
        player.JailTurnsRemaining = 3;

        PlayerSentToJail?.Invoke(player, _playerPiece[player]);

        GameResultDTO<bool> successResult = GameResultDTO<bool>.Success(true);

        return successResult;
    }

    public GameResultDTO<bool> ReleaseFromJail(IPlayer player)
    {
        if (player == null)
        {
            GameResultDTO<bool> nullResult = GameResultDTO<bool>.Failure(
                "Player tidak boleh null."
            );
            return nullResult;
        }

        player.IsInJail = false;
        player.JailTurnsRemaining = 0;

        GameResultDTO<bool> successResult = GameResultDTO<bool>.Success(true);
        return successResult;
    }

    public GameResultDTO<int> HandleDiceRoll(IDice? firstDice = null, IDice? secondDice = null)
    {
        IDice dice1 = firstDice ?? new Dice();
        IDice dice2 = secondDice ?? new Dice();

        if (dice1.MaxRolled == dice2.MaxRolled)
            CurrentPlayer.DoubleRoll++;

        int total = dice1.MaxRolled + dice2.MaxRolled;
        GameResultDTO<int> result = GameResultDTO<int>.Success(total);
        return result;
    }

    public bool CheckBankruptcy(IPlayer player)
    {
        bool isBankrupt = player.IsBankrupt;
        return isBankrupt;
    }

    public bool CheckPlayerJailStatus(IPlayer player)
    {
        bool isInJail = player?.IsInJail ?? false;
        return isInJail;
    }

    private bool IsOnOwnedProperty(ITile tile)
    {
        bool isOwned = tile.Owner != null;
        return isOwned;
    }

    public GameResultDTO<bool> RemovePlayer(IPlayer player)
    {
        if (player == null)
        {
            return GameResultDTO<bool>.Failure("Player tidak boleh null.");
        }

        int index = _players.IndexOf(player);

        if (index < 0)
        {
            return GameResultDTO<bool>.Failure("Player tidak ditemukan.");
        }

        _players.RemoveAt(index);
        _playerPiece.Remove(player);
        _playerData.Remove(player);

        if (_players.Count == 0)
        {
            return GameResultDTO<bool>.Success(true);
        }

        if (index <= _currentPlayerIndex && _currentPlayerIndex > 0)
        {
            _currentPlayerIndex--;
        }

        if (_currentPlayerIndex >= _players.Count)
        {
            _currentPlayerIndex = 0;
        }

        GameResultDTO<bool> successResult = GameResultDTO<bool>.Success(true);
        return successResult;
    }

    public GameResultDTO<bool> SubstractPlayerMoney(IPlayer player, IMoney money)
    {
        if (player == null)
        {
            GameResultDTO<bool> nullPlayerResult = GameResultDTO<bool>.Failure(
                "Player tidak boleh null."
            );
            return nullPlayerResult;
        }
        if (money == null)
        {
            GameResultDTO<bool> nullMoneyResult = GameResultDTO<bool>.Failure(
                "Money tidak boleh null."
            );
            return nullMoneyResult;
        }
        if (!_playerData.ContainsKey(player))
        {
            GameResultDTO<bool> notFoundResult = GameResultDTO<bool>.Failure(
                "Data player tidak ditemukan."
            );
            return notFoundResult;
        }

        int currentMoney = _playerData[player].Sum(m => m.Value);
        if (currentMoney < money.Value)
        {
            player.IsBankrupt = true;
            PlayerBankrupt?.Invoke(player);
            GameResultDTO<bool> insufficientResult = GameResultDTO<bool>.Failure(
                $"Uang tidak cukup. Saldo saat ini: {currentMoney}, dibutuhkan: {money.Value}."
            );
            return insufficientResult;
        }

        _playerData[player].Add(new Money(-money.Value));

        GameResultDTO<bool> successResult = GameResultDTO<bool>.Success(true);
        return successResult;
    }

    public GameResultDTO<bool> TransferPlayerMoney(IPlayer from, IPlayer to, IMoney money)
    {
        if (from == null || to == null)
        {
            GameResultDTO<bool> nullResult = GameResultDTO<bool>.Failure(
                "Player tidak boleh null."
            );
            return nullResult;
        }
        if (money == null)
        {
            GameResultDTO<bool> nullMoneyResult = GameResultDTO<bool>.Failure(
                "Money tidak boleh null."
            );
            return nullMoneyResult;
        }
        if (!_playerData.ContainsKey(from) || !_playerData.ContainsKey(to))
        {
            GameResultDTO<bool> notFoundResult = GameResultDTO<bool>.Failure(
                "Data player tidak ditemukan."
            );
            return notFoundResult;
        }

        int fromMoney = _playerData[from].Sum(m => m.Value);
        if (fromMoney < money.Value)
        {
            GameResultDTO<bool> insufficientResult = GameResultDTO<bool>.Failure(
                $"Uang tidak cukup untuk transfer. Saldo {from.Name}: {fromMoney}, dibutuhkan: {money.Value}."
            );
            return insufficientResult;
        }

        _playerData[from].Add(new Money(-money.Value));
        _playerData[to].Add(new Money(money.Value));

        GameResultDTO<bool> successResult = GameResultDTO<bool>.Success(true);
        return successResult;
    }

    public GameResultDTO<bool> AddPlayerMoney(IPlayer player, IMoney money)
    {
        if (player == null)
        {
            GameResultDTO<bool> nullPlayerResult = GameResultDTO<bool>.Failure(
                "Player tidak boleh null."
            );
            return nullPlayerResult;
        }
        if (money == null)
        {
            GameResultDTO<bool> nullMoneyResult = GameResultDTO<bool>.Failure(
                "Money tidak boleh null."
            );
            return nullMoneyResult;
        }
        if (!_playerData.ContainsKey(player))
        {
            GameResultDTO<bool> notFoundResult = GameResultDTO<bool>.Failure(
                "Data player tidak ditemukan."
            );
            return notFoundResult;
        }

        _playerData[player].Add(new Money(money.Value));

        GameResultDTO<bool> successResult = GameResultDTO<bool>.Success(true);
        return successResult;
    }

    public GameResultDTO<int> GetPlayerBalance(IPlayer player)
    {
        if (player == null)
        {
            GameResultDTO<int> nullResult = GameResultDTO<int>.Failure("Player tidak boleh null.");
            return nullResult;
        }
        if (!_playerData.ContainsKey(player))
        {
            GameResultDTO<int> notFoundResult = GameResultDTO<int>.Failure(
                "Data player tidak ditemukan."
            );
            return notFoundResult;
        }

        int balance = _playerData[player].Sum(m => m.Value);
        GameResultDTO<int> balanceResult = GameResultDTO<int>.Success(balance);
        return balanceResult;
    }

    public GameResultDTO<bool> AttemptBuyCurrentProperty(IPlayer player, bool wantsToBuy)
    {
        if (!wantsToBuy)
        {
            GameResultDTO<bool> skipResult = GameResultDTO<bool>.Success(false);
            return skipResult;
        }

        GameResultDTO<ITile?> tileResult = GetCurrentTile(player);
        if (!tileResult.IsSuccess || tileResult.Data == null)
        {
            GameResultDTO<bool> tileFailResult = GameResultDTO<bool>.Failure(
                "Gagal mendapatkan tile saat ini."
            );
            return tileFailResult;
        }

        ITile tile = tileResult.Data;

        if (!IsPropertyAvailable(tile))
        {
            GameResultDTO<bool> unavailableResult = GameResultDTO<bool>.Failure(
                "Properti ini tidak tersedia untuk dibeli."
            );
            return unavailableResult;
        }

        int price = tile.Asset!.Price.Value;

        GameResultDTO<int> balanceResult = GetPlayerBalance(player);
        if (!balanceResult.IsSuccess)
        {
            GameResultDTO<bool> balanceFailResult = GameResultDTO<bool>.Failure(
                balanceResult.Error!
            );
            return balanceFailResult;
        }

        if (balanceResult.Data < price)
        {
            GameResultDTO<bool> insufficientResult = GameResultDTO<bool>.Failure(
                $"Uang tidak cukup untuk membeli properti. Saldo: {balanceResult.Data}, harga: {price}."
            );
            return insufficientResult;
        }

        GameResultDTO<bool> subtractResult = SubstractPlayerMoney(player, new Money(price));
        if (!subtractResult.IsSuccess)
        {
            GameResultDTO<bool> subtractFailResult = GameResultDTO<bool>.Failure(
                subtractResult.Error!
            );
            return subtractFailResult;
        }

        tile.Owner = player;

        GameResultDTO<bool> successResult = GameResultDTO<bool>.Success(true);
        return successResult;
    }

    public GameResultDTO<bool> HandleBuyDecision(bool wantsToBuy)
    {
        if (Phase != GamePhase.WaitingBuyDecision)
        {
            GameResultDTO<bool> wrongPhaseResult = GameResultDTO<bool>.Failure(
                "Bukan fase yang tepat untuk keputusan beli properti."
            );
            return wrongPhaseResult;
        }

        AttemptBuyCurrentProperty(CurrentPlayer, wantsToBuy);

        // Check if game ended?
        EndGame();

        //private field GameEnded
        if (!GameEnded)
            NextPlayer();
        Phase = GamePhase.WaitingRoll;

        GameResultDTO<bool> successResult = GameResultDTO<bool>.Success(true);
        return successResult;
    }

    public GameResultDTO<bool> BuyBuilding(IPlayer player, PropertyCity city, bool buildHotel)
    {
        if (player == null)
        {
            GameResultDTO<bool> nullResult = GameResultDTO<bool>.Failure(
                "Player tidak boleh null."
            );
            return nullResult;
        }

        ITile tile = GetTileByCity(city);

        if (tile.Asset == null)
        {
            GameResultDTO<bool> noAssetResult = GameResultDTO<bool>.Failure(
                "Tile ini tidak memiliki aset."
            );
            return noAssetResult;
        }

        if (tile.Owner == null || !tile.Owner.Equals(player))
        {
            GameResultDTO<bool> notOwnerResult = GameResultDTO<bool>.Failure(
                $"Properti {city} bukan milik {player.Name}."
            );
            return notOwnerResult;
        }

        Color? color = tile.Asset.Color;
        if (color == null)
        {
            GameResultDTO<bool> noColorResult = GameResultDTO<bool>.Failure(
                "Properti ini tidak memiliki warna (tidak bisa dibangun)."
            );
            return noColorResult;
        }

        List<ITile> sameColorTiles = _board.Tiles.Where(t => t.Asset?.Color == color).ToList();
        bool hasMonopoly = sameColorTiles.All(t => t.Owner != null && t.Owner.Equals(player));
        if (!hasMonopoly)
        {
            GameResultDTO<bool> noMonopolyResult = GameResultDTO<bool>.Failure(
                $"Pemain harus memiliki monopoli warna {color} untuk membangun."
            );
            return noMonopolyResult;
        }

        int housePrice = GetHousePrice(tile.Asset);
        int currentHouses = tile.House ?? 0;
        bool hasHotel = tile.HasHotel ?? false;

        if (hasHotel)
        {
            GameResultDTO<bool> hotelExistsResult = GameResultDTO<bool>.Failure(
                "Tile ini sudah memiliki hotel, tidak bisa dibangun lagi."
            );
            return hotelExistsResult;
        }

        if (buildHotel)
        {
            if (currentHouses < 3)
            {
                GameResultDTO<bool> notEnoughHousesResult = GameResultDTO<bool>.Failure(
                    $"Dibutuhkan minimal 3 rumah sebelum membangun hotel. Saat ini: {currentHouses}."
                );
                return notEnoughHousesResult;
            }

            GameResultDTO<bool> subtractResult = SubstractPlayerMoney(
                player,
                new Money(housePrice * 5)
            );
            if (!subtractResult.IsSuccess)
            {
                GameResultDTO<bool> subtractFailResult = GameResultDTO<bool>.Failure(
                    subtractResult.Error!
                );
                return subtractFailResult;
            }

            tile.House = 0;
            tile.HasHotel = true;
        }
        else
        {
            if (currentHouses >= 3)
            {
                GameResultDTO<bool> maxHousesResult = GameResultDTO<bool>.Failure(
                    "Sudah mencapai maksimum 3 rumah. Bangun hotel sebagai gantinya."
                );
                return maxHousesResult;
            }

            GameResultDTO<bool> subtractResult = SubstractPlayerMoney(
                player,
                new Money(housePrice)
            );
            if (!subtractResult.IsSuccess)
            {
                GameResultDTO<bool> subtractFailResult = GameResultDTO<bool>.Failure(
                    subtractResult.Error!
                );
                return subtractFailResult;
            }

            tile.House = currentHouses + 1;
        }

        GameResultDTO<bool> successResult = GameResultDTO<bool>.Success(true);
        return successResult;
    }

    public GameResultDTO<int> SellAllAssetsToBank(IPlayer player)
    {
        if (player == null)
        {
            GameResultDTO<int> nullResult = GameResultDTO<int>.Failure("Player tidak boleh null.");
            return nullResult;
        }

        int totalIncome = 0;
        foreach (ITile tile in GetPlayerProperties(player).ToList())
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

        GameResultDTO<int> successResult = GameResultDTO<int>.Success(totalIncome);
        return successResult;
    }

    private int GetHousePrice(IAsset asset)
    {
        int price = asset.Color switch
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
        return price;
    }

    public GameResultDTO<int> SellBuildingsToBank(SendBuildingToBankResult sellInfo)
    {
        if (sellInfo == null)
        {
            GameResultDTO<int> nullResult = GameResultDTO<int>.Failure(
                "Info penjualan bangunan tidak boleh null."
            );
            return nullResult;
        }

        ITile tile = GetTileByCity(sellInfo.City);

        if (tile.Owner == null || !tile.Owner.Equals(sellInfo.Player))
        {
            GameResultDTO<int> notOwnerResult = GameResultDTO<int>.Failure(
                $"Properti {sellInfo.City} bukan milik {sellInfo.Player?.Name}."
            );
            return notOwnerResult;
        }
        if (tile.Asset == null)
        {
            GameResultDTO<int> noAssetResult = GameResultDTO<int>.Failure(
                "Tile ini tidak memiliki aset."
            );
            return noAssetResult;
        }

        int houseCount = tile.House ?? 0;
        bool hasHotel = tile.HasHotel ?? false;

        if (!hasHotel && houseCount == 0)
        {
            GameResultDTO<int> noBuildingResult = GameResultDTO<int>.Failure(
                "Tidak ada bangunan untuk dijual di properti ini."
            );
            return noBuildingResult;
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
            GameResultDTO<int> tooManyResult = GameResultDTO<int>.Failure(
                $"Jumlah rumah yang dijual ({sellInfo.HousesToSell}) melebihi yang tersedia ({houseCount})."
            );
            return tooManyResult;
        }

        if (sellInfo.HousesToSell > 0)
        {
            tile.House = houseCount - sellInfo.HousesToSell;
            soldValue += houseSellPrice * sellInfo.HousesToSell;
        }

        if (soldValue > 0)
            AddPlayerMoney(sellInfo.Player, new Money(soldValue));

        GameResultDTO<int> successResult = GameResultDTO<int>.Success(soldValue);
        return successResult;
    }

    public GameResultDTO<int> SellPropertyToBank(
        IPlayer owner,
        PropertyCity city,
        bool includeBuildings = true
    )
    {
        if (owner == null)
        {
            GameResultDTO<int> nullResult = GameResultDTO<int>.Failure("Owner tidak boleh null.");
            return nullResult;
        }

        ITile tile = GetTileByCity(city);

        if (tile.Owner == null || !tile.Owner.Equals(owner))
        {
            GameResultDTO<int> notOwnerResult = GameResultDTO<int>.Failure(
                $"Properti {city} bukan milik {owner.Name}."
            );
            return notOwnerResult;
        }
        if (tile.Asset == null)
        {
            GameResultDTO<int> noAssetResult = GameResultDTO<int>.Failure(
                "Tile ini tidak memiliki aset."
            );
            return noAssetResult;
        }

        int totalIncome = 0;
        int houses = tile.House ?? 0;
        bool hasHotel = tile.HasHotel ?? false;

        if ((houses > 0 || hasHotel) && !includeBuildings)
        {
            GameResultDTO<int> hasBuildingResult = GameResultDTO<int>.Failure(
                "Properti masih memiliki bangunan. Jual bangunan terlebih dahulu atau gunakan includeBuildings=true."
            );
            return hasBuildingResult;
        }

        if (includeBuildings)
        {
            if (hasHotel)
            {
                GameResultDTO<int> hotelResult = SellBuildingsToBank(
                    new SendBuildingToBankResult(owner, city, 0, true)
                );
                if (hotelResult.IsSuccess)
                    totalIncome += hotelResult.Data;
            }

            houses = tile.House ?? 0;

            if (houses > 0)
            {
                GameResultDTO<int> houseResult = SellBuildingsToBank(
                    new SendBuildingToBankResult(owner, city, houses, false)
                );
                if (houseResult.IsSuccess)
                    totalIncome += houseResult.Data;
            }
        }

        int propertySellValue = tile.Asset.Price.Value / 2;
        tile.Owner = null;
        tile.House = 0;
        tile.HasHotel = false;

        AddPlayerMoney(owner, new Money(propertySellValue));
        totalIncome += propertySellValue;

        GameResultDTO<int> successResult = GameResultDTO<int>.Success(totalIncome);
        return successResult;
    }

    public bool IsPropertyAvailable(ITile tile)
    {
        bool isAvailable = tile?.Asset != null && tile.Owner == null;
        return isAvailable;
    }

    public ICard? DrawCard(TileType drawType)
    {
        List<ICard> candidates = _cards
            .Where(card =>
                (drawType == TileType.DrawChance && card is ChanceCard)
                || (drawType == TileType.DrawCommunity && card is CommunityCard)
            )
            .ToList();

        if (candidates.Count == 0)
            return null;

        ICard chosenCard = candidates[new Random().Next(candidates.Count)];
        return chosenCard;
    }

    public GameResultDTO<bool> ExecuteCard(ICard card, IPlayer player)
    {
        if (card == null)
        {
            GameResultDTO<bool> nullCardResult = GameResultDTO<bool>.Failure(
                "Card tidak boleh null."
            );
            return nullCardResult;
        }
        if (player == null)
        {
            GameResultDTO<bool> nullPlayerResult = GameResultDTO<bool>.Failure(
                "Player tidak boleh null."
            );
            return nullPlayerResult;
        }

        GameResultDTO<bool> executeResult = card switch
        {
            CommunityCard => ExecuteCommunityCard(card, player),
            ChanceCard => ExecuteChanceCard(card, player),
            _ => GameResultDTO<bool>.Failure("Tipe card tidak dikenali."),
        };
        return executeResult;
    }

    private int HandleStreetRepairs(IPlayer player)
    {
        if (player == null)
        {
            return 0;
        }

        int totalCost = _board
            .Tiles.Where(t => t.Owner != null && t.Owner.Equals(player) && t.Asset != null)
            .Sum(t =>
                (t.House ?? 0) * BuildingValue.HOUSE_COST
                + (t.HasHotel == true ? BuildingValue.HOTEL_COST : 0)
            );

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
                AddPlayerMoney(player, new Money(MoneyValue.FIFTY));
                break;
            case CardBehaviour.DoctorFees:
                SubstractPlayerMoney(player, new Money(MoneyValue.FIFTY));
                break;
            case CardBehaviour.FromSaleOfStock:
                AddPlayerMoney(player, new Money(MoneyValue.FIFTY));
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
                AddPlayerMoney(player, new Money(MoneyValue.HUNDRED));
                break;
            case CardBehaviour.IncomeTaxRefund:
                AddPlayerMoney(player, new Money(MoneyValue.HUNDRED));
                break;
            case CardBehaviour.Birthday:
                foreach (IPlayer p in _players)
                    if (!p.Equals(player))
                    {
                        TransferPlayerMoney(p, player, new Money(MoneyValue.TEN));
                    }
                break;
            case CardBehaviour.LifeInsuranceMatures:
                AddPlayerMoney(player, new Money(MoneyValue.HUNDRED));
                break;
            case CardBehaviour.PayHospitalFees:
                SubstractPlayerMoney(player, new Money(MoneyValue.HUNDRED));
                break;
            case CardBehaviour.PaySchoolFees:
                SubstractPlayerMoney(player, new Money(MoneyValue.FIFTY));
                break;
            case CardBehaviour.ConsultancyFee:
                AddPlayerMoney(player, new Money(MoneyValue.TWENTY + MoneyValue.FIVE));
                break;
            case CardBehaviour.StreetRepairs:
                int repairCost = HandleStreetRepairs(player);
                SubstractPlayerMoney(player, new Money(repairCost));
                break;
            case CardBehaviour.BeautyContestPrize:
                AddPlayerMoney(player, new Money(MoneyValue.HUNDRED));
                break;
            case CardBehaviour.InheritMoney:
                AddPlayerMoney(player, new Money(MoneyValue.HUNDRED));
                break;
            default:
                GameResultDTO<bool> unknownResult = GameResultDTO<bool>.Failure(
                    $"CardBehaviour {card.Behaviour} tidak dikenali untuk Community Card."
                );
                return unknownResult;
        }

        GameResultDTO<bool> successResult = GameResultDTO<bool>.Success(true);
        return successResult;
    }

    private GameResultDTO<bool> ExecuteChanceCard(ICard card, IPlayer player)
    {
        switch (card.Behaviour)
        {
            case CardBehaviour.AdvanceToGo:
                MovePieceTo(player, GetTileByType(TileType.StartTile));
                AddPlayerMoney(player, new Money(MoneyValue.TWO_HUNDRED));
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
                AddPlayerMoney(player, new Money(MoneyValue.FIFTY));
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
                int repairCost = HandleStreetRepairs(player);
                SubstractPlayerMoney(player, new Money(repairCost));
                break;
            case CardBehaviour.PayPoorTax:
                SubstractPlayerMoney(player, new Money(MoneyValue.TEN + MoneyValue.FIVE));
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
                    {
                        TransferPlayerMoney(player, p, new Money(MoneyValue.FIFTY));
                    }
                break;
            case CardBehaviour.YourBuildingLoanMatures:
                AddPlayerMoney(player, new Money(MoneyValue.HUNDRED + MoneyValue.FIFTY));
                break;
            default:
                GameResultDTO<bool> unknownResult = GameResultDTO<bool>.Failure(
                    $"CardBehaviour {card.Behaviour} tidak dikenali untuk Chance Card."
                );
                return unknownResult;
        }

        GameResultDTO<bool> successResult = GameResultDTO<bool>.Success(true);
        return successResult;
    }

    public GameResultDTO<ICard?> ExecuteTile(ITile tile, IPlayer player, bool doubleRent = false)
    {
        if (tile == null)
        {
            GameResultDTO<ICard?> nullTileResult = GameResultDTO<ICard?>.Failure(
                "Tile tidak boleh null."
            );
            return nullTileResult;
        }
        if (player == null)
        {
            GameResultDTO<ICard?> nullPlayerResult = GameResultDTO<ICard?>.Failure(
                "Player tidak boleh null."
            );
            return nullPlayerResult;
        }

        switch (tile.Type)
        {
            case TileType.RentTile:
            case TileType.UtilityTile:
            case TileType.RailroadTile:
                if (IsOnOwnedProperty(tile) && !tile.Owner!.Equals(player))
                {
                    int rent = Math.Max(10, tile.Asset!.Price.Value / 10);
                    if (doubleRent)
                        rent *= 2;

                    GameResultDTO<bool> subtractResult = SubstractPlayerMoney(
                        player,
                        new Money(rent)
                    );
                    if (subtractResult.IsSuccess && !player.IsBankrupt)
                        AddPlayerMoney(tile.Owner, new Money(rent));
                }
                GameResultDTO<ICard?> rentResult = GameResultDTO<ICard?>.Success(null);
                return rentResult;

            case TileType.TaxTile:
            case TileType.PayTaxTile:
                SubstractPlayerMoney(player, new Money(MoneyValue.HUNDRED));
                GameResultDTO<ICard?> taxResult = GameResultDTO<ICard?>.Success(null);
                return taxResult;

            case TileType.GoToJailTile:
                SendPieceToJail(player);
                GameResultDTO<ICard?> jailResult = GameResultDTO<ICard?>.Success(null);
                return jailResult;

            case TileType.DrawChance:
                ICard? chanceCard = DrawCard(tile.Type);
                if (chanceCard != null)
                {
                    ExecuteCard(chanceCard, player);
                }
                GameResultDTO<ICard?> chanceResult = GameResultDTO<ICard?>.Success(chanceCard);
                return chanceResult;

            case TileType.DrawCommunity:
                ICard? communityCard = DrawCard(tile.Type);
                if (communityCard != null)
                {
                    ExecuteCard(communityCard, player);
                }
                GameResultDTO<ICard?> communityResult = GameResultDTO<ICard?>.Success(
                    communityCard
                );
                return communityResult;

            default:
                GameResultDTO<ICard?> unknownResult = GameResultDTO<ICard?>.Failure(
                    $"Tipe tile '{tile.Type}' tidak memiliki efek yang dapat dieksekusi."
                );
                return unknownResult;
        }
    }

    public List<ITile> GetPlayerProperties(IPlayer player)
    {
        List<ITile> properties = _board
            .Tiles.Where(t => t.Owner != null && t.Owner.Equals(player))
            .ToList();
        return properties;
    }

    public GameResultDTO<IPlayer?> GetWinnerOrNull()
    {
        List<IPlayer> activePlayers = _players.Where(p => !p.IsBankrupt).ToList();

        if (activePlayers.Count == 1)
        {
            GameResultDTO<IPlayer?> winnerResult = GameResultDTO<IPlayer?>.Success(
                activePlayers[0]
            );
            return winnerResult;
        }

        GameResultDTO<IPlayer?> noWinnerResult = GameResultDTO<IPlayer?>.Failure(
            "Belum ada pemenang."
        );
        return noWinnerResult;
    }

    public IPlayer? FindPlayerByName(string playerName)
    {
        IPlayer? player = _players.FirstOrDefault(p =>
            p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase)
        );
        return player;
    }

    public bool IsPropertyOwnedBy(ITile tile, IPlayer player)
    {
        bool isOwned = tile.Owner != null && tile.Owner.Equals(player);
        return isOwned;
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
