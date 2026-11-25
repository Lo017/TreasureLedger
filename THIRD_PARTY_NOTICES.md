# 来源声明

以下列出挖宝账本当前实现中移植、复用和参考的来源，整理日期为 2026-11-25。

## SimpleMapTracker — Caraxi

上游项目：[Caraxi/SimpleMapTracker](https://github.com/Caraxi/SimpleMapTracker)。

- `TreasureLedger/Maps/MapProbe.cs`：从上游 [GameFunctions.cs](https://github.com/Caraxi/SimpleMapTracker/blob/master/GameFunctions.cs) 移植并调整宝图类型、宝藏点编号及管理器地址的读取逻辑，保留对应的函数特征码和委托调用方式。
- `TreasureLedger/Maps/MapTracker.cs`：参考 [PlayerMapState.cs](https://github.com/Caraxi/SimpleMapTracker/blob/master/PlayerMapState.cs) 的 `TreasureSpot` 坐标解析、地图打开与标旗流程，并参考上游 `Plugin.cs` 的地图标记处理方式，整合为本插件的地图读取与图标绘制。

上游仓库根目录未列出许可文件。上述部分的原始来源与作者见本节。

## TreasureForecast — yimo0908

上游项目：[yimo0908/TreasureForecast](https://github.com/yimo0908/TreasureForecast)。

`TreasureLedger/Plugin.cs` 的宝库日志处理参考上游 [Plugin.cs](https://github.com/yimo0908/TreasureForecast/blob/master/TreasureForecast/Plugin.cs) 中通过 `IChatGui.LogMessage` 识别宝库层数事件、读取第 0 个整数参数的方式。宝库日志编号及其含义用于本插件的层数记录；具体日志内容同时以国服游戏数据核对。仓库交付结构也参考该项目。

上游 [LICENSE.md](https://github.com/yimo0908/TreasureForecast/blob/master/LICENSE.md) 为 GNU Affero General Public License v3，许可原文副本见 [LICENSES/TreasureForecast-AGPL-3.0.txt](LICENSES/TreasureForecast-AGPL-3.0.txt)。

## MapPartyAssist — wrath16

上游项目：[wrath16/MapPartyAssist](https://github.com/wrath16/MapPartyAssist)。

`TreasureLedger/Services/TreasureProgress.cs` 的 G18 日志编号及召唤、击败、特殊阶段的区分参考上游 [DutyManager.cs](https://github.com/wrath16/MapPartyAssist/blob/master/MapPartyAssist/Services/DutyManager.cs)，结合国服 `LogMessage` 数据实现轮次记录。

## 挖宝会计 — ler725

上游项目：[ler725/ff14-treasure-bookkeeper](https://github.com/ler725/ff14-treasure-bookkeeper)。

图数、洞数、层数、金币、掉落的消息匹配条件与奖品分类参考挖宝会计，使用的规则版本为 `7.3挖宝会计(1).xml`。对应实现位于 `TreasureLedger/Services/TreasureParser.cs`、`TreasureLedger/Data/LootPreset.cs` 和 `TreasureLedger/Plugin.cs`。

## 选门小助手

分配与保送提示的消息匹配、指令选门的方向选择参考 `选门小助手新版本可用.xml`，对应实现位于 `TreasureLedger/Services/TreasureRules.cs` 和 `TreasureLedger/Plugin.cs`。XML 中的触发流程按 Dalamud 事件接口改写为 C#。

## 运行依赖

插件通过 Dalamud SDK 引用 Dalamud、FFXIVClientStructs、Lumina 与 Newtonsoft.Json，分别用于插件接口、游戏结构访问、本地游戏数据读取及配置存档。这些项目的署名与许可见各自上游项目。
