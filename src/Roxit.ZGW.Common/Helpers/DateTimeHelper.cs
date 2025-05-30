using System;

namespace Roxit.ZGW.Common.Helpers;

public class DateTimeHelpers
{
    public static bool IsOverlapped(DateOnly beginDate1, DateOnly? endDate1, DateOnly beginDate2, DateOnly? endDate2)
    {
        if (endDate1.HasValue && beginDate1 > endDate1) // Note: Special flow which can be happen while creating multiple versions in one day
        {
            return false;
        }
        return beginDate1 <= endDate2.GetValueOrDefault(DateOnly.MaxValue) && beginDate2 <= endDate1.GetValueOrDefault(DateOnly.MaxValue);
    }
}
