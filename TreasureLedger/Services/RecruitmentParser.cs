using TreasureLedger.Models;
using TreasureLedger.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TreasureLedger.Services;

public static partial class RecruitmentParser
{
    public static RecruitmentTerms Parse(string text)
    {
        text = text.Normalize(NormalizationForm.FormKC);
        var gradeMatch = GradePattern().Match(text);
        var grade = gradeMatch.Success ? "G" + gradeMatch.Groups[1].Value : null;
        if (LootPreset.All.All(preset => preset.Grade != grade))
        {
            grade = null;
        }

        var mapsMatch = MapsPattern().Match(text);
        int? maps = null;
        if (mapsMatch.Success && int.TryParse(mapsMatch.Groups[1].Value, out var count) && count > 0)
        {
            maps = count;
        }

        var payMatch = BasePayPattern().Match(text);
        if (!payMatch.Success && mapsMatch.Success)
        {
            payMatch = FollowingPayPattern().Match(text[(mapsMatch.Index + mapsMatch.Length)..]);
        }

        long? pay = null;
        if (payMatch.Success && decimal.TryParse(payMatch.Groups[1].Value, NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var amount) && amount <= long.MaxValue / 10_000m)
        {
            pay = (long)(amount * 10_000m);
        }

        return new RecruitmentTerms(grade, maps, pay);
    }

    [GeneratedRegex(@"(?<![A-Za-z0-9])G\s*(\d{1,2})(?!\d)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex GradePattern();

    [GeneratedRegex(@"(?<!\d)(\d+)\s*(?:张\s*)?图", RegexOptions.CultureInvariant)]
    private static partial Regex MapsPattern();

    [GeneratedRegex(@"(?:底薪|底|工资)\s*[:：]?\s*(\d+(?:\.\d+)?)\s*[wW万]?", RegexOptions.CultureInvariant)]
    private static partial Regex BasePayPattern();

    [GeneratedRegex(@"^\s*[,，:：/|、-]?\s*(\d+(?:\.\d+)?)(?:\s*[wW万]|(?=$|[\s,，;；。/|]))", RegexOptions.CultureInvariant)]
    private static partial Regex FollowingPayPattern();
}
