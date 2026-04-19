using Backend.Domain.Enums;
using Backend.Domain.Interfaces;

public class Money : IMoney
{
    public int Value { get; set; }

    public Money()
    {
        Value = MoneyValue.one;
    }

    public Money(int value)
    {
        Value = value;
    }
}
