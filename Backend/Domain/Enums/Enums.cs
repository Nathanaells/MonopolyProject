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
    GoToJailTile,
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

    // Orange
    StJamesPlace,
    TennesseeAvenue,
    NewYorkAvenue,
    KentuckyAvenue,
    IndianaAvenue,
    IllinoisAvenue,

    AtlanticAvenue,
    VentnorAvenue,
    MarvinGardens,
    PacificAvenue,
    NorthCarolinaAvenue,
    PennsylvaniaAvenue,
    ParkPlace,
    Boardwalk,

    ReadingRailroad,
    PennsylvaniaRailroad,
    BORailroad,
    ShortLineRailroad,
    ElectricCompany,
    WaterWorks
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
