namespace GameService.Tests;

using Backend.Domain.DTOs;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Domain.Interfaces;
using Backend.Factories;

[TestFixture]
public class GameService_FullTest
{
    private Game _game;

    [SetUp]
    public void Setup()
    {
        IBoard board = BoardFactory.CreateBoard();
        List<IPlayer> players = PlayerFactory.CreatePlayers([
            "Player1",
            "Player2",
            "Player3",
            "Player4",
        ]);
        List<IPiece> pieces = PieceFactory.CreateStandardPieces();
        List<ICard> cards = CardFactory.CreateDefaultCards();
        List<IMoney> money = MoneyFactory.CreateMoney();
        List<IDice> dice = new List<IDice> { new FakeDice(6), new FakeDice(6) };
        _game = new Game(board, players, pieces, cards, money, dice);
    }

    private static void SetGameEnded(Game game, bool value)
    {
        var field = typeof(Game).GetField(
            "_gameEnded",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        field!.SetValue(game, value);
    }

    private static void SetGamePhase(Game game, GamePhase phase)
    {
        var prop = typeof(Game).GetProperty(
            "Phase",
            System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic
        );
        prop!.SetValue(game, phase);
    }

    private IPlayer CurrentPlayerWithPiece()
    {
        IPlayer player = _game.CurrentPlayer;
        _game.AssignPieceToPlayer(player, _game.Pieces.First().Type);
        return player;
    }

    [Test]
    public void IsPieceAvailable_ReturnsTrue_WhenPieceNotAssigned()
    {
        Assert.That(_game.IsPieceAvailable(_game.Pieces.First().Type), Is.True);
    }

    [Test]
    public void IsPieceAvailable_ReturnsFalse_WhenPieceAssigned()
    {
        _game.AssignPieceToPlayer(_game.Players.First(), _game.Pieces.First().Type);
        Assert.That(_game.IsPieceAvailable(_game.Pieces.First().Type), Is.False);
    }

    [Test]
    public void AssignPiece_Success_WhenValidPlayerAndPiece()
    {
        GameResultDTO<bool> result = _game.AssignPieceToPlayer(_game.Players.First(), _game.Pieces.First().Type);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.True);
    }

    [Test]
    public void AssignPiece_Failure_WhenPlayerIsNull()
    {
        GameResultDTO<bool> result = _game.AssignPieceToPlayer(null, _game.Pieces.First().Type);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Player tidak boleh null."));
    }

    [Test]
    public void AssignPiece_Failure_WhenPlayerNotInGame()
    {
        IPlayer stranger = new Player("Stranger");
        GameResultDTO<bool> result = _game.AssignPieceToPlayer(stranger, _game.Pieces.First().Type);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Player tidak ditemukan dalam game ini."));
    }

