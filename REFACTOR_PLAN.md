<!-- Generated: 2026-09-09 | Incremental plan; preserve runtime-built-scene behavior -->

# 重构计划

目标是降低耦合、获得可测试的核心规则，同时不改变“空场景即 Play”的交互和现有数值。

## Phase 0 — 建立基线（先做）

1. 新建并加入 Build Settings 的最小启动场景，验证 Runtime Bootstrap。
2. 在 Unity 6.5 执行一次编译与 Play Mode 烟测，记录当前五类塔、三类索敌、重开、胜负与弹幕行为。
3. 为 `GameConfig` 关键表、总敌人数和评分结果补可比对的测试夹具。

验收：不改规则；能够稳定构建、启动、重开一局。

## Phase 1 — 分离纯逻辑

1. 从 `GameManager` 提取不依赖 `MonoBehaviour` 的 `GameSession`：金币、生命、分数、P 点、连击、胜负、结果计算。
2. 提取 `CombatQueryService`：活跃实体登记、最近目标、范围伤害/减速查询；保留现有 `GameManager` 作为适配入口。
3. 把 `GameResult` 计算写为输入确定的纯函数测试。

验收：Actor 的可观察行为不变；核心规则可在不启动 Unity 场景的情况下测试。

## Phase 2 — 消除隐藏依赖

1. 为 Actor 引入窄接口：`ICombatQueries`、`IGameCommands`、`IWorldContext`。
2. 在 Spawn/Init 时注入这些接口，而非在 Actor 中读取 `GameManager.Instance`。
3. UI 保持从 `GameManager` 获取一个只读 `IGameView`，命令经 `IGameCommands` 发出。

验收：`Enemy`、`Tower`、`Projectile` 的构造/初始化依赖可在文件顶部看见。

## Phase 3 — 性能与资源生命周期

1. 用 Unity Profiler 量化索引重建、HUD `Update`、特效和对象池分配。
2. 若数据证明需要，改空间网格为敌人移动时更新，策略堆按策略启用维护。
3. 集中释放运行时 Texture/Material/RenderTexture，并为静态缓存定义 editor/play 生命周期。

验收：以测量结果设定预算，不进行未验证的“优化”。

## Phase 4 — 表现层适配

1. 明确 Built-in Render Pipeline 目标，或把 `ScreenBlurFx` 迁移到目标 SRP 的渲染扩展点。
2. 将 CJK TMP 字体作为项目资源纳入版本控制。
3. 把 HUD 的状态刷新改为事件驱动；保留动画组件的逐帧更新。

## 建议的提交切片

`test: add bootstrap and pure-rule coverage` → `refactor: extract game session` → `refactor: inject combat services` → `perf: profile and optimize indexes` → `fix: harden rendering and font assets`。
