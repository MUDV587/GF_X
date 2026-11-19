# Getting Started — 框架运行最小配置

本 sample 包含本工程验证过的**设置三件套**（原封不动拷贝，仅资产 GUID 重生成，随导入落到 `Assets/Samples/com.final5788.ugfx/<version>/GettingStarted/`）：

| 资产 | 说明 |
|---|---|
| `GFRuntimeSettings.asset` | 业务根目录 `GameRoot=Assets/GF_X` 与全部框架派生路径（约定优于配置） |
| `AppSettings.asset` | 启动配置：资源模式（2=全热更）、热更版本检测 URL、屏幕设计分辨率、加密 dll 列表 |
| `AppConfigs.asset` | 框架/业务表名单 + 配置表 + 多语言表 + Procedure 类型名单（**本工程示例，新项目替换为自己的**） |

## 导入

Package Manager → 本包 → Samples → Getting Started → Import

## 导入后必做

1. `Assets/Resources/GFRuntimeSettings.asset`：按需改 `GameRoot`（派生路径自动跟随）；
2. `Assets/Resources/AppSettings.asset`：`ResourceMode`（Package=单机 / Updatable=热更）、`CheckVersionUrl` 改为你的资源服务器地址；
3. `Assets/GF_X/Settings/Core/AppConfigs.asset`：`mDataTables/mConfigs/mLanguages` 换成你的表；`mProcedures` 换成你的 Procedure 全类型名（**Inspector 内置 Validate Procedures 按钮可校验**）；
4. Launch 场景接线：参考 `Assets/GF_X/Scene/Launch.unity` 与 `Assets/GF_X/Prefabs/Core/GFExtension.prefab`：
   - `GameFramework` 预制体实例（根节点，挂框架全部组件）；
   - `BuiltinViews` 根（Loading 进度条 + Tips 对话框 + 背景/面板，`BuiltinViewComponent` 组件）；
   - `GF_X` 组件（`DefaultExecutionOrder(-10000)` Bootstrap）；
5. 首次真机：菜单 `HybridCLR → Installer` 装环境；打热更前先 `Build App` 全量构建一次。

## 注意

- 热更出包必须走 `HybridCLR → CompileDll And Copy` 或 AppBuilder 面板（不要手工 Assembly-CSharp）；
- 导入 sample 与工程内现有 `Assets/GF_X/Settings/Core/AppConfigs.asset` 重名无冲突（GUID 不同），按需删除其一。
