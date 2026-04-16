using Backend.Domain.Enums;

public interface ICard
{
    public string Description { get; set; }
    public CardBehaviour behaviour { get; set; }
}
