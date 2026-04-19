namespace Backend.Services;
using Backend.Domain.Interfaces;
using Backend.Domain.Entities;




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