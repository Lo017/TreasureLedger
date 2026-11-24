namespace TreasureLedger.Models;

public sealed class PaySettings
{
    public string MapGrade
    {
        get;
        set;
    } = "自定义";

    public Dictionary<string, List<BonusRule>> BonusPresets
    {
        get;
        set;
    } = new(StringComparer.Ordinal);

    public int PlannedMaps
    {
        get;
        set;
    } = 12;

    public long BasePay
    {
        get;
        set;
    } = 1_400_000;

    public bool PassengerFeeEnabled
    {
        get;
        set;
    }

    public long PassengerFeePerBottom
    {
        get;
        set;
    }

    public bool TankExtraPayEnabled
    {
        get;
        set;
    }

    public long TankExtraPay
    {
        get;
        set;
    }

    public List<BonusRule> Bonuses
    {
        get;
        set;
    } = [];

    public void SelectMap(string grade)
    {
        if (MapGrade == grade)
        {
            return;
        }

        BonusPresets[MapGrade] = Bonuses;
        MapGrade = grade;
        Bonuses = BonusPresets.GetValueOrDefault(grade) ?? [];
    }
}
