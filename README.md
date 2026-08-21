# 自动塔防与子弹追踪系统（Tower Defense & Homing Missile）

SAST 游戏开发与设计组免试题 · 程序方向 · **选项二 选项 B**

一个纯代码、零外部资源的 Unity 塔防 Demo。工程打开后按下 Play 即可运行，无需手动搭建场景、导入素材或配置任何 UGUI/物理层。

---

## 一、如何运行

1. 用 **Unity Hub** 打开本目录 `TowerDefenseUnity`（已在 **Unity 6.5 / 6000.5.9f1** 上验证，其它 Unity 6.x 也可，若提示升级请选择继续）。
2. 首次打开时 Unity 会自动生成 `Assets/**/*.meta` 与缺失的 `ProjectSettings` 文件，属正常现象，请一并提交。
3. 若 Unity 提示「没有可打开的场景」，直接新建一个空场景（或保持当前空场景）即可 —— 本工程通过 `RuntimeInitializeOnLoadMethod` 自动启动，**场景里不需要放任何东西**。
4. 点击 **Play**，游戏会自动创建相机、地图、路径、HUD 并进入布防阶段。

> 若你用的是自带 `SampleScene` 的新建工程，同样直接 Play 即可：脚本会自动把已有相机配置为正交俯视相机。

## 二、操作说明

| 操作 | 说明 |
|------|------|
| 左键点击空地 | 放置当前选中的塔（需足够金币，且不在路径/已有塔上） |
| 右侧四个按钮 | 切换要建造的塔（机枪塔 / 狙击塔 / 导弹塔 / 减速塔） |
| 「开始下一波」 | 开始刷怪 |
| 「索敌：…（点击切换）」 | 切换全局智能索敌策略（★★★） |
| 「重新开始」 | 重置整局游戏 |

## 三、题目要求对照

### 门槛要求（必达）

- **敌人与路径**：敌人沿预设 Waypoints 路径向终点移动，具备血量、死亡销毁与到达终点逻辑 → `Actors/Enemy.cs`、`Systems/MapSystem.cs`。
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
│   │   ├── GameManager.cs      # 总控：经济/生命/波次/敌人注册表/索敌策略
│   │   └── GameConfig.cs       # 所有可调数值（路径、塔、敌人、波次）
│   ├── Data/
│   │   └── Definitions.cs      # 枚举 + 可序列化数值定义
│   ├── Actors/
│   │   ├── Enemy.cs            # 敌人移动/血量/减速/死亡（对象池）
│   │   ├── Tower.cs            # 塔：索敌 + 攻击/减速
│   │   └── Projectile.cs       # 子弹：直线/追踪/AOE/淡出/重锁定（对象池）
│   ├── Systems/
│   │   ├── MapSystem.cs        # 路径绘制 + 网格 + 阻挡判定
│   │   ├── WaveSpawner.cs      # 波次刷怪
│   │   └── TowerPlacer.cs      # 鼠标放置交互 + 占用格子登记
│   ├── UI/
│   │   ├── HudController.cs    # IMGUI HUD（金币/生命/选塔/开波/切换索敌）
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
- **不用物理引擎做命中检测**：敌人与子弹的碰撞按距离在 `Update` 里手动判断，省去 Layer/Tag/Rigidbody 的配置，保证开箱即跑。
- **对象池**：敌人、子弹均走 `ObjectPool`，避免频繁 `Instantiate`/`Destroy` 造成的 GC 抖动。
- **数据驱动**：塔/敌人/波次数值集中在 `GameConfig`，便于平衡性调整。
- **全局索敌策略**：三种策略（最近/最低血/最近终点）全部实现并可运行时切换，塔只需调用 `SelectTarget`。

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
