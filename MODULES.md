<!-- Generated: 2026-09-09 | Files scanned: 46 first-party C# files | Token estimate: ~900 -->

# 模块索引

## Core

| 文件 | 责任 |
|---|---|
| `GameBootstrap.cs` | `RuntimeInitializeOnLoadMethod` 自启动 |
| `GameManager.cs` | 会话状态、经济、索敌、伤害、重开、相机/世界组装 |
| `GameConfig.cs` | 地图、塔、敌人、波次、经济、视觉的集中参数 |
| `WorldArt.cs` | 世界层颜色、亮度和 sorting layer 令牌 |

## Data 与 Systems

| 文件 | 责任 |
|---|---|
| `Definitions.cs` | `TowerDefinition`、`EnemyDefinition`、`RoundDef`、`GameResult` 和枚举 |
| `MapSystem.cs` | 路线、道路格、可部署格、BFS 距离场、地图绘制 |
| `WaveSpawner.cs` | 波次协程与每幕生成计划 |
| `TowerPlacer.cs` | 点击放塔、格子占用、选中/撤退、范围指示器 |
| `SpatialGrid.cs` | 敌人空间分桶与圆形/切比雪夫范围查询 |

## Actors

| 文件 | 责任 |
|---|---|
| `Enemy.cs` | 路径移动、减速、近战攻击塔、受伤/死亡/到达基地、对象池 |
| `Tower.cs` | 选敌、射击、减速或治疗、血量、BOOM 技能、世界视觉 |
| `Projectile.cs` | 直线/追踪飞行、重锁定、命中、AOE、淡出、对象池 |

## UI

`TerminalHud` 是装配器和快捷键转发层。`HudTopBar`、`HudDeployBar`、`HudUnitCard`、`HudEnemyInspector`、`HudFeedback` 和 `TerminalResult` 各自拥有一个明确的界面职责。

基础设施：`TdTheme`（令牌）、`TdLayout`（锚点布局）、`TdKit`（复合控件）、`UiFactory`（Canvas/EventSystem/基础控件）、`UiText`/`UiFont`（TMP 与 legacy 回退）、`UiIcon`、`RoundedRectImage`、`RadialProgressImage`、`UiButtonFx`、`UiEasings`。`HealthBarView` 与 `FloatingTextView` 是世界空间 UI，`HudRangeIndicator` 表达塔射程。

## Effects 与 Util

| 文件组 | 责任 |
|---|---|
| `BurstEffect` / `EffectFactory` / `TrailDot` | 运行时命中特效与拖尾 |
| `Sfx` | 程序化音频和缓存的 `AudioClip` |
| `ScreenShake` / `CameraDrift` / `ScreenBlurFx` | 相机运动和后处理 |
| `AmbientDust` | 粒子氛围 |
| `SpriteFactory` | 缓存的运行时 Texture2D/Sprite 图元 |
| `ObjectPool<T>` | `Component` 对象复用 |
| `MinHeap<T>` | 面向最低血/最近终点索敌的最小堆 |

## 关键调用关系

`Tower` 和 `Projectile` 都依赖 `GameManager` 的查询/伤害 API；`Enemy` 依赖它进行生命周期通知并取得最近塔。UI 读取 `GameManager` 展示状态，并调用其公开命令。该关系图是后续提取 `GameSession`/接口时应保持的行为基线。
