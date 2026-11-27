using TreasureLedger.Models;
using TreasureLedger.Services;
using System.Numerics;
using Dalamud.Hooking;
using Dalamud.Game.Chat;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using Map = Lumina.Excel.Sheets.Map;

namespace TreasureLedger.Maps;

public sealed unsafe class MapTracker : IDisposable
{
    private readonly Configuration configuration;
    private readonly IDataManager data;
    private readonly IGameGui gameInterface;
    private readonly IPluginLog log;
    private readonly NotificationService notifications;
    private readonly MapProbe probe;
    private readonly DiagnosticService diagnostics;
    private readonly Hook<AgentMap.Delegates.ResetMapMarkers> mapHook;
    private readonly Hook<AgentMap.Delegates.ResetMiniMapMarkers> miniMapHook;
    private DateTimeOffset nextUpdate;
    private (uint Rank, ushort Spot) previousMap;

    public MapTracker(Configuration configuration, IDataManager data, IGameGui gameInterface,
        IGameInteropProvider interop, IPluginLog log, NotificationService notifications, DiagnosticService diagnostics)
    {
        this.configuration = configuration;
        this.data = data;
        this.gameInterface = gameInterface;
        this.log = log;
        this.notifications = notifications;
        this.diagnostics = diagnostics;
        probe = new MapProbe(interop);
        mapHook = interop.HookFromAddress<AgentMap.Delegates.ResetMapMarkers>(AgentMap.Addresses.ResetMapMarkers.Value,
            ResetMapMarkers);
        miniMapHook =
            interop.HookFromAddress<AgentMap.Delegates.ResetMiniMapMarkers>(
                AgentMap.Addresses.ResetMiniMapMarkers.Value, ResetMiniMapMarkers);
        mapHook.Enable();
        miniMapHook.Enable();
    }

    public List<MapLocation> Locations
    {
        get;
    } = [];

    public string PlayerName
    {
        get;
        private set;
    } = string.Empty;

    public void Update(bool loggedIn)
    {
        if (DateTimeOffset.UtcNow < nextUpdate)
        {
            return;
        }

        nextUpdate = DateTimeOffset.UtcNow.AddSeconds(1);
        if (!loggedIn)
        {
            if (Locations.Count > 0)
            {
                Locations.Clear();
                RefreshMarkers();
            }

            previousMap = default;
            PlayerName = string.Empty;
            return;
        }

        var player = PlayerState.Instance();
        if (!player->IsLoaded)
        {
            return;
        }

        PlayerName = player->CharacterNameString;
        var map = probe.Read();
        var previousLocation = Locations.FirstOrDefault(location => !location.IsParty);
        var ownLocation = Resolve(map.Rank, map.Spot);
        if (ownLocation != previousLocation)
        {
            Locations.RemoveAll(location => !location.IsParty);
            if (ownLocation is not null)
            {
                Locations.Insert(0, ownLocation);
            }

            RefreshMarkers();
        }

        if (map == previousMap)
        {
            return;
        }

        previousMap = map;
        diagnostics.Record("OwnMap", $"rank={map.Rank}; spot={map.Spot}; resolved={ownLocation is not null}");
        if (ownLocation is null || !configuration.AutoFlag)
        {
            return;
        }

        SetFlag(ownLocation);
        if (configuration.AutoSendFlag)
        {
            notifications.Send(NoticeChannel.Party, configuration.FlagTemplate);
        }
    }

