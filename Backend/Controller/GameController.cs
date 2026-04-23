using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Domain.Interfaces;
using Backend.DTOs;
using Backend.Helpers;
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
    ActionResult<SellResultResponseDTO> SellPropertyToBank(SellPropertyRequestDTO request);
    ActionResult<SellResultResponseDTO> SellBuildingsToBank(SellBuildingRequestDTO request);
    ActionResult<GameStateResponse> ExecuteCardForCurrentPlayer(ExecuteCardRequestDTO request);
}

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase, IGameController
{
    private static Game? _activeGame;

    [HttpPost("start")]
    public ActionResult<GameStateResponse> StartGame([FromBody] StartGameRequestDTO request)
    {
        if (request.PlayerNames == null || request.PlayerNames.Count < 2)
            return BadRequest("Minimal 2 pemain.");

        var board = BoardFactory.CreateBoard();
        var players = PlayerFactory.CreatePlayers(request.PlayerNames.Distinct().ToArray());
        var pieces = PieceFactory.CreateStandardPieces();
        var cards = CardFactory.CreateDefaultCards();
        var money = MoneyFactory.CreateMoney();

        _activeGame = new Game(board, players, pieces, cards, money);

        return Ok(GameStateMapper.BuildState(_activeGame));
    }

    [HttpGet("state")]
    public ActionResult<GameStateResponse> GetState()
    {
        if (_activeGame == null)
            return BadRequest("Game belum dimulai.");

        return Ok(GameStateMapper.BuildState(_activeGame));
    }

    [HttpGet("board/tiles")]
    public ActionResult<List<TileResponseDTO>> GetBoardTiles()
    {
        if (_activeGame == null)
            return BadRequest("Game belum dimulai.");

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
                                tile.Asset.Price.Value,
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
            return BadRequest("Game belum dimulai.");

        if (_activeGame.GameEnded)
            return BadRequest("Game sudah selesai.");

        try
        {
            var result = _activeGame.RollTurn();

            return Ok(
                new RollTurnResponseDTO(
                    result.DiceTotal,
                    result.LandedTile?.Type.ToString() ?? "None",
                    result.LandedTile?.Asset?.City.PropertyCity.ToString(),
                    result.RequiresBuyDecision,
                    result.DrawnCard?.Description,
                    GameStateMapper.BuildState(_activeGame)
                )
            );
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("turn/buy-property")]
    public ActionResult<GameStateResponse> BuyProperty([FromBody] BuyPropertyRequestDTO request)
    {
        if (_activeGame == null)
            return BadRequest("Game Not Started.");

        try
        {
            _activeGame.HandleBuyDecision(request.Buy);

            return Ok(GameStateMapper.BuildState(_activeGame));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("sell-property")]
    public ActionResult<SellResultResponseDTO> SellPropertyToBank(
        [FromBody] SellPropertyRequestDTO request
    )
    {
        if (_activeGame == null)
            return BadRequest("Game belum dimulai.");

        var player = _activeGame.FindPlayerByName(request.PlayerName);
        if (player == null)
            return BadRequest("Pemain tidak ditemukan.");

        int income = _activeGame.SellPropertyToBank(player, request.City, request.IncludeBuildings);
        _activeGame.EndGame();

        return Ok(new SellResultResponseDTO(income, GameStateMapper.BuildState(_activeGame)));
    }

    [HttpPost("sell-buildings")]
    public ActionResult<SellResultResponseDTO> SellBuildingsToBank(
        [FromBody] SellBuildingRequestDTO request
    )
    {
        if (_activeGame == null)
            return BadRequest("Game belum dimulai.");

        var player = _activeGame.FindPlayerByName(request.PlayerName);
        if (player == null)
            return BadRequest("Pemain tidak ditemukan.");

        int income = _activeGame.SellBuildingsToBank(
            player,
            request.City,
            request.HousesToSell,
            request.SellHotel
        );

        return Ok(new SellResultResponseDTO(income, GameStateMapper.BuildState(_activeGame)));
    }

    [HttpPost("execute-card")]
    public ActionResult<GameStateResponse> ExecuteCardForCurrentPlayer(
        [FromBody] ExecuteCardRequestDTO request
    )
    {
        if (_activeGame == null)
            return BadRequest("Game belum dimulai.");

        ICard card = request.CardType.Equals("Chance", StringComparison.OrdinalIgnoreCase)
            ? new ChanceCard(request.Description ?? request.Behaviour.ToString(), request.Behaviour)
            : new CommunityCard(
                request.Description ?? request.Behaviour.ToString(),
                request.Behaviour
            );

        // Console.WriteLine($"Executing card: {card.Description} with behaviour {card.Behaviour}");
        _activeGame.ExecuteCard(card, _activeGame.CurrentPlayer);

        return Ok(GameStateMapper.BuildState(_activeGame));
    }
}
