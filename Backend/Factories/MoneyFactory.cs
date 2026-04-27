namespace Backend.Services;

using Backend.Domain.Interfaces;

public class MoneyFactory
{
    public static List<IMoney> CreateMoney()
    {
        return new List<IMoney>
        {
            new Money(MoneyValue.ONE),
            new Money(MoneyValue.FIVE),
            new Money(MoneyValue.TEN),
            new Money(MoneyValue.TWENTY),
            new Money(MoneyValue.FIFTY),
            new Money(MoneyValue.HUNDRED),
            new Money(MoneyValue.TWO_HUNDRED),
            new Money(MoneyValue.FIVE_HUNDRED),
        };
    }
}
