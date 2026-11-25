# 挖宝账本 TreasureLedger

面向国服的挖宝记录、工资结算与宝图位置插件。

我真的厌倦了加群领取最新挖宝会计。

## 安装

通过第三方插件库安装：

```text
https://raw.githubusercontent.com/Lo017/DalamudPlugin/main/repo.json
```

1. 打开 Dalamud 设置，在自定义插件仓库中添加本项目的库地址并保存。
2. 打开插件安装器，搜索“挖宝账本”或 `TreasureLedger` 并安装。
3. 在游戏中输入 `/tledger`，打开主窗口。

## 挖宝记录

记录挖宝中的掉落物、系统金币、宝图类型、图数、洞数、层数和下底次数。每趟挖宝手动开始、结束记录，支持查看逐图明细和历史记录。

## 工资结算

在“薪酬结算”中设置约定图数、每人底薪、奖金物品和单件奖金。底薪与奖金按每人独立结算，物品掉落多件时累计奖金；不足约定图数时仍按设定底薪结算。

| 结算项 | 计算方式 |
| --- | --- |
| 打工仔工资 | 每人底薪 + 各奖金物品数量 × 单件奖金之和 |
| （可选）T 加薪工资 | 打工仔工资 + 固定加薪；勾选“T 加薪”后显示 |
| （可选）挂件费用 | 每次下底费用 × 累计下底次数；勾选“挂件收费”后显示 |

T 加薪和挂件收费默认关闭，勾选后可填写具体金额。

支持粘贴招募文本，导入宝图类型、约定图数和底薪。例如，`G18 15图 150w 大羊100 小羊10` 会填入 G18、15 图和 150 万底薪，并切换至已保存的 G18 奖金列表。物品奖金金额在列表中自行设置。

## 奖金物品预设

在“掉落奖金”首行选择宝图。各宝图分别保存奖金物品列表，初始为空；“自定义”用于一趟挖多种图。添加物品后可设置单件奖金，也可删除已添加的物品。

下拉栏提供以下物品预设，“其他”支持自行填写物品名。手动修正使用相同的物品预设。

| 宝图分类 | 预设奖品 |
| --- | --- |
| G10 | 纳夏薄绢、深层传送魔纹的地图 |
| G12 | 附魔榆木木材、小牛皮、前沿布料、古菩台灯、黄金大河狸 |
| G14 | 绚烂华丽的羽毛、惊奇补剂、灼热的火角、轻柔棉布、竞技布 |
| G15 | 绚烂华丽的羽毛、惊奇补剂、灼热的火角、轻柔棉布、竞技布、迷你好友、迷你艾多纽斯 |
| G16 | 图拉尔古布 |
| G17 | 幻梦武器箱、弹性皮带、分光性染剂、薄亚麻布、神话传令官布料、运动布、靛蓝斜纹布、火炬 |
| G18 | 幻梦武器箱、弹性皮带、分光性染剂、薄亚麻布、神话传令官布料、运动布、靛蓝斜纹布、火炬、索取小猫、小小羊、羊角笛 |
| 深层传送魔纹 | 金鲶 |
| 鞣革制的隐藏地图 | 鞣革制的隐藏地图、雾绢、魔石精兜帽、莲叶、礼服面料、萨维奈绢布 |

同一奖品在相应宝图分类中分别列出，结算按物品名称累计。

## 宝图位置与提醒

解读宝图后，在“宝图位置”中查看地图名和坐标，可打开地图、标旗或发送位置到小队（默认关闭）。支持记录队友在小队频道发送的地图坐标。

进洞前后遇到尚未完成分配的提示时，可发送分配提醒。两项提醒分别设置频道和文本，默认内容为 `冰冰冰<se.1>`。

运河宝库出现发光门（保送）时，可发送保送提示，默认内容为 `哪里亮了点哪里<se.1>`。

在可以选门的宝库内输入 `/tledger door`，随机选择一个方向。默认内容为 `随机选门：{门}`，其中 `{门}` 会替换为左、中（如有）或右。

分配提醒、保送提示和选门输出均支持自定义文本，频道可选“关闭”“/e 仅自己”或“/p 小队”，默认频道为 `/e`。

小队消息由窗口顶部“允许向小队发消息”开关控制，默认关闭。

## 命令

