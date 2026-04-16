namespace Domain.Interfaces;

public interface IDice
{
    public int MaxRolled { get; set; }
    public int Roll();
}
