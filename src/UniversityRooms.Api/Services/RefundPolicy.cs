namespace UniversityRooms.Api.Services;

/// <summary>
/// Works out how much of a booking is refunded when it is cancelled, based on
/// how many days remain before check-in.
/// </summary>
public static class RefundPolicy
{
    /// <summary>
    /// Full refund when cancelling 7 or more days before check-in, half if
    /// cancelling within the last week, and nothing once the stay has started.
    /// </summary>
    public static decimal RefundAmount(decimal totalPrice, int daysUntilCheckIn)
    {
        if (daysUntilCheckIn > 7)
            return totalPrice;

        if (daysUntilCheckIn >= 1)
            return totalPrice * 0.5m;

        return 0m;
    }
}