| 命令 | 操作 |
| --- | --- |
| `/tledger` | 打开主窗口 |
| `/tledger config`、`/tledger cfg` | 打开插件设置 |
| `/tledger start`、`/tledger stop` | 开始/结束挖宝记录 |
| `/tledger report` | 打开本次掉落统计页 |
| `/tledger door` | 随机选门 |
| `/tledger team on`、`/tledger team off`、`/tledger team switch` | 开启、关闭或切换小队消息发送 |
| `/tledger yan` | 在当前掉落列表里添加 99 个羊角笛 |

## 项目架构

```text
TreasureLedger/
├── TreasureLedger/
│   ├── Data/
│   │   ├── LootPreset.cs          —— 宝图分类与奖金物品预设
│   │   └── TreasureMapCatalog.cs  —— 游戏宝图类型与 Gxx 对照
│   ├── Models/
│   │   ├── BonusRule.cs           —— 物品奖金规则
│   │   ├── DiagnosticEntry.cs     —— 诊断事件记录
│   │   ├── LootEntry.cs           —— 掉落物品与数量
│   │   ├── MapRun.cs              —— 逐图记录
│   │   ├── NoticeChannel.cs       —— 提醒频道
│   │   ├── PaySettings.cs         —— 薪酬设置与各宝图奖金列表
│   │   ├── PayrollSummary.cs      —— 工资结算结果
│   │   ├── RecruitmentTerms.cs    —— 招募文本解析结果
│   │   ├── Session.cs             —— 本趟记录、统计与手动修正
│   │   └── TreasureReminder.cs    —— 提醒类型
│   ├── Services/
│   │   ├── DiagnosticService.cs   —— Debug 记录、复制与导出
│   │   ├── NotificationService.cs —— 提醒发送与小队消息开关
│   │   ├── Payroll.cs             —— 工资与挂件费用计算
│   │   ├── RecruitmentParser.cs   —— 招募文本解析
│   │   ├── TreasureParser.cs      —— 挖宝消息与掉落解析
│   │   ├── TreasureProgress.cs    —— 宝库层数与召唤轮次记录
│   │   └── TreasureRules.cs       —— 宝库区域、分配与保送提醒规则
│   ├── Maps/
│   │   ├── MapLocation.cs         —— 宝图位置
│   │   ├── MapProbe.cs            —— 宝图坐标读取
│   │   ├── MapTracker.cs          —— 宝图标记、地图显示与位置发送
│   │   └── TreasureMapReader.cs   —— 实际宝图类型识别
│   ├── Windows/
│   │   └── MainWindow.cs          —— 掉落、结算、位置、历史与设置窗口
│   ├── Plugin.cs                 —— 插件生命周期、指令与游戏事件
│   ├── Configuration.cs          —— 插件设置与历史记录
│   ├── TreasureLedger.csproj     —— 插件项目
│   └── TreasureLedger.json       —— Dalamud 插件清单
└── TreasureLedger.slnx            —— 解决方案
```

## 参考与致谢

本项目的挖宝消息匹配规则与奖品分类移植、改写自 [ler725 的挖宝会计（ACT 插件）](https://github.com/ler725/ff14-treasure-bookkeeper)，用于记录图数、洞数、层数、金币和掉落。

宝图类型与宝藏点的读取逻辑移植自 [Caraxi 的 Simple Map Tracker](https://github.com/Caraxi/SimpleMapTracker)，宝图位置解析、打开地图、标旗和地图图标等功能参考该项目实现。

宝库层数的日志事件处理参考了 [yimo0908 的 TreasureForecast](https://github.com/yimo0908/TreasureForecast)。分配提醒、保送提示和随机选门参考了“选门小助手”的规则，并按 Dalamud 事件接口改写。

G18 召唤与完成事件的区分参考了 [wrath16 的 MapPartyAssist](https://github.com/wrath16/MapPartyAssist)，日志编号结合国服游戏数据核对。

插件基于 Dalamud 开发，并使用 FFXIVClientStructs、Lumina 和 Newtonsoft.Json 等组件。感谢这些项目的作者与维护者。

具体来源、对应文件和许可信息见 [来源声明](THIRD_PARTY_NOTICES.md)。

## 授权

本项目的原创代码采用 [MIT 许可证](LICENSE) 授权。第三方移植、复用和参考内容的来源与许可说明见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。

