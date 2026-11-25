using System.Runtime.Serialization;
using System.Globalization;
using TreasureLedger.Data;

namespace TreasureLedger.Models;

public sealed class Session
{
    public DateTimeOffset StartedAt
    {
        get;
        set;
    } = DateTimeOffset.Now;

    public string Title
    {
        get;
        set;
    } = DateTime.Now.ToString("MM-dd HH:mm 挖宝", CultureInfo.InvariantCulture);

    public bool Active
    {
        get;
        set;
    }

    public List<MapRun> Maps
    {
        get;
        set;
    } = [];

    public PaySettings Pay
    {
        get;
        set;
    } = new();

    public List<LootEntry> LootAdjustments
    {
        get;
        set;
    } = [];

    public HashSet<string> CustomLootItems
    {
        get;
        set;
    } = new(StringComparer.Ordinal);

    public int MapAdjustment
    {
        get;
        set;
    }

    public int PortalAdjustment
    {
        get;
        set;
    }

    public int BottomAdjustment
    {
        get;
        set;
    }

    public long GilAdjustment
    {
        get;
        set;
    }

    [IgnoreDataMember] public int MapCount => Math.Max(0, Maps.Count(map => map.Counted) + MapAdjustment);

    [IgnoreDataMember] public int PortalCount => Math.Max(0, Maps.Count(map => map.Portal) + PortalAdjustment);

    [IgnoreDataMember] public int BottomCount => Math.Max(0, Maps.Count(map => map.Bottom) + BottomAdjustment);

    [IgnoreDataMember] public long Gil => Math.Max(0, Maps.Sum(map => map.Gil) + GilAdjustment);

    [IgnoreDataMember] public MapRun? CurrentMap => Maps.LastOrDefault(map => !map.Closed);

    [IgnoreDataMember]
    public List<LootEntry> TotalLoot =>
    [
        .. Maps.SelectMany(map => map.Loot).Concat(LootAdjustments).Where(item => IsLootAllowed(item.ItemName))
            .GroupBy(item => item.ItemName, StringComparer.Ordinal).Select(group => new LootEntry
            {
                ItemName = group.Key,
                Quantity = group.Sum(item => item.Quantity)
            }).Where(item => item.Quantity > 0).OrderBy(item => item.ItemName, StringComparer.Ordinal)
    ];

    public void AdjustLoot(string itemName, int change)
    {
        itemName = itemName.Trim();
        if (itemName.Length == 0 || change == 0)
        {
            return;
        }

        if (change > 0 && !LootPreset.ItemNames.Contains(itemName))
        {
            CustomLootItems.Add(itemName);
        }

        var quantity = TotalLoot.FirstOrDefault(item => item.ItemName == itemName)?.Quantity ?? 0;
        var corrected = Math.Max(0, checked(quantity + change));
        var difference = corrected - quantity;
        if (difference == 0)
        {
            return;
        }

        var adjustment = LootAdjustments.FirstOrDefault(item => item.ItemName == itemName);
        if (adjustment is null)
        {
            LootAdjustments.Add(new LootEntry
            {
                ItemName = itemName,
                Quantity = difference
            });
            return;
        }

        adjustment.Quantity = checked(adjustment.Quantity + difference);
        if (adjustment.Quantity == 0)
        {
            LootAdjustments.Remove(adjustment);
        }
    }

    public void BeginMap()
    {
        if (!Active)
        {
            return;
        }

        var current = CurrentMap;
        if (current is not null && !current.Counted)
        {
            return;
        }

        current?.Closed = true;
        Maps.Add(new MapRun());
    }

    public bool IdentifyMap(string grade)
    {
        if (!Active || CurrentMap is not
            {
                MapGrade.Length: 0
            } current || grade.Length == 0)
        {
            return false;
        }

        current.MapGrade = grade;
        return true;
    }

    public void CompleteMap()
    {
        BeginMap();
        if (Active && CurrentMap is
            {
            } current)
        {
            current.Counted = true;
        }
    }

    public void RecordPortal()
    {
        if (!Active)
        {
            return;
        }

        var current = CurrentMap;
        if (current is null)
        {
            current = new MapRun();
            Maps.Add(current);
        }

        current.Portal = true;
    }

    public void EnterDungeon(uint territory)
    {
        if (!Active)
        {
            return;
        }

        var current = CurrentMap;
        if (current is null)
        {
            current = new MapRun();
            Maps.Add(current);
        }

        current.Territory = territory;
        var grade = TreasureMapCatalog.GetDungeonGrade(territory);
        if (grade.Length > 0)
        {
            current.MapGrade = grade;
        }

        current.Floor = Math.Max(1, current.Floor);
        if (territory == 725)
        {
            current.Counted = true;
        }
    }

    public bool IsLootAllowed(string itemName)
    {
        return LootPreset.ItemNames.Contains(itemName) || CustomLootItems.Contains(itemName);
    }

    public bool RecordLoot(string itemName, int count)
    {
        if (!Active || count <= 0 || !IsLootAllowed(itemName))
        {
            return false;
        }

        var current = CurrentMap;
        if (current is null)
        {
            current = new MapRun();
            Maps.Add(current);
        }

        var item = current.Loot.FirstOrDefault(entry => entry.ItemName == itemName);
        if (item is null)
        {
            item = new LootEntry
            {
                ItemName = itemName
            };
            current.Loot.Add(item);
        }

        item.Quantity = checked(item.Quantity + count);
        return true;
    }
}
