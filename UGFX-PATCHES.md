# UGFX 补丁说明（MUDV587/GF_X fork）

> 本分支 `feature-optimize` = 上游 `master`（sunsvip/GF_X 逐字节镜像）+ UPM 化 + 工程化优化。
> `master` 分支与上游保持一致，**禁止直接提交**。

## 上游

- 仓库：https://github.com/sunsvip/GF_X
- 基线：`master`（2026-08-06 前最后一次推送）
- 许可证：MIT

## 分支治理

- `master`：上游快照，逐字节镜像，只读同步；
- `feature-optimize`：本仓库唯一开发分支。本包即 `feature-optimize` 分支内容的快照（仓库根 = UPM 包根）。

## 与上游的差异（feature-optimize vs master）

### 包结构

- 上游为完整 Unity 工程（`Assets/`、`ProjectSettings/`、`Tools/Jenkins` 含 jenkins.war 300MB+）；本分支仓库根直接是 UPM 包：`Runtime/` + `Editor/` + `package.json`（`.meta` 全部保留，GUID 与嵌入包阶段一致）。
- 删除 Jenkins 本地产物（`jenkins.war` 等）；构建管线源码保留在 `Editor/Build/`。

### 程序集/命名

- 程序集名保持 `GF_X.Runtime` / `GF_X.Editor` / `GF.*.Editor` 不变（UGF `Type.cs` 程序集扫描名单、`Obfuz.asset`、`HybridCLRSettings.patchAOTAssemblies`、`Assets/Tests/EditMode` 不变式测试全部依赖这些名字）。
- 包名 `com.final5788.gf_x` → `com.final5788.ugfx`；`displayName` GF_X → UGFX；硬编码的 `Packages/com.final5788.gf_x/…` 路径已全部改为 `Packages/com.final5788.ugfx/…`（`AppBuilderEditorSettings.PackageRuntimeRoot`、`GameDataShared`、`GameDataSettings`、`LocalizationSettings`、`ArtFontSettings`、`AppConfigsEditorNavigationService`、`AppBuilderEditorSettings` 注释、`AppBuilderEditorSettings.asset`）。

### 功能修复/重构（含 T10 与 Phase 1）

- **命名统一**（T9/T10）：`GF_X_RuntimeSettings` → `GFRuntimeSettings`；`GF_X_EditorSettings` / `AppBuilderEditorSettings` → **合并为 `AppBuilderEditorSettings`**（嵌套序列化 SO、`[FilePath("ProjectSettings/GameFrameworkX/AppBuilderEditorSettings.asset")]`）；`GFBuiltin` → `GF_X`；`UtilityBuiltin` → `GFUtility`；`GFHelper` → `Helpers/`；`ScriptableObject/` → `ScriptableObjects/`。
- **反射消除**：`GF_X.cs` 不再反射 `ResourceComponent.m_ResourceMode`，改在 `Start()` 中调用公开 `SetResourceMode`；AppSettings Inspector 不再反射 `UIComponent.m_InstanceRoot`，改用 fork 新增的 `UIComponent.InstanceRoot`（见 UGF fork `GF_X-PATCHES.md` 补丁 #11）；`EditorScriptableSingleton` 改为包装 Unity 公开的 `ScriptableSingleton<T>`（替代 `InternalEditorUtility.LoadSerializedFileAndForget` 私有 API），5 个设置类 `[FilePath]` 标注补 `FilePathAttribute.Location.ProjectFolder`；`ScriptableSingleton<T>.Save` 为受保护成员，经 Curiously-Recurring 约束 `where T : EditorScriptableSingleton<T>` 访问（静态便捷入口 `Save()` 保持既有调用）。
- **构建管线**：AppBuilder 面板与执行服务不再反射 UGF 私有 `Type.GetConfigurationPath<T>` / `ResourceEditor.Open()`，改用 fork 公开 API（UGF fork 补丁 #12 / #13）；`BuildPlayerLaunchService` 加 `#if UNITY_2022_3_OR_NEWER` 守护 + 反射失败逐项诊断日志 + 异常捕获（E3-B；E3-A 后续用 `BuildPipeline.BuildPlayer` 重写）。
- **AppConfigs Inspector**：新增 `Validate Procedures` 按钮（`GameFramework.Utility.Assembly.GetType` 解析 + `ProcedureBase` 子类校验，一键报告缺失/非流程类型）。
- **子工具注册表**：`AssetSelectionSubToolRegistry` 的 `Assembly.GetTypes()` 包裹 try/catch + 过滤 null（单个损坏程序集不炸编辑器 reload）；`Utility.Assembly.Assembly.GetTypes()` 在核包 fork 加容错（核包补丁 #10）。
- **TextureCompressionEditorBridge**：文件头标注 Unity 内部 API 清单 + 三个反射调用逐一 try/catch（失败记 Error 并走 fallback/跳过）——`TextureImportValidFormats`/`TextureImporter.GetImportWarnings`/`TextureUtil.GetStorageMemorySizeLong` 均无公开替代，Unity 升级时按此清单重测。
- **RootCanvas 空值保护**：未找到根 Canvas 时记错并跳过 UICamera/CanvasScaler 初始化（原实现直接 NRE）。

### 与上游合并注意

1. 先同步 `master`（ff-only），再 `master → feature-optimize` 合并；
2. 冲突重灾区：本包文件整体由上游根目录改造而来（目录映射 `Assets/ScriptsBuiltin/* → Runtime/*`、`ScriptsEditor/* → Editor/*`），git 无法自动识别，需要按迁移规则手工对照合并；
3. `.meta` GUID 不可变（Launch.unity、GFExtension.prefab、场景引用均锚定这些 GUID）；
4. 上游若新增文件，需补 `.meta`（新 GUID）；
5. 上游 Unity 工程专有文件（`ProjectSettings/`、`Packages/manifest.json` 等）本分支不保留，合并后按「包 = 仓库根」重新归位。

## 来源归属

派生自 sunsvip/GF_X（MIT），补丁由 MUDV587 维护。与上游 EllanJiang 引擎家族（GameFramework / UnityGameFramework）的 fork 治理一致。
