namespace Backend.Domain.Entities;

using Backend.Domain.Interfaces;

public class User : IUser
{
    public int Id { get; init; }
    public string Name { get; init; }
    public string Username { get; init; }
    public string Password { get; init; }

    public User(string name, string username, string password)
    {
        Name = name;
        Username = username;
        Password = password;
    }
}
