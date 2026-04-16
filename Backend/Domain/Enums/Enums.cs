namespace Backend.Domain.Enums;

public enum TileType
{
    RentTile,
    ActionTile,
    JailTile,
    StartTile,
    TaxTile,
    DrawChance,
    DrawCommunity,
    PayTaxTile,
    FreeParkingTile,
}

public enum PropertyCity
{
    MediterraneanAvenue,
    BalticAvenue,

    OrientalAvenue,
    VermontAvenue,
    ConnecticutAvenue,

    StCharlesPlace,
    StatesAvenue,
    VirginiaAvenue,

    Boardwalk,
    ParkPlace,
}

public enum Color
{
    Brown,
    LightBlue,
    Pink,
    Orange,
    Red,
    Yellow,
    Green,
    DarkBlue,
}

public enum PieceType
{
    Tophat,
    Car,
    ScottieDog,
    Battleship,
    Horse,
    Thimble,
    Cannon,
    Wheelbarrow,
}

public enum MoneyValue
{
    One = 1,
    Five = 5,
    Ten = 10,
    Twenty = 20,
    Fifty = 50,
    OneHundred = 100,
    FiveHundred = 500,
}

public enum CardBehaviour
{
    //ChanceCards
    AdvanceToGo,
    AdvanceToIllinois,
    AdvanceToStCharles,
    AdvanceNearestUtility,
    AdvanceNearestRailroad,
    AdvanceNearestRailroadPayDouble,
    BankPaysDividend,
    GetOutOfJailFree,
    GoBackThreeSpaces,
    GoToJail,
    MakeGeneralRepairs,
    PayPoorTax,
    TakeTripToReadingRailroad,
    AdvanceToBoardwalk,
    ChairmanOfTheBoard,
    YourBuildingLoanMatures,

    //CommunityChestCards

    BankError,
    DoctorFees,
    FromSaleOfStock,

    HolidayFundMatures,
    IncomeTaxRefund,
    Birthday,
    LifeInsuranceMatures,
    PayHospitalFees,
    PaySchoolFees,
    ConsultancyFee,
    StreetRepairs,
    BeautyContestPrize,
    InheritMoney,
}
