using Backend.Domain.Enums;
using Backend.Domain.Interfaces;

public class ChanceCard : ICard
{
    public string Description { get; set; }

    public CardBehaviour Behaviour { get; set; }

    public ChanceCard(string description, CardBehaviour behaviour)
    {
        Description = description;
        Behaviour = behaviour;
    }
}
