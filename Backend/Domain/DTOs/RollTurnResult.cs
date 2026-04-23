namespace Backend.Domain.DTOs;

using Backend.Domain.Interfaces;

public record RollTurnResult(
    int DiceTotal,
    ITile LandedTile,
    bool RequiresBuyDecision,
    ICard? DrawnCard
);
