using Backend.Domain.Enums;
using Backend.Domain.Interfaces;

public class Asset : IAsset
{
    public IMoney Price { get; set; }
    public ICity City { get; set; }
    public Color? Color { get; set; }

    public Asset(IMoney price, ICity city, Color? color)
    {

        Price = price;
        City = city;
        Color = color;
    }
}
