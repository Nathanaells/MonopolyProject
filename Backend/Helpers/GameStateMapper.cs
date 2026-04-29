namespace Backend.Helpers;

using Backend.Domain.DTOs;
using Backend.Domain.Entities;
using Backend.Domain.Interfaces;
using Backend.DTOs;

public static class GameStateMapper
{
    public static GameStateResponse BuildState(Game game)
    {
        List<PlayerResponseDTO> players = game
            .Players.Select(p =>
            {
                GameResultDTO<int> balanceResult = game.GetPlayerBalance(p);
                int balance = balanceResult.IsSuccess ? balanceResult.Data : 0;

                GameResultDTO<ITile?> currentTileResult = game.GetCurrentTile(p);
                int tileIndex = -1;
                if (currentTileResult.IsSuccess && currentTileResult.Data != null)
                {
                    tileIndex = Array.IndexOf(game.Board.Tiles, currentTileResult.Data);
                }

                List<string> properties = game.GetPlayerProperties(p)
                    .Select(t => t.Asset?.City.PropertyCity.ToString())
                    .Where(x => x != null)
                    .Cast<string>()
                    .ToList();

                return new PlayerResponseDTO(
                    p.Name,
                    balance,
                    p.IsInJail,
                    p.IsBankrupt,
                    properties,
                    tileIndex
                );
            })
            .ToList();

        GameResultDTO<IPlayer?> winnerResult = game.GetWinnerOrNull();
        string? winnerName = winnerResult.IsSuccess ? winnerResult.Data?.Name : null;

        return new GameStateResponse(game.GameEnded, winnerName, game.CurrentPlayer.Name, players);
    }
}
