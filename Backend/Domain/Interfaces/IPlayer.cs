namespace Backend.Domain.Interfaces;

public interface IPlayer
{
    public string Name { get; set; }
    public int DoubleRoll { get; set; }
    public bool IsInJail { get; set; }
    public bool IsBankrupt { get; set; }
}
