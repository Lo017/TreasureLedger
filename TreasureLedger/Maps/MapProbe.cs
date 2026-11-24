using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;

namespace TreasureLedger.Maps;

public sealed class MapProbe
{
    private delegate uint ReadRank(nint manager);

    private delegate ushort ReadSpot(nint manager);

    [Signature("E8 ?? ?? ?? ?? 0F B6 D0 45 0F B6 CE")]
    private readonly ReadRank readRank = null!;

    [Signature("E8 ?? ?? ?? ?? 44 0F B7 C0 45 33 C9 0F B7 D3")]
    private readonly ReadSpot readSpot = null!;

    [Signature("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 84 C0 75 E4", Offset = 3, ScanType = ScanType.StaticAddress)]
    private readonly nint manager = nint.Zero;

    public MapProbe(IGameInteropProvider interop)
    {
        interop.InitializeFromAttributes(this);
    }

    public (uint Rank, ushort Spot) Read()
    {
        return (readRank(manager), readSpot(manager));
    }
}
