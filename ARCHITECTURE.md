<!-- Generated: 2026-09-09 | Files scanned: 46 C# + 3 runtime shaders | Token estimate: ~760 -->

# 架构地图

## 启动与组装

```
[Any loaded Unity scene]
  GameBootstrap.AutoStart()
      -> GameManager.Awake()
          -> EnsureCamera()
          -> CreateHud() -> UiFactory.Canvas -> BattleUiRoot
          -> CreateWorld()
              -> MapSystem.Init()
              -> TowerPlacer.Init()
              -> WaveSpawner
          -> ResetState() -> GameLoop()
```

`GameManager` 是当前的 composition root、会话状态机和服务定位器。它持有活跃敌人/塔列表，按帧重建 `SpatialGrid` 与两个 `MinHeap`，并向 Actor/UI/System 暴露查询和命令。

## 战斗数据流

```
GameConfig + Definitions
  -> WaveSpawner -> Enemy.Spawn -> GameManager enemies
  -> TowerPlacer -> Tower -> GameManager towers

Tower.Update -> SelectTarget -> SpatialGrid / MinHeap
             -> Projectile -> Enemy.TakeDamage
             -> GameManager.DamageEnemiesInRadius (AOE)
Enemy death/reach-base -> GameManager economy, score, life, result
```

`MapSystem` 从基地反向 BFS 生成距离场；敌人读取预建路线移动。`TowerPlacer` 负责屏幕点击到网格格子的转换、占用管理和选中状态。`WaveSpawner` 通过协程依次执行 `GameConfig.Rounds`。

## 表现与 UI

```
BattleUiRoot (input + assembly)
  ├─ mission/readout/time islands   任务、幕次、结界、倍速
  ├─ deployment hand                五类塔与索敌策略
  ├─ unit dossier                   已选塔详情/BOOM/撤退
  ├─ barrage command                P 点积累与弹幕
  └─ pause/result screens           暂停与胜负结算

BattleUiTheme + UiFactory + UiText/UiFont
  -> runtime UGUI/TMP tree
```

`TerminalHud`、`HudTopBar`、`HudDeployBar` 等旧表现类当前只作为迁移对照保留；默认入口不会实例化它们，因此不会产生重复快捷键或重复 `EventSystem`。

世界层由 `WorldArt`、`SpriteFactory` 和 Actor 内部子对象绘制。`ScreenBlurFx` 是相机上的唯一 `OnRenderImage` 后处理链；它为 UI 亚克力和 Bloom 提供纹理。

## 生命周期与所有权

- `GameManager` 与 `Sfx` 使用 `DontDestroyOnLoad`。
- `WorldRoot` 拥有一局中创建的地图、系统、活跃 Actor 和氛围效果；`Restart()` 销毁并重建它。
- `Enemy`、`Projectile` 由静态 `ObjectPool<T>` 复用；回收时脱离 `WorldRoot`，避免重开时一并销毁。
- HUD 在 `GameManager` 首次 `Awake` 时创建一次，重开局不重建 HUD。

## 架构边界现状

边界以文件夹划分，但不是强制边界：Actor、UI、System 大量直接访问 `GameManager.Instance`。这是小型 Demo 中可接受的便利取舍，但会限制可测性和模块替换；重构方向见 [REFACTOR_PLAN.md](REFACTOR_PLAN.md)。
