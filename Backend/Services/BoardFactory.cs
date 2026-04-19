namespace Backend.Services;

using Backend.Domain.Interfaces;
using Backend.Domain.Enums;
using Backend.Domain.ValueObjects;

public class BoardFactory
{
    public static IBoard CreateBoard()
    {
        var boardTiles = new ITile[40];

        boardTiles[0] = new Tile(TileType.StartTile, new Point(10, 0));
        boardTiles[1] = new Tile(TileType.RentTile, new Point(9, 0))
        {
            Asset = new Asset(
                new Money(MoneyValue.fifty),
                new City(PropertyCity.MediterraneanAvenue),
                Color.Brown
            )
        };
        boardTiles[2] = new Tile(TileType.DrawCommunity, new Point(8, 0));
        boardTiles[3] = new Tile(TileType.RentTile, new Point(7, 0))
        {
            Asset = new Asset(
                new Money(MoneyValue.fifty),
                new City(PropertyCity.BalticAvenue),
                Color.Brown
            )
        };
        boardTiles[4] = new Tile(TileType.TaxTile, new Point(6, 0));
        boardTiles[5] = new Tile(TileType.RentTile, new Point(5, 0))
        {
            Asset = new Asset(
                new Money(MoneyValue.hundred),
                new City(PropertyCity.ReadingRailroad),
                null
            )
        };
        boardTiles[6] = new Tile(TileType.RentTile, new Point(4, 0))
        {
            Asset = new Asset(
                new Money(MoneyValue.hundred),
                new City(PropertyCity.OrientalAvenue),
                Color.LightBlue
            )
        };
        boardTiles[7] = new Tile(TileType.DrawChance, new Point(3, 0));
        boardTiles[8] = new Tile(TileType.RentTile, new Point(2, 0))
        {
            Asset = new Asset(
                new Money(MoneyValue.hundred),
                new City(PropertyCity.VermontAvenue),
                Color.LightBlue
            )
        };
        boardTiles[9] = new Tile(TileType.RentTile, new Point(1, 0))
        {
            Asset = new Asset(
                new Money(MoneyValue.hundred),
                new City(PropertyCity.ConnecticutAvenue),
                Color.LightBlue
            )
        };
        boardTiles[10] = new Tile(TileType.JailTile, new Point(0, 0));

        boardTiles[11] = new Tile(TileType.RentTile, new Point(0, 1))
        {
            Asset = new Asset(
                new Money(MoneyValue.hundred + MoneyValue.fifty),
                new City(PropertyCity.StCharlesPlace),
                Color.Pink
            )
        };

        boardTiles[12] = new Tile(TileType.RentTile, new Point(0, 2))
        {
            Asset = new Asset(
                new Money(MoneyValue.hundred + MoneyValue.fifty),
                new City(PropertyCity.ElectricCompany),
                null
            )
        };

        boardTiles[13] = new Tile(TileType.RentTile, new Point(0, 3))
        {
            Asset = new Asset(
                new Money(MoneyValue.hundred + MoneyValue.fifty),
                new City(PropertyCity.StatesAvenue),
                Color.Pink
            )
        };

        boardTiles[14] = new Tile(TileType.RentTile, new Point(0, 4))
        {
            Asset = new Asset(
                new Money(MoneyValue.hundred + MoneyValue.fifty),
                new City(PropertyCity.VirginiaAvenue),
                Color.Pink
            )
        };

        boardTiles[15] = new Tile(TileType.RentTile, new Point(0, 5))
        {
            Asset = new Asset(
                new Money(MoneyValue.twoHundred),
                new City(PropertyCity.PennsylvaniaRailroad),
                null
            )
        };

        boardTiles[16] = new Tile(TileType.RentTile, new Point(0, 6))
        {
            Asset = new Asset(
                new Money(MoneyValue.twoHundred),
                new City(PropertyCity.StJamesPlace),
                Color.Orange
            )
        };

        boardTiles[17] = new Tile(TileType.DrawCommunity, new Point(0, 7));
        boardTiles[18] = new Tile(TileType.RentTile, new Point(0, 8))
        {
            Asset = new Asset(
                new Money(MoneyValue.twoHundred),
                new City(PropertyCity.TennesseeAvenue),
                Color.Orange
            )
        };


        boardTiles[19] = new Tile(TileType.RentTile, new Point(0, 9))
        {
            Asset = new Asset(
                new Money(MoneyValue.twoHundred),
                new City(PropertyCity.NewYorkAvenue),
                Color.Orange
            )
        };

        boardTiles[20] = new Tile(TileType.FreeParkingTile, new Point(0, 10));


        boardTiles[21] = new Tile(TileType.RentTile, new Point(1, 10))
        {
            Asset = new Asset(
                new Money(MoneyValue.twoHundred + MoneyValue.hundred),
                new City(PropertyCity.KentuckyAvenue),
                Color.Red
            )
        };

        boardTiles[22] = new Tile(TileType.DrawChance, new Point(2, 10));

        boardTiles[23] = new Tile(TileType.RentTile, new Point(3, 10))
        {
            Asset = new Asset(
                new Money(MoneyValue.twoHundred + MoneyValue.hundred),
                new City(PropertyCity.IndianaAvenue),
                Color.Red
            )
        };
        boardTiles[24] = new Tile(TileType.RentTile, new Point(4, 10))
        {
            Asset = new Asset(
                new Money(MoneyValue.twoHundred + MoneyValue.hundred),
                new City(PropertyCity.IllinoisAvenue),
                Color.Red
            )
        };

        boardTiles[25] = new Tile(TileType.RentTile, new Point(5, 10))
        {
            Asset = new Asset(
                new Money(MoneyValue.twoHundred + MoneyValue.hundred + MoneyValue.fifty),
                new City(PropertyCity.BORailroad),
                null
            )
        };

        boardTiles[26] = new Tile(TileType.RentTile, new Point(6, 10))
        {
            Asset = new Asset(
                new Money(MoneyValue.twoHundred + MoneyValue.twoHundred),
                new City(PropertyCity.AtlanticAvenue),
                Color.Yellow
            )
        };

        boardTiles[27] = new Tile(TileType.RentTile, new Point(7, 10))
        {
            Asset = new Asset(
                new Money(MoneyValue.twoHundred + MoneyValue.twoHundred),
                new City(PropertyCity.VentnorAvenue),
                Color.Yellow
            )
        };
        boardTiles[28] = new Tile(TileType.RentTile, new Point(8, 10))
        {
            Asset = new Asset(
                new Money(MoneyValue.hundred + MoneyValue.fifty),
                new City(PropertyCity.WaterWorks),
                null
            )
        };

        boardTiles[29] = new Tile(TileType.RentTile, new Point(9, 10))
        {
            Asset = new Asset(
                new Money(MoneyValue.twoHundred + MoneyValue.hundred),
                new City(PropertyCity.MarvinGardens),
                Color.Yellow
            )
        };

        boardTiles[30] = new Tile(TileType.GoToJailTile, new Point(10, 10));

        boardTiles[31] = new Tile(TileType.RentTile, new Point(10, 9))
        {
            Asset = new Asset(
                new Money(MoneyValue.twoHundred + MoneyValue.hundred),
                new City(PropertyCity.PacificAvenue),
                Color.Green
            )
        };

        boardTiles[32] = new Tile(TileType.RentTile, new Point(10, 8))
        {
            Asset = new Asset(
                new Money(MoneyValue.twoHundred + MoneyValue.hundred),
                new City(PropertyCity.NorthCarolinaAvenue),
                Color.Green
            )
        };

        boardTiles[33] = new Tile(TileType.DrawCommunity, new Point(10, 7));

        boardTiles[34] = new Tile(TileType.RentTile, new Point(10, 6))
        {
            Asset = new Asset(
                new Money(MoneyValue.twoHundred + MoneyValue.hundred),
                new City(PropertyCity.PennsylvaniaAvenue),
                Color.Green
            )
        };

        boardTiles[35] = new Tile(TileType.RentTile, new Point(10, 5))
        {
            Asset = new Asset(
                new Money(MoneyValue.twoHundred + MoneyValue.twoHundred + MoneyValue.fifty),
                new City(PropertyCity.ShortLineRailroad),
                null
            )
        };

        boardTiles[36] = new Tile(TileType.DrawChance, new Point(10, 4));
        boardTiles[37] = new Tile(TileType.RentTile, new Point(10, 3))
        {
            Asset = new Asset(
                new Money(MoneyValue.twoHundred + MoneyValue.hundred + MoneyValue.fifty),
                new City(PropertyCity.ParkPlace),
                Color.DarkBlue
            )
        };

        boardTiles[38] = new Tile(TileType.TaxTile, new Point(10, 2));
        boardTiles[39] = new Tile(TileType.RentTile, new Point(10, 1))
        {
            Asset = new Asset(
                new Money(MoneyValue.twoHundred + MoneyValue.twoHundred),
                new City(PropertyCity.Boardwalk),
                Color.DarkBlue
            )
        };
        return new Board(boardTiles);
    }
}