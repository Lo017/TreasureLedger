using System.Numerics;

namespace TreasureLedger.Maps;

public sealed record MapLocation(
    uint Territory,
    uint Map,
    string Zone,
    Vector3 Position,
    Vector2 Coordinates,
    string Owner = "",
    uint World = 0)
{
    public bool IsParty => Owner.Length > 0;
}
