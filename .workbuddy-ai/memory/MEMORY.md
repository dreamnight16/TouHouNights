# TowerDefenseUnity — 项目长期记忆

Unity 6000.5.9f1，2D 塔防（东方 Project 世界观包装）。工程结构：
`Assets/Scripts/{Core,Actors,Systems,Data,UI,Effects,Util}`、`Assets/Editor`（PlayMode 校验脚本）。

## 世界观术语 ↔ 代码标识符（务必按这套命名理解需求）

| 游戏内术语 | 代码含义 |
|---|---|
| 灵力 / SPIRIT | 部署符卡的唯一资源，即金币（`GameManager.Spirit`） |
| 符卡 | 防御塔 `Tower` / `TowerDefinition` |
| 敌影 | 敌人 `Enemy` / `EnemyDefinition` |
| 结界 | 生命值 `GameManager.Lives` |
| 弹幕 / P点 / 弹幕威力 POWER | `GameManager.Power`，左下角充能条，与灵力是两套东西 |
| 时之流速 | 倍速 `SpeedLevels` |
| 幕次 / Stage | `GameConfig.Rounds` |

**坑点**：`BattleUiRoot.Views.cs` 的注释里出现过「灵力条」指代 POWER 充能条，属于历史笔误；
UI 上 `灵力 SPIRIT` 标签始终标在金币大数字上。遇到「灵力」需求先确认指的是哪一个。

## 关键约定

- 所有可调数值集中在 `GameConfig.cs`，不把魔法数字散进逻辑代码。
- 灵力累计逻辑集中在 `GameConfig` 的「灵力 SPIRIT（部署费用）」块：
  初始灵力 `StartingSpirit` / 自然回复 `SpiritRegenPerSecond` / 击破回复 `Enemies[type].SpiritReward` /
  上限 `MaxSpirit` / 返还 `RetreatRefundRatio` `DefeatRefundRatio`。手感对齐明日方舟 DP。
- 表现层通过装配点注入依赖（`BattleUiRoot.Init(GameManager)`），不自行读 `GameManager.Instance`；
  `Enemy` / `Projectile` 等世界对象例外，允许直接用单例。
- 新增 UI 走 `BattleUiRoot` 体系（`BattleUiTheme` + `UiKit` + `TdTheme`），`HudXxx` / `TerminalHud` 是旧体系。
- 提交信息用中文，格式 `fix:` / `feat:` + 简述。

## 工具链注意

- **Edit 工具并行改同一文件会丢改动**（写覆盖 / EBUSY）。同一文件多处修改必须串行，改完全局检索复查。
- **Unity batchmode 在本机跑不通**（2026-09-16 实测三次）：
  不加 `-noUpm` → UPM IPC 连接失败直接 exit 1；
  加 `-noUpm` → 卡在 `Application.AssetDatabase Initial Refresh Start`，17 分钟零进展。
  **所以本工程无法在本机做编译验证**，改完代码只能靠静态校验 + 请用户开工程确认。
- 静态校验脚本：`.workbuddy-ai/verify_symbols.py <Assets路径>` ——
  花括号配平 + `GameConfig.X`/`game.X`/`def.X` 等引用的符号存在性检查。
  经验已沉淀为 skill `unity-csharp-refactor-verify`。
- PlayMode 自动化校验在 `Assets/Editor/*Verification.cs`，靠 `SessionState` 开关触发，
  内含硬编码的期望数值（改初始资源数值时要同步更新断言）。
