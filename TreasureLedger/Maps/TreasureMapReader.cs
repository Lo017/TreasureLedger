using TreasureLedger.Models;
using TreasureLedger.Data;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using TreasureLedger.Services;

namespace TreasureLedger.Maps;

public sealed unsafe class TreasureMapReader(DiagnosticService diagnostics)
{
    private DateTimeOffset nextUpdate;
    private string previousObservation = string.Empty;

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
        var ambiguous = false;
        var observations = new List<string>();
        ref var directors = ref framework->DirectorModule.DirectorList;
        for (var entry = directors.First; entry < directors.Last; entry++)
        {
            var director = entry->Value;
            if (director == null || director->EventHandlerInfo == null ||
                director->EventHandlerInfo->EventId.ContentId != EventHandlerContent.TreasureHuntDirector)
            {
                continue;
            }

            var entryId = director->EventHandlerInfo->EventId.EntryId;
            var rank = entryId >= PARTY_DIRECTOR_OFFSET ? (uint)(entryId - PARTY_DIRECTOR_OFFSET) : entryId;
            var candidate = TreasureMapCatalog.GetGrade(rank);
            observations.Add($"event={entryId}; rank={rank}; grade={candidate}");
            if (candidate.Length == 0)
            {
                continue;
            }

            if (grade is not null && grade != candidate)
            {
                ambiguous = true;
            }

            grade = candidate;
        }

        var observation = string.Join(" | ", observations);
        if (observation != previousObservation)
        {
            previousObservation = observation;
            diagnostics.Record("MapDirector", observation);
        }

        if (ambiguous || grade is null || !session.IdentifyMap(grade))
        {
            return false;
        }

        diagnostics.Record("MapIdentified", $"source=director; grade={grade}");
        return true;
    }

    #region 常量

    private const int PARTY_DIRECTOR_OFFSET = 1000;

    #endregion
}
