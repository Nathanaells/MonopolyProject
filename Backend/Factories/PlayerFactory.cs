namespace Backend.Factories;

using Backend.Domain.Entities;
using Backend.Domain.Interfaces;

public class PlayerFactory
{
    public static List<IPlayer> CreatePlayers(string[] playerNames)
    {
        var players = new List<IPlayer>();
        foreach (var name in playerNames)
        {
            players.Add(new Player(name));
        }
        return players;
    }
}
