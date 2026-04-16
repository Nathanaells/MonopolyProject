using Backend.Domain.Enums;
using Backend.Domain.Interfaces;

class Asset : IAsset
{
    public int Price { get; set; }
    public ICity City { get; set; }
    public Color Color { get; set; }

    public Asset(int price, ICity city, Color color)
    {
        Price = price;
        City = city;
        Color = color;
    }
}
