using Domain.Enums;

namespace Backend.Domain.Interfaces;

public interface ICard
{
    public string Description { get; set; }
    public CardBehaviour Behaviour { get; set; }
}
