using TreasureLedger.Models;

namespace TreasureLedger.Services;

public static class TreasureProgress
{
    public static bool Apply(MapRun current, uint messageId, int? round)
    {
        if (current.Territory == 1279)
        {
            if (current.ProgressEnded)
            {
                return false;
            }

            switch (messageId)
            {
                case 11269 or 11262 when current is
                {
                    SummonActive: false, Summons: < 5
                }:
                    current.Summons++;
                    current.SummonActive = true;
                    current.Floor = Math.Max(current.Floor, current.Summons);
                    return true;
                case 11270 or 11265 or 11274 when current.SummonActive:
                    current.SummonActive = false;
                    if (current.Summons != 5)
                    {
                        return true;
                    }

                    current.Bottom = true;
                    current.ProgressEnded = true;
                    return true;
                case 11260 or 11261:
                    current.SummonActive = false;
                    current.ProgressEnded = true;
                    return true;
                default:
                    return false;
            }
        }

        switch (messageId)
        {
            case 6998 or 9365 when round is >= 0:
                var floor = round.Value + 1;
                if (floor <= current.Floor)
                {
                    return false;
                }

                current.Floor = floor;
                return true;
            case 9351 when current.Territory is 794 or 924 or 1123:
                current.Summons++;
                current.Floor = Math.Max(current.Floor, current.Summons);
                return true;
            default:
                return false;
        }
    }
}
