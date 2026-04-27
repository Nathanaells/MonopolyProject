using Backend.Domain.DTOs;
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
    ActionResult<List<PieceResponseDTO>> GetAvailablePieces();
    ActionResult<GameStateResponse> SelectPiece(SelectPieceRequestDTO request);
    ActionResult<List<TileResponseDTO>> GetPlayerProperties(string playerName);
    ActionResult<GameStateResponse> BuyBuilding(BuyBuildingRequestDTO request);
    ActionResult<SellResultResponseDTO> SellAllAssets(SellAllAssetsRequestDTO request);
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
        {
            return BadRequest("Minimal 2 pemain diperlukan untuk memulai game.");
        }

        IBoard board = BoardFactory.CreateBoard();
        List<IPlayer> players = PlayerFactory.CreatePlayers(
            request.PlayerNames.Distinct().ToArray()
        );
        List<IPiece> pieces = PieceFactory.CreateStandardPieces();
        List<ICard> cards = CardFactory.CreateDefaultCards();
        List<IMoney> money = MoneyFactory.CreateMoney();

        _activeGame = new Game(board, players, pieces, cards, money);

        return Ok(GameStateMapper.BuildState(_activeGame));
    }

    [HttpGet("state")]
    public ActionResult<GameStateResponse> GetState()
    {
        if (_activeGame == null)
        {
            return BadRequest("Game belum dimulai.");
        }

        return Ok(GameStateMapper.BuildState(_activeGame));
    }

    [HttpGet("board/tiles")]
    public ActionResult<List<TileResponseDTO>> GetBoardTiles()
    {
        if (_activeGame == null)
        {
            return BadRequest("Game belum dimulai.");
        }

        List<TileResponseDTO> tiles = _activeGame
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
                        tile.Owner == null ? null : tile.Owner.Name,
                        tile.House,
                        tile.HasHotel
                    )
            )
            .ToList();

        return Ok(tiles);
    }

    [HttpGet("pieces")]
    public ActionResult<List<PieceResponseDTO>> GetAvailablePieces()
    {
        List<PieceResponseDTO> allPieces = Enum.GetValues<PieceType>()
            .Select(p => new PieceResponseDTO(
                p.ToString(),
                _activeGame == null || _activeGame.IsPieceAvailable(p)
            ))
            .ToList();

        return Ok(allPieces);
    }

    [HttpPost("select-piece")]
    public ActionResult<GameStateResponse> SelectPiece([FromBody] SelectPieceRequestDTO request)
    {
        if (_activeGame == null)
        {
            return BadRequest("Game belum dimulai.");
        }

        IPlayer? player = _activeGame.FindPlayerByName(request.PlayerName);
        if (player == null)
        {
            return BadRequest($"Pemain dengan nama '{request.PlayerName}' tidak ditemukan.");
        }

        if (!Enum.TryParse<PieceType>(request.PieceType, true, out PieceType pieceType))
        {
            return BadRequest(
                $"Piece '{request.PieceType}' tidak valid. Gunakan salah satu dari: {string.Join(", ", Enum.GetNames<PieceType>())}."
            );
        }

        GameResultDTO<bool> result = _activeGame.AssignPieceToPlayer(player, pieceType);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Ok(GameStateMapper.BuildState(_activeGame));
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
            return BadRequest("Game sudah selesai. Mulai game baru untuk bermain lagi.");
        }

        GameResultDTO<RollTurnResult> result = _activeGame.RollTurn();

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        RollTurnResult rollResult = result.Data!;

        return Ok(
            new RollTurnResponseDTO(
                rollResult.DiceTotal,
                rollResult.Dice1,
                rollResult.Dice2,
                rollResult.LandedTile?.Type.ToString() ?? "None",
                rollResult.LandedTile?.Asset?.City.PropertyCity.ToString(),
                rollResult.RequiresBuyDecision,
                rollResult.DrawnCard?.Description,
                rollResult.JailRollResult,
                GameStateMapper.BuildState(_activeGame)
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

        GameResultDTO<bool> result = _activeGame.HandleBuyDecision(request.Buy);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(GameStateMapper.BuildState(_activeGame));
    }

    [HttpPost("sell-property")]
    public ActionResult<SellResultResponseDTO> SellPropertyToBank(
        [FromBody] SellPropertyRequestDTO request
    )
    {
        if (_activeGame == null)
        {
            return BadRequest("Game belum dimulai.");
        }

        IPlayer? player = _activeGame.FindPlayerByName(request.PlayerName);
        if (player == null)
        {
            return BadRequest($"Pemain dengan nama '{request.PlayerName}' tidak ditemukan.");
        }

        if (!Enum.TryParse<PropertyCity>(request.City, true, out PropertyCity city))
        {
            return BadRequest($"Kota '{request.City}' tidak valid.");
        }

        GameResultDTO<int> sellResult = _activeGame.SellPropertyToBank(
            player,
            city,
            request.IncludeBuildings
        );

        if (!sellResult.IsSuccess)
        {
            return BadRequest(sellResult.Error);
        }

        _activeGame.EndGame();

        return Ok(
            new SellResultResponseDTO(sellResult.Data, GameStateMapper.BuildState(_activeGame))
        );
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

        IPlayer? player = _activeGame.FindPlayerByName(request.PlayerName);
        if (player == null)
        {
            return BadRequest($"Pemain dengan nama '{request.PlayerName}' tidak ditemukan.");
        }

        if (!Enum.TryParse<PropertyCity>(request.City, true, out PropertyCity city))
        {
            return BadRequest($"Kota '{request.City}' tidak valid.");
        }

        GameResultDTO<int> sellResult = _activeGame.SellBuildingsToBank(
            new SendBuildingToBankResult(player, city, request.HousesToSell, request.SellHotel)
        );

        if (!sellResult.IsSuccess)
        {
            return BadRequest(sellResult.Error);
        }

        return Ok(
            new SellResultResponseDTO(sellResult.Data, GameStateMapper.BuildState(_activeGame))
        );
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

        GameResultDTO<bool> executeResult = _activeGame.ExecuteCard(
            card,
            _activeGame.CurrentPlayer
        );

        if (!executeResult.IsSuccess)
        {
            return BadRequest(executeResult.Error);
        }

        return Ok(GameStateMapper.BuildState(_activeGame));
    }

    [HttpGet("player-properties")]
    public ActionResult<List<TileResponseDTO>> GetPlayerProperties([FromQuery] string playerName)
    {
        if (_activeGame == null)
        {
            return BadRequest("Game belum dimulai.");
        }

        IPlayer? player = _activeGame.FindPlayerByName(playerName);
        if (player == null)
        {
            return BadRequest($"Pemain dengan nama '{playerName}' tidak ditemukan.");
        }

        List<TileResponseDTO> properties = _activeGame
            .GetPlayerProperties(player)
            .Select(
                (tile, index) =>
                {
                    int tileIndex = Array.IndexOf(_activeGame.Board.Tiles, tile);
                    return new TileResponseDTO(
                        tileIndex,
                        tile.Type.ToString(),
                        new PointDTO(tile.Point.X, tile.Point.Y),
                        tile.Asset == null
                            ? null
                            : new AssetResponseDTO(
                                tile.Asset.Price.Value,
                                tile.Asset.City.PropertyCity.ToString(),
                                tile.Asset.Color?.ToString() ?? ""
                            ),
                        player.Name,
                        tile.House,
                        tile.HasHotel
                    );
                }
            )
            .ToList();

        return Ok(properties);
    }

    [HttpPost("buy-building")]
    public ActionResult<GameStateResponse> BuyBuilding([FromBody] BuyBuildingRequestDTO request)
    {
        if (_activeGame == null)
        {
            return BadRequest("Game belum dimulai.");
        }

        IPlayer? player = _activeGame.FindPlayerByName(request.PlayerName);
        if (player == null)
        {
            return BadRequest($"Pemain dengan nama '{request.PlayerName}' tidak ditemukan.");
        }

        if (!Enum.TryParse<PropertyCity>(request.City, true, out PropertyCity city))
        {
            return BadRequest($"Kota '{request.City}' tidak valid.");
        }

        GameResultDTO<bool> buildResult = _activeGame.BuyBuilding(player, city, request.BuildHotel);

        if (!buildResult.IsSuccess)
        {
            return BadRequest(new { message = buildResult.Error });
        }

        return Ok(GameStateMapper.BuildState(_activeGame));
    }

    [HttpPost("sell-all-assets")]
    public ActionResult<SellResultResponseDTO> SellAllAssets(
        [FromBody] SellAllAssetsRequestDTO request
    )
    {
        if (_activeGame == null)
        {
            return BadRequest("Game belum dimulai.");
        }

        IPlayer? player = _activeGame.FindPlayerByName(request.PlayerName);
        if (player == null)
        {
            return BadRequest($"Pemain dengan nama '{request.PlayerName}' tidak ditemukan.");
        }

        GameResultDTO<int> sellResult = _activeGame.SellAllAssetsToBank(player);

        if (!sellResult.IsSuccess)
        {
            return BadRequest(sellResult.Error);
        }

        return Ok(
            new SellResultResponseDTO(sellResult.Data, GameStateMapper.BuildState(_activeGame))
        );
    }
}
