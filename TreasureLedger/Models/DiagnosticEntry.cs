namespace TreasureLedger.Models;

public sealed record DiagnosticEntry(DateTimeOffset Time, string Event, uint Territory, string State, string Details);
