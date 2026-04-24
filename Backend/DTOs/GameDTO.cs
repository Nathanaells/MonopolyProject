namespace Backend.DTOs;

using Backend.Domain.Enums;
using Backend.Domain.Interfaces;

public record StartGameRequestDTO(List<string> PlayerNames);

public record BuyPropertyRequestDTO(bool Buy);

public record SellPropertyRequestDTO(string PlayerName, string City, bool IncludeBuildings = true);

public record SellBuildingRequestDTO(
    string PlayerName,
    string City,
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
    int Dice1,
    int Dice2,
    string LandedTileType,
    string? LandedProperty,
    bool RequiresBuyDecision,
    string? DrawnCardDescription,
    JailRollResult JailRollResult,
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
    List<string> Properties,
    int? CurrentTileIndex = null
);

public record PieceResponseDTO(string PieceType, bool IsAvailable);

public record SelectPieceRequestDTO(string PlayerName, string PieceType);

public record BuyBuildingRequestDTO(string PlayerName, string City, bool BuildHotel = false);

public record SellAllAssetsRequestDTO(string PlayerName);
