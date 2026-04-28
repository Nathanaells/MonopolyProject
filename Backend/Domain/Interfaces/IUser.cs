namespace Backend.Domain.Interfaces;

public interface IUser
{
    public string Name { get; }
    public string Username { get; }
    public string Password { get; }
}
