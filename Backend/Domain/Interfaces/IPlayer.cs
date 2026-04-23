namespace Backend.Domain.Interfaces;

public interface IPlayer
{
    string Name { get; }
    bool IsInJail { get; set; }
    bool IsBankrupt { get; set; }
    int DoubleRoll { get; set; }
    int JailFreeCardCount { get; set; }

    int JailTurnsRemaining { get; set; }
}
