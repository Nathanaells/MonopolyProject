using Backend.Domain.Enums;

public interface IAsset
{
    public int Price { get; set; }
    public ICity City { get; set; }
    Color Color { get; set; }
}
