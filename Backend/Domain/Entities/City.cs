using Backend.Domain.Enums;

class City : ICity
{
    public PropertyCity PropertyCity { get; set; }

    public City(PropertyCity city)
    {
        PropertyCity = city;
    }
}
