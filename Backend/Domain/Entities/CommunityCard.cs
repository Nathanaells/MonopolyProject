using Backend.Domain.Enums;
using Backend.Domain.Interfaces;

public class CommunityCard : ICard
{
    public string Description { get; set; }

    public CardBehaviour Behaviour { get; set; }

    public CommunityCard(string description, CardBehaviour behaviour)
    {
        Description = description;
        Behaviour = behaviour;
    }
}
