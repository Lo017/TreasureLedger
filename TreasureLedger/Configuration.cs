using TreasureLedger.Models;
using TreasureLedger.Data;
using Dalamud.Configuration;

namespace TreasureLedger;

public sealed class Configuration : IPluginConfiguration
{
    public int Version
    {
        get;
        set;
    } = 3;

    public Session Session
    {
        get;
        set;
    } = new();

    public List<Session> History
    {
        get;
        set;
    } = [];

    public bool AllowPartyMessages
    {
        get;
        set;
    }

    public bool AutoFlag
    {
        get;
        set;
    }

    public bool DebugEnabled
    {
        get;
        set;
    }

    public bool AutoSendFlag
    {
        get;
        set;
    }

    public string FlagTemplate
    {
        get;
        set;
    } = "<flag>";

    public NoticeChannel RollReminder
    {
        get;
        set;
    } = NoticeChannel.Echo;

    public NoticeChannel GlowingDoor
    {
        get;
        set;
    } = NoticeChannel.Echo;

    public NoticeChannel AfterEntranceReminder
    {
        get;
        set;
    } = NoticeChannel.Echo;

    public string AfterEntranceText
    {
        get;
        set;
    } = "冰冰冰<se.1>";

    public string GlowingDoorText
    {
        get;
        set;
    } = "哪里亮了点哪里<se.1>";

    public NoticeChannel DoorChoice
    {
        get;
        set;
    } = NoticeChannel.Echo;

    public string DoorChoiceText
    {
        get;
        set;
    } = "随机选门：{门}";

    public string RollText
    {
        get;
        set;
    } = "冰冰冰<se.1>";

    public bool ShowMapMarkers
    {
        get;
        set;
    } = true;

    public bool ShowMiniMapMarkers
    {
        get;
        set;
    } = true;

    public bool MigrateCustomLootItems()
    {
        if (Version >= 3)
        {
            return false;
        }

        foreach (var session in History.Append(Session))
        {
            var names = session.LootAdjustments.Select(item => item.ItemName)
                .Concat(session.Pay.Bonuses.Select(rule => rule.ItemName)).Concat(session.Pay.BonusPresets.Values
                    .SelectMany(rules => rules).Select(rule => rule.ItemName));
            session.CustomLootItems.UnionWith(names.Where(name =>
                !string.IsNullOrWhiteSpace(name) && !LootPreset.ItemNames.Contains(name)));
        }

        Version = 3;
        return true;
    }
}
