using Backend.Domain.Entities;
using Backend.Domain.Interfaces;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controller;

public interface IGameController
{
    ActionResult<GameStateResponse> StartGame(StartGameRequestDTO request);
    ActionResult<GameStateResponse> GetState();
    ActionResult<List<TileResponseDTO>> GetBoardTiles();
    ActionResult<RollTurnResponseDTO> RollTurn();
    ActionResult<GameStateResponse> BuyProperty(BuyPropertyRequestDTO request);
    ActionResult<GameStateResponse> EndTurn();
    ActionResult<SellResultResponseDTO> SellPropertyToBank(SellPropertyRequestDTO request);
    ActionResult<SellResultResponseDTO> SellBuildingsToBank(SellBuildingRequestDTO request);
    ActionResult<GameStateResponse> ExecuteCardForCurrentPlayer(ExecuteCardRequestDTO request);
}

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private static Game? _activeGame;

    [HttpPost("start")]
    public ActionResult<GameStateResponse> StartGame([FromBody] StartGameRequestDTO request)
    {
        if (request.PlayerNames == null || request.PlayerNames.Count < 2)
        {
            return BadRequest("Minimal 2 pemain.");
        }

        var board = BoardFactory.CreateBoard();
        var players = PlayerFactory.CreatePlayers(request.PlayerNames.Distinct().ToArray());
        var pieces = PieceFactory.CreateStandardPieces();
        var cards = CardFactory.CreateDefaultCards();
        var money = MoneyFactory.CreateMoney();

        _activeGame = new Game(board, players, pieces, cards, money);

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

    [HttpGet("board/tiles")]
    public ActionResult<List<TileResponseDTO>> GetBoardTiles()
    {
        if (_activeGame == null)
        {
            return BadRequest("Game belum dimulai.");
        }

        var tiles = _activeGame
            .Board.Tiles.Select(
                (tile, index) =>
                    new TileResponseDTO(
                        index,
                        tile.Type.ToString(),
                        new PointDTO(tile.Point.X, tile.Point.Y),
                        tile.Asset == null
                            ? null
                            : new AssetResponseDTO(
                                (int)tile.Asset.Price.Value,
                                tile.Asset.City.PropertyCity.ToString(),
                                tile.Asset.Color?.ToString() ?? ""
                            ),
                        tile.Owner == null
                            ? null
                            : new PlayerResponseDTO(
                                tile.Owner.Name,
                                _activeGame.GetPlayerBalance(tile.Owner),
                                tile.Owner.IsInJail,
                                tile.Owner.IsBankrupt,
                                _activeGame
                                    .GetPlayerProperties(tile.Owner)
                                    .Select(t => t.Asset?.City.PropertyCity.ToString())
                                    .Where(x => x is not null)
                                    .Cast<string>()
                                    .ToList()
                            ),
                        tile.House,
                        tile.HasHotel
                    )
            )
            .ToList();

        return Ok(tiles);
    }

    [HttpPost("turn/roll")]
    public ActionResult<RollTurnResponseDTO> RollTurn()
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
            new RollTurnResponseDTO(
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
    public ActionResult<GameStateResponse> BuyProperty([FromBody] BuyPropertyRequestDTO request)
    {
        if (_activeGame == null)
        {
            return BadRequest("Game belum dimulai.");
        }

        bool result = _activeGame.AttemptBuyCurrentProperty(_activeGame.CurrentPlayer, request.Buy);
        if (!result && request.Buy)
        {
            return BadRequest(
                "Gagal beli properti (mungkin uang tidak cukup / bukan properti tersedia)."
            );
        }

        _activeGame.EndGame();
        return Ok(BuildState(_activeGame));
    }

    // [HttpPost("turn/end")]
    // public ActionResult<GameStateResponse> EndTurn()
    // {
    //     if (_activeGame == null)
    //     {
    //         return BadRequest("Game belum dimulai.");
    //     }

    //     _activeGame.EndGame();
    //     if (!_activeGame.GameEnded)
    //     {
    //         _activeGame.Playturn();
    //     }

    //     return Ok(BuildState(_activeGame));
    // }

    [HttpPost("sell-property")]
    public ActionResult<SellResultResponseDTO> SellPropertyToBank(
        [FromBody] SellPropertyRequestDTO request
    )
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

        return Ok(new SellResultResponseDTO(income, BuildState(_activeGame)));
    }

    [HttpPost("sell-buildings")]
    public ActionResult<SellResultResponseDTO> SellBuildingsToBank(
        [FromBody] SellBuildingRequestDTO request
    )
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
        return Ok(new SellResultResponseDTO(income, BuildState(_activeGame)));
    }

    [HttpPost("execute-card")]
    public ActionResult<GameStateResponse> ExecuteCardForCurrentPlayer(
        [FromBody] ExecuteCardRequestDTO request
    )
    {
        if (_activeGame == null)
        {
            return BadRequest("Game belum dimulai.");
        }

        ICard card = request.CardType.Equals("Chance", StringComparison.OrdinalIgnoreCase)
            ? new ChanceCard(request.Description ?? request.Behaviour.ToString(), request.Behaviour)
            : new CommunityCard(
                request.Description ?? request.Behaviour.ToString(),
                request.Behaviour
            );

        _activeGame.ExecuteCard(card, _activeGame.CurrentPlayer);
        _activeGame.EndGame();

        return Ok(BuildState(_activeGame));
    }

    private static GameStateResponse BuildState(Game game)
    {
        var players = game
            .Players.Select(p =>
            {
                var tile = game.GetCurrentTile(p);
                var properties = game.GetPlayerProperties(p)
                    .Select(t => t.Asset?.City.PropertyCity.ToString())
                    .Where(x => x != null)
                    .Cast<string>()
                    .ToList();

                return new PlayerResponseDTO(
                    p.Name,
                    game.GetPlayerBalance(p),
                    p.IsInJail,
                    p.IsBankrupt,
                    game.GetPlayerProperties(p)
                        .Select(t => t.Asset?.City.PropertyCity.ToString())
                        .Where(x => x is not null)
                        .Cast<string>()
                        .ToList()
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
