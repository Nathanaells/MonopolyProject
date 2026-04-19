using Backend.Domain.Enums;

namespace Backend.Domain.Interfaces;

public interface IAsset
{
    public IMoney Price { get; set; }
    public ICity City { get; set; }
    public Color? Color { get; set; }
}
