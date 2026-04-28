namespace GameService.Tests;

using Backend.Domain.DTOs;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Domain.Interfaces;
using Backend.Factories;

[TestFixture]
public class GameService_UnitTest
{
    private Game _gameService;

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
        _gameService = new Game(board, players, pieces, cards, money, dice);
    }

    [Test]
    public void GetBoard_ShouldReturnBoard_WhenCalled()
    {
        IBoard board = _gameService.Board;
        Assert.That(board, Is.Not.Null, "GetBoard should return a board");
    }

    [Test]
    public void GetCurrentPlayer_ShouldReturnCurrentPlayer_WhenCalled()
    {
        IPlayer currentPlayer = _gameService.CurrentPlayer;
        Assert.That(
            currentPlayer,
            Is.Not.Null,
            "GetCurrentPlayer should return the current player"
        );
    }

    [Test]
    public void GetCurrentPlayerIndex_ShouldReturnCurrentPlayerIndex_WhenCalled()
    {
        int currentPlayerIndex = _gameService.CurrentPlayerIndex;
        Assert.That(
            currentPlayerIndex,
            Is.GreaterThanOrEqualTo(0),
            "GetCurrentPlayerIndex should return a valid index"
        );
    }

    [Test]
    public void GetCards_ShouldReturnCards_WhenCalled()
    {
        List<ICard> cards = _gameService.Cards;
        Assert.That(cards, Is.Not.Null, "GetCards should return a list of cards");
        Assert.That(cards, Is.Not.Empty, "GetCards should return at least one card");
    }

    [Test]
    public void IsPieceAvailable_ShouldReturnTrue_WhenPieceIsAvailable()
    {
        IPiece piece = _gameService.Pieces.First();
        bool isAvailable = _gameService.IsPieceAvailable(piece.Type);
        Assert.That(
            isAvailable,
            Is.EqualTo(true),
            "IsPieceAvailable should return true for an available piece"
        );
    }

    [Test]
    public void IsPieceAvailable_ShouldReturnFalse_WhenPieceIsNotAvailable()
    {
        IPlayer player = _gameService.Players.First();
        IPiece piece = _gameService.Pieces.First();

        _gameService.AssignPieceToPlayer(player, piece.Type);

        bool isAvailable = _gameService.IsPieceAvailable(piece.Type);
        Assert.That(
            isAvailable,
            Is.EqualTo(false),
            "IsPieceAvailable should return false for an unavailable piece"
        );
    }

    [Test]
    public void AssignPieceToPlayer_ShouldAssignPiece_WhenCalled()
    {
        IPlayer player = _gameService.Players.First();
        IPiece piece = _gameService.Pieces.First();
        GameResultDTO<bool> resultDTO = _gameService.AssignPieceToPlayer(player, piece.Type);
        Assert.That(
            resultDTO.Data,
            Is.EqualTo(true),
            "AssignPieceToPlayer should assign the piece to the player"
        );
    }

    [Test]
    public void AssignPieceToPlayer_ShouldReturnFailure_WhenPlayerIsNull()
    {
        // No Player Parsed in parameter
        IPiece piece = _gameService.Pieces.First();
        _gameService.AssignPieceToPlayer(null, piece.Type);

        GameResultDTO<bool> resultDTO = _gameService.AssignPieceToPlayer(null, piece.Type);
        Assert.That(
            resultDTO.IsSuccess,
            Is.EqualTo(false),
            "AssignPieceToPlayer should return failure when the player is null"
        );
    }

    [Test]
    public void AssignPieceToPlayer_ShouldReturnFailure_WhenPlayerIsNotFound()
    {
        IPlayer player = new Player("NonExistentPlayer");

        IPiece piece = _gameService.Pieces.First();

        GameResultDTO<bool> resultDTO = _gameService.AssignPieceToPlayer(player, piece.Type);
        Assert.That(
            resultDTO.IsSuccess,
            Is.EqualTo(false),
            "AssignPieceToPlayer should return failure when the player is null"
        );

        Assert.That(
            resultDTO.Error,
            Is.EqualTo("Player tidak ditemukan dalam game ini."),
            "AssignPieceToPlayer should return failure when the player is not found in the game"
        );
    }

    [Test]
    public void AssignPieceToPlayer_ShouldReturnFailure_WhenPieceIsNotAvailable()
    {
        IPlayer player = _gameService.Players.First();
        IPiece piece = _gameService.Pieces.First();

        _gameService.AssignPieceToPlayer(player, piece.Type);
        GameResultDTO<bool> resultDTO = _gameService.AssignPieceToPlayer(player, piece.Type);

        Console.WriteLine(resultDTO.Error);
        Assert.That(
            resultDTO.IsSuccess,
            Is.EqualTo(false),
            "AssignPieceToPlayer should return failure when the piece is not available"
        );

        Assert.That(
            resultDTO.Error,
            Is.EqualTo($"Piece {piece.Type} sudah diambil oleh pemain lain."),
            "AssignPieceToPlayer should return failure when the piece is not available"
        );
    }

    [Test]
    public void AssignPieceToPlayer_ShouldReturnFailure_WhenPieceNotFound()
    {
        IPlayer player = _gameService.Players.First();
        GameResultDTO<bool> resultDTO = _gameService.AssignPieceToPlayer(
            player,
            PieceType.Tophat + 999
        );

        Assert.That(
            resultDTO.IsSuccess,
            Is.EqualTo(false),
            "AssignPieceToPlayer should return failure when the piece is not found"
        );

        Assert.That(
            resultDTO.Error,
            Is.EqualTo($"Piece {PieceType.Tophat + 999} tidak ditemukan."),
            "AssignPieceToPlayer should return failure when the piece is not found"
        );
    }

    [Test]
    public void AssignPieceToPlayer_ShouldReturnFailure_WhenPlayerAlreadyHasPiece()
    {
        IPlayer player = _gameService.Players.First();
        IPiece piece1 = _gameService.Pieces.First();
        IPiece piece2 = _gameService.Pieces.Skip(1).First();

        _gameService.AssignPieceToPlayer(player, piece1.Type);

        GameResultDTO<bool> resultDTO = _gameService.AssignPieceToPlayer(player, piece2.Type);

        Assert.That(
            resultDTO.IsSuccess,
            Is.EqualTo(false),
            "AssignPieceToPlayer should return failure when the player already has a piece assigned"
        );

        Assert.That(
            resultDTO.Error,
            Is.EqualTo($"Player sudah memiliki piece."),
            "AssignPieceToPlayer should return failure when the player already has a piece assigned"
        );
    }

    [Test]
    public void GetPiece_ShouldReturnPiece_WhenPlayerPassed()
    {
        IPlayer player = _gameService.Players.First();
        IPiece piece = _gameService.Pieces.First();

        _gameService.AssignPieceToPlayer(player, piece.Type);

        GameResultDTO<IPiece> resultDTO = _gameService.GetPiece(player);
        Assert.That(
            resultDTO.IsSuccess,
            Is.EqualTo(true),
            "GetPiece should return success when the player has a piece assigned"
        );

        Assert.That(
            resultDTO.Data,
            Is.EqualTo(piece),
            "GetPiece should return the correct piece assigned to the player"
        );
    }

    [Test]
    public void GetPiece_ShouldReturnFailure_WhenPlayerNotAssigned()
    {
        GameResultDTO<IPiece> resultDTO = _gameService.GetPiece(null);
        Assert.That(
            resultDTO.IsSuccess,
            Is.EqualTo(false),
            "GetPiece should return failure when the player does not have a piece assigned"
        );

        Assert.That(
            resultDTO.Error,
            Is.EqualTo("Player tidak boleh kosong."),
            "GetPiece should return failure when the player does not have a piece assigned"
        );
    }

    [Test]
    public void GetPiece_ShouldReturnFailure_WhenPlayerNotHavePiece()
    {
        IPlayer player = _gameService.Players.First();

        GameResultDTO<IPiece> resultDTO = _gameService.GetPiece(player);
        Assert.That(
            resultDTO.IsSuccess,
            Is.EqualTo(false),
            "GetPiece should return failure when the player does not have a piece assigned"
        );

        Assert.That(
            resultDTO.Error,
            Is.EqualTo("Player tidak memiliki piece yang di-assign."),
            "GetPiece should return failure when the player does not have a piece assigned"
        );
    }

    [Test]
    public void NextPlayer_ShouldAdvanceToNextPlayer_WhenCalled()
    {
        int initialIndex = _gameService.CurrentPlayerIndex;
        _gameService.NextPlayer();
        int nextIndex = _gameService.CurrentPlayerIndex;

        Assert.That(
            nextIndex,
            Is.EqualTo((initialIndex + 1) % _gameService.Players.Count),
            "NextPlayer should advance to the next player in the list"
        );
    }

    [Test]
    public void NextPlayer_ShouldNotAdvance_WhenGameEnded()
    {
        int initialIndex = _gameService.CurrentPlayerIndex;
        SetGameEnded(_gameService, true);

        _gameService.NextPlayer();
        int nextIndex = _gameService.CurrentPlayerIndex;

        Assert.That(
            nextIndex,
            Is.EqualTo(initialIndex),
            "NextPlayer should not advance when the game has ended"
        );
    }

    [Test]
    public void NextPlayer_ShouldSkipBankruptPlayer_WhenNextIsBankrupt()
    {
        _gameService.Players[1].IsBankrupt = true;

        int initialIndex = _gameService.CurrentPlayerIndex;

        _gameService.NextPlayer();

        Assert.That(
            _gameService.CurrentPlayerIndex,
            Is.EqualTo((initialIndex + 2) % _gameService.Players.Count),
            "NextPlayer should skip bankrupt players"
        );
    }

    [Test]
    public void RollTurn_ShouldReturnSuccess_WhenCalled()
    {
        IPlayer player = _gameService.CurrentPlayer;
        IPiece piece = _gameService.Pieces.First();
        _gameService.AssignPieceToPlayer(player, piece.Type);

        GameResultDTO<RollTurnResult> resultDTO = _gameService.RollTurn();
        Assert.That(
            resultDTO.IsSuccess,
            Is.EqualTo(true),
            "RollTurn should return success when called"
        );

        Assert.That(
            resultDTO.Data,
            Is.Not.Null,
            "RollTurn should return a RollTurnResult when called"
        );
    }

    [Test]
    public void RollTurn_ShouldReturnFailure_PhaseNotWaitingRoll()
    {
        SetGamePhase(_gameService, GamePhase.WaitingBuyDecision);

        GameResultDTO<RollTurnResult> resultDTO = _gameService.RollTurn();
        Assert.That(
            resultDTO.IsSuccess,
            Is.EqualTo(false),
            "RollTurn should return failure when the game phase is not WaitingRoll"
        );

        Assert.That(
            resultDTO.Error,
            Is.EqualTo("Bukan fase yang tepat untuk melempar dadu."),
            "RollTurn should return failure with correct error message when the game phase is not WaitingRoll"
        );
    }

    [Test]
    public void RollTurn_ShouldReturnFailute_WhenPlayerIsInJail()
    {
        IPlayer player = _gameService.CurrentPlayer;
        IPiece piece = _gameService.Pieces.First();
        player.IsInJail = true;
        _gameService.AssignPieceToPlayer(player, piece.Type);

        GameResultDTO<RollTurnResult> resultDTO = _gameService.RollTurn();

        Assert.That(
            resultDTO.IsSuccess,
            Is.EqualTo(true),
            "RollTurn should return failure when the player is in jail"
        );

        Assert.That(
            resultDTO.Data,
            Is.Not.Null,
            "RollTurn should return a RollTurnResult even when the player is in jail"
        );
    }

    [Test]
    public void WhenPlayerIsInJailAndJailTurnRemainingZero()
    {
        IPlayer player = _gameService.CurrentPlayer;
        IPiece piece = _gameService.Pieces.First();
        player.IsInJail = true;
        player.JailTurnsRemaining = 0;
        _gameService.AssignPieceToPlayer(player, piece.Type);

        GameResultDTO<RollTurnResult> resultDTO = _gameService.RollTurn();

        Assert.That(
            resultDTO.IsSuccess,
            Is.EqualTo(true),
            "RollTurn should return failure when the player is in jail"
        );

        Assert.That(
            resultDTO.Data,
            Is.Not.Null,
            "RollTurn should return a RollTurnResult even when the player is in jail with 0 turns remaining"
        );
    }

    [Test]
    public void RollTurn_ShouldReturnReleased_WhenPlayerIsInJailAndTurnsRemaining()
    {
        IPlayer player = _gameService.CurrentPlayer;
        IPiece piece = _gameService.Pieces.First();
        player.IsInJail = true;
        player.JailTurnsRemaining = 3;
        _gameService.AssignPieceToPlayer(player, piece.Type);

        GameResultDTO<RollTurnResult> resultDTO = _gameService.RollTurn();

        Assert.That(resultDTO.IsSuccess, Is.EqualTo(true));
        Assert.That(resultDTO.Data?.JailRollResult, Is.EqualTo(JailRollResult.Released));
    }

    [Test]
    public void RollTurn_ShouldHandleBankruptPlayer_WhenPlayerIsBankrupt()
    {
        IPlayer player = _gameService.CurrentPlayer;
        IPiece piece = _gameService.Pieces.First();
        _gameService.AssignPieceToPlayer(player, piece.Type);
        player.IsBankrupt = true;

        GameResultDTO<RollTurnResult> resultDTO = _gameService.RollTurn();

        Assert.That(resultDTO.IsSuccess, Is.EqualTo(true));
        Assert.That(resultDTO.Data?.DiceTotal, Is.EqualTo(0));
    }

    [Test]
    public void RollTurn_ShouldSendToJail_WhenTripleDoubleRoll()
    {
        IPlayer player = _gameService.CurrentPlayer;
        IPiece piece = _gameService.Pieces.First();
        _gameService.AssignPieceToPlayer(player, piece.Type);
        player.DoubleRoll = 2; // sudah 2x double sebelumnya

        GameResultDTO<RollTurnResult> resultDTO = _gameService.RollTurn();

        Assert.That(resultDTO.IsSuccess, Is.EqualTo(true));

        Assert.That(
            player.IsInJail,
            Is.EqualTo(true),
            "Player seharusnya masuk penjara setelah roll triple double"
        );
    }

    [Test]
    public void EndTurn_ShouldReturnChanceTheGamePhase_WhenRepeatRollFalse()
    {
        IPlayer player = _gameService.CurrentPlayer;
        IPiece piece = _gameService.Pieces.First();
        _gameService.AssignPieceToPlayer(player, piece.Type);

        SetGamePhase(_gameService, GamePhase.WaitingBuyDecision);
        _gameService.EndTurn(false);

        Assert.That(
            _gameService.Phase,
            Is.EqualTo(GamePhase.WaitingRoll),
            "EndTurn should set the game phase to WaitingRoll when repeatRoll is false"
        );
    }

    [Test]
    public void RollResult_ShouldReturnSuccess_WhenRequiresBuyIsTrue()
    {
        IPlayer player = _gameService.CurrentPlayer;
        IPiece piece = _gameService.Pieces.First();
        _gameService.AssignPieceToPlayer(player, piece.Type);
    }

    [Test]
    public void HandleTileEffectsAfterMove_ShouldReturnSuccess_WhenCalled()
    {
        IPlayer player = _gameService.CurrentPlayer;
        IPiece piece = _gameService.Pieces.First();
        _gameService.AssignPieceToPlayer(player, piece.Type);

        ITile tile = _gameService.Board.Tiles.First(t => t.Type == TileType.DrawChance);

        HandleTileResultDTO resultDTO = _gameService.HandleTileEffectsAfterMove(player, tile);

        Assert.That(
            resultDTO.DrawnCard,
            Is.Not.Null,
            "HandleTileEffectsAfterMove should return success when called"
        );

        Assert.That(
            resultDTO.RequiresBuyDecision,
            Is.EqualTo(false),
            "HandleTileEffectsAfterMove should return false when called"
        );
    }

    [Test]
    public void HandleTileEffectsAfterMove_ShouldReturnRequiresBuyDecision_WhenPropertyAvailable()
    {
        IPlayer player = _gameService.CurrentPlayer;
        IPiece piece = _gameService.Pieces.First();
        _gameService.AssignPieceToPlayer(player, piece.Type);

        ITile tile = _gameService.Board.Tiles.First(t => t.Type == TileType.RentTile);

        HandleTileResultDTO resultDTO = _gameService.HandleTileEffectsAfterMove(player, tile);

        Assert.That(
            resultDTO.RequiresBuyDecision,
            Is.EqualTo(true),
            "HandleTileEffectsAfterMove should return true for RequiresBuyDecision when the tile is an available property"
        );

        Assert.That(
            resultDTO.DrawnCard,
            Is.Null,
            "HandleTileEffectsAfterMove should not return a drawn card when the tile is an available property"
        );
    }

    // [Test]
    // public void

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
        var property = typeof(Game).GetProperty(
            "Phase",
            System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic
        );

        property!.SetValue(game, phase);
    }
}
