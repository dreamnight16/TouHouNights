<!-- Generated: 2026-09-09 | Static review; Unity Editor compilation/play-mode was not available in this environment -->

# 代码问题与风险清单

以下是静态审计结论，按优先级排序；并非已在运行时复现的缺陷。

## P1 — 交付构建没有启动场景

`ProjectSettings/EditorBuildSettings.asset` 的 `m_Scenes` 为空。自动启动代码只能在“已经加载的场景”之后运行，因此编辑器 Play 可以工作，但 Player Build 没有受支持的首个场景。应添加一个最小 Bootstrap 场景并在 CI/发布前验证构建。

## P1 — 后处理路径对渲染管线敏感

`ScreenBlurFx` 依赖相机 `OnRenderImage` 和 `Shader.Find`。该回调在部分 SRP 配置中不执行或行为不同；缺失 shader 时模糊/Bloom 会悄然退化。为每个 shader 加显式诊断，或在确定目标渲染管线后迁移到对应 Renderer Feature/Render Pass。

## P2 — `GameManager` 是过宽的全局依赖中心

682 行的 `GameManager` 同时承担组装、状态、经济、规则、索引、相机、流程、结算和输入命令；Actor 和多个 HUD 组件直接访问 `GameManager.Instance`。这造成隐藏依赖，抬高了单元测试、重放和新增模式的成本。优先提取只读查询与命令接口，再拆分会话、战斗查询和流程控制。

## P2 — 每帧完整重建索引，优化收益受限

`GameManager.Update()` 对全部活跃敌人完整重建空间哈希和两个堆。对于当前小规模 Demo 很直接，但敌人数/塔数增长后会形成固定 O(N) 每帧成本；而 `SelectByHeap` 还会为每次越界候选 Pop/Push。应先 profile，再改为敌人移动时增量更新，或在目标策略启用时才维护对应索引。

## P2 — 运行时资源的释放与缓存策略不完整

`SpriteFactory`、`Sfx`、材质和 `ScreenBlurFx` 均创建/缓存 Unity 对象；其中静态 Sprite/Audio 缓存没有显式应用退出清理，`ScreenBlurFx` 也未销毁其材质。重载场景、禁用 Domain Reload 或长时间编辑器会话时，建议明确所有权和释放策略。

## P2 — UI 字体可移植性仍依赖设备

`UiFont` 依赖操作系统中存在中文字体；探测与 legacy 回退减少了方框风险，但并不保证所有目标平台的中文排版一致。面向发布时应把一个有授权的 CJK TMP 字体资产随项目发布，而不是仅依赖 OS 字体。

## P3 — UI 以轮询更新为主

顶部栏、部署栏、单位卡、敌人检视、反馈和范围提示在各自 `Update` 中轮询状态并做布局/颜色更新。规模不大时可接受，但它分散了刷新触发点。可先引入轻量 `GameSessionChanged`/事件，再把高频动画保留在各组件内。

## P3 — 没有自动验证护栏

未发现 NUnit/PlayMode tests、CI 或构建脚本。至少应补充纯逻辑的 `MinHeap`、`SpatialGrid`、评分、经济、波次计划与地图距离场测试；随后加入一次无界面 batchmode 编译检查。

## P3 — 当前工作树已脏，审计/提交需隔离

扫描开始时已有大量 `Assets/Scripts` 修改、UI 文件删除/新增、Resources 与 TMP 资源变更及 README 改动。文档生成没有触碰它们；后续提交或重构必须显式暂存目标文件，避免混入现有未审计变更。
