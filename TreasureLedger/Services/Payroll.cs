using TreasureLedger.Models;

namespace TreasureLedger.Services;

public static class Payroll
{
    public static PayrollSummary Calculate(Session session)
    {
        var quantities =
            session.TotalLoot.ToDictionary(item => item.ItemName, item => item.Quantity, StringComparer.Ordinal);
        var bonus = session.Pay.Bonuses.Where(rule => rule.Enabled).Sum(rule =>
            checked(Math.Max(0, rule.PerItem) * Math.Max(0, quantities.GetValueOrDefault(rule.ItemName))));
        var basePay = Math.Max(0, session.Pay.BasePay);
        var extra = session.Pay.TankExtraPayEnabled ? Math.Max(0, session.Pay.TankExtraPay) : 0;
        var worker = checked(basePay + bonus);
        var tank = checked(worker + extra);
        var passenger = session.Pay.PassengerFeeEnabled
            ? checked(session.BottomCount * Math.Max(0, session.Pay.PassengerFeePerBottom))
            : 0;
        return new PayrollSummary(basePay, bonus, extra, worker, tank, passenger);
    }
}
