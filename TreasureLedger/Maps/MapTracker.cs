using TreasureLedger.Models;
using TreasureLedger.Services;
using System.Numerics;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;

namespace TreasureLedger.Maps;

public sealed unsafe class MapTracker : IDisposable
{
    private readonly Configuration configuration;
    private readonly IDataManager data;
    private readonly IPluginLog log;
    private readonly NotificationService notifications;
    private readonly MapProbe probe;
    private readonly Hook<AgentMap.Delegates.ResetMapMarkers> mapHook;
    private readonly Hook<AgentMap.Delegates.ResetMiniMapMarkers> miniMapHook;
    private DateTimeOffset nextUpdate;
    private (uint Rank, ushort Spot) previousMap;

    public MapTracker(Configuration configuration, IDataManager data, IGameInteropProvider interop, IPluginLog log,
        NotificationService notifications)
    {
        this.configuration = configuration;
        this.data = data;
        this.log = log;
        this.notifications = notifications;
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

    public void Update(bool loggedIn)
    {
        if (DateTimeOffset.UtcNow < nextUpdate)
        {
            return;
        }

        nextUpdate = DateTimeOffset.UtcNow.AddSeconds(1);
        var player = PlayerState.Instance();
        if (!player->IsLoaded)
        {
            Locations.Clear();
            if (!loggedIn)
            {
                previousMap = default;
            }

            return;
        }

        var map = probe.Read();
        var previousLocation = Locations.FirstOrDefault();
        Locations.Clear();
        var ownLocation = Resolve(map.Rank, map.Spot);
        if (ownLocation is not null)
        {
            Locations.Add(ownLocation);
        }

        if (map != previousMap)
        {
            previousMap = map;
            if (ownLocation is not null && configuration.AutoFlag)
            {
                SetFlag(ownLocation);
                if (configuration.AutoSendFlag)
                {
                    notifications.Send(NoticeChannel.Party, configuration.FlagTemplate);
                }
            }
        }

        if (ownLocation != previousLocation)
        {
            RefreshMarkers();
        }
    }

    public void Open(MapLocation location)
    {
        AgentMap.Instance()->OpenMap(location.Map, location.Territory, location.Zone);
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
        AgentMap.Instance()->SetFlagMapMarker(location.Territory, location.Map, location.Position, 60354);
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

            foreach (var location in Locations.Where(location => location.Territory == agent->SelectedTerritoryId))
            {
                agent->AddMapMarker(location.Position, 60355);
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
                agent->AddMiniMapMarker(location.Position, 60355);
            }
        }
        catch (Exception exception)
        {
            log.Error(exception, "绘制小地图宝图标记时出错");
        }
    }
}
