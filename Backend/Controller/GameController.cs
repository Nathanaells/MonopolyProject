using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Domain.Interfaces;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controller;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private static readonly object _lock = new();
    private static Game? _activeGame;

    [HttpPost("start")]
    public ActionResult<GameStateResponse> StartGame([FromBody] StartGameRequest request)
    {
        if (request.PlayerNames == null || request.PlayerNames.Count < 2)
        {
            return BadRequest("Minimal 2 pemain.");
        }

        lock (_lock)
        {
            var board = BoardFactory.CreateBoard();
            var players = PlayerFactory.CreatePlayers(request.PlayerNames.Distinct().ToArray());
            var pieces = PieceFactory.CreateStandardPieces();
            var cards = CreateDefaultCards();
            var money = MoneyFactory.CreateMoney();

            _activeGame = new Game(board, players, pieces, cards, money);
        }

        return Ok(BuildState(_activeGame!));
    }

    [HttpGet("state")]
    public ActionResult<GameStateResponse> GetState()
    {
        if (_activeGame == null)
        {
            return BadRequest("Game belum dimulai.");
        }

        return Ok(BuildState(_activeGame));
    }

    [HttpPost("turn/roll")]
    public ActionResult<RollTurnResponse> RollTurn()
    {
        if (_activeGame == null)
        {
            return BadRequest("Game belum dimulai.");
        }

        if (_activeGame.GameEnded)
        {
            return BadRequest("Game sudah selesai.");
        }

        var currentPlayer = _activeGame.CurrentPlayer;
        var dice1 = new Dice();
        var dice2 = new Dice();
        var total = _activeGame.HandleDiceRoll(dice1, dice2);

        _activeGame.MovePiece(currentPlayer, total);

        var currentTile = _activeGame.GetCurrentTile(currentPlayer);
        bool requiresBuyDecision = _activeGame.isPropertyAvailable(currentTile);
        ICard? drawnCard = null;

        if (!requiresBuyDecision)
        {
            _activeGame.TryExecuteLandingForCurrentPlayer(out drawnCard);
        }

        _activeGame.EndGame();

        return Ok(
            new RollTurnResponse(
                total,
                currentTile.Type.ToString(),
                currentTile.Asset?.City.PropertyCity.ToString(),
                requiresBuyDecision,
                drawnCard?.Description,
                BuildState(_activeGame)
            )
        );
    }

    [HttpPost("turn/buy-property")]
    public ActionResult<GameStateResponse> BuyProperty([FromBody] BuyPropertyRequest request)
    {
        if (_activeGame == null)
        {
            return BadRequest("Game belum dimulai.");
        }

        bool result = _activeGame.AttemptBuyCurrentProperty(_activeGame.CurrentPlayer, request.Buy);
        if (!result && request.Buy)
        {
            return BadRequest("Gagal beli properti (mungkin uang tidak cukup / bukan properti tersedia).");
        }

        _activeGame.EndGame();
        return Ok(BuildState(_activeGame));
    }

    [HttpPost("turn/end")]
    public ActionResult<GameStateResponse> EndTurn()
    {
        if (_activeGame == null)
        {
            return BadRequest("Game belum dimulai.");
        }

        _activeGame.EndGame();
        if (!_activeGame.GameEnded)
        {
            _activeGame.Playturn();
        }

        return Ok(BuildState(_activeGame));
    }

    [HttpPost("sell-property")]
    public ActionResult<SellResultResponse> SellPropertyToBank([FromBody] SellPropertyRequest request)
    {
        if (_activeGame == null)
        {
            return BadRequest("Game belum dimulai.");
        }

        var player = _activeGame.FindPlayerByName(request.PlayerName);
        if (player == null)
        {
            return BadRequest("Pemain tidak ditemukan.");
        }

        int income = _activeGame.SellPropertyToBank(player, request.City, request.IncludeBuildings);
        _activeGame.EndGame();

        return Ok(new SellResultResponse(income, BuildState(_activeGame)));
    }

    [HttpPost("sell-buildings")]
    public ActionResult<SellResultResponse> SellBuildingsToBank([FromBody] SellBuildingRequest request)
    {
        if (_activeGame == null)
        {
            return BadRequest("Game belum dimulai.");
        }

        var player = _activeGame.FindPlayerByName(request.PlayerName);
        if (player == null)
        {
            return BadRequest("Pemain tidak ditemukan.");
        }

        int income = _activeGame.SellBuildingsToBank(
            player,
            request.City,
            request.HousesToSell,
            request.SellHotel
        );

        _activeGame.EndGame();
        return Ok(new SellResultResponse(income, BuildState(_activeGame)));
    }

    [HttpPost("execute-card")]
    public ActionResult<GameStateResponse> ExecuteCardForCurrentPlayer([FromBody] ExecuteCardRequest request)
    {
        if (_activeGame == null)
        {
            return BadRequest("Game belum dimulai.");
        }

        ICard card = request.CardType.Equals("Chance", StringComparison.OrdinalIgnoreCase)
            ? new ChanceCard(request.Description ?? request.Behaviour.ToString(), request.Behaviour)
            : new CommunityCard(request.Description ?? request.Behaviour.ToString(), request.Behaviour);

        _activeGame.ExecuteCard(card, _activeGame.CurrentPlayer);
        _activeGame.EndGame();

        return Ok(BuildState(_activeGame));
    }

    private static List<ICard> CreateDefaultCards()
    {
        return new List<ICard>
        {
            new ChanceCard("Advance to GO", CardBehaviour.AdvanceToGo),
            new ChanceCard("Advance to Illinois Avenue", CardBehaviour.AdvanceToIllinois),
            new ChanceCard("Advance to St. Charles Place", CardBehaviour.AdvanceToStCharles),
            new ChanceCard("Advance token to nearest Utility", CardBehaviour.AdvanceNearestUtility),
            new ChanceCard("Advance token to nearest Railroad", CardBehaviour.AdvanceNearestRailroad),
            new ChanceCard("Bank pays you dividend of 50", CardBehaviour.BankPaysDividend),
            new ChanceCard("Get out of Jail Free", CardBehaviour.GetOutOfJailFree),
            new ChanceCard("Go back three spaces", CardBehaviour.GoBackThreeSpaces),
            new ChanceCard("Go to Jail", CardBehaviour.GoToJail),
            new ChanceCard("Make general repairs", CardBehaviour.MakeGeneralRepairs),
            new ChanceCard("Pay poor tax of 15", CardBehaviour.PayPoorTax),
            new ChanceCard("Take trip to Reading Railroad", CardBehaviour.TakeTripToReadingRailroad),
            new ChanceCard("Advance to Boardwalk", CardBehaviour.AdvanceToBoardwalk),
            new ChanceCard("Chairman of the Board", CardBehaviour.ChairmanOfTheBoard),
            new ChanceCard("Your building loan matures", CardBehaviour.YourBuildingLoanMatures),

            new CommunityCard("Advance to GO", CardBehaviour.AdvanceToGo),
            new CommunityCard("Bank error in your favor", CardBehaviour.BankError),
            new CommunityCard("Doctor's fees", CardBehaviour.DoctorFees),
            new CommunityCard("From sale of stock you get 50", CardBehaviour.FromSaleOfStock),
            new CommunityCard("Get out of jail free", CardBehaviour.GetOutOfJailFree),
            new CommunityCard("Go to jail", CardBehaviour.GoToJail),
            new CommunityCard("Holiday fund matures", CardBehaviour.HolidayFundMatures),
            new CommunityCard("Income tax refund", CardBehaviour.IncomeTaxRefund),
            new CommunityCard("Birthday", CardBehaviour.Birthday),
            new CommunityCard("Life insurance matures", CardBehaviour.LifeInsuranceMatures),
            new CommunityCard("Pay hospital fees", CardBehaviour.PayHospitalFees),
            new CommunityCard("Pay school fees", CardBehaviour.PaySchoolFees),
            new CommunityCard("Receive consultancy fee", CardBehaviour.ConsultancyFee),
            new CommunityCard("Street repairs", CardBehaviour.StreetRepairs),
            new CommunityCard("Beauty contest prize", CardBehaviour.BeautyContestPrize),
            new CommunityCard("Inherit money", CardBehaviour.InheritMoney),
        };
    }

    private static GameStateResponse BuildState(Game game)
    {
        var players = game
            .Players.Select(p =>
            {
                var tile = game.GetCurrentTile(p);
                var properties = game
                    .GetPlayerProperties(p)
                    .Select(t => t.Asset?.City.PropertyCity.ToString())
                    .Where(x => x != null)
                    .Cast<string>()
                    .ToList();

                return new PlayerStateResponse(
                    p.Name,
                    game.GetPlayerBalance(p),
                    p.IsInJail,
                    p.IsBankrupt,
                    tile.Type.ToString(),
                    tile.Asset?.City.PropertyCity.ToString(),
                    properties
                );
            })
            .ToList();

        return new GameStateResponse(
            game.GameEnded,
            game.GetWinnerOrNull()?.Name,
            game.CurrentPlayer.Name,
            players
        );
    }
}

public record StartGameRequest(List<string> PlayerNames);

public record BuyPropertyRequest(bool Buy);

public record SellPropertyRequest(string PlayerName, PropertyCity City, bool IncludeBuildings = true);

public record SellBuildingRequest(
    string PlayerName,
    PropertyCity City,
    int HousesToSell = 0,
    bool SellHotel = false
);

public record ExecuteCardRequest(CardBehaviour Behaviour, string CardType, string? Description = null);

public record RollTurnResponse(
    int DiceTotal,
    string LandedTileType,
    string? LandedProperty,
    bool RequiresBuyDecision,
    string? DrawnCardDescription,
    GameStateResponse State
);

public record SellResultResponse(int Income, GameStateResponse State);

public record GameStateResponse(
    bool IsGameEnded,
    string? Winner,
    string CurrentPlayer,
    List<PlayerStateResponse> Players
);

public record PlayerStateResponse(
    string Name,
    int Balance,
    bool IsInJail,
    bool IsBankrupt,
    string TileType,
    string? TileProperty,
    List<string> Properties
);
