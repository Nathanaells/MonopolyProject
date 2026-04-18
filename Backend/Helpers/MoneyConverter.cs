namespace Backend.Helpers;

using Backend.Domain.Interfaces;

class MoneyConverter
{
    public static int ConvertTonInt(IMoney money)
    {
        return (int)money.Value;
    }
}