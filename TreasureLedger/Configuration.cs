using TreasureLedger.Models;
using Dalamud.Configuration;

namespace TreasureLedger;

public sealed class Configuration : IPluginConfiguration
{
    public int Version
    {
        get;
        set;
    } = 2;

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
}
