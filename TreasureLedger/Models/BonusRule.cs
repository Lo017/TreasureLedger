namespace TreasureLedger.Models;

public sealed class BonusRule
{
    public string ItemName
    {
        get;
        set;
    } = string.Empty;

    public long PerItem
    {
        get;
        set;
    }

    public bool Enabled
    {
        get;
        set;
    } = true;
}
