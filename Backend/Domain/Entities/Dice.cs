using Backend.Domain.Interfaces;

class Dice : IDice
{
    public int MaxRolled { get; set; }
    private Random _random = new Random();

    public Dice()
    {
        MaxRolled = Roll();
    }

    public int Roll()
    {
        return _random.Next(1, 7);
    }
}
