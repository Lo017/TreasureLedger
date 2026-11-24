namespace TreasureLedger.Models;

public sealed class LootEntry
{
    public string ItemName
    {
        get;
        set;
    } = string.Empty;

    public int Quantity
    {
        get;
        set;
    }
}
