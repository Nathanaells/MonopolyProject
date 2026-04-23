namespace Backend.DTOs;

using Backend.Domain.Enums;
using Backend.Domain.Interfaces;

public record StartGameRequestDTO(List<string> PlayerNames);

public record BuyPropertyRequestDTO(bool Buy);

public record SellPropertyRequestDTO(
    string PlayerName,
    PropertyCity City,
    bool IncludeBuildings = true
);

public record SellBuildingRequestDTO(
    string PlayerName,
    PropertyCity City,
    int HousesToSell = 0,
    bool SellHotel = false
);

public record ExecuteCardRequestDTO(
    CardBehaviour Behaviour,
    string CardType,
    string? Description = null
);

public record TileResponseDTO(
    int Index,
    string Type,
    PointDTO Position,
    AssetResponseDTO? Asset,
    PlayerResponseDTO? Owner,
    int? Houses,
    bool? HasHotel
);

public record PointDTO(int X, int Y);

public record AssetResponseDTO(int Price, string City, string Color);

public record RollTurnResponseDTO(
    int DiceTotal,
    string LandedTileType,
    string? LandedProperty,
    bool RequiresBuyDecision,
    string? DrawnCardDescription,
    GameStateResponse State
);

public record SellResultResponseDTO(int Income, GameStateResponse State);

public record GameStateResponse(
    bool IsGameEnded,
    string? Winner,
    string CurrentPlayer,
    List<PlayerResponseDTO> Players
);

public record PlayerResponseDTO(
    string Name,
    int Balance,
    bool IsInJail,
    bool IsBankrupt,
    List<string> Properties
);

public record RollTurnResult(
    int DiceTotal,
    ITile LandedTile,
    bool RequiresBuyDecision,
    ICard? DrawnCard
);
