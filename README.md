# 东方阵符录（TouHouNights）

SAST 游戏开发与设计组免试题 · 程序方向 · **选项二 选项 B：自动塔防与子弹追踪系统**。

以东方 Project 为主题的即时部署塔防。敌人从三条路线进攻基地，玩家用灵力部署五种塔，配合撤退、符卡技能和弹幕防守。攻击自动进行，建造与战斗同时推进；标题页提供战役、单关练习、音乐盒和设置。

## 运行

### Windows 发布包

[下载 Windows x64 发布包](https://github.com/dreamnight16/TouHouNights/releases/download/v1.0.0-submission/TouHouNights-Windows-x64.zip)。

1. 下载 ZIP，并将**整个压缩包**解压到一个文件夹。
2. 运行 `TouHouNights.exe`。
3. 保留 `UnityPlayer.dll`、`TouHouNights_Data`、`MonoBleedingEdge` 等随包文件，与 EXE 放在同一目录；不要只复制 EXE。

### Unity 工程

1. 安装 **Unity 6000.5.9f1**，通过 Unity Hub 打开本项目根目录。
2. 等待资源导入，打开 `Assets/Scenes/Bootstrap.unity`。
3. 点击 Play，进入标题页后选择「开始游戏」或「练习模式」。

场景启动由 [GameBootstrap.cs](Assets/Scripts/Core/GameBootstrap.cs) 完成，相机、地图、单位和界面在运行时创建。Windows 构建使用 Bootstrap 场景。

### 从源码重新构建

安装同版本 Unity 的 Windows Build Support（Mono），在项目根目录执行 `powershell -ExecutionPolicy Bypass -File tools/Build-Windows.ps1`，输出为 `Builds/Windows/`。脚本在独立构建副本中使用编辑器自带的 UGUI/TMP 源码，绕过本机已复现的包管理器解析错误；不会将缓存或临时 Vendor 源码加入提交工程。正常包解析环境也可使用 Unity 菜单 `Tools > Tower Defense > Build Windows Submission`。

## 操作

| 操作 | 作用 |
| --- | --- |
| 点击部署卡 / 数字键 `1–5` | 依次选择速射、狙击、导弹、减速、治疗塔 |
| 左键点击可部署空格 | 放置当前塔类型；道路、基地和已占用格不可部署，需有足够灵力和部署位 |
| 左键点击已有塔 | 查看耐久、属性、符卡状态和撤退返还 |
| 鼠标悬停敌人 | 查看敌人耐久、速度、灵力奖励和距离等信息 |
| 塔详情中的「撤退」按钮 | 撤回选中塔，返还造价的 50% 灵力 |
| 塔详情中的「符卡发动」按钮（BOOM） | 攻击塔累计造成伤害后，释放一次范围爆炸；减速和治疗塔没有该技能 |
| `E` / 弹幕「发动」按钮 | 威力充满后释放全局弹幕；使用后清空威力 |
| 「目标」按钮 | 循环切换最近、残血、突进三种索敌策略 |
| `Space` / `Ⅱ` 按钮 | 暂停或继续 |
| `X` / 倍速按钮 | 切换 `0.5× / 1× / 2× / 4×`；`X` 切换时会继续游戏 |
| `R` | 重开当前战役或所选练习关 |
| `Esc` | 战斗中取消已选塔；暂停时继续；前端子页返回标题页 |

撤退与 BOOM 通过塔详情按钮操作，没有独立键盘快捷键。暂停菜单可切换音乐、重新开始或返回标题；设置页可调整音量、镜头效果及全屏状态。

## 五种塔

| 键位 | 塔名 | 职能 |
| --- | --- | --- |
| 1 | 博丽御札 | 单体速射 |
| 2 | 银刃飞刀 | 单体高伤 |
| 3 | 七曜追星 | 追踪弹与范围伤害 |
| 4 | 冰霜结界 | 持续范围减速，不发射子弹 |
| 5 | 祈愿之阵 | 持续修复范围内友方塔，不发射子弹 |

塔与敌人均有耐久，敌人可攻击塔；被击毁的塔返还 20% 造价。单位数值、刷怪时间和关卡倍率集中在 [GameConfig.cs](Assets/Scripts/Core/GameConfig.cs)。

## 战役、评分与练习

战役连续进行五关，每关刷怪完毕且场上敌人清空后计为通过，再发放补给并进入下一关。评级依据**本局实际通过关数**，失败不会抹掉已经通过的关卡。

| 已通过关数 | 评级 | 综合评分区间 |
| --- | --- | --- |
| 0 | C | 0–59 |
| 1 | B | 60–69 |
| 2 | A | 70–79 |
| 3 | S | 80–89 |
| 4 | V | 90–94 |
| 5 | V | 95–99 |

同档评分由击破比例、剩余生命和最高连击决定；击破、连击比例以本局实际刷出的敌人数为分母。仅在**战役五关胜利、全部敌人击破且无漏怪**时获得 **100 分、Φ**。累计通过第 1–5 关的设计目标为 `80% / 60% / 40% / 20% / 5%`，这些是难度目标，尚非真人统计结果。

练习模式只进行所选一关，实际清场数最多为 1；评分按该局表现计算，不授予战役 Φ，也不更新战役最高得分。结算中的「得分」是击破奖励累计值，与 0–100 的综合评分分别显示。

## 题目要求与进阶挑战

门槛功能包括敌人沿路线移动、受伤死亡、抵达基地扣除生命，防御塔范围索敌与发射子弹，以及子弹命中扣血后回收。

| 级别 | 实现 | 对应源码 |
| --- | --- | --- |
| ★ | 追踪弹持续转向，速度向目标方向收敛 | [Projectile.cs](Assets/Scripts/Actors/Projectile.cs)：`Update` |
| ★★ | 目标失效后，追踪弹尝试就近重锁；无法重锁的弹体逐渐缩小、淡出 | [Projectile.cs](Assets/Scripts/Actors/Projectile.cs)：`TryRetarget`、`BeginFade`、`UpdateFade` |
| ★★★ | 导弹命中后产生 AOE 范围伤害 | [Projectile.cs](Assets/Scripts/Actors/Projectile.cs)：`Impact`；[GameManager.cs](Assets/Scripts/Core/GameManager.cs)：`DamageEnemiesInRadius` |
| ★★★★ | 五种塔覆盖速射、高伤、AOE、减速与治疗 | [Tower.cs](Assets/Scripts/Actors/Tower.cs)、[GameConfig.cs](Assets/Scripts/Core/GameConfig.cs) |
| ★★★★★ | 三种索敌策略可切换：距离最近、血量比例最低、距基地最近 | [Definitions.cs](Assets/Scripts/Data/Definitions.cs)：`TargetingPriority`；[GameManager.cs](Assets/Scripts/Core/GameManager.cs)：`SelectTarget` |

## 技术实现

- **Unity / C#**：使用 SpriteRenderer 绘制战场，UGUI 创建界面；TextMeshPro 优先显示文字，字体不可用时回退到 legacy Text。
- **路径与推进**：[MapSystem.cs](Assets/Scripts/Systems/MapSystem.cs) 将道路格构成四邻接图，从基地 BFS 生成距离场；[WaveSpawner.cs](Assets/Scripts/Systems/WaveSpawner.cs) 通过协程安排刷怪与清场推进。
- **空间查询**：[SpatialGrid.cs](Assets/Scripts/Systems/SpatialGrid.cs) 按网格分桶，筛选范围内候选；[MinHeap.cs](Assets/Scripts/Util/MinHeap.cs) 为最低血量比例、最短剩余距离维护索敌堆，索引每帧重建。
- **对象池与命中**：[ObjectPool.cs](Assets/Scripts/Util/ObjectPool.cs) 复用敌人、子弹及部分特效；子弹命中和 AOE 使用代码距离判断。
- **界面与资源**：[BattleUiRoot.cs](Assets/Scripts/UI/BattleUiRoot.cs) 装配战斗 HUD，[FrontEndUi.cs](Assets/Scripts/UI/FrontEndUi.cs) 管理前端页面；[SpriteFactory.cs](Assets/Scripts/Util/SpriteFactory.cs) 生成基础图元，[BattleMusic.cs](Assets/Scripts/Effects/BattleMusic.cs) 加载本地音乐并处理切曲淡入淡出。

## 字体与音乐

中文字体为随工程提供的 **Noto Sans CJK SC**，位于 `Assets/Resources/Fonts/NotoSansCJKsc-Regular.otf`，采用 **SIL Open Font License 1.1**；来源与许可见 [字体说明](Assets/Resources/Fonts/README.txt) 和 [LICENSE.txt](Assets/Resources/Fonts/LICENSE.txt)。

五关背景音乐采用东方 Project 曲目，音频位于 `Assets/Resources/Audio/stage1.mp3` 至 `stage5.mp3`，运行时本地加载。曲目下载来源和 SHA-256 见 [music-sources.json](Assets/Resources/Audio/music-sources.json)。
