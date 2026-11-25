using System.Text;
using Dalamud.Game.Chat;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;
using TreasureLedger.Models;

namespace TreasureLedger.Services;

public sealed class DiagnosticService(Configuration configuration, DirectoryInfo directory, IPluginLog log)
{
    private readonly Queue<DiagnosticEntry> entries = new();
    private readonly Lock sync = new();

    public int Count
    {
        get
        {
            lock (sync)
            {
                return entries.Count;
            }
        }
    }

    public void Record(string kind, string details, uint territory = 0)
    {
        if (!configuration.DebugEnabled)
        {
            return;
        }

        var session = configuration.Session;
        var current = session.CurrentMap;
        var state = $"active={session.Active}; maps={session.MapCount}; portals={session.PortalCount}; " +
                    $"bottoms={session.BottomCount}; grade={current?.MapGrade}; floor={current?.Floor}; " +
                    $"summons={current?.Summons}; summonActive={current?.SummonActive}; ended={current?.ProgressEnded}";
        var entry = new DiagnosticEntry(DateTimeOffset.Now, kind, territory, state,
            details.Length > MAX_DETAIL_LENGTH ? details[..MAX_DETAIL_LENGTH] : details);
        lock (sync)
        {
            if (entries.Count == MAX_ENTRIES)
            {
                entries.Dequeue();
            }

            entries.Enqueue(entry);
        }
    }

    public void RecordLog(ILogMessage message, uint territory)
    {
        if (!configuration.DebugEnabled)
        {
            return;
        }

        var details = new StringBuilder($"id={message.LogMessageId}; parameters={message.ParameterCount}");
        for (var index = 0; index < Math.Min(message.ParameterCount, 16); index++)
        {
            if (message.TryGetIntParameter(index, out var number))
            {
                details.Append($"; [{index}]={number}");
            }
            else if (message.TryGetStringParameter(index, out var text))
            {
                details.Append($"; [{index}]={text.ExtractText()}");
            }
        }

        Record("LogMessage", details.ToString(), territory);
    }

    public string Snapshot()
    {
        lock (sync)
        {
            return JsonConvert.SerializeObject(new
            {
                Plugin = "TreasureLedger",
                Version = typeof(DiagnosticService).Assembly.GetName().Version?.ToString(),
                ExportedAt = DateTimeOffset.Now,
                configuration.DebugEnabled,
                Entries = entries.ToArray()
            }, Formatting.Indented).ReplaceLineEndings("\n");
        }
    }

    public string Export()
    {
        try
        {
            var folder = Path.Combine(directory.FullName, "diagnostics");
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, $"TreasureLedger-{DateTime.Now:yyyyMMdd-HHmmss-fff}.json");
            File.WriteAllText(path, Snapshot(), new UTF8Encoding(false));
            return $"已导出：{path}";
        }
        catch (Exception exception)
        {
            log.Error(exception, "导出诊断记录时出错");
            return $"导出失败：{exception.Message}";
        }
    }

    public void Clear()
    {
        lock (sync)
        {
            entries.Clear();
        }
    }

    #region 常量

    private const int MAX_ENTRIES = 2000;
    private const int MAX_DETAIL_LENGTH = 2048;

    #endregion
}
