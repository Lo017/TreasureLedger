using TreasureLedger.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TreasureLedger.Services;

public static partial class TreasureParser
{
    public static bool Apply(Session session, string text)
    {
        if (!session.Active)
        {
            return false;
        }

        if (text.Contains("将陷阱引来的魔物全部打倒了！", StringComparison.Ordinal))
        {
            session.CompleteMap();
            return true;
        }

        if (text.Contains("通往宝物库的传送魔纹出现了！", StringComparison.Ordinal))
        {
            session.RecordPortal();
            return true;
        }

        var current = session.CurrentMap;
        if (current is not null && current.Territory != 0)
        {
            if (text.Contains("最终区的封锁解除了", StringComparison.Ordinal))
            {
                current.Bottom = true;
                return true;
            }

            var floor = FloorPattern().Match(text);
            if (floor.Success)
            {
                var number = floor.Groups[1].Value;
                var value = "一二三四五六七八九".IndexOf(number, StringComparison.Ordinal) + 1;
                if (int.TryParse(number, out var digit))
                {
                    value = digit;
                }

                current.Floor = Math.Max(current.Floor, value);
                return true;
            }
        }

        var gil = GilPattern().Match(text);
        if (current is not null && gil.Success)
        {
            current.Gil = checked(current.Gil + long.Parse(gil.Groups[1].Value, NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture));
            return true;
        }

        var loot = LootPattern().Match(text);
        if (!loot.Success)
        {
            return false;
        }

        var count = loot.Groups[2].Success ? int.Parse(loot.Groups[2].Value, CultureInfo.InvariantCulture) : 1;
        return session.RecordLoot(loot.Groups[1].Value.Trim('“', '”', ' ', '\uE0BB'), count);
    }

    [GeneratedRegex(@"打开了通往第([一二三四五六七八九\d]+)区的大门")]
    private static partial Regex FloorPattern();

    [GeneratedRegex(@"^获得了([\d,]+)金币。$")]
    private static partial Regex GilPattern();

    [GeneratedRegex(@"^获得了新的战利品[\s\uE0BB]*(.+?)(?:×(\d{1,4}))?。$")]
    private static partial Regex LootPattern();
}