    [Test]
    public void AssignPiece_Failure_WhenPieceAlreadyTaken()
    {
        _game.AssignPieceToPlayer(_game.Players[0], _game.Pieces.First().Type);
        GameResultDTO<bool> result = _game.AssignPieceToPlayer(_game.Players[1], _game.Pieces.First().Type);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("sudah diambil oleh pemain lain"));
    }

    [Test]
    public void AssignPiece_Failure_WhenPieceTypeInvalid()
    {
        GameResultDTO<bool> result = _game.AssignPieceToPlayer(_game.Players.First(), PieceType.Tophat + 999);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("tidak ditemukan"));
    }

    [Test]
    public void AssignPiece_Failure_WhenPlayerAlreadyHasPiece()
    {
        _game.AssignPieceToPlayer(_game.Players.First(), _game.Pieces[0].Type);
        GameResultDTO<bool> result = _game.AssignPieceToPlayer(_game.Players.First(), _game.Pieces[1].Type);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Player sudah memiliki piece."));
    }

    [Test]
    public void GetPiece_ReturnsCorrectPiece_WhenAssigned()
    {
        IPlayer player = _game.Players.First();
        IPiece piece = _game.Pieces.First();
        _game.AssignPieceToPlayer(player, piece.Type);

        GameResultDTO<IPiece> result = _game.GetPiece(player);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.EqualTo(piece));
    }

    [Test]
    public void GetPiece_Failure_WhenPlayerIsNull()
    {
        GameResultDTO<IPiece> result = _game.GetPiece(null);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Player tidak boleh kosong."));
    }

    [Test]
    public void GetPiece_Failure_WhenPlayerHasNoPiece()
    {
        GameResultDTO<IPiece> result = _game.GetPiece(_game.Players.First());
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Player tidak memiliki piece yang di-assign."));
    }

    [Test]
    public void NextPlayer_AdvancesIndex()
    {
        int start = _game.CurrentPlayerIndex;
        _game.NextPlayer();
        Assert.That(_game.CurrentPlayerIndex, Is.EqualTo((start + 1) % _game.Players.Count));
    }

    [Test]
    public void NextPlayer_DoesNotAdvance_WhenGameEnded()
    {
        int start = _game.CurrentPlayerIndex;
        SetGameEnded(_game, true);
        _game.NextPlayer();
        Assert.That(_game.CurrentPlayerIndex, Is.EqualTo(start));
    }

    [Test]
    public void NextPlayer_SkipsBankruptPlayer()
    {
        _game.Players[1].IsBankrupt = true;
        int start = _game.CurrentPlayerIndex;
        _game.NextPlayer();
        Assert.That(_game.CurrentPlayerIndex, Is.EqualTo((start + 2) % _game.Players.Count));
    }

    [Test]
    public void NextPlayer_WrapsAround_WhenAtLastPlayer()
    {
        // advance to last player
        for (int i = 0; i < _game.Players.Count - 1; i++)
            _game.NextPlayer();

        int last = _game.CurrentPlayerIndex;
        _game.NextPlayer();
        Assert.That(_game.CurrentPlayerIndex, Is.EqualTo(0));
    }


    [Test]
    public void RollTurn_Success_WhenNormalConditions()
    {
        CurrentPlayerWithPiece();
        GameResultDTO<RollTurnResult> result = _game.RollTurn();
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.Not.Null);
    }

    [Test]
    public void RollTurn_Failure_WhenWrongPhase()
    {
        SetGamePhase(_game, GamePhase.WaitingBuyDecision);
        GameResultDTO<RollTurnResult> result = _game.RollTurn();
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Bukan fase yang tepat untuk melempar dadu."));
    }

    [Test]
    public void RollTurn_Success_WhenPlayerIsInJail()
    {
        IPlayer player = CurrentPlayerWithPiece();
        player.IsInJail = true;
        GameResultDTO<RollTurnResult> result = _game.RollTurn();
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void RollTurn_ReleasedFromJail_WhenRollsDouble()
    {
        IPlayer player = CurrentPlayerWithPiece();
        player.IsInJail = true;
        player.JailTurnsRemaining = 3;

        GameResultDTO<RollTurnResult> result = _game.RollTurn();
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data!.JailRollResult, Is.EqualTo(JailRollResult.Released));
        Assert.That(player.IsInJail, Is.False);
    }

    [Test]
    public void RollTurn_StaysInJail_WhenNoDoubleAndTurnsRemain()
    {

        IBoard board = BoardFactory.CreateBoard();
        List<IPlayer> players = PlayerFactory.CreatePlayers(["P1", "P2"]);
        List<IPiece> pieces = PieceFactory.CreateStandardPieces();
        List<ICard> cards = CardFactory.CreateDefaultCards();
        List<IMoney> money = MoneyFactory.CreateMoney();
        List<IDice> dice = new List<IDice> { new FakeDice(3), new FakeDice(4) }; // 3+4, not double
        Game game = new Game(board, players, pieces, cards, money, dice);

        IPlayer player = game.CurrentPlayer;
        game.AssignPieceToPlayer(player, game.Pieces.First().Type);
        player.IsInJail = true;
        player.JailTurnsRemaining = 2;

        GameResultDTO<RollTurnResult> result = game.RollTurn();
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data!.JailRollResult, Is.EqualTo(JailRollResult.StayedInJail));
        Assert.That(player.IsInJail, Is.True);
        Assert.That(player.JailTurnsRemaining, Is.EqualTo(1));
    }

    [Test]
    public void RollTurn_ReleasesAndMoves_WhenJailTurnsReachZero()
    {
        IBoard board = BoardFactory.CreateBoard();
        List<IPlayer> players = PlayerFactory.CreatePlayers(["P1", "P2"]);
        List<IPiece> pieces = PieceFactory.CreateStandardPieces();
        List<ICard> cards = CardFactory.CreateDefaultCards();
        List<IMoney> money = MoneyFactory.CreateMoney();
        List<IDice> dice = new List<IDice> { new FakeDice(3), new FakeDice(4) };
        Game game = new Game(board, players, pieces, cards, money, dice);

        IPlayer player = game.CurrentPlayer;
        game.AssignPieceToPlayer(player, game.Pieces.First().Type);
        player.IsInJail = true;
        player.JailTurnsRemaining = 1;
        GameResultDTO<RollTurnResult> result = game.RollTurn();
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data!.JailRollResult, Is.EqualTo(JailRollResult.Released));
    }

    [Test]
    public void RollTurn_SendsToJail_AfterTripleDouble()
    {
        IPlayer player = CurrentPlayerWithPiece();
        player.DoubleRoll = 2;
        GameResultDTO<RollTurnResult> result = _game.RollTurn();
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(player.IsInJail, Is.True);
    }

    [Test]
    public void RollTurn_HandlesBankruptPlayer_ReturnsZeroDice()
    {
        IPlayer player = CurrentPlayerWithPiece();
        player.IsBankrupt = true;

        GameResultDTO<RollTurnResult> result = _game.RollTurn();
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data!.DiceTotal, Is.EqualTo(0));
    }

    [Test]
    public void RollTurn_SetsPhaseToWaitingBuyDecision_WhenLandOnAvailableProperty()
    {

        IPlayer player = CurrentPlayerWithPiece();
        GameResultDTO<RollTurnResult> result = _game.RollTurn();
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void RollTurn_Failure_WhenMovePieceFails()
    {

        IPlayer player = _game.CurrentPlayer;
        GameResultDTO<RollTurnResult> result = _game.RollTurn();
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Piece tidak ditemukan."));
    }

    [Test]
    public void RollTurn_HandlesBankruptPlayerAfterMove()
    {

        IPlayer player = CurrentPlayerWithPiece();
        IPlayer owner = _game.Players[1];
        _game.AssignPieceToPlayer(owner, _game.Pieces[1].Type);

        ITile propertyTile = _game.Board.Tiles.First(t => t.Asset != null && t.Type == TileType.RentTile);
        _game.MovePieceTo(owner, propertyTile);
        _game.AttemptBuyCurrentProperty(owner, true);

        _game.SubstractPlayerMoney(player, new Money(_game.GetPlayerBalance(player).Data - 10));
        _game.MovePieceTo(player, propertyTile);


        GameResultDTO<RollTurnResult> result = _game.RollTurn();
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void MovePieceTo_Success_WhenValidPlayerAndTile()
    {
        IPlayer player = CurrentPlayerWithPiece();
        ITile target = _game.Board.Tiles[3];

        GameResultDTO<bool> result = _game.MovePieceTo(player, target);
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void MovePieceTo_Failure_WhenPlayerHasNoPiece()
    {
        IPlayer player = _game.Players.First();
        ITile target = _game.Board.Tiles[3];

        GameResultDTO<bool> result = _game.MovePieceTo(player, target);
        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public void MovePieceTo_PieceAppearsOnTargetTile()
    {
        IPlayer player = CurrentPlayerWithPiece();
        IPiece piece = _game.GetPiece(player).Data!;
        ITile target = _game.Board.Tiles[5];

        _game.MovePieceTo(player, target);
        Assert.That(target.Pieces.Contains(piece), Is.True);
    }

    [Test]
    public void MoveToNearestUtility_MovesPlayerToUtilityTile()
    {
        IPlayer player = CurrentPlayerWithPiece();
        _game.MoveToNearestUtility(player);

        GameResultDTO<ITile?> tileResult = _game.GetCurrentTile(player);
        Assert.That(tileResult.IsSuccess, Is.True);
        Assert.That(tileResult.Data!.Type, Is.EqualTo(TileType.UtilityTile));
    }

    [Test]
    public void MoveToNearestUtility_DoesNotThrow_WhenPlayerHasNoPiece()
    {
        IPlayer player = _game.Players.First();
        Assert.DoesNotThrow(() => _game.MoveToNearestUtility(player));
    }

    [Test]
    public void GetCurrentTile_Success_WhenPieceAssigned()
    {
        IPlayer player = CurrentPlayerWithPiece();
        GameResultDTO<ITile?> result = _game.GetCurrentTile(player);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.Not.Null);
    }

    [Test]
    public void GetCurrentTile_Failure_WhenPlayerIsNull()
    {
        GameResultDTO<ITile?> result = _game.GetCurrentTile(null);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Player tidak boleh null."));
    }

    [Test]
    public void GetCurrentTile_Failure_WhenNoPieceAssigned()
    {
        GameResultDTO<ITile?> result = _game.GetCurrentTile(_game.Players.First());
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("Piece tidak ditemukan"));
    }

    [Test]
    public void SendPieceToJail_Success_AndPlayerIsInJail()
    {
        IPlayer player = CurrentPlayerWithPiece();
        GameResultDTO<bool> result = _game.SendPieceToJail(player);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(player.IsInJail, Is.True);
        Assert.That(player.JailTurnsRemaining, Is.EqualTo(3));
    }

    [Test]
    public void SendPieceToJail_Failure_WhenPlayerIsNull()
    {
        GameResultDTO<bool> result = _game.SendPieceToJail(null);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Player tidak boleh null."));
    }

    [Test]
    public void SendPieceToJail_Failure_WhenPlayerHasNoPiece()
    {
        GameResultDTO<bool> result = _game.SendPieceToJail(_game.Players.First());
        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public void SendPieceToJail_RaisesPlayerSentToJailEvent()
    {
        IPlayer player = CurrentPlayerWithPiece();
        bool eventFired = false;
        _game.PlayerSentToJail += (p, _) => eventFired = true;

        _game.SendPieceToJail(player);
        Assert.That(eventFired, Is.True);
    }

    [Test]
    public void ReleaseFromJail_Success_ClearsJailStatus()
    {
        IPlayer player = CurrentPlayerWithPiece();
        _game.SendPieceToJail(player);

        GameResultDTO<bool> result = _game.ReleaseFromJail(player);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(player.IsInJail, Is.False);
        Assert.That(player.JailTurnsRemaining, Is.EqualTo(0));
    }

    [Test]
    public void ReleaseFromJail_Failure_WhenPlayerIsNull()
    {
        GameResultDTO<bool> result = _game.ReleaseFromJail(null);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Player tidak boleh null."));
    }

    [Test]
    public void CheckBankruptcy_ReturnsFalse_WhenPlayerNotBankrupt()
    {
        Assert.That(_game.CheckBankruptcy(_game.Players.First()), Is.False);
    }

    [Test]
    public void CheckBankruptcy_ReturnsTrue_WhenPlayerBankrupt()
    {
        _game.Players.First().IsBankrupt = true;
        Assert.That(_game.CheckBankruptcy(_game.Players.First()), Is.True);
    }

    [Test]
    public void CheckPlayerJailStatus_ReturnsFalse_WhenNotInJail()
    {
        Assert.That(_game.CheckPlayerJailStatus(_game.Players.First()), Is.False);
    }

    [Test]
    public void CheckPlayerJailStatus_ReturnsTrue_WhenInJail()
    {
        _game.Players.First().IsInJail = true;
        Assert.That(_game.CheckPlayerJailStatus(_game.Players.First()), Is.True);
    }

    [Test]
    public void CheckPlayerJailStatus_ReturnsFalse_WhenPlayerIsNull()
    {
        Assert.That(_game.CheckPlayerJailStatus(null), Is.False);
    }

    [Test]
    public void RemovePlayer_Success_RemovesFromList()
    {
        IPlayer player = _game.Players.First();
        int countBefore = _game.Players.Count;
        GameResultDTO<bool> result = _game.RemovePlayer(player);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(_game.Players.Count, Is.EqualTo(countBefore - 1));
    }

    [Test]
    public void RemovePlayer_Failure_WhenPlayerIsNull()
    {
        GameResultDTO<bool> result = _game.RemovePlayer(null);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Player tidak boleh null."));
    }

    [Test]
    public void RemovePlayer_Failure_WhenPlayerNotInGame()
    {
        GameResultDTO<bool> result = _game.RemovePlayer(new Player("Ghost"));
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Player tidak ditemukan."));
    }

    [Test]
    public void RemovePlayer_AdjustsCurrentPlayerIndex()
    {
        IPlayer first = _game.Players[0];
        _game.RemovePlayer(first);
        Assert.That(_game.CurrentPlayerIndex, Is.LessThan(_game.Players.Count));
        Assert.That(_game.CurrentPlayerIndex, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void GetPlayerBalance_ReturnsPositiveBalance()
    {
        GameResultDTO<int> result = _game.GetPlayerBalance(_game.Players.First());
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.GreaterThan(0));
    }

    [Test]
    public void GetPlayerBalance_Failure_WhenPlayerIsNull()
    {
        GameResultDTO<int> result = _game.GetPlayerBalance(null);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Player tidak boleh null."));
    }

    [Test]
    public void GetPlayerBalance_Failure_WhenPlayerNotInGame()
    {
        GameResultDTO<int> result = _game.GetPlayerBalance(new Player("Ghost"));
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Data player tidak ditemukan."));
    }

    [Test]
    public void AddPlayerMoney_IncreasesBalance()
    {
        IPlayer player = _game.Players.First();
        int before = _game.GetPlayerBalance(player).Data;
        _game.AddPlayerMoney(player, new Money(100));
        int after = _game.GetPlayerBalance(player).Data;
        Assert.That(after, Is.EqualTo(before + 100));
    }

    [Test]
    public void AddPlayerMoney_Failure_WhenPlayerIsNull()
    {
        GameResultDTO<bool> result = _game.AddPlayerMoney(null, new Money(100));
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Player tidak boleh null."));
    }

    [Test]
    public void AddPlayerMoney_Failure_WhenMoneyIsNull()
    {
        GameResultDTO<bool> result = _game.AddPlayerMoney(_game.Players.First(), null);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Money tidak boleh null."));
    }

    [Test]
    public void AddPlayerMoney_Failure_WhenPlayerNotInGame()
    {
        GameResultDTO<bool> result = _game.AddPlayerMoney(new Player("Ghost"), new Money(100));
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Data player tidak ditemukan."));
    }

    [Test]
    public void SubstractPlayerMoney_DecreasesBalance()
    {
        IPlayer player = _game.Players.First();
        int before = _game.GetPlayerBalance(player).Data;
        _game.SubstractPlayerMoney(player, new Money(100));
        int after = _game.GetPlayerBalance(player).Data;
        Assert.That(after, Is.EqualTo(before - 100));
    }

    [Test]
    public void SubstractPlayerMoney_Failure_WhenPlayerIsNull()
    {
        GameResultDTO<bool> result = _game.SubstractPlayerMoney(null, new Money(50));
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Player tidak boleh null."));
    }

    [Test]
    public void SubstractPlayerMoney_Failure_WhenMoneyIsNull()
    {
        GameResultDTO<bool> result = _game.SubstractPlayerMoney(_game.Players.First(), null);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Money tidak boleh null."));
    }

    [Test]
    public void SubstractPlayerMoney_Failure_WhenPlayerNotInGame()
    {
        GameResultDTO<bool> result = _game.SubstractPlayerMoney(new Player("Ghost"), new Money(50));
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Data player tidak ditemukan."));
    }

    [Test]
    public void SubstractPlayerMoney_SetsBankrupt_WhenInsufficientFunds()
    {
        IPlayer player = _game.Players.First();
        int balance = _game.GetPlayerBalance(player).Data;
        _game.SubstractPlayerMoney(player, new Money(balance + 10_000));
        Assert.That(player.IsBankrupt, Is.True);
    }

    [Test]
    public void SubstractPlayerMoney_RaisesPlayerBankruptEvent_WhenInsufficientFunds()
    {
        IPlayer player = _game.Players.First();
        bool eventFired = false;
        _game.PlayerBankrupt += _ => eventFired = true;

        int balance = _game.GetPlayerBalance(player).Data;
        _game.SubstractPlayerMoney(player, new Money(balance + 10_000));
        Assert.That(eventFired, Is.True);
    }

    [Test]
    public void TransferPlayerMoney_Success_TransfersCorrectly()
    {
        IPlayer from = _game.Players[0];
        IPlayer to = _game.Players[1];
        int fromBefore = _game.GetPlayerBalance(from).Data;
        int toBefore = _game.GetPlayerBalance(to).Data;

        _game.TransferPlayerMoney(from, to, new Money(100));

        Assert.That(_game.GetPlayerBalance(from).Data, Is.EqualTo(fromBefore - 100));
        Assert.That(_game.GetPlayerBalance(to).Data, Is.EqualTo(toBefore + 100));
    }

    [Test]
    public void TransferPlayerMoney_Failure_WhenFromIsNull()
    {
        GameResultDTO<bool> result = _game.TransferPlayerMoney(null, _game.Players[1], new Money(100));
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Player tidak boleh null."));
    }

    [Test]
    public void TransferPlayerMoney_Failure_WhenToIsNull()
    {
        GameResultDTO<bool> result = _game.TransferPlayerMoney(_game.Players[0], null, new Money(100));
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Player tidak boleh null."));
    }

    [Test]
    public void TransferPlayerMoney_Failure_WhenMoneyIsNull()
    {
        GameResultDTO<bool> result = _game.TransferPlayerMoney(_game.Players[0], _game.Players[1], null);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Money tidak boleh null."));
    }

    [Test]
    public void TransferPlayerMoney_Failure_WhenInsufficientFunds()
    {
        IPlayer from = _game.Players[0];
        IPlayer to = _game.Players[1];
        int fromBalance = _game.GetPlayerBalance(from).Data;

        GameResultDTO<bool> result = _game.TransferPlayerMoney(from, to, new Money(fromBalance + 9999));
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("Uang tidak cukup untuk transfer"));
    }

    [Test]
    public void TransferPlayerMoney_Failure_WhenPlayerNotInGame()
    {
        GameResultDTO<bool> result = _game.TransferPlayerMoney(new Player("Ghost"), _game.Players[1], new Money(100));
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Data player tidak ditemukan."));
    }

    [Test]
    public void AttemptBuyCurrentProperty_ReturnsFalse_WhenWantsToBuyFalse2()
    {
        IPlayer player = CurrentPlayerWithPiece();
        GameResultDTO<bool> result = _game.AttemptBuyCurrentProperty(player, false);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.False);
    }

    [Test]
    public void AttemptBuyCurrentProperty_Success_WhenOnAvailablePropertyWithFunds()
    {
        IPlayer player = CurrentPlayerWithPiece();
        ITile rentTile = _game.Board.Tiles.First(t => t.Type == TileType.RentTile);
        _game.MovePieceTo(player, rentTile);

        GameResultDTO<bool> result = _game.AttemptBuyCurrentProperty(player, true);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.True);
        Assert.That(rentTile.Owner, Is.EqualTo(player));
    }

    [Test]
    public void AttemptBuyCurrentProperty_Failure_WhenPropertyAlreadyOwned()
    {
        IPlayer player = CurrentPlayerWithPiece();
        ITile rentTile = _game.Board.Tiles.First(t => t.Type == TileType.RentTile);
        rentTile.Owner = _game.Players[1];
        _game.MovePieceTo(player, rentTile);

        GameResultDTO<bool> result = _game.AttemptBuyCurrentProperty(player, true);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("tidak tersedia untuk dibeli"));
    }

    [Test]
    public void AttemptBuyCurrentProperty_Failure_WhenInsufficientFunds2()
    {
        IPlayer player = CurrentPlayerWithPiece();
        ITile rentTile = _game.Board.Tiles.First(t => t.Type == TileType.RentTile);
        _game.MovePieceTo(player, rentTile);


        int bal = _game.GetPlayerBalance(player).Data;
        _game.SubstractPlayerMoney(player, new Money(bal));

        player.IsBankrupt = false;

        GameResultDTO<bool> result = _game.AttemptBuyCurrentProperty(player, true);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("Uang tidak cukup"));
    }

    [Test]
    public void HandleBuyDecision_Success_WhenInCorrectPhase()
    {
        IPlayer player = CurrentPlayerWithPiece();
        ITile rentTile = _game.Board.Tiles.First(t => t.Type == TileType.RentTile);
        _game.MovePieceTo(player, rentTile);
        SetGamePhase(_game, GamePhase.WaitingBuyDecision);

        GameResultDTO<bool> result = _game.HandleBuyDecision(true);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(_game.Phase, Is.EqualTo(GamePhase.WaitingRoll));
    }

    [Test]
    public void HandleBuyDecision_Failure_WhenWrongPhase()
    {
        GameResultDTO<bool> result = _game.HandleBuyDecision(true);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("Bukan fase yang tepat untuk keputusan beli"));
    }

    [Test]
    public void HandleBuyDecision_SkipsBuy_WhenWantsToBuyFalse()
    {
        CurrentPlayerWithPiece();
        SetGamePhase(_game, GamePhase.WaitingBuyDecision);

        GameResultDTO<bool> result = _game.HandleBuyDecision(false);
        Assert.That(result.IsSuccess, Is.True);
    }


    private ITile SetupMonopolyForPlayer(IPlayer player)
    {
        IList<ITile> coloredTiles = _game.Board.Tiles
            .Where(t => t.Asset?.Color == Color.Brown)
            .ToList();

        foreach (ITile t in coloredTiles)
            t.Owner = player;

        return coloredTiles.First();
    }

    [Test]
    public void BuyBuilding_Success_AddsHouse()
    {
        IPlayer player = _game.Players.First();
        ITile tile = SetupMonopolyForPlayer(player);
        int housesBefore = tile.House ?? 0;

        GameResultDTO<bool> result = _game.BuyBuilding(player, tile.Asset!.City.PropertyCity, false);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(tile.House, Is.EqualTo(housesBefore + 1));
    }

    [Test]
    public void BuyBuilding_Success_AddsHotel_WhenThreeHousesExist()
    {
        IPlayer player = _game.Players.First();
        ITile tile = SetupMonopolyForPlayer(player);
        tile.House = 3;

        GameResultDTO<bool> result = _game.BuyBuilding(player, tile.Asset!.City.PropertyCity, true);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(tile.HasHotel, Is.True);
        Assert.That(tile.House, Is.EqualTo(0));
    }

    [Test]
    public void BuyBuilding_Failure_WhenPlayerIsNull()
    {
        GameResultDTO<bool> result = _game.BuyBuilding(null, PropertyCity.MediterraneanAvenue, false);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Player tidak boleh null."));
    }

    [Test]
    public void BuyBuilding_Failure_WhenPlayerDoesNotOwnProperty()
    {
        IPlayer player = _game.Players.First();
        ITile tile = _game.Board.Tiles.First(t => t.Asset?.Color == Color.Brown);

        GameResultDTO<bool> result = _game.BuyBuilding(player, tile.Asset!.City.PropertyCity, false);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("bukan milik"));
    }

    [Test]
    public void BuyBuilding_Failure_WhenNoMonopoly()
    {
        IPlayer player = _game.Players.First();
        IList<ITile> brownTiles = _game.Board.Tiles.Where(t => t.Asset?.Color == Color.Brown).ToList();
        brownTiles[0].Owner = player;

        GameResultDTO<bool> result = _game.BuyBuilding(player, brownTiles[0].Asset!.City.PropertyCity, false);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("monopoli"));
    }

    [Test]
    public void BuyBuilding_Failure_WhenMaxHousesReached()
    {
        IPlayer player = _game.Players.First();
        ITile tile = SetupMonopolyForPlayer(player);
        tile.House = 3;

        GameResultDTO<bool> result = _game.BuyBuilding(player, tile.Asset!.City.PropertyCity, false);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("maksimum 3 rumah"));
    }

    [Test]
    public void BuyBuilding_Failure_WhenHotelAlreadyExists()
    {
        IPlayer player = _game.Players.First();
        ITile tile = SetupMonopolyForPlayer(player);
        tile.House = 3;
        _game.BuyBuilding(player, tile.Asset!.City.PropertyCity, true);

        GameResultDTO<bool> result = _game.BuyBuilding(player, tile.Asset!.City.PropertyCity, true);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("sudah memiliki hotel"));
    }

    [Test]
    public void BuyBuilding_Failure_BuildHotelWithLessThanThreeHouses()
    {
        IPlayer player = _game.Players.First();
        ITile tile = SetupMonopolyForPlayer(player);
        tile.House = 1;

        GameResultDTO<bool> result = _game.BuyBuilding(player, tile.Asset!.City.PropertyCity, true);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("minimal 3 rumah"));
    }

    [Test]
    public void SellBuildingsToBank_Success_SellingHouses()
    {
        IPlayer player = _game.Players.First();
        ITile tile = SetupMonopolyForPlayer(player);
        tile.House = 2;

        int balBefore = _game.GetPlayerBalance(player).Data;
        GameResultDTO<int> result = _game.SellBuildingsToBank(
            new SendBuildingToBankResult(player, tile.Asset!.City.PropertyCity, 2, false));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.GreaterThan(0));
        Assert.That(_game.GetPlayerBalance(player).Data, Is.GreaterThan(balBefore));
    }

    [Test]
    public void SellBuildingsToBank_Success_SellingHotel()
    {
        IPlayer player = _game.Players.First();
        ITile tile = SetupMonopolyForPlayer(player);
        tile.HasHotel = true;

        GameResultDTO<int> result = _game.SellBuildingsToBank(
            new SendBuildingToBankResult(player, tile.Asset!.City.PropertyCity, 0, true));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(tile.HasHotel, Is.False);
    }

    [Test]
    public void SellBuildingsToBank_Failure_WhenNullInput()
    {
        GameResultDTO<int> result = _game.SellBuildingsToBank(null);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("null"));
    }

    [Test]
    public void SellBuildingsToBank_Failure_WhenNotOwner()
    {
        IPlayer player = _game.Players.First();
        ITile tile = _game.Board.Tiles.First(t => t.Asset?.Color == Color.Brown);
        tile.Owner = _game.Players[1];

        GameResultDTO<int> result = _game.SellBuildingsToBank(
            new SendBuildingToBankResult(player, tile.Asset!.City.PropertyCity, 1, false));

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("bukan milik"));
    }

    [Test]
    public void SellBuildingsToBank_Failure_WhenNoBuildingExists()
    {
        IPlayer player = _game.Players.First();
        ITile tile = SetupMonopolyForPlayer(player);
        tile.House = 0;
        tile.HasHotel = false;

        GameResultDTO<int> result = _game.SellBuildingsToBank(
            new SendBuildingToBankResult(player, tile.Asset!.City.PropertyCity, 0, false));

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("Tidak ada bangunan"));
    }

    [Test]
    public void SellBuildingsToBank_Failure_WhenSellingMoreHousesThanExist()
    {
        IPlayer player = _game.Players.First();
        ITile tile = SetupMonopolyForPlayer(player);
        tile.House = 1;

        GameResultDTO<int> result = _game.SellBuildingsToBank(
            new SendBuildingToBankResult(player, tile.Asset!.City.PropertyCity, 3, false));

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("melebihi"));
    }

    [Test]
    public void SellPropertyToBank_Success_ReturnsHalfPrice()
    {
        IPlayer player = _game.Players.First();
        ITile tile = SetupMonopolyForPlayer(player);

        int price = tile.Asset!.Price.Value;
        GameResultDTO<int> result = _game.SellPropertyToBank(player, tile.Asset.City.PropertyCity);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(tile.Owner, Is.Null);
    }

    [Test]
    public void SellPropertyToBank_Failure_WhenOwnerIsNull()
    {
        GameResultDTO<int> result = _game.SellPropertyToBank(null, PropertyCity.MediterraneanAvenue);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Owner tidak boleh null."));
    }

    [Test]
    public void SellPropertyToBank_Failure_WhenNotOwner()
    {
        IPlayer player = _game.Players.First();
        ITile tile = _game.Board.Tiles.First(t => t.Asset != null);
        tile.Owner = _game.Players[1];

        GameResultDTO<int> result = _game.SellPropertyToBank(player, tile.Asset!.City.PropertyCity);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("bukan milik"));
    }

    [Test]
    public void SellPropertyToBank_Failure_WhenHasBuildingsAndIncludeBuildingsFalse()
    {
        IPlayer player = _game.Players.First();
        ITile tile = SetupMonopolyForPlayer(player);
        tile.House = 2;

        GameResultDTO<int> result = _game.SellPropertyToBank(player, tile.Asset!.City.PropertyCity, false);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("Jual bangunan terlebih dahulu"));
    }

    [Test]
    public void SellPropertyToBank_IncludesBuildings_WhenIncludeBuildingsTrue()
    {
        IPlayer player = _game.Players.First();
        ITile tile = SetupMonopolyForPlayer(player);
        tile.House = 2;

        GameResultDTO<int> result = _game.SellPropertyToBank(player, tile.Asset!.City.PropertyCity, true);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(tile.House, Is.EqualTo(0));
    }

    [Test]
    public void SellAllAssetsToBank_Success_ReturnsPositiveIncome()
    {
        IPlayer player = _game.Players.First();
        SetupMonopolyForPlayer(player);

        GameResultDTO<int> result = _game.SellAllAssetsToBank(player);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.GreaterThan(0));
    }

    [Test]
    public void SellAllAssetsToBank_Failure_WhenPlayerIsNull()
    {
        GameResultDTO<int> result = _game.SellAllAssetsToBank(null);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Player tidak boleh null."));
    }

    [Test]
    public void SellAllAssetsToBank_ReturnsZero_WhenNoProperties()
    {
        GameResultDTO<int> result = _game.SellAllAssetsToBank(_game.Players.First());
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.EqualTo(0));
    }

    public enum PropertyAvailabilityScenario
    {
        NoOwner,
        Owned,
        NoAsset,
        NullTile,
    }

    [TestCase(PropertyAvailabilityScenario.NoOwner, true)]
    [TestCase(PropertyAvailabilityScenario.Owned, false)]
    [TestCase(PropertyAvailabilityScenario.NoAsset, false)]
    [TestCase(PropertyAvailabilityScenario.NullTile, false)]
    public void IsPropertyAvailable_ByScenario(
        PropertyAvailabilityScenario scenario,
        bool expected
    )
    {
        ITile? tile = scenario switch
        {
            PropertyAvailabilityScenario.NoOwner => _game.Board.Tiles.First(t => t.Asset != null),
            PropertyAvailabilityScenario.Owned => _game.Board.Tiles.First(t => t.Asset != null),
            PropertyAvailabilityScenario.NoAsset => _game.Board.Tiles.First(t => t.Asset == null),
            _ => null,
        };

        if (scenario == PropertyAvailabilityScenario.Owned)
        {
            tile!.Owner = _game.Players.First();
        }

        if (scenario == PropertyAvailabilityScenario.NoOwner)
        {
            tile!.Owner = null;
        }

        Assert.That(_game.IsPropertyAvailable(tile!), Is.EqualTo(expected));
    }

    [Test]
    public void DrawCard_ReturnsChanceCard_WhenDrawChanceCalled()
    {
        ICard? card = _game.DrawCard(TileType.DrawChance);
        Assert.That(card, Is.Not.Null);
        Assert.That(card, Is.InstanceOf<ChanceCard>());
    }

    [Test]
    public void DrawCard_ReturnsCommunityCard_WhenDrawCommunityCalled()
    {
        ICard? card = _game.DrawCard(TileType.DrawCommunity);
        Assert.That(card, Is.Not.Null);
        Assert.That(card, Is.InstanceOf<CommunityCard>());
    }

    [Test]
    public void DrawCard_ReturnsNull_WhenUnrecognizedType()
    {
        ICard? card = _game.DrawCard(TileType.StartTile);
        Assert.That(card, Is.Null);
    }

    [Test]
    public void ExecuteCard_Success_WithChanceCard()
    {
        IPlayer player = CurrentPlayerWithPiece();
        ICard? card = _game.DrawCard(TileType.DrawChance);
        Assert.That(card, Is.Not.Null);

        GameResultDTO<bool> result = _game.ExecuteCard(card!, player);
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void ExecuteCard_Success_WithCommunityCard()
    {
        IPlayer player = CurrentPlayerWithPiece();
        ICard? card = _game.DrawCard(TileType.DrawCommunity);
        Assert.That(card, Is.Not.Null);

        GameResultDTO<bool> result = _game.ExecuteCard(card!, player);
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void ExecuteCard_Failure_WhenCardIsNull()
    {
        GameResultDTO<bool> result = _game.ExecuteCard(null!, _game.Players.First());
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Card tidak boleh null."));
    }

    [Test]
    public void ExecuteCard_Failure_WhenPlayerIsNull()
    {
        ICard? card = _game.DrawCard(TileType.DrawChance);
        GameResultDTO<bool> result = _game.ExecuteCard(card!, null!);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Player tidak boleh null."));
    }

    private void PrepareRepairsForPlayer(IPlayer player)
    {
        ITile tile = _game.Board.Tiles.First(t => t.Asset != null);
        tile.Owner = player;
        tile.House = 1;
        tile.HasHotel = true;
    }

    [TestCase(CardBehaviour.AdvanceToGo, true, false, false)]
    [TestCase(CardBehaviour.BankError, true, false, false)]
    [TestCase(CardBehaviour.DoctorFees, true, false, false)]
    [TestCase(CardBehaviour.FromSaleOfStock, true, false, false)]
    [TestCase(CardBehaviour.GetOutOfJailFree, true, true, false)]
    [TestCase(CardBehaviour.GoToJail, true, false, false)]
    [TestCase(CardBehaviour.HolidayFundMatures, true, false, false)]
    [TestCase(CardBehaviour.IncomeTaxRefund, true, false, false)]
    [TestCase(CardBehaviour.Birthday, true, false, false)]
    [TestCase(CardBehaviour.LifeInsuranceMatures, true, false, false)]
    [TestCase(CardBehaviour.PayHospitalFees, true, false, false)]
    [TestCase(CardBehaviour.PaySchoolFees, true, false, false)]
    [TestCase(CardBehaviour.ConsultancyFee, true, false, false)]
    [TestCase(CardBehaviour.StreetRepairs, true, false, true)]
    [TestCase(CardBehaviour.BeautyContestPrize, true, false, false)]
    [TestCase(CardBehaviour.InheritMoney, true, false, false)]
    [TestCase(CardBehaviour.AdvanceToGo, false, false, false)]
    [TestCase(CardBehaviour.AdvanceToIllinois, false, false, false)]
    [TestCase(CardBehaviour.AdvanceToStCharles, false, false, false)]
    [TestCase(CardBehaviour.AdvanceNearestUtility, false, false, false)]
    [TestCase(CardBehaviour.AdvanceNearestRailroad, false, false, false)]
    [TestCase(CardBehaviour.BankPaysDividend, false, false, false)]
    [TestCase(CardBehaviour.GetOutOfJailFree, false, true, false)]
    [TestCase(CardBehaviour.GoBackThreeSpaces, false, false, false)]
    [TestCase(CardBehaviour.GoToJail, false, false, false)]
    [TestCase(CardBehaviour.MakeGeneralRepairs, false, false, true)]
    [TestCase(CardBehaviour.PayPoorTax, false, false, false)]
    [TestCase(CardBehaviour.TakeTripToReadingRailroad, false, false, false)]
    [TestCase(CardBehaviour.AdvanceToBoardwalk, false, false, false)]
    [TestCase(CardBehaviour.ChairmanOfTheBoard, false, false, false)]
    [TestCase(CardBehaviour.YourBuildingLoanMatures, false, false, false)]
    public void ExecuteCard_Success_ForBehaviours(
        CardBehaviour behaviour,
        bool isCommunity,
        bool setInJail,
        bool setupRepairs
    )
    {
        IPlayer player = CurrentPlayerWithPiece();

        if (setInJail)
        {
            player.IsInJail = true;
        }

        if (setupRepairs)
        {
            PrepareRepairsForPlayer(player);
        }

        ICard card = _game.Cards.First(c =>
            c.Behaviour == behaviour && (isCommunity ? c is CommunityCard : c is ChanceCard)
        );

        GameResultDTO<bool> result = _game.ExecuteCard(card, player);
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void BuyBuilding_Failure_WhenPlayerNull()
    {
        GameResultDTO<bool> result = _game.BuyBuilding(null!, PropertyCity.MediterraneanAvenue, false);
        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public void BuyBuilding_Failure_WhenTileNotOwned()
    {
        IPlayer player = CurrentPlayerWithPiece();
        GameResultDTO<bool> result = _game.BuyBuilding(player, PropertyCity.MediterraneanAvenue, false);
        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public void BuyBuilding_Failure_WhenNotOwner()
    {
        IPlayer player = CurrentPlayerWithPiece();
        IPlayer owner = _game.Players[1];
        ITile tile = _game.GetTileByCity(PropertyCity.MediterraneanAvenue);
        tile.Owner = owner;
        GameResultDTO<bool> result = _game.BuyBuilding(player, PropertyCity.MediterraneanAvenue, false);
        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public void BuyBuilding_Failure_WhenNoMonopoly2()
    {
        IPlayer player = CurrentPlayerWithPiece();
        ITile tile = _game.GetTileByCity(PropertyCity.MediterraneanAvenue);
        tile.Owner = player;

        GameResultDTO<bool> result = _game.BuyBuilding(player, PropertyCity.MediterraneanAvenue, false);
        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public void BuyBuilding_Failure_WhenAlreadyHasHotel2()
    {
        IPlayer player = CurrentPlayerWithPiece();
        ITile tile = _game.GetTileByCity(PropertyCity.MediterraneanAvenue);
        tile.Owner = player;
        tile.HasHotel = true;
        GameResultDTO<bool> result = _game.BuyBuilding(player, PropertyCity.MediterraneanAvenue, false);
        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public void BuyBuilding_Failure_WhenMaxHousesReached2()
    {
        IPlayer player = CurrentPlayerWithPiece();
        ITile tile = _game.GetTileByCity(PropertyCity.MediterraneanAvenue);
        tile.Owner = player;
        tile.House = 3;
        GameResultDTO<bool> result = _game.BuyBuilding(player, PropertyCity.MediterraneanAvenue, false);
        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public void BuyBuilding_Failure_WhenInsufficientFunds()
    {
        IPlayer player = CurrentPlayerWithPiece();
        _game.SubstractPlayerMoney(player, new Money(1400)); // Leave little money
        ITile tile = _game.GetTileByCity(PropertyCity.MediterraneanAvenue);
        tile.Owner = player;
        GameResultDTO<bool> result = _game.BuyBuilding(player, PropertyCity.MediterraneanAvenue, false);
        Assert.That(result.IsSuccess, Is.False);
    }
    [TestCase(TileType.DrawChance, true, false)]
    [TestCase(TileType.DrawCommunity, true, false)]
    [TestCase(TileType.TaxTile, true, false)]
    [TestCase(TileType.GoToJailTile, true, true)]
    [TestCase(TileType.StartTile, false, false)]
    public void ExecuteTile_ByType(TileType type, bool expectedSuccess, bool expectJail)
    {
        IPlayer player = CurrentPlayerWithPiece();
        ITile tile = _game.Board.Tiles.First(t => t.Type == type);
        GameResultDTO<ICard?> result = _game.ExecuteTile(tile, player);

        Assert.That(result.IsSuccess, Is.EqualTo(expectedSuccess));
        if (expectJail)
        {
            Assert.That(player.IsInJail, Is.True);
        }
    }

    [TestCase(true, false, "Tile tidak boleh null.")]
    [TestCase(false, true, "Player tidak boleh null.")]
    public void ExecuteTile_Failure_WhenNullArgs(
        bool tileNull,
        bool playerNull,
        string expectedError
    )
    {
        ITile? tile = tileNull ? null : _game.Board.Tiles.First(t => t.Type == TileType.DrawChance);
        IPlayer? player = playerNull ? null : _game.Players.First();

        GameResultDTO<ICard?> result = _game.ExecuteTile(tile!, player!);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo(expectedError));
    }

    [Test]
    public void ExecuteTile_ChargesDoubleRent_WhenDoubleRentTrue()
    {
        IPlayer owner = _game.Players[1];
        IPlayer renter = CurrentPlayerWithPiece();
        ITile tile = _game.Board.Tiles.First(t => t.Type == TileType.RentTile);
        tile.Owner = owner;

        int renterBefore = _game.GetPlayerBalance(renter).Data;
        _game.ExecuteTile(tile, renter, true); // doubleRent = true
        int renterAfter = _game.GetPlayerBalance(renter).Data;
        int rent = Math.Max(10, tile.Asset!.Price.Value / 10) * 2;
        Assert.That(renterAfter, Is.EqualTo(renterBefore - rent));
    }

    [Test]
    public void ExecuteTile_DoesNotChargeRent_WhenOwnerLandsOnOwnedProperty()
    {
        IPlayer owner = CurrentPlayerWithPiece();
        ITile tile = _game.Board.Tiles.First(t => t.Type == TileType.RentTile);
        tile.Owner = owner;

        int before = _game.GetPlayerBalance(owner).Data;
        GameResultDTO<ICard?> result = _game.ExecuteTile(tile, owner);
        int after = _game.GetPlayerBalance(owner).Data;

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(after, Is.EqualTo(before));
    }

    [Test]
    public void MovePieceTo_Failure_WhenPieceNotOnCurrentTile()
    {
        IPlayer player = CurrentPlayerWithPiece();
        ITile currentTile = _game.GetCurrentTile(player).Data!;
        IPiece piece = _game.GetPiece(player).Data!;
        currentTile.Pieces.Remove(piece);

        ITile target = _game.Board.Tiles.First(t => !ReferenceEquals(t, currentTile));
        GameResultDTO<bool> result = _game.MovePieceTo(player, target);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Piece tidak berada di tile manapun."));
    }

    [Test]
    public void SendPieceToJail_Failure_WhenCurrentTileMissing()
    {
        IPlayer player = CurrentPlayerWithPiece();
        IPiece piece = _game.GetPiece(player).Data!;
        foreach (ITile t in _game.Board.Tiles)
            t.Pieces.Remove(piece);

        GameResultDTO<bool> result = _game.SendPieceToJail(player);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Gagal mendapatkan tile saat ini."));
    }

    [Test]
    public void DrawCard_ReturnsNull_WhenNoCandidates()
    {
        var field = typeof(Game).GetField(
            "_cards",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        field!.SetValue(_game, new List<ICard>());

        ICard? card = _game.DrawCard(TileType.DrawChance);
        Assert.That(card, Is.Null);
    }

    [Test]
    public void SellBuildingsToBank_SellsHotelAndHouses_WhenAvailable()
    {
        IPlayer player = CurrentPlayerWithPiece();
        ITile tile = _game.Board.Tiles.First(t => t.Asset != null);
        tile.Owner = player;
        tile.HasHotel = true;
        tile.House = 2;

        GameResultDTO<int> result = _game.SellBuildingsToBank(
            new SendBuildingToBankResult(player, tile.Asset!.City.PropertyCity, 1, true)
        );

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(tile.HasHotel, Is.False);
        Assert.That(tile.House, Is.EqualTo(1));
    }

    [Test]
    public void SellBuildingsToBank_Failure_WhenSellingTooManyHouses()
    {
        IPlayer player = CurrentPlayerWithPiece();
        ITile tile = _game.Board.Tiles.First(t => t.Asset != null);
        tile.Owner = player;
        tile.House = 1;

        GameResultDTO<int> result = _game.SellBuildingsToBank(
            new SendBuildingToBankResult(player, tile.Asset!.City.PropertyCity, 3, false)
        );

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("melebihi"));
    }

    [Test]
    public void SellPropertyToBank_IncludesBuildings_WhenRequested()
    {
        IPlayer player = CurrentPlayerWithPiece();
        ITile tile = _game.Board.Tiles.First(t => t.Asset != null);
        tile.Owner = player;
        tile.House = 2;
        tile.HasHotel = true;

        GameResultDTO<int> result = _game.SellPropertyToBank(player, tile.Asset!.City.PropertyCity, true);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(tile.Owner, Is.Null);
    }

    [Test]
    public void MovePiece_AddsTwoHundred_WhenPassingStart()
    {
        IPlayer player = CurrentPlayerWithPiece();
        ITile startFrom = _game.Board.Tiles[5];
        _game.MovePieceTo(player, startFrom);
        int currentIndex = Array.IndexOf(_game.Board.Tiles, startFrom);

        int steps = _game.Board.Tiles.Length - currentIndex + 1;
        int before = _game.GetPlayerBalance(player).Data;

        GameResultDTO<bool> result = typeof(Game)
            .GetMethod("MovePiece", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            !.Invoke(_game, new object?[] { player, steps }) as GameResultDTO<bool>;

        int after = _game.GetPlayerBalance(player).Data;
        Assert.That(result!.IsSuccess, Is.True);
        Assert.That(after, Is.GreaterThan(before));
    }

    [Test]
    public void RollTurn_Failure_WhenInJailDoubleButMoveFails()
    {
        IPlayer player = CurrentPlayerWithPiece();
        player.IsInJail = true;
        IPiece piece = _game.GetPiece(player).Data!;
        foreach (ITile t in _game.Board.Tiles)
            t.Pieces.Remove(piece);

        GameResultDTO<RollTurnResult> result = _game.RollTurn();
        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public void RollTurn_Failure_WhenJailTurnsZeroAndMoveFails()
    {
        IBoard board = BoardFactory.CreateBoard();
        List<IPlayer> players = PlayerFactory.CreatePlayers(["P1", "P2"]);
        List<IPiece> pieces = PieceFactory.CreateStandardPieces();
        List<ICard> cards = CardFactory.CreateDefaultCards();
        List<IMoney> money = MoneyFactory.CreateMoney();
        List<IDice> dice = new List<IDice> { new FakeDice(3), new FakeDice(4) };
        Game game = new Game(board, players, pieces, cards, money, dice);

        IPlayer player = game.CurrentPlayer;
        game.AssignPieceToPlayer(player, game.Pieces.First().Type);
        player.IsInJail = true;
        player.JailTurnsRemaining = 1;

        IPiece piece = game.GetPiece(player).Data!;
        foreach (ITile t in game.Board.Tiles)
            t.Pieces.Remove(piece);

        GameResultDTO<RollTurnResult> result = game.RollTurn();
        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public void RollTurn_Failure_WhenTripleDoubleSendToJailFails()
    {
        IPlayer player = CurrentPlayerWithPiece();
        player.DoubleRoll = 2;

        IPiece piece = _game.GetPiece(player).Data!;
        foreach (ITile t in _game.Board.Tiles)
            t.Pieces.Remove(piece);

        GameResultDTO<RollTurnResult> result = _game.RollTurn();
        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public void AttemptBuyCurrentProperty_ReturnsFalse_WhenWantsToBuyFalse()
    {
        IPlayer player = CurrentPlayerWithPiece();
        GameResultDTO<bool> result = _game.AttemptBuyCurrentProperty(player, false);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.False);
    }

    [Test]
    public void AttemptBuyCurrentProperty_Failure_WhenPropertyUnavailable()
    {
        IPlayer player = CurrentPlayerWithPiece();
        ITile tile = _game.Board.Tiles.First(t => t.Asset != null);
        tile.Owner = _game.Players[1];
        _game.MovePieceTo(player, tile);

        GameResultDTO<bool> result = _game.AttemptBuyCurrentProperty(player, true);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Properti ini tidak tersedia untuk dibeli."));
    }

    [Test]
    public void AttemptBuyCurrentProperty_Failure_WhenBalanceLookupFails()
    {
        IPlayer player = CurrentPlayerWithPiece();
        ITile tile = _game.Board.Tiles.First(t => t.Asset != null && t.Owner == null);
        _game.MovePieceTo(player, tile);

        var field = typeof(Game).GetField(
            "_playerData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        Dictionary<IPlayer, List<IMoney>> data = (Dictionary<IPlayer, List<IMoney>>)field!.GetValue(_game)!;
        data.Remove(player);

        GameResultDTO<bool> result = _game.AttemptBuyCurrentProperty(player, true);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Data player tidak ditemukan."));
    }

    [Test]
    public void AttemptBuyCurrentProperty_Failure_WhenInsufficientFunds()
    {
        IPlayer player = CurrentPlayerWithPiece();
        ITile tile = _game.Board.Tiles.First(t => t.Asset != null && t.Owner == null);
        _game.MovePieceTo(player, tile);

        int balance = _game.GetPlayerBalance(player).Data;
        _game.SubstractPlayerMoney(player, new Money(balance - 10));

        GameResultDTO<bool> result = _game.AttemptBuyCurrentProperty(player, true);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("Uang tidak cukup"));
    }

    [Test]
    public void RemovePlayer_ReturnsSuccess_WhenRemovingLastPlayer()
    {
        IBoard board = BoardFactory.CreateBoard();
        List<IPlayer> players = PlayerFactory.CreatePlayers(["Solo"]);
        List<IPiece> pieces = PieceFactory.CreateStandardPieces();
        List<ICard> cards = CardFactory.CreateDefaultCards();
        List<IMoney> money = MoneyFactory.CreateMoney();
        List<IDice> dice = new List<IDice> { new FakeDice(1), new FakeDice(2) };
        Game game = new Game(board, players, pieces, cards, money, dice);

        GameResultDTO<bool> result = game.RemovePlayer(players[0]);
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void ExecuteCard_Success_WithAdvanceNearestRailroad_WhenNoPiece()
    {
        IPlayer player = _game.Players.First();
        ICard card = _game.Cards.First(c => c.Behaviour == CardBehaviour.AdvanceNearestRailroad && c is ChanceCard);
        GameResultDTO<bool> result = _game.ExecuteCard(card, player);
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void ExecuteCard_GetOutOfJailFree_IncrementsCard_WhenNotInJail()
    {
        IPlayer player = CurrentPlayerWithPiece();
        int before = player.JailFreeCardCount;
        ICard card = _game.Cards.First(c => c.Behaviour == CardBehaviour.GetOutOfJailFree && c is ChanceCard);

        GameResultDTO<bool> result = _game.ExecuteCard(card, player);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(player.JailFreeCardCount, Is.EqualTo(before + 1));
    }

    [Test]
    public void GetPlayerProperties_ReturnsOwnedTiles()
    {
        IPlayer player = _game.Players.First();
        SetupMonopolyForPlayer(player);

        List<ITile> props = _game.GetPlayerProperties(player);
        Assert.That(props, Is.Not.Empty);
        Assert.That(props.All(t => t.Owner == player), Is.True);
    }

    [Test]
    public void GetPlayerProperties_ReturnsEmpty_WhenNoProperties()
    {
        List<ITile> props = _game.GetPlayerProperties(_game.Players.First());
        Assert.That(props, Is.Empty);
    }

    [Test]
    public void FindPlayerByName_ReturnsPlayer_WhenExists()
    {
        IPlayer? found = _game.FindPlayerByName("Player1");
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Name, Is.EqualTo("Player1"));
    }

    [Test]
    public void FindPlayerByName_ReturnsNull_WhenNotFound()
    {
        IPlayer? found = _game.FindPlayerByName("NonExistent");
        Assert.That(found, Is.Null);
    }

    [Test]
    public void FindPlayerByName_IsCaseInsensitive()
    {
        IPlayer? found = _game.FindPlayerByName("player1");
        Assert.That(found, Is.Not.Null);
    }


    [Test]
    public void GetWinnerOrNull_ReturnsFailure_WhenMultipleActivePlayers()
    {
        GameResultDTO<IPlayer?> result = _game.GetWinnerOrNull();
        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public void GetWinnerOrNull_ReturnsWinner_WhenOnePlayerRemains()
    {
        for (int i = 1; i < _game.Players.Count; i++)
            _game.Players[i].IsBankrupt = true;

        GameResultDTO<IPlayer?> result = _game.GetWinnerOrNull();
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.Not.Null);
    }

    [Test]
    public void CheckWinner_ReturnsFalse_WhenMultiplePlayers()
    {
        Assert.That(_game.CheckWinner(), Is.False);
    }

    [Test]
    public void CheckWinner_ReturnsTrue_WhenOnePlayerLeft()
    {
        for (int i = 1; i < _game.Players.Count; i++)
            _game.Players[i].IsBankrupt = true;

        Assert.That(_game.CheckWinner(), Is.True);
    }

    [Test]
    public void CheckWinner_RaisesIsGameEndedEvent_WhenWinner()
    {
        for (int i = 1; i < _game.Players.Count; i++)
            _game.Players[i].IsBankrupt = true;

        bool eventFired = false;
        _game.IsGameEnded += player => eventFired = true;
        _game.CheckWinner();
        Assert.That(eventFired, Is.True);
    }

    [Test]
    public void EndGame_SetsGameEndedTrue_WhenOnePlayerLeft()
    {
        for (int i = 1; i < _game.Players.Count; i++)
            _game.Players[i].IsBankrupt = true;

        _game.EndGame();
        Assert.That(_game.GameEnded, Is.True);
    }

    [Test]
    public void EndGame_ReturnsFalse_WhenMultiplePlayers()
    {
        bool ended = _game.EndGame();
        Assert.That(ended, Is.False);
        Assert.That(_game.GameEnded, Is.False);
    }

    [Test]
    public void GetTileByCity_ReturnsCorrectTile()
    {
        ITile tile = _game.Board.Tiles.First(t => t.Asset != null);
        ITile found = _game.GetTileByCity(tile.Asset!.City.PropertyCity);
        Assert.That(found, Is.EqualTo(tile));
    }

    [Test]
    public void EndTurn_RepeatRollFalse_SetsPhaseToWaitingRoll()
    {
        IPlayer player = CurrentPlayerWithPiece();
        SetGamePhase(_game, GamePhase.WaitingBuyDecision);
        _game.HandleBuyDecision(false);
        Assert.That(_game.Phase, Is.EqualTo(GamePhase.WaitingRoll));
    }

    [Test]
    public void HandleDiceRoll_ReturnsTotalOfTwoDice()
    {
        GameResultDTO<int> result = _game.HandleDiceRoll(new FakeDice(3), new FakeDice(4));
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.EqualTo(7));
    }

    [Test]
    public void HandleDiceRoll_IncrementsDoubleRoll_WhenDiceMatch()
    {
        IPlayer player = _game.CurrentPlayer;
        int before = player.DoubleRoll;
        _game.HandleDiceRoll(new FakeDice(5), new FakeDice(5));
        Assert.That(player.DoubleRoll, Is.EqualTo(before + 1));
    }
}