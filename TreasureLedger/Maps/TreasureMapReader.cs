using TreasureLedger.Models;
using TreasureLedger.Data;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using Lumina.Excel.Sheets;

namespace TreasureLedger.Maps;

public sealed unsafe class TreasureMapReader
{
    private readonly Dictionary<uint, string> grades = [];
    private DateTimeOffset nextUpdate;

    public TreasureMapReader(IDataManager data)
    {
        foreach (var rank in data.GetExcelSheet<TreasureHuntRank>())
        {
            var grade = TreasureMapCatalog.GetGrade(rank.RowId);
            if (grade.Length == 0)
            {
                continue;
            }

            if (rank.KeyItemName.RowId != 0)
            {
                grades[rank.KeyItemName.RowId] = grade;
            }

            if (rank.InstanceMap.RowId != 0)
            {
                grades[rank.InstanceMap.RowId] = grade;
            }
        }
    }

    public bool Update(Session session)
    {
        if (!session.Active || session.CurrentMap is not
            {
                MapGrade.Length: 0, Territory: 0
            } || DateTimeOffset.UtcNow < nextUpdate)
        {
            return false;
        }

        nextUpdate = DateTimeOffset.UtcNow.AddSeconds(1);
        var framework = EventFramework.Instance();
        if (framework == null)
        {
            return false;
        }

        string? grade = null;
        ref var directors = ref framework->DirectorModule.DirectorList;
        for (var entry = directors.First; entry < directors.Last; entry++)
        {
            var director = entry->Value;
            if (director == null || director->EventHandlerInfo == null ||
                director->EventHandlerInfo->EventId.ContentId != EventHandlerContent.TreasureHuntDirector ||
                !grades.TryGetValue(director->EventItemId, out var candidate))
            {
                continue;
            }

            if (grade is not null && grade != candidate)
            {
                return false;
            }

            grade = candidate;
        }

        return grade is not null && session.IdentifyMap(grade);
    }
}