    public bool RecordPartyLocation(IChatMessage message)
    {
        var link = SeString.Parse(message.OriginalMessage.Data.Span).Payloads.OfType<MapLinkPayload>().FirstOrDefault();
        var owner = SeString.Parse(message.OriginalSender.Data.Span).Payloads.OfType<PlayerPayload>().FirstOrDefault();
        if (link is null || owner is null)
        {
            return false;
        }

        var map = data.GetExcelSheet<Map>().GetRowOrDefault(link.Map.RowId);
        var territory = data.GetExcelSheet<TerritoryType>().GetRowOrDefault(link.TerritoryType.RowId);
        if (map is null || territory is null || map.Value.TerritoryType.RowId != territory.Value.RowId)
        {
            return false;
        }

        var position = new Vector3(link.RawX / 1000f, 0, link.RawY / 1000f);
        var location = new MapLocation(territory.Value.RowId, map.Value.RowId,
            territory.Value.PlaceName.Value.Name.ExtractText(), position,
            MapUtil.WorldToMap(new Vector2(position.X, position.Z), map.Value), owner.PlayerName, owner.World.RowId);
        var previous = Locations.FindIndex(entry => entry.Owner == location.Owner && entry.World == location.World);
        if (previous >= 0)
        {
            if (Locations[previous] == location)
            {
                return false;
            }

            Locations[previous] = location;
        }
        else
        {
            Locations.Add(location);
        }

        diagnostics.Record("PartyMap",
            $"map={location.Map}; x={location.Coordinates.X:F1}; y={location.Coordinates.Y:F1}", location.Territory);
        RefreshMarkers();
        return true;
    }

    public void Open(MapLocation location)
    {
        gameInterface.OpenMapWithMapLink(new MapLinkPayload(location.Territory, location.Map,
            (int)(location.Position.X * 1000), (int)(location.Position.Z * 1000)));
    }

    public void SendToParty(MapLocation location)
    {
        if (!configuration.AllowPartyMessages)
        {
            return;
        }

        SetFlag(location);
        notifications.Send(NoticeChannel.Party, configuration.FlagTemplate);
    }

    public void RefreshMarkers()
    {
        var agent = AgentMap.Instance();
        agent->ResetMapMarkers();
        agent->ResetMiniMapMarkers();
    }

    public void Dispose()
    {
        mapHook.Dispose();
        miniMapHook.Dispose();
        Locations.Clear();
        RefreshMarkers();
    }

    private MapLocation? Resolve(uint rank, ushort spot)
    {
        if (rank == 0)
        {
            return null;
        }

        var treasure = data.GetSubrowExcelSheet<TreasureSpot>().GetSubrowOrDefault(rank, spot);
        var location = treasure?.Location.ValueNullable;
        var map = location?.Map.ValueNullable;
        var territory = location?.Territory.ValueNullable;
        if (location is null || map is null || territory is null)
        {
            return null;
        }

        return new MapLocation(territory.Value.RowId, map.Value.RowId,
            territory.Value.PlaceName.Value.Name.ExtractText(),
            new Vector3(location.Value.X, location.Value.Y, location.Value.Z),
            MapUtil.WorldToMap(new Vector2(location.Value.X, location.Value.Z), map.Value));
    }

    public static void SetFlag(MapLocation location)
    {
        AgentMap.Instance()->SetFlagMapMarker(location.Territory, location.Map, location.Position,
            location.IsParty ? PARTY_CHEST_ICON : 60354);
    }

    private void ResetMapMarkers(AgentMap* agent)
    {
        mapHook.Original(agent);
        try
        {
            if (!configuration.ShowMapMarkers)
            {
                return;
            }

            foreach (var location in Locations.Where(location => location.Map == agent->SelectedMapId))
            {
                agent->AddMapMarker(location.Position, location.IsParty ? PARTY_CHEST_ICON : OWN_CHEST_ICON);
            }
        }
        catch (Exception exception)
        {
            log.Error(exception, "绘制宝图标记时出错");
        }
    }

    private void ResetMiniMapMarkers(AgentMap* agent)
    {
        miniMapHook.Original(agent);
        try
        {
            if (!configuration.ShowMiniMapMarkers)
            {
                return;
            }

            foreach (var location in Locations.Where(location => location.Territory == agent->CurrentTerritoryId))
            {
                agent->AddMiniMapMarker(location.Position, location.IsParty ? PARTY_CHEST_ICON : OWN_CHEST_ICON);
            }
        }
        catch (Exception exception)
        {
            log.Error(exception, "绘制小地图宝图标记时出错");
        }
    }

    #region 常量

    private const uint OWN_CHEST_ICON = 60355;
    private const uint PARTY_CHEST_ICON = 60356;

    #endregion
}
