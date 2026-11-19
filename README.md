# UGFX

**UGFX** 是基于 [GF_X](https://github.com/sunsvip/GF_X)（GameFramework + HybridCLR 热更新 + Obfuz 加固）打造的 Unity 工业化游戏框架包，在原项目验证过的工作流之上做了 UPM 化与工程化重构。

包名：`com.final5788.ugfx` ｜ 许可证：[MIT](LICENSE) ｜ 上游：[sunsvip/GF_X](https://github.com/sunsvip/GF_X)（本仓库 `master` 分支逐字节镜像上游，**禁止直接提交**；所有优化在 `feature-optimize` 分支）

## 本包包含

| 目录 | 程序集 | 内容 |
|---|---|---|
| `Runtime/` | `GF_X.Runtime` | AOT 启动层：Builtin Procedures（Launch → UpdateResources → LoadHotfixDll）、HotfixRuntimeInitializer、内置 Loading/Dialog、GFRuntimeSettings/AppSettings/AppConfigs、数据表解析（含 AB 测试组）、Helpers（ZStringText/NewtonsoftJson/CustomSoundAgent/…）、GFUtility.* |
| `Editor/` | `GF_X.Editor` + `GF.*.Editor`（9 个工具程序集） | Hub 工具箱、数据表自动导表/代码生成、GameData 导出、多语言（扫描/百度翻译/AI 翻译）、一键打包（Jenkins + HybridCLR + Obfuz）、link.xml 剥离配置、贴图/图集/动画压缩 |
| `Samples~/GettingStarted/` | — | 设置三件套模板（GFRuntimeSettings/AppSettings/AppConfigs） |

## 快速开始

1. 安装 HybridCLR 环境：菜单 `HybridCLR → Installer`；
2. Package Manager 导入本包 `Getting Started` sample（或手工拷贝 `Samples~/GettingStarted/` 里的三个 .asset）；
3. `Assets/Resources/GFRuntimeSettings.asset`：`GameRoot` 指向你的业务根（如 `Assets/YourGame`，目录布局是框架约定，见包内注释）；
4. 场景内放置 `GameFramework` 预制体实例 + BuiltinViews（Loading 进度条 + Tips 对话框），并挂 `GF_X` 组件；
5. 菜单 `Game Framework/GF Tools/工具箱` 打开工具箱；首次打热更前先 `Build App` 一键出包。

## 依赖

`com.jiangyin.gameframework`、`com.jiangyin.unitygameframework`（均为上游 `EllanJiang/GameFramework` / `UnityGameFramework` 的 MUDV587 fork UPM 分支，见 [UGFX-PATCHES.md](UGFX-PATCHES.md)）+ UniTask / ZString / HybridCLR / Obfuz。

## 上游合并

`master` 与上游保持一致；上游更新时同步 `master` 后 `master → feature-optimize` 合并。重灾区与注意事项见 `UGFX-PATCHES.md` 的「合并上游注意」一节。
