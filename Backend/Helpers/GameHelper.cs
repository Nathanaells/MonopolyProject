namespace Backend.Helpers;

using Backend.Domain.Enums;
using Backend.Domain.Interfaces;

public class GameHelper
{
    public static int GetHousePrice(IAsset asset)
    {
        int price = asset.Color switch
        {
            Color.Brown => 50,
            Color.LightBlue => 50,
            Color.Pink => 100,
            Color.Orange => 100,
            Color.Red => 150,
            Color.Yellow => 150,
            Color.Green => 200,
            Color.DarkBlue => 200,
            _ => 100,
        };
        return price;
    }
}
