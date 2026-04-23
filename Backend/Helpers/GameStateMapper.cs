namespace Backend.Helpers;

using Backend.Domain.Entities;
using Backend.DTOs;

public static class GameStateMapper
{
    public static GameStateResponse BuildState(Game game)
    {
        var players = game
            .Players.Select(p => new PlayerResponseDTO(
                p.Name,
                game.GetPlayerBalance(p),
                p.IsInJail,
                p.IsBankrupt,
                game.GetPlayerProperties(p)
                    .Select(t => t.Asset?.City.PropertyCity.ToString())
                    .Where(x => x != null)
                    .Cast<string>()
                    .ToList(),
                Array.IndexOf(game.Board.Tiles, game.GetCurrentTile(p))
            ))
            .ToList();

        return new GameStateResponse(
            game.GameEnded,
            game.GetWinnerOrNull()?.Name,
            game.CurrentPlayer.Name,
            players
        );
    }
}
