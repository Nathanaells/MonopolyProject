namespace Backend.Factories;

using Backend.Domain.Interfaces;

public class DiceFactory
{
    public static List<IDice> CreateDice()
    {
        List<IDice> dice = [new Dice(), new Dice()];
        return dice;
    }
}
