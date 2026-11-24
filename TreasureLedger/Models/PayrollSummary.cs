namespace TreasureLedger.Models;

public sealed record PayrollSummary(
    long BasePay,
    long Bonus,
    long ExtraPay,
    long PerWorker,
    long PerTank,
    long PerPassenger);
