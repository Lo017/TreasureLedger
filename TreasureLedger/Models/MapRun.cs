namespace TreasureLedger.Models;

public sealed class MapRun
{
    public DateTimeOffset StartedAt
    {
        get;
        set;
    } = DateTimeOffset.Now;

    public bool Counted
    {
        get;
        set;
    }

    public bool Portal
    {
        get;
        set;
    }

    public uint Territory
    {
        get;
        set;
    }

    public string MapGrade
    {
        get;
        set;
    } = string.Empty;

    public int Floor
    {
        get;
        set;
    }

    public int Summons
    {
        get;
        set;
    }

    public bool Bottom
    {
        get;
        set;
    }

    public bool Closed
    {
        get;
        set;
    }

    public long Gil
    {
        get;
        set;
    }

    public List<LootEntry> Loot
    {
        get;
        set;
    } = [];
}
