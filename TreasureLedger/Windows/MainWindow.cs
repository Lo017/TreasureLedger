using TreasureLedger.Models;
using TreasureLedger.Data;
using TreasureLedger.Services;
using System.Globalization;
using System.Numerics;
using System.Text;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using TreasureLedger.Maps;

namespace TreasureLedger.Windows;

public sealed class MainWindow : Window
{
    private readonly Plugin plugin;
    private bool customBonusItem;
    private int correctionPresetIndex;
    private string newItem = string.Empty;
    private string correctionItem = string.Empty;
    private int correctionQuantity = 1;
    private int correctionCategory;
    private int correctionOperation;
    private bool customCorrectionItem;
    private bool customCorrectionQuantity;
    private string correctionError = string.Empty;
    private string recruitmentText = string.Empty;
    private string recruitmentResult = string.Empty;
    private string diagnosticResult = string.Empty;
    private Session? history;

    public MainWindow(Plugin plugin) : base("挖宝账本###TreasureLedger")
    {
        this.plugin = plugin;
        Size = new Vector2(920, 650);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public string? RequestedTab
    {
        get;
        set;
    }

    public override void Draw()
    {
        if (RequestedTab is not null)
        {
            history = null;
        }

        var configuration = plugin.Configuration;
        var session = history ?? configuration.Session;
        ImGui.TextUnformatted(session.Title);
        ImGui.SameLine();
        ImGui.TextUnformatted(session.Active ? "记录中" : "已停止");
        if (history is null)
        {
            switch (session.Active)
            {
                case false when ImGui.Button("开始"):
                    plugin.StartSession();
                    session = configuration.Session;
                    break;
                case true when ImGui.Button("结束"):
                    session.Active = false;
                    plugin.Save();
                    break;
            }
        }
        else if (ImGui.Button("返回当前记录"))
        {
            history = null;
            session = configuration.Session;
        }

        ImGui.SameLine();
        var allowed = configuration.AllowPartyMessages;
        if (ImGui.Checkbox("允许向小队发消息", ref allowed))
        {
            configuration.AllowPartyMessages = allowed;
            plugin.Save();
        }

        ImGui.Separator();
        if (!ImGui.BeginTabBar("账本分页"))
        {
            return;
        }

        if (ImGui.BeginTabItem("掉落统计", RequestedTab == "掉落统计" ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None))
        {
            DrawStatistics(session);
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("薪酬结算"))
        {
            DrawPayroll(session);
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("宝图位置"))
        {
            DrawMaps();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("设置", RequestedTab == "设置" ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None))
        {
            DrawSettings();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("历史记录"))
        {
            foreach (var previous in configuration.History.AsEnumerable().Reverse())
            {
                if (ImGui.Selectable(
                        $"{previous.Title} · {previous.MapCount} 图 · {previous.Gil:N0} 金币##{previous.StartedAt:O}"))
                {
                    history = previous;
                }
            }

            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
        RequestedTab = null;
    }

    private void DrawStatistics(Session session)
    {
        ImGui.TextUnformatted(
            $"图数 {session.MapCount}    洞数 {session.PortalCount}    下底 {session.BottomCount}    获得金币 {session.Gil:N0}");
        if (ImGui.CollapsingHeader("逐图记录", ImGuiTreeNodeFlags.DefaultOpen) &&
            ImGui.BeginTable("逐图", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            foreach (var title in new[]
                     {
                         "序号", "宝图", "洞", "最高层数", "下底", "金币"
                     })
            {
                ImGui.TableSetupColumn(title);
            }

            ImGui.TableHeadersRow();
            for (var index = 0; index < session.Maps.Count; index++)
            {
                var map = session.Maps[index];
                ImGui.TableNextRow();
                foreach (var text in new[]
                         {
                             (index + 1).ToString(), map.MapGrade.Length == 0 ? "未识别" : map.MapGrade,
                             map.Portal ? "有" : "无", map.Floor.ToString(), map.Bottom ? "是" : "否",
                             map.Gil.ToString("N0")
                         })
                {
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(text);
                }
            }

            ImGui.EndTable();
        }

        if (ImGui.CollapsingHeader("掉落统计", ImGuiTreeNodeFlags.DefaultOpen) &&
            ImGui.BeginTable("掉落", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            foreach (var title in new[]
                     {
                         "物品", "掉落数量", "每人累计奖金"
                     })
            {
                ImGui.TableSetupColumn(title);
            }

            ImGui.TableHeadersRow();
            foreach (var loot in session.TotalLoot)
            {
                var rate = session.Pay.Bonuses.Where(rule => rule.Enabled && rule.ItemName == loot.ItemName)
                    .Sum(rule => rule.PerItem);
                ImGui.TableNextRow();
                foreach (var text in new[]
                         {
                             loot.ItemName, loot.Quantity.ToString(), checked(rate * loot.Quantity).ToString("N0")
                         })
                {
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(text);
                }
            }

            ImGui.EndTable();
        }

        if (!ImGui.CollapsingHeader("手动修正"))
        {
            return;
        }

        ImGui.SetNextItemWidth(80);
        ImGui.Combo("##修正类别", ref correctionCategory, "宝图\0进洞\0下底\0金币\0物品\0");
        if (correctionCategory == 4)
        {
            ImGui.SameLine();
            var grade = correctionPresetIndex < LootPreset.All.Count
                ? LootPreset.All[correctionPresetIndex].Grade
                : "本趟掉落";
            ImGui.SetNextItemWidth(130);
            if (ImGui.BeginCombo("##修正宝图", grade))
            {
                for (var index = 0; index <= LootPreset.All.Count; index++)
                {
                    var name = index < LootPreset.All.Count ? LootPreset.All[index].Grade : "本趟掉落";
                    if (!ImGui.Selectable(name, correctionPresetIndex == index))
                    {
                        continue;
                    }

                    correctionPresetIndex = index;
                    correctionItem = string.Empty;
                    customCorrectionItem = false;
                }

                ImGui.EndCombo();
            }

            ImGui.SameLine();
            var items = correctionPresetIndex < LootPreset.All.Count
                ? LootPreset.All[correctionPresetIndex].Items
                : session.TotalLoot.Select(item => item.ItemName);
            DrawItemSelector("##修正物品", ref correctionItem, ref customCorrectionItem, items);
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(80);
        ImGui.Combo("##修正方向", ref correctionOperation, "增加\0减少\0");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120);
        if (ImGui.BeginCombo("##修正数量", customCorrectionQuantity ? "自定义数量" : correctionQuantity.ToString("N0")))
        {
            foreach (var amount in new[]
                     {
                         1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 20, 50, 100, 1_000, 10_000, 100_000, 1_000_000
                     })
            {
                if (!ImGui.Selectable(amount.ToString("N0"), !customCorrectionQuantity && correctionQuantity == amount))
                {
                    continue;
                }

                correctionQuantity = amount;
                customCorrectionQuantity = false;
            }

            if (ImGui.Selectable("自定义数量", customCorrectionQuantity))
            {
                customCorrectionQuantity = true;
            }

            ImGui.EndCombo();
        }

        if (customCorrectionQuantity)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(120);
            var quantity = (long)correctionQuantity;
            if (InputNumber("##输入修正数量", ref quantity))
            {
                correctionQuantity = (int)Math.Clamp(quantity, 1, int.MaxValue);
            }
        }

        if (ImGui.Button("应用修正"))
        {
            correctionError = string.Empty;
            var increase = correctionOperation == 0;
            try
            {
                switch (correctionCategory)
                {
                    case 0:
                        session.MapAdjustment = checked(session.MapAdjustment +
                                                        (increase
                                                            ? correctionQuantity
                                                            : -Math.Min(correctionQuantity, session.MapCount)));
                        break;
                    case 1:
                        session.PortalAdjustment = checked(session.PortalAdjustment +
                                                           (increase
                                                               ? correctionQuantity
                                                               : -Math.Min(correctionQuantity, session.PortalCount)));
                        break;
                    case 2:
                        session.BottomAdjustment = checked(session.BottomAdjustment +
                                                           (increase
                                                               ? correctionQuantity
                                                               : -Math.Min(correctionQuantity, session.BottomCount)));
                        break;
                    case 3:
                        session.GilAdjustment = checked(session.GilAdjustment +
                                                        (increase
                                                            ? correctionQuantity
                                                            : -Math.Min(correctionQuantity, session.Gil)));
                        break;
                    case 4:
                        var name = correctionItem.Trim();
                        if (name.Length == 0)
                        {
                            correctionError = "请选择或填写物品名称。";
                            break;
                        }

                        session.AdjustLoot(name, increase ? correctionQuantity : -correctionQuantity);
                        break;
                }

                plugin.Save();
            }
            catch (OverflowException)
            {
                correctionError = "数量超出可记录范围。";
            }
        }

        if (correctionError.Length > 0)
        {
            ImGui.TextUnformatted(correctionError);
        }
    }

    private void DrawPayroll(Session session)
    {
        if (ImGui.CollapsingHeader("招募导入"))
        {
            ImGui.InputTextMultiline("##招募内容", ref recruitmentText, 2048, new Vector2(-1, 70));
            if (ImGui.Button("粘贴招募"))
            {
                recruitmentText = ImGui.GetClipboardText();
            }

            ImGui.SameLine();
            if (ImGui.Button("导入薪酬"))
            {
                var terms = RecruitmentParser.Parse(recruitmentText);
                var imported = new List<string>();
                if (terms.Grade is not null)
                {
                    session.Pay.SelectMap(terms.Grade);
                    newItem = string.Empty;
                    customBonusItem = false;
                    imported.Add($"宝图 {terms.Grade}");
                }

                if (terms.PlannedMaps is
                    {
                    } maps)
                {
                    session.Pay.PlannedMaps = maps;
                    imported.Add($"约定 {maps} 图");
                }

                if (terms.BasePay is
                    {
                    } pay)
                {
                    session.Pay.BasePay = pay;
                    imported.Add($"底薪 {pay:N0} 金币");
                }

                recruitmentResult = imported.Count > 0 ? "已导入：" + string.Join("；", imported) : "未识别到宝图、图数或底薪。";
                plugin.Save();
            }

            ImGui.TextWrapped("例如：G18 15图 150w。底薪按万金币填写，也支持 150 或底150。");
            if (recruitmentResult.Length > 0)
            {
                ImGui.TextWrapped(recruitmentResult);
            }
        }

        var planned = (long)session.Pay.PlannedMaps;
        var basePay = session.Pay.BasePay;
        var passengerEnabled = session.Pay.PassengerFeeEnabled;
        var passenger = session.Pay.PassengerFeePerBottom;
        var tankEnabled = session.Pay.TankExtraPayEnabled;
        var tankExtra = session.Pay.TankExtraPay;
        var changed = InputNumber("约定图数", ref planned);
        changed |= InputNumber("每人底薪（金币）", ref basePay);
        changed |= ImGui.Checkbox("T 加薪", ref tankEnabled);
        if (tankEnabled)
        {
            changed |= InputNumber("T 固定加薪（金币）", ref tankExtra);
        }

        changed |= ImGui.Checkbox("挂件收费", ref passengerEnabled);
        if (passengerEnabled)
        {
            changed |= InputNumber("挂件每次下底付老板（金币）", ref passenger);
        }

        if (changed)
        {
            session.Pay.PlannedMaps = (int)Math.Clamp(planned, 1, int.MaxValue);
            session.Pay.BasePay = basePay;
            session.Pay.PassengerFeeEnabled = passengerEnabled;
            session.Pay.PassengerFeePerBottom = passenger;
            session.Pay.TankExtraPayEnabled = tankEnabled;
            session.Pay.TankExtraPay = tankExtra;
            plugin.Save();
        }

        ImGui.TextUnformatted("底薪与奖金按每人计算；本趟不足约定图数时仍按设定底薪结算。");
        if (ImGui.CollapsingHeader("掉落奖金", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.SetNextItemWidth(180);
            if (ImGui.BeginCombo("宝图", session.Pay.MapGrade))
            {
                foreach (var preset in LootPreset.All)
                {
                    if (!ImGui.Selectable(preset.Grade, session.Pay.MapGrade == preset.Grade))
                    {
                        continue;
                    }

                    session.Pay.SelectMap(preset.Grade);
                    newItem = string.Empty;
                    customBonusItem = false;
                    plugin.Save();
                }

                ImGui.EndCombo();
            }

            ImGui.SameLine();
            if (ImGui.Button("自定义"))
            {
                session.Pay.SelectMap("自定义");
                newItem = string.Empty;
                customBonusItem = false;
                plugin.Save();
            }

            for (var index = 0; index < session.Pay.Bonuses.Count; index++)
            {
                var rule = session.Pay.Bonuses[index];
                ImGui.PushID(index);
                var enabled = rule.Enabled;
                var amount = rule.PerItem;
                var edited = ImGui.Checkbox(rule.ItemName, ref enabled);
                ImGui.SameLine();
                if (ImGui.SmallButton("删除"))
                {
                    session.Pay.Bonuses.RemoveAt(index);
                    plugin.Save();
                    ImGui.PopID();
                    index--;
                    continue;
                }

                edited |= InputNumber("每件奖金", ref amount);
                if (edited)
                {
                    rule.Enabled = enabled;
                    rule.PerItem = amount;
                    plugin.Save();
                }

                ImGui.PopID();
            }

            var selected = LootPreset.All.FirstOrDefault(preset => preset.Grade == session.Pay.MapGrade);
            var items = session.Pay.MapGrade == "自定义"
                ? LootPreset.All.SelectMany(preset => preset.Items)
                : selected?.Items ?? [];
            DrawItemSelector("奖金物品", ref newItem, ref customBonusItem,
                items.Concat(session.CustomLootItems).Except(session.Pay.Bonuses.Select(rule => rule.ItemName),
                    StringComparer.Ordinal));
            if (ImGui.Button("添加奖金物品") && !string.IsNullOrWhiteSpace(newItem) &&
                session.Pay.Bonuses.All(rule => rule.ItemName != newItem.Trim()))
            {
                if (customBonusItem && !LootPreset.ItemNames.Contains(newItem.Trim()))
                {
                    session.CustomLootItems.Add(newItem.Trim());
                }

                session.Pay.Bonuses.Add(new BonusRule
                {
                    ItemName = newItem.Trim()
                });
                newItem = string.Empty;
                customBonusItem = false;
                plugin.Save();
            }
        }

        var summary = Payroll.Calculate(session);
        ImGui.Separator();
        ImGui.TextUnformatted($"打工仔每人工资：{summary.PerWorker:N0} 金币");
        ImGui.TextUnformatted($"每人底薪 {summary.BasePay:N0} + 累计奖金 {summary.Bonus:N0}");
        if (session.Pay.TankExtraPayEnabled)
        {
            ImGui.TextUnformatted($"T 加薪后每人工资：{summary.PerTank:N0} 金币（固定加薪 {summary.ExtraPay:N0}）");
        }

        if (session.Pay.PassengerFeeEnabled)
        {
            ImGui.TextUnformatted($"老板应收每个挂件费用：{summary.PerPassenger:N0} 金币");
            ImGui.TextUnformatted($"每次下底 {session.Pay.PassengerFeePerBottom:N0} × {session.BottomCount} 次");
        }

        if (!ImGui.Button("复制结算明细"))
        {
            return;
        }

        var output = new StringBuilder($"{session.Title}：{session.MapCount} 图，{session.BottomCount} 次下底\n");
        output.AppendLine($"打工仔每人工资：{summary.PerWorker:N0} 金币（底薪 {summary.BasePay:N0} + 奖金 {summary.Bonus:N0}）");
        if (session.Pay.TankExtraPayEnabled)
        {
            output.AppendLine($"T 加薪后每人工资：{summary.PerTank:N0} 金币（固定加薪 {summary.ExtraPay:N0}）");
        }

        if (session.Pay.PassengerFeeEnabled)
        {
            output.AppendLine($"老板应收每个挂件费用：{summary.PerPassenger:N0} 金币");
        }

        ImGui.SetClipboardText(output.ToString());
    }

    private static void DrawItemSelector(string label, ref string itemName, ref bool custom, IEnumerable<string> items)
    {
        var preview = itemName.Length == 0 ? "选择物品" : itemName;
        if (custom)
        {
            preview = "其他";
        }

        ImGui.SetNextItemWidth(200);
        if (ImGui.BeginCombo(label, preview, ImGuiComboFlags.HeightLarge))
        {
            foreach (var name in items.Distinct(StringComparer.Ordinal))
            {
                if (!ImGui.Selectable(name, !custom && itemName == name))
                {
                    continue;
                }

                itemName = name;
                custom = false;
            }

            ImGui.Separator();
            if (ImGui.Selectable("其他（输入物品名）", custom))
            {
                itemName = string.Empty;
                custom = true;
            }

            ImGui.EndCombo();
        }

        if (!custom)
        {
            return;
        }

        ImGui.SetNextItemWidth(220);
        ImGui.InputText($"物品名##{label}", ref itemName, 128);
    }

    private void DrawMaps()
    {
        var hasPartyLocations = plugin.Maps.Locations.Any(location => location.IsParty);
        if (hasPartyLocations && ImGui.Button("清空队友位置"))
        {
            plugin.Maps.Locations.RemoveAll(location => location.IsParty);
            plugin.Maps.RefreshMarkers();
            hasPartyLocations = false;
        }

        if (plugin.Maps.Locations.Count == 0)
        {
            ImGui.TextUnformatted("当前没有记录的地图位置。");
            return;
        }

        var style = ImGui.GetStyle();
        var nameWidth = 0f;
        var coordinateWidth = 0f;
        foreach (var location in plugin.Maps.Locations)
        {
            var name = location.IsParty ? location.Owner : plugin.Maps.PlayerName;
            nameWidth = Math.Max(nameWidth, ImGui.CalcTextSize(name).X);
            coordinateWidth = Math.Max(coordinateWidth,
                ImGui.CalcTextSize($"({location.Coordinates.X:F1}, {location.Coordinates.Y:F1})").X);
        }

        var buttonCount = hasPartyLocations ? 4 : 3;
        var buttonText = hasPartyLocations ? "查看标旗发送到小队移除" : "查看标旗发送到小队";
        var buttonWidth = ImGui.CalcTextSize(buttonText).X + style.FramePadding.X * buttonCount * 2 +
                          style.ItemSpacing.X * (buttonCount - 1);
        var cellPadding = style.FramePadding with
        {
            X = style.ItemSpacing.X / 2
        };
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, cellPadding);
        if (!ImGui.BeginTable("地图位置", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoSavedSettings))
        {
            ImGui.PopStyleVar();
            return;
        }

        ImGui.TableSetupColumn("姓名", ImGuiTableColumnFlags.WidthFixed, nameWidth);
        ImGui.TableSetupColumn("地图", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("坐标", ImGuiTableColumnFlags.WidthFixed, coordinateWidth);
        ImGui.TableSetupColumn("操作", ImGuiTableColumnFlags.WidthFixed, buttonWidth);
        for (var index = 0; index < plugin.Maps.Locations.Count; index++)
        {
            var location = plugin.Maps.Locations[index];
            if (index > 0 && !plugin.Maps.Locations[index - 1].IsParty && location.IsParty)
            {
                ImGui.TableNextRow(ImGuiTableRowFlags.None, ImGui.GetTextLineHeight());
            }

            ImGui.PushID(index);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(location.IsParty ? location.Owner : plugin.Maps.PlayerName);
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            var mapNameWidth = ImGui.GetContentRegionAvail().X;
            ImGui.TextUnformatted(location.Zone);
            if (ImGui.IsItemHovered() && ImGui.CalcTextSize(location.Zone).X > mapNameWidth)
            {
                ImGui.SetTooltip(location.Zone);
            }

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted($"({location.Coordinates.X:F1}, {location.Coordinates.Y:F1})");
            ImGui.TableNextColumn();
            if (ImGui.Button("查看"))
            {
                plugin.Maps.Open(location);
            }

            ImGui.SameLine();
            if (ImGui.Button("标旗"))
            {
                MapTracker.SetFlag(location);
            }

            ImGui.SameLine();
            ImGui.BeginDisabled(!plugin.Configuration.AllowPartyMessages);
            if (ImGui.Button("发送到小队"))
            {
                plugin.Maps.SendToParty(location);
            }

            ImGui.EndDisabled();
            if (location.IsParty)
            {
                ImGui.SameLine();
                if (ImGui.Button("移除"))
                {
                    plugin.Maps.Locations.RemoveAt(index);
                    plugin.Maps.RefreshMarkers();
                    index--;
                }
            }

            ImGui.PopID();
        }

        ImGui.EndTable();
        ImGui.PopStyleVar();
    }

    private void DrawSettings()
    {
        var configuration = plugin.Configuration;
        var autoFlag = configuration.AutoFlag;
        var sendFlag = configuration.AutoSendFlag;
        var flagText = configuration.FlagTemplate;
        var mapMarkers = configuration.ShowMapMarkers;
        var miniMarkers = configuration.ShowMiniMapMarkers;
        var roll = (int)configuration.RollReminder;
        var afterEntrance = (int)configuration.AfterEntranceReminder;
        var glowing = (int)configuration.GlowingDoor;
        var door = (int)configuration.DoorChoice;
        var rollText = configuration.RollText;
        var afterEntranceText = configuration.AfterEntranceText;
        var glowingText = configuration.GlowingDoorText;
        var doorText = configuration.DoorChoiceText;

        var changed = ImGui.Checkbox("解读自己的宝图后自动标旗", ref autoFlag);
        changed |= ImGui.Checkbox("标旗后自动向小队发送位置", ref sendFlag);
        changed |= ImGui.InputText("位置消息模板", ref flagText, 256);
        ImGui.TextWrapped("模板支持 <flag>；小队发送由窗口顶部的“允许向小队发消息”开关控制。");
        var markersChanged = ImGui.Checkbox("大地图显示宝藏图标", ref mapMarkers);
        markersChanged |= ImGui.Checkbox("小地图显示宝藏图标", ref miniMarkers);
        changed |= markersChanged;
        ImGui.Separator();
        changed |= ImGui.Combo("进洞前分配提醒", ref roll, "关闭\0/e 仅自己\0/p 小队\0");
        changed |= ImGui.InputText("进洞前分配文本", ref rollText, 256);
        changed |= ImGui.Combo("进洞后分配提醒", ref afterEntrance, "关闭\0/e 仅自己\0/p 小队\0");
        changed |= ImGui.InputText("进洞后分配文本", ref afterEntranceText, 256);
        changed |= ImGui.Combo("保送提示", ref glowing, "关闭\0/e 仅自己\0/p 小队\0");
        changed |= ImGui.InputText("保送提示文本", ref glowingText, 256);
        changed |= ImGui.Combo("选门输出", ref door, "关闭\0/e 仅自己\0/p 小队\0");
        changed |= ImGui.InputText("选门文本", ref doorText, 256);
        ImGui.TextWrapped("使用 /tledger door 在所选频道进行选门。");
        ImGui.Separator();
        ImGui.TextUnformatted(Plugin.COMMAND_HELP);
        ImGui.Separator();
        var debug = configuration.DebugEnabled;
        if (ImGui.Checkbox("Debug 模式", ref debug))
        {
            configuration.DebugEnabled = debug;
            plugin.Diagnostics.Record("DebugEnabled", "开始记录诊断信息");
            plugin.Save();
        }

        ImGui.TextWrapped("记录宝图识别、层数变化、物品筛选和相关系统消息，保留最近 2000 条。复现问题后可复制或导出记录用于反馈。");
        ImGui.TextUnformatted($"诊断记录：{plugin.Diagnostics.Count} 条");
        if (ImGui.Button("复制诊断记录"))
        {
            ImGui.SetClipboardText(plugin.Diagnostics.Snapshot());
            diagnosticResult = "已复制诊断记录。";
        }

        ImGui.SameLine();
        if (ImGui.Button("导出诊断记录"))
        {
            diagnosticResult = plugin.Diagnostics.Export();
        }

        ImGui.SameLine();
        if (ImGui.Button("清空诊断记录"))
        {
            plugin.Diagnostics.Clear();
            diagnosticResult = string.Empty;
        }

        if (diagnosticResult.Length > 0)
        {
            ImGui.TextWrapped(diagnosticResult);
        }

        if (changed)
        {
            configuration.AutoFlag = autoFlag;
            configuration.AutoSendFlag = sendFlag;
            configuration.FlagTemplate = flagText;
            configuration.ShowMapMarkers = mapMarkers;
            configuration.ShowMiniMapMarkers = miniMarkers;
            configuration.RollReminder = (NoticeChannel)roll;
            configuration.AfterEntranceReminder = (NoticeChannel)afterEntrance;
            configuration.GlowingDoor = (NoticeChannel)glowing;
            configuration.DoorChoice = (NoticeChannel)door;
            configuration.RollText = rollText;
            configuration.AfterEntranceText = afterEntranceText;
            configuration.GlowingDoorText = glowingText;
            configuration.DoorChoiceText = doorText;
            plugin.Save();
        }

        if (markersChanged)
        {
            plugin.Maps.RefreshMarkers();
        }
    }

    private static bool InputNumber(string label, ref long value)
    {
        var text = value.ToString("N0", CultureInfo.InvariantCulture);
        if (!ImGui.InputText(label, ref text, 32,
                ImGuiInputTextFlags.CallbackCharFilter | ImGuiInputTextFlags.CallbackEdit, FormatNumberInput))
        {
            return false;
        }

        if (text.Length == 0)
        {
            value = 0;
            return true;
        }

        if (!long.TryParse(text, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var amount))
        {
            return false;
        }

        value = amount;
        return true;
    }

    private static int FormatNumberInput(ref ImGuiInputTextCallbackData data)
    {
        if (data.EventFlag == ImGuiInputTextFlags.CallbackCharFilter)
        {
            return data.EventChar is >= '0' and <= '9' or ',' ? 0 : 1;
        }

        var text = Encoding.UTF8.GetString(data.BufTextSpan);
        var numberText = text.Replace(",", string.Empty, StringComparison.Ordinal);
        var amount = 0L;
        if (numberText.Length > 0 &&
            !long.TryParse(numberText, NumberStyles.None, CultureInfo.InvariantCulture, out amount))
        {
            return 0;
        }

        var formatted = numberText.Length == 0 ? string.Empty : amount.ToString("N0", CultureInfo.InvariantCulture);
        if (text == formatted)
        {
            return 0;
        }

        var cursorDigits = 0;
        var selectionStartDigits = 0;
        var selectionEndDigits = 0;
        for (var index = 0; index < text.Length; index++)
        {
            if (!char.IsAsciiDigit(text[index]))
            {
                continue;
            }

            if (index < data.CursorPos)
            {
                cursorDigits++;
            }

            if (index < data.SelectionStart)
            {
                selectionStartDigits++;
            }

            if (index < data.SelectionEnd)
            {
                selectionEndDigits++;
            }
        }

        var cursor = FindPosition(cursorDigits);
        var selectionStart = FindPosition(selectionStartDigits);
        var selectionEnd = FindPosition(selectionEndDigits);
        if (selectionStart == selectionEnd && cursor < formatted.Length && formatted[cursor] == ',' &&
            ImGui.IsKeyPressed(ImGuiKey.Delete))
        {
            cursor++;
            selectionStart = cursor;
            selectionEnd = cursor;
        }

        data.DeleteChars(0, data.BufTextLen);
        data.InsertChars(0, formatted);
        data.CursorPos = cursor;
        data.SelectionStart = selectionStart;
        data.SelectionEnd = selectionEnd;
        return 0;

        int FindPosition(int digits)
        {
            if (digits == 0)
            {
                return 0;
            }

            for (var index = 0; index < formatted.Length; index++)
            {
                if (char.IsAsciiDigit(formatted[index]) && --digits == 0)
                {
                    return index + 1;
                }
            }

            return formatted.Length;
        }
    }
}
