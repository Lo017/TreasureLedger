using TreasureLedger.Models;
using System.Text.RegularExpressions;

namespace TreasureLedger.Services;

public static partial class TreasureRules
{
    public static bool IsDungeon(uint territory)
    {
        return territory is 588 or 712 or 725 or 794 or 879 or 924 or 1000 or 1123 or 1209 or 1279;
    }

    public static int GetDoorCount(uint territory)
    {
        return territory switch
        {
            725 => 3,
            588 or 712 or 879 or 1000 or 1209 => 2,
            _ => 0
        };
    }

    public static TreasureReminder GetReminder(string text, uint territory, out string door)
    {
        door = string.Empty;
        var inside = IsDungeon(territory);
        if (text.Contains("尚未完成分配", StringComparison.Ordinal))
        {
            return inside ? TreasureReminder.AfterEntrance : TreasureReminder.BeforeEntrance;
        }

        if (!inside)
        {
            return TreasureReminder.None;
        }

        if (text.Contains("好像还有没拿走的财宝", StringComparison.Ordinal))
        {
            return TreasureReminder.AfterEntrance;
        }

        var glowing = GlowingDoorPattern().Match(text);
        if (!glowing.Success)
        {
            return TreasureReminder.None;
        }

        door = glowing.Groups[1].Value;
        return TreasureReminder.GuaranteedPassage;
    }

    [GeneratedRegex("([左中右])(?:侧|间)的门发光了", RegexOptions.CultureInvariant)]
    private static partial Regex GlowingDoorPattern();
}
