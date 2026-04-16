using Backend.Domain.Enums;
using Backend.Domain.Interfaces;

class Money : IMoney
{
    public MoneyValue Value { get; set; }

    public Money(MoneyValue value)
    {
        Value = value;
    }
}
