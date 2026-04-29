using Backend.Domain.DTOs;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Domain.Interfaces;
using Backend.DTOs;
using Backend.Factories;
using Backend.Helpers;
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
//COntroller Base Dari Microsoft.AspNetCore.Mvc, IGameController Dari Interface yang kita buat
{
    private static Game? _activeGame;
    private readonly ILogger<GameController> _logger;

    public GameController(ILogger<GameController> logger)
    {
        _logger = logger;
    }

    [HttpPost("start")]
    public ActionResult<GameStateResponse> StartGame([FromBody] StartGameRequestDTO request)
    {
        _logger.LogInformation(
            "Starting new game with {PlayerCount} players: {Players}",
            request.PlayerNames.Count,
            request.PlayerNames
        );

        if (request.PlayerNames == null || request.PlayerNames.Count < 2)
        {
            _logger.LogWarning("Invalid player count.");
            return BadRequest("Minimal 2 pemain diperlukan untuk memulai game.");
        }

        IBoard board = BoardFactory.CreateBoard();
        List<IPlayer> players = PlayerFactory.CreatePlayers(
            request.PlayerNames.Distinct().ToArray()
        );
        List<IPiece> pieces = PieceFactory.CreateStandardPieces();
        List<ICard> cards = CardFactory.CreateDefaultCards();
        List<IMoney> money = MoneyFactory.CreateMoney();
        List<IDice> dice = DiceFactory.CreateDice();

        _activeGame = new Game(board, players, pieces, cards, money, dice);

        _logger.LogInformation(
            "Game Started With Players: {players}",
            string.Join(", ", request.PlayerNames)
        );

        return Ok(GameStateMapper.BuildState(_activeGame));
    }

    [HttpGet("state")]
    public ActionResult<GameStateResponse> GetState()
    {
        if (_activeGame == null)
        {
            _logger.LogWarning("Attempted to get game state without an active game.");
            return BadRequest("Game belum dimulai.");
        }

        return Ok(GameStateMapper.BuildState(_activeGame));
    }

    [HttpGet("board/tiles")]
    public ActionResult<List<TileResponseDTO>> GetBoardTiles()
    {
        if (_activeGame == null)
        {
            _logger.LogWarning("Attempted to get board tiles without an active game.");
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

        _logger.LogInformation(
            "Retrieved TileResponseDTO with data total tiles: {TileCount}",
            tiles.Count
        );

        return Ok(tiles);
    }

    [HttpGet("pieces")]
    public ActionResult<List<PieceResponseDTO>> GetAvailablePieces()
    {
        _logger.LogInformation("Retrieving available pieces for selection.");

        List<PieceResponseDTO> allPieces = Enum.GetValues<PieceType>()
            .Select(p => new PieceResponseDTO(
                p.ToString(),
                _activeGame == null || _activeGame.IsPieceAvailable(p)
            ))
            .ToList();

        _logger.LogInformation(
            "Available pieces retrieved: {Pieces}",
            string.Join(", ", allPieces.Select(p => $"{p.PieceType} (Available: {p.IsAvailable})"))
        );
        return Ok(allPieces);
    }

    [HttpPost("select-piece")]
    public ActionResult<GameStateResponse> SelectPiece([FromBody] SelectPieceRequestDTO request)
    {
        _logger.LogInformation(
            "Player '{PlayerName}' is attempting to select piece '{PieceType}'.",
            request.PlayerName,
            request.PieceType
        );

        if (_activeGame == null)
        {
            _logger.LogWarning(
                "Player '{PlayerName}' attempted to select a piece when game is not start yet.",
                request.PlayerName
            );
            return BadRequest("Game belum dimulai.");
        }

        IPlayer? player = _activeGame.FindPlayerByName(request.PlayerName);

        if (player == null)
        {
            _logger.LogWarning(
                "Player '{PlayerName}' attempted to select a piece but was not found.",
                request.PlayerName
            );

            return BadRequest($"Pemain dengan nama '{request.PlayerName}' tidak ditemukan.");
        }

        if (!Enum.TryParse(request.PieceType, true, out PieceType pieceType))
        {
            _logger.LogWarning(
                "Player '{PlayerName}' attempted to select an invalid piece '{PieceType}'.",
                request.PlayerName,
                request.PieceType
            );
            return BadRequest(
                $"Piece '{request.PieceType}' tidak valid. Gunakan salah satu dari: {string.Join(", ", Enum.GetNames<PieceType>())}."
            );
        }

        GameResultDTO<bool> result = _activeGame.AssignPieceToPlayer(player, pieceType);

        if (!result.IsSuccess)
        {
            _logger.LogWarning(
                "Player '{PlayerName}' failed to select piece '{PieceType}': {Error}",
                request.PlayerName,
                request.PieceType,
                result.Error
            );
            return BadRequest(result.Error);
        }

        _logger.LogInformation(
            "Player '{PlayerName}' successfully selected piece '{PieceType}'.",
            request.PlayerName,
            request.PieceType
        );

        return Ok(GameStateMapper.BuildState(_activeGame));
    }

    [HttpPost("turn/roll")]
    public ActionResult<RollTurnResponseDTO> RollTurn()
    {
        _logger.LogInformation("Rolling turn for current player.");

        if (_activeGame == null)
        {
            _logger.LogWarning("Attempted to roll turn without an active game.");

            return BadRequest("Game belum dimulai.");
        }

        if (_activeGame.GameEnded)
        {
            _logger.LogWarning("Attempted to roll turn but game has already ended.");

            return BadRequest("Game sudah selesai. Mulai game baru untuk bermain lagi.");
        }

        GameResultDTO<RollTurnResult> result = _activeGame.RollTurn();

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to roll turn: {Error}", result.Error);

            return BadRequest(result.Error);
        }

        RollTurnResult rollResult = result.Data!;

        _logger.LogInformation(
            "Turn rolled: Dice Total={DiceTotal}, Landed Tile={LandedTile}, Requires Buy Decision={RequiresBuyDecision}",
            rollResult.DiceTotal,
            rollResult.LandedTile?.Type.ToString() ?? "None",
            rollResult.RequiresBuyDecision
        );

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
        _logger.LogInformation(
            "Current player is attempting to buy property: Buy={Buy}",
            request.Buy
        );

        if (_activeGame == null)
        {
            _logger.LogWarning("Attempted to buy property without an active game.");

            return BadRequest("Game belum dimulai.");
        }

        GameResultDTO<bool> result = _activeGame.HandleBuyDecision(request.Buy);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to handle buy decision: {Error}", result.Error);

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
            _logger.LogWarning("Attempted to sell property without an active game.");

            return BadRequest("Game belum dimulai.");
        }

        IPlayer? player = _activeGame.FindPlayerByName(request.PlayerName);
        if (player == null)
        {
            _logger.LogWarning(
                "Attempted to sell property for unknown player: {PlayerName}",
                request.PlayerName
            );

            return BadRequest($"Pemain dengan nama '{request.PlayerName}' tidak ditemukan.");
        }

        if (!Enum.TryParse<PropertyCity>(request.City, true, out PropertyCity city))
        {
            _logger.LogWarning(
                "Attempted to sell property with invalid city: {City}",
                request.City
            );

            return BadRequest($"Kota '{request.City}' tidak valid.");
        }

        GameResultDTO<int> sellResult = _activeGame.SellPropertyToBank(
            player,
            city,
            request.IncludeBuildings
        );

        if (!sellResult.IsSuccess)
        {
            _logger.LogWarning("Failed to sell property: {Error}", sellResult.Error);
            return BadRequest(sellResult.Error);
        }

        _activeGame.EndGame();

        _logger.LogInformation(
            "Property sold successfully: Player={PlayerName}, City={City}, Income={Income}",
            request.PlayerName,
            city,
            sellResult.Data
        );

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
            _logger.LogWarning("Attempted to sell buildings without an active game.");
            return BadRequest("Game belum dimulai.");
        }

        IPlayer? player = _activeGame.FindPlayerByName(request.PlayerName);
        if (player == null)
        {
            _logger.LogWarning(
                "Attempted to sell buildings for unknown player: {PlayerName}",
                request.PlayerName
            );
            return BadRequest($"Pemain dengan nama '{request.PlayerName}' tidak ditemukan.");
        }

        if (!Enum.TryParse<PropertyCity>(request.City, true, out PropertyCity city))
        {
            _logger.LogWarning(
                "Attempted to sell buildings with invalid city: {City}",
                request.City
            );
            return BadRequest($"Kota '{request.City}' tidak valid.");
        }

        GameResultDTO<int> sellResult = _activeGame.SellBuildingsToBank(
            new SendBuildingToBankResult(player, city, request.HousesToSell, request.SellHotel)
        );

        if (!sellResult.IsSuccess)
        {
            _logger.LogWarning("Failed to sell buildings: {Error}", sellResult.Error);

            return BadRequest(sellResult.Error);
        }

        _logger.LogInformation(
            "Buildings sold successfully: Player={PlayerName}, City={City}, Houses Sold={HousesToSell}, Hotel Sold={SellHotel}, Income={Income}",
            request.PlayerName,
            city,
            request.HousesToSell,
            request.SellHotel,
            sellResult.Data
        );

        return Ok(
            new SellResultResponseDTO(sellResult.Data, GameStateMapper.BuildState(_activeGame))
        );
    }

    [HttpPost("execute-card")]
    public ActionResult<GameStateResponse> ExecuteCardForCurrentPlayer(
        [FromBody] ExecuteCardRequestDTO request
    )
    {
        _logger.LogInformation(
            "Current player is attempting to execute card: Type={CardType}, Behaviour={Behaviour}",
            request.CardType,
            request.Behaviour
        );

        if (_activeGame == null)
        {
            _logger.LogWarning("Attempted to execute card without an active game.");

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
            _logger.LogWarning("Failed to execute card: {Error}", executeResult.Error);
            return BadRequest(executeResult.Error);
        }

        _logger.LogInformation(
            "Card executed successfully: Type={CardType}, Behaviour={Behaviour}",
            request.CardType,
            request.Behaviour
        );

        return Ok(GameStateMapper.BuildState(_activeGame));
    }

    [HttpGet("player-properties")]
    public ActionResult<List<TileResponseDTO>> GetPlayerProperties([FromQuery] string playerName)
    {
        _logger.LogInformation("Retrieving properties for player: {PlayerName}", playerName);

        if (_activeGame == null)
        {
            _logger.LogWarning("Attempted to retrieve player properties without an active game.");

            return BadRequest("Game belum dimulai.");
        }

        IPlayer? player = _activeGame.FindPlayerByName(playerName);

        if (player == null)
        {
            _logger.LogWarning(
                "Player not Found {playerName} attempted to retrieve properties but was not found.",
                playerName
            );
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

        _logger.LogInformation(
            "Retrieved properties for player {PlayerName}: {PropertyCount} properties found.",
            playerName,
            properties.Count
        );

        return Ok(properties);
    }

    [HttpPost("buy-building")]
    public ActionResult<GameStateResponse> BuyBuilding([FromBody] BuyBuildingRequestDTO request)
    {
        _logger.LogInformation(
            "Player '{PlayerName}' is attempting to buy building in city '{City}' with hotel option: {BuildHotel}",
            request.PlayerName,
            request.City,
            request.BuildHotel
        );

        if (_activeGame == null)
        {
            _logger.LogWarning(
                "Player '{PlayerName}' attempted to buy building when game is not start yet.",
                request.PlayerName
            );

            return BadRequest("Game belum dimulai.");
        }

        IPlayer? player = _activeGame.FindPlayerByName(request.PlayerName);

        if (player == null)
        {
            _logger.LogWarning(
                "Player '{PlayerName}' attempted to buy building but was not found.",
                request.PlayerName
            );

            return BadRequest($"Pemain dengan nama '{request.PlayerName}' tidak ditemukan.");
        }

        if (!Enum.TryParse<PropertyCity>(request.City, true, out PropertyCity city))
        {
            _logger.LogWarning(
                "Player '{PlayerName}' attempted to buy building with invalid city: {City}",
                request.PlayerName,
                request.City
            );

            return BadRequest($"Kota '{request.City}' tidak valid.");
        }

        GameResultDTO<bool> buildResult = _activeGame.BuyBuilding(player, city, request.BuildHotel);

        if (!buildResult.IsSuccess)
        {
            _logger.LogWarning(
                "Player '{PlayerName}' failed to buy building in city '{City}': {Error}",
                request.PlayerName,
                request.City,
                buildResult.Error
            );

            return BadRequest(new { message = buildResult.Error });
        }

        _logger.LogInformation(
            "Player '{PlayerName}' successfully bought building in city '{City}' with hotel option: {BuildHotel}",
            request.PlayerName,
            request.City,
            request.BuildHotel
        );

        return Ok(GameStateMapper.BuildState(_activeGame));
    }

    [HttpPost("sell-all-assets")]
    public ActionResult<SellResultResponseDTO> SellAllAssets(
        [FromBody] SellAllAssetsRequestDTO request
    )
    {
        _logger.LogInformation(
            "Player '{PlayerName}' is attempting to sell all assets to bank.",
            request.PlayerName
        );

        if (_activeGame == null)
        {
            _logger.LogWarning(
                "Player '{PlayerName}' attempted to sell all assets when game is not start yet.",
                request.PlayerName
            );

            return BadRequest("Game belum dimulai.");
        }

        IPlayer? player = _activeGame.FindPlayerByName(request.PlayerName);

        if (player == null)
        {
            _logger.LogWarning(
                "Player '{PlayerName}' attempted to sell all assets but was not found.",
                request.PlayerName
            );

            return BadRequest($"Pemain dengan nama '{request.PlayerName}' tidak ditemukan.");
        }

        GameResultDTO<int> sellResult = _activeGame.SellAllAssetsToBank(player);

        if (!sellResult.IsSuccess)
        {
            _logger.LogWarning(
                "Player '{PlayerName}' failed to sell all assets to bank: {Error}",
                request.PlayerName,
                sellResult.Error
            );
            return BadRequest(sellResult.Error);
        }

        _logger.LogInformation(
            "Player '{PlayerName}' successfully sold all assets to bank for {Amount} coins.",
            request.PlayerName,
            sellResult.Data
        );

        return Ok(
            new SellResultResponseDTO(sellResult.Data, GameStateMapper.BuildState(_activeGame))
        );
    }
}
