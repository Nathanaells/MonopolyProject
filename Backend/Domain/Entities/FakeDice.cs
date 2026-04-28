using Backend.Domain.Interfaces;

public class FakeDice : IDice
{
    public int MaxRolled { get; set; }

    public FakeDice(int maxRolled)
    {
        MaxRolled = maxRolled;
    }

    public int Roll()
    {
        return MaxRolled;
    }
}
