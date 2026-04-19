namespace Backend.Services;

using Backend.Domain.Interfaces;
using Backend.Domain.Enums;


public class MoneyFactory
{
    public static List<IMoney> CreateMoney()
    {
        return new List<IMoney>
        {
            new Money(MoneyValue.one),
            new Money(MoneyValue.five),
            new Money(MoneyValue.ten),
            new Money(MoneyValue.twenty),
            new Money(MoneyValue.fifty),
            new Money(MoneyValue.hundred),
            new Money(MoneyValue.twoHundred),
            new Money(MoneyValue.fiveHundred),

        };
    }
}