<!-- Generated: 2026-09-09 | Scope: 46 first-party C# files + Unity configuration | Static scan only -->

# Tower Defense Unity — 项目总览

## 目的与运行方式

这是一个 Unity 6.5（`6000.5.9f1`）的纯代码塔防 Demo。它不依赖预制体或已保存场景：`GameBootstrap` 在任意场景加载后创建 `GameManager`，后者在运行时组装相机、地图、HUD、塔、防御波次和特效。

核心玩法是“建造与战斗并行”：玩家部署五类塔，敌人沿三条正交路线抵达基地；游戏支持三种索敌策略、追踪/AOE 子弹、减速、治疗、对象池、P 点弹幕、暂停/倍速与结算。

## 快速地图

```
GameBootstrap
  -> GameManager (composition root + session state)
      -> MapSystem / TowerPlacer / WaveSpawner
      -> Enemy <-> Tower -> Projectile
      -> SpatialGrid + MinHeap (targeting/query acceleration)
      -> BattleUiRoot -> runtime tactical HUD
      -> EffectFactory / Sfx / Screen effects
```

## 关键约束

- 运行时生成世界和 UI；场景中的手工引用不是架构的一部分。
- `GameConfig` 是塔、敌人、波次和地图参数的主要数据源；`Definitions` 定义数据结构和枚举。
- 自定义脚本未使用 `.asmdef`，因此全部编译进 Unity 默认程序集。
- `Assets/Resources` 的三个 shader 是屏幕模糊、Bloom 和 UI 亚克力材质的运行时依赖。
- `EditorBuildSettings.asset` 当前 `m_Scenes: []`：编辑器 Play 可自启动，但正式构建仍需至少加入一个启动场景。

## 目录职责

| 路径 | 职责 |
|---|---|
| `Assets/Scripts/Core` | 启动、会话状态、可调数值、世界视觉令牌 |
| `Assets/Scripts/Data` | 枚举与序列化定义 |
| `Assets/Scripts/Actors` | 敌人、塔、子弹的运行时行为 |
| `Assets/Scripts/Systems` | 地图/BFS、波次、放置、空间哈希 |
| `Assets/Scripts/UI` | 运行时构建的 UGUI/TMP HUD 与设计系统 |
| `Assets/Scripts/Effects` | 特效、音效、镜头与后处理 |
| `Assets/Scripts/Util` | 贴图工厂、对象池、最小堆 |
| `Assets/Resources` | 运行时通过 shader 名称寻找的三个 shader |

详见 [ARCHITECTURE.md](ARCHITECTURE.md)、[MODULES.md](MODULES.md)、[DEPENDENCIES.md](DEPENDENCIES.md)。
