namespace Backend.Domain.DTOs;

using Backend.Domain.Interfaces;

public record HandleTileResultDTO
{
    public ICard? DrawnCard { get; init; }
    public bool RequiresBuyDecision { get; init; }
}
