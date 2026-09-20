# UI 重构验收记录 · 2026-09-09

## 实现
- BattleUiRoot.cs：绑定游戏状态、刷新卡片、暂停/重开状态与事件清理。
- BattleUiRoot.Views.cs：战况、部署卡、单位档案、暂停和结算视图装配。
- BattleUiRoot.Primitives.cs：控件颜色、导航行为与布局基础。
- UiFont.cs：优先使用随项目交付的 Noto Sans CJK SC；附 SIL OFL 1.1 许可证。
- 部署卡使用兵种图形、独立费用区与状态文字；弹幕指令移至左下。

## 已验证
Unity 6000.5.9f1 编译并进入 Play Mode。ui-play-verification.log 包含两条 PASS：
- 按钮白色基础色与状态色分离，Canvas Expand 保留参考尺寸。
- 自动启动、TMP 字体加载、点击狙击塔部署卡、暂停面板显示、暂停禁止部署、重开后暂停及部署数量重置。

截图：battle-ui.png、battle-ui-pause.png。由实际 Play Mode 世界画面与 UI 分层渲染，UI 不额外经过世界相机后处理。

## 复现
编辑器菜单 Tools > Tower Defense > Verify UI 可运行基础控件检查。
批处理使用 -executeMethod BattleUiPlayVerification.Run；此入口完成后会退出编辑器，应使用独立验证进程。

## 验证边界
截图分辨率为 1280×720。未完成触屏、全部窗口比例、所有敌人/单位面板组合和完整胜负流程验收。没有据此宣称所有游戏 BUG 已修复。
