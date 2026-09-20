<!-- Generated: 2026-09-09 | Source: Packages/manifest.json + ProjectSettings | Token estimate: ~520 -->

# 依赖与运行环境

## Unity

- 编辑器：Unity `6000.5.9f1`（README 标注 Unity 6.5 验证）。
- 脚本语言：C#；没有 `.asmdef`，使用默认 Unity 脚本程序集。
- 项目目标分辨率：1920×1080；非编辑器运行时由 `GameManager` 请求 1080p 全屏。

## Package Manifest

仅声明 Unity 内置模块：Animation、Audio、Director、Image Conversion、IMGUI、JSON Serialize、Particle System、Physics/Physics2D、Screen Capture、UI/UIElements、UGUI、UnityWebRequest（及 AssetBundle/Audio/Texture/WWW）、Video。未声明第三方包或网络服务。

## 运行时资源与平台 API

| 依赖 | 使用位置 | 风险/说明 |
|---|---|---|
| TextMesh Pro | `UiFont`、`UiText` | TMP 在 Unity 环境内可用；中文依赖本机 CJK 字体 |
| UGUI / EventSystem | `UiFactory`、全部 HUD | 运行时创建 Canvas/EventSystem |
| `Assets/Resources/*.shader` | `ScreenBlurFx`、`UiFactory` | 通过 `Shader.Find` 名称加载，改名会静默降级或失效 |
| OS Fonts | `UiFont` | 依赖候选字体和 `HasCharacter('中')`；无中文字体会回退 |
| PlayerPrefs | `GameManager` | 仅保存最高分，未版本化/未封装 |
| Unity AudioClip | `Sfx` | 运行时合成、静态缓存 |

## 非依赖/未发现项

- 未发现网络通信、后端、数据库、广告、分析 SDK、第三方 DLL 或原生插件。
- 未发现测试框架、CI 配置、构建脚本或场景资产。
- `EditorBuildSettings.asset` 为空，当前不具备明确的 Player 启动场景配置。

## 变更注意

添加 SRP（URP/HDRP）或修改渲染路径前，应先验证 `OnRenderImage`、`Shader.Find` 与自定义 shader 的兼容性；这些是当前表现层最敏感的外部边界。
