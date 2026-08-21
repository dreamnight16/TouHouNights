# 自动塔防与子弹追踪系统（Tower Defense & Homing Missile）

SAST 游戏开发与设计组免试题 · 程序方向 · **选项二 选项 B**

一个纯代码、零外部资源的 Unity 塔防 Demo。工程打开后按下 Play 即可运行，无需手动搭建场景、导入素材或配置任何 UGUI/物理层。

---

## 一、如何运行

1. 用 **Unity Hub** 打开本目录 `TowerDefenseUnity`（已在 **Unity 6.5 / 6000.5.9f1** 上验证，其它 Unity 6.x 也可，若提示升级请选择继续）。
2. 首次打开时 Unity 会自动生成 `Assets/**/*.meta` 与缺失的 `ProjectSettings` 文件，属正常现象，请一并提交。
3. 若 Unity 提示「没有可打开的场景」，直接新建一个空场景（或保持当前空场景）即可 —— 本工程通过 `RuntimeInitializeOnLoadMethod` 自动启动，**场景里不需要放任何东西**。
4. 点击 **Play**，游戏会自动创建相机、地图、轨迹、HUD，波次自动推进（建造与战斗并行，无需手动开波）。

> 若你用的是自带 `SampleScene` 的新建工程，同样直接 Play 即可：脚本会自动把已有相机配置为正交俯视相机。

## 二、操作说明

| 操作 | 说明 |
|------|------|
| 左键点击空地 | 放置当前选中的塔（不可放置时格子变红） |
| 左键点击已有塔 | 撤退该塔，返还 50% 金币 |
| 底栏四个卡片 | 切换要建造的塔（机枪 / 狙击 / 导弹 / 减速），金币不足自动标灰 |
| 顶栏「速度 1x / 2x」 | 切换一倍速 / 二倍速 |
| 顶栏「索敌：…」 | 切换全局智能索敌策略（★★★） |
| 顶栏「重新开始」 | 重置整局游戏 |

> 波次自动推进：开局有 2 秒缓冲、每波之间有约 1.2 秒缓冲，期间均可继续放塔/撤退。

## 三、题目要求对照

### 门槛要求（必达）

- **敌人与路径**：敌人沿各自的预设轨迹（每种敌人一条，固定起点/终点）向终点移动，具备血量、死亡销毁与到达终点逻辑 → `Actors/Enemy.cs`、`Systems/MapSystem.cs`。
- **索敌与攻击**：防御塔在检测范围内锁定敌人并发射子弹 → `Actors/Tower.cs`、`GameManager.SelectTarget`。
- **子弹命中**：子弹命中后扣血并销毁子弹 → `Actors/Projectile.cs`。

### 进阶挑战（按级别实现）

| 级别 | 挑战 | 实现位置 |
|------|------|----------|
| ★ | 追踪子弹（转向/追踪加速度，实时追踪目标） | `Projectile.Update` 中 `Vector2.MoveTowards` 转向 |
| ★★ | 目标死亡后子弹平滑消失或重新索敌 | `Projectile.TryRetarget` + `BeginFade`（追踪弹就近重锁定，直线弹淡出） |
| ★★★ | 命中产生 AOE 爆炸伤害 | `Projectile.Impact` → `GameManager.DamageEnemiesInRadius` |
| ★★★★ | 至少 2 种防御塔（减速塔 / AOE 塔 / 单体高伤塔） | 4 种塔：机枪（单体快）、狙击（单体高伤）、导弹（追踪+AOE）、减速（范围减速） |
| ★★★★★ | 智能索敌（血量最低 / 距终点最近 / 距离最近，三选一） | `TargetingPriority` 枚举 + HUD 切换，三种策略全部实现 |

## 四、工程结构

```
TowerDefenseUnity/
├── Assets/Scripts/
│   ├── Core/
│   │   ├── GameBootstrap.cs    # RuntimeInitializeOnLoadMethod 自启动入口
│   │   ├── GameManager.cs      # 总控：经济/生命/波次/敌塔注册表/空间哈希/索敌/撤退
│   │   └── GameConfig.cs       # 所有可调数值（轨迹、塔、敌人、波次、返还比例）
│   ├── Data/
│   │   └── Definitions.cs      # 枚举 + 可序列化数值定义
│   ├── Actors/
│   │   ├── Enemy.cs            # 敌人移动/攻击塔/血量/减速/死亡（对象池）
│   │   ├── Tower.cs            # 塔：索敌 + 攻击/减速 + 血量 + 被打败
│   │   └── Projectile.cs       # 子弹：直线/追踪/AOE/淡出/重锁定（对象池）
│   ├── Systems/
│   │   ├── MapSystem.cs        # 开放地图：多条敌人轨迹 + 网格 + 阻挡判定
│   │   ├── WaveSpawner.cs      # 波次刷怪（按敌人类型选轨迹）
│   │   ├── TowerPlacer.cs      # 鼠标放置/撤退交互 + 占用格子登记
│   │   └── SpatialGrid.cs      # 均匀网格空间哈希（ICPC 式索敌加速）
│   ├── UI/
│   │   ├── HudController.cs    # IMGUI HUD（金币/生命/选塔/速度/切换索敌）
│   │   ├── HudLayout.cs        # 固定布局 + UI 区域命中判断
│   │   └── HealthBarView.cs    # 世界空间血条
│   ├── Effects/
│   │   ├── BurstEffect.cs      # 扩散淡出特效
│   │   └── EffectFactory.cs    # 特效入口
│   └── Util/
│       ├── SpriteFactory.cs    # 运行时生成圆形/方形贴图
│       └── ObjectPool.cs       # 通用对象池
├── Packages/manifest.json
├── ProjectSettings/
└── README.md
```

## 五、设计说明（要点）

- **零资源自包含**：所有 Sprite 由 `SpriteFactory` 在运行时用 `Texture2D` 生成，工程无任何外部图片/模型依赖。
- **开放地图 + 多轨迹**：每种敌人一条固定轨迹（固定起点/终点），地图大部分开放可放塔，仅轨迹细线禁放。
- **建造/战斗并行**：波次由协程自动推进，放塔、撤退、开火与刷怪同时进行，无独立阶段。
- **塔的血量与撤退**：敌人会近战攻击范围内的塔；塔被击毁返还 20%、主动撤退返还 50% 金币。
- **不用物理引擎做命中检测**：敌人与子弹的碰撞按距离在 `Update` 里手动判断，省去 Layer/Tag/Rigidbody 的配置，保证开箱即跑。
- **对象池**：敌人、子弹均走 `ObjectPool`，避免频繁 `Instantiate`/`Destroy` 造成的 GC 抖动。
- **数据驱动**：塔/敌人/波次数值集中在 `GameConfig`，便于平衡性调整。
- **全局索敌策略**：三种策略（最近/最低血/最近终点）全部实现并可运行时切换，塔只需调用 `SelectTarget`。
- **空间哈希（ICPC 式优化）**：`SpatialGrid` 用均匀网格把敌人分桶，索敌/AOE/减速的范围查询从 O(N) 全量扫描降为 O(1) 摊销 + 候选集过滤，每帧由 `GameManager` 重建。

## 六、提交到 GitHub

按照题目「交付要求」：

```bash
# 在本目录（TowerDefenseUnity）内
git init
git add .
git commit -m "feat: 自动塔防与子弹追踪系统"

# 在 GitHub 网页新建一个空仓库（不要勾选 README）
git remote add origin <你的仓库地址>
git push -u origin main
```

然后把仓库链接提交到作品收集表。
