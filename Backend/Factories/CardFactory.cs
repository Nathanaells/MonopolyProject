namespace Backend.Factories;

using Backend.Domain.Enums;
using Backend.Domain.Interfaces;

public class CardFactory
{
    public static List<ICard> CreateDefaultCards()
    {
        return new List<ICard>
        {
            new ChanceCard("Advance to GO", CardBehaviour.AdvanceToGo),
            new ChanceCard("Advance to Illinois Avenue", CardBehaviour.AdvanceToIllinois),
            new ChanceCard("Advance to St. Charles Place", CardBehaviour.AdvanceToStCharles),
            new ChanceCard("Advance token to nearest Utility", CardBehaviour.AdvanceNearestUtility),
            new ChanceCard(
                "Advance token to nearest Railroad",
                CardBehaviour.AdvanceNearestRailroad
            ),
            new ChanceCard("Bank pays you dividend of 50", CardBehaviour.BankPaysDividend),
            new ChanceCard("Get out of Jail Free", CardBehaviour.GetOutOfJailFree),
            new ChanceCard("Go back three spaces", CardBehaviour.GoBackThreeSpaces),
            new ChanceCard("Go to Jail", CardBehaviour.GoToJail),
            new ChanceCard("Make general repairs", CardBehaviour.MakeGeneralRepairs),
            new ChanceCard("Pay poor tax of 15", CardBehaviour.PayPoorTax),
            new ChanceCard(
                "Take trip to Reading Railroad",
                CardBehaviour.TakeTripToReadingRailroad
            ),
            new ChanceCard("Advance to Boardwalk", CardBehaviour.AdvanceToBoardwalk),
            new ChanceCard("Chairman of the Board", CardBehaviour.ChairmanOfTheBoard),
            new ChanceCard("Your building loan matures", CardBehaviour.YourBuildingLoanMatures),
            new CommunityCard("Advance to GO", CardBehaviour.AdvanceToGo),
            new CommunityCard("Bank error in your favor", CardBehaviour.BankError),
            new CommunityCard("Doctor's fees", CardBehaviour.DoctorFees),
            new CommunityCard("From sale of stock you get 50", CardBehaviour.FromSaleOfStock),
            new CommunityCard("Get out of jail free", CardBehaviour.GetOutOfJailFree),
            new CommunityCard("Go to jail", CardBehaviour.GoToJail),
            new CommunityCard("Holiday fund matures", CardBehaviour.HolidayFundMatures),
            new CommunityCard("Income tax refund", CardBehaviour.IncomeTaxRefund),
            new CommunityCard("Birthday", CardBehaviour.Birthday),
            new CommunityCard("Life insurance matures", CardBehaviour.LifeInsuranceMatures),
            new CommunityCard("Pay hospital fees", CardBehaviour.PayHospitalFees),
            new CommunityCard("Pay school fees", CardBehaviour.PaySchoolFees),
            new CommunityCard("Receive consultancy fee", CardBehaviour.ConsultancyFee),
            new CommunityCard("Street repairs", CardBehaviour.StreetRepairs),
            new CommunityCard("Beauty contest prize", CardBehaviour.BeautyContestPrize),
            new CommunityCard("Inherit money", CardBehaviour.InheritMoney),
        };
    }
}
