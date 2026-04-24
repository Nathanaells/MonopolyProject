namespace Backend.Domain.Entities;

using Backend.Domain.Interfaces;

class Player : IPlayer
{
    public string Name { get; set; }
    public int DoubleRoll { get; set; }
    public bool IsInJail { get; set; }
    public bool IsBankrupt { get; set; }
    public int JailFreeCardCount { get; set; }
    public int JailTurnsRemaining { get; set; }

    public Player(string name)
    {
        Name = name;
        DoubleRoll = 0;
        IsInJail = false;
        IsBankrupt = false;
        JailFreeCardCount = 0;
        JailTurnsRemaining = 0;
    }
}
