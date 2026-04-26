namespace Backend.Domain.DTOs;
using Backend.Domain.Enums;
using Backend.Domain.Interfaces;

public record RollTurnResult(
    int DiceTotal,
    int Dice1,
    int Dice2,
    string LandedTileType,
    ITile? LandedProperty,
    ITile? LandedTile,
    bool RequiresBuyDecision,
    ICard? DrawnCard,
    JailRollResult JailRollResult
);

public record SendBuildingToBankResult(
    IPlayer Player,
    PropertyCity City,
    int HousesToSell,
    bool SellHotel
);
