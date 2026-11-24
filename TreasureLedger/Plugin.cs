using TreasureLedger.Models;
using TreasureLedger.Services;
using Newtonsoft.Json;
using Dalamud.Game.Chat;
using Dalamud.Game.Command;
using Dalamud.Game.DutyState;
using Dalamud.Game.Text;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using TreasureLedger.Maps;
using TreasureLedger.Windows;

namespace TreasureLedger;

public sealed class Plugin : IDalamudPlugin
{
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ICommandManager commands;
    private readonly IChatGui chat;
    private readonly IClientState client;
    private readonly IFramework framework;
    private readonly IPluginLog log;
    private readonly IDutyState duty;
    private readonly WindowSystem windows = new("TreasureLedger");
    private readonly NotificationService notifications;
    private readonly MainWindow mainWindow;
    private readonly TreasureMapReader mapReader;

    public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commands, IChatGui chat, IClientState client,
        IFramework framework, IDataManager data, IGameInteropProvider interop, IPluginLog log, IDutyState duty)
    {
        this.pluginInterface = pluginInterface;
        this.commands = commands;
        this.chat = chat;
        this.client = client;
        this.framework = framework;
        this.log = log;
        this.duty = duty;
        Configuration = pluginInterface.ConfigFile.Exists
            ? JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(pluginInterface.ConfigFile.FullName),
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None
                }) ?? new Configuration()
            : new Configuration();
        if (Configuration.Version < 2)
        {
            if (Configuration.RollText == "<se.1>")
            {
                Configuration.RollText = "冰冰冰<se.1>";
            }

            if (Configuration.AfterEntranceText == "<se.1>")
            {
                Configuration.AfterEntranceText = "冰冰冰<se.1>";
            }

            Configuration.GlowingDoorText = Configuration.GlowingDoorText == "保送：{洞}洞 <se.1>"
                ? "哪里亮了点哪里<se.1>"
                : Configuration.GlowingDoorText.Replace("{洞}", "{门}", StringComparison.Ordinal);
            Configuration.DoorChoiceText = Configuration.DoorChoiceText == "随机选洞：{洞}"
                ? "随机选门：{门}"
                : Configuration.DoorChoiceText.Replace("{洞}", "{门}", StringComparison.Ordinal);
            Configuration.Version = 2;
            Save();
        }

        notifications = new NotificationService(Configuration, chat, log);
        Maps = new MapTracker(Configuration, data, interop, log, notifications);
        mapReader = new TreasureMapReader(data);
        mainWindow = new MainWindow(this);
        windows.AddWindow(mainWindow);
        commands.AddHandler("/tledger", new CommandInfo(OnCommand)
        {
            HelpMessage = COMMAND_HELP["/tledger → ".Length..]
        });

        pluginInterface.UiBuilder.Draw += windows.Draw;
        pluginInterface.UiBuilder.OpenMainUi += Open;
        pluginInterface.UiBuilder.OpenConfigUi += OpenConfiguration;
        chat.ChatMessage += OnChat;
        chat.LogMessage += OnLog;
        client.TerritoryChanged += OnTerritoryChanged;
        framework.Update += OnUpdate;
        duty.DutyCompleted += OnDutyCompleted;
    }

    public Configuration Configuration
    {
        get;
    }

    public MapTracker Maps
    {
        get;
    }

    public void StartSession()
    {
        var previous = Configuration.Session;
        if (previous.Active)
        {
            return;
        }

        if (previous.Maps.Count > 0 || previous.LootAdjustments.Count > 0)
        {
            Configuration.History.Add(previous);
        }

        Configuration.Session = new Session
        {
            Active = true,
            Pay = new PaySettings
            {
                MapGrade = previous.Pay.MapGrade,
                BonusPresets = previous.Pay.BonusPresets.ToDictionary(pair => pair.Key, pair => pair.Value
                    .Select(rule => new BonusRule
                    {
                        ItemName = rule.ItemName,
                        PerItem = rule.PerItem,
                        Enabled = rule.Enabled
                    }).ToList(), StringComparer.Ordinal),
                PlannedMaps = previous.Pay.PlannedMaps,
                BasePay = previous.Pay.BasePay,
                PassengerFeeEnabled = previous.Pay.PassengerFeeEnabled,
                PassengerFeePerBottom = previous.Pay.PassengerFeePerBottom,
                TankExtraPayEnabled = previous.Pay.TankExtraPayEnabled,
                TankExtraPay = previous.Pay.TankExtraPay,
                Bonuses =
                [
                    .. previous.Pay.Bonuses.Select(rule => new BonusRule
                    {
                        ItemName = rule.ItemName,
                        PerItem = rule.PerItem,
                        Enabled = rule.Enabled
                    })
                ]
            }
        };
        OnTerritoryChanged(client.TerritoryType);
        Save();
    }

    public void Save()
    {
        pluginInterface.SavePluginConfig(Configuration);
    }

    public void Dispose()
    {
        duty.DutyCompleted -= OnDutyCompleted;
        framework.Update -= OnUpdate;
        client.TerritoryChanged -= OnTerritoryChanged;
        chat.LogMessage -= OnLog;
        chat.ChatMessage -= OnChat;
        pluginInterface.UiBuilder.Draw -= windows.Draw;
        pluginInterface.UiBuilder.OpenMainUi -= Open;
        pluginInterface.UiBuilder.OpenConfigUi -= OpenConfiguration;
        commands.RemoveHandler("/tledger");
        windows.RemoveAllWindows();
        Maps.Dispose();
        Save();
    }

    private void Open()
    {
        mainWindow.IsOpen = true;
    }

    private void OpenConfiguration()
    {
        mainWindow.RequestedTab = "设置";
        mainWindow.IsOpen = true;
    }

    private void OnCommand(string command, string arguments)
    {
        var parts = arguments.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        switch (parts)
        {
            case []:
                Open();
                return;
            case ["config" or "cfg"]:
                OpenConfiguration();
                return;
            case ["start"]:
                StartSession();
                Open();
                return;
            case ["stop"]:
                Configuration.Session.Active = false;
                Save();
                Open();
                return;
            case ["report"]:
                mainWindow.RequestedTab = "掉落统计";
                mainWindow.IsOpen = true;
                return;
            case ["team", "on" or "off" or "switch"]:
                Configuration.AllowPartyMessages = parts[1] == "on" ||
                                                   parts[1] == "switch" && !Configuration.AllowPartyMessages;
                Save();
                return;
            case ["yan"]:
                try
                {
                    Configuration.Session.AdjustLoot("羊角笛", 99);
                    Save();
                }
                catch (OverflowException)
                {
                    chat.PrintError("挖宝账本：羊角笛数量超出可记录范围。");
                }

                return;
            case ["door"]:
                var count = TreasureRules.GetDoorCount(client.TerritoryType);
                if (count == 0)
                {
                    chat.PrintError("挖宝账本：请在可以选门的宝库内使用该指令。");
                    return;
                }

                string[] choices = count == 3 ? ["左", "中", "右"] : ["左", "右"];
                var door = choices[Random.Shared.Next(choices.Length)];
                notifications.Send(Configuration.DoorChoice,
                    Configuration.DoorChoiceText.Replace("{门}", door, StringComparison.Ordinal));
                return;
            default:
                chat.PrintError("挖宝账本：未知指令。使用 /tledger config 查看指令列表。");
                return;
        }
    }

    private void OnUpdate(IFramework sender)
    {
        notifications.Update();
        Maps.Update(client.IsLoggedIn);
        if (client.IsLoggedIn && mapReader.Update(Configuration.Session))
        {
            Save();
        }
    }

    private void OnTerritoryChanged(uint territory)
    {
        if (TreasureRules.IsDungeon(territory))
        {
            Configuration.Session.EnterDungeon(territory);
        }
        else if (Configuration.Session.CurrentMap is
                 {
                     Territory: > 0
                 } current)
        {
            current.Closed = true;
        }

        Save();
    }

    private void OnLog(ILogMessage message)
    {
        if (Configuration.Session.Active && message.LogMessageId == 3756 &&
            !TreasureRules.IsDungeon(client.TerritoryType))
        {
            Configuration.Session.BeginMap();
            Save();
            return;
        }

        if (!Configuration.Session.Active || Configuration.Session.CurrentMap is not
            {
                Territory: > 0
            } current)
        {
            return;
        }

        switch (message.LogMessageId)
        {
            case 6998 or 9365 when message.TryGetIntParameter(0, out var round):
                current.Floor = Math.Max(current.Floor, round + 1);
                break;
            case 9351 when current.Territory is 794 or 924 or 1123 or 1279:
                current.Summons++;
                current.Floor = Math.Max(current.Floor, current.Summons);
                break;
            default:
                return;
        }

        Save();
    }

    private void OnDutyCompleted(IDutyStateEventArgs arguments)
    {
        if (Configuration.Session is not
            {
                Active: true,
                CurrentMap:
                {
                    Territory: > 0
                } current
            } || current.Territory != arguments.TerritoryType.RowId)
        {
            return;
        }

        current.Bottom = true;
        Save();
    }

    private void OnChat(IHandleableChatMessage message)
    {
        try
        {
            if (message.LogKind is not (XivChatType.SystemMessage or XivChatType.SystemError or XivChatType.ErrorMessage
                or XivChatType.LootNotice))
            {
                return;
            }

            var text = message.OriginalMessage.ExtractText();
            if (TreasureParser.Apply(Configuration.Session, text))
            {
                Save();
            }

            switch (TreasureRules.GetReminder(text, client.TerritoryType, out var door))
            {
                case TreasureReminder.BeforeEntrance:
                    notifications.Send(Configuration.RollReminder, Configuration.RollText);
                    break;
                case TreasureReminder.AfterEntrance:
                    notifications.Send(Configuration.AfterEntranceReminder, Configuration.AfterEntranceText);
                    break;
                case TreasureReminder.GuaranteedPassage:
                    notifications.Send(Configuration.GlowingDoor,
                        Configuration.GlowingDoorText.Replace("{门}", door, StringComparison.Ordinal));
                    break;
                case TreasureReminder.None:
                default:
                    break;
            }
        }
        catch (Exception exception)
        {
            log.Error(exception, "处理挖宝消息时出错");
        }
    }

    #region 常量

    public const string COMMAND_HELP = """
                                       /tledger → 打开主窗口
                                       /tledger config | cfg → 打开插件设置
                                       /tledger start | stop → 开始/结束挖宝记录
                                       /tledger report → 显示本次挖宝掉落统计
                                       /tledger door → 随机选门
                                       /tledger team on | off | switch → 发送消息到小队开/关/切换
                                       /tledger yan → 在当前掉落列表里添加 99 个羊角笛
                                       """;

    #endregion
}
