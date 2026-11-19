#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    /// <summary>
    /// Project Settings / GameFrameworkX 页面。
    /// Unity 的 SettingsProvider 只支持一个顶层条目，因此 Runtime / Editor 与校验在页面内部分类展示。
    /// Editor 分类展示所有 Editor-only 的 SO 设置项（按工具域分组）。
    /// </summary>
    internal sealed class GFProjectSettingsProvider : SettingsProvider
    {
        private const string ProviderPath = "Project/GameFrameworkX";
        private const string RuntimeAssetPath = GFRuntimeSettings.RuntimeAssetPath;
        private const string RuntimeTab = "Runtime";
        private const string EditorTab = "Editor";
        private const string ValidationTab = "校验";

        private int _tab;
        private Vector2 _scroll;
        private SerializedObject _runtimeSettings;
        private SerializedObject _appBuilder;
        private SerializedObject _gameData;
        private SerializedObject _compression;
        private static IReadOnlyList<RuntimeLayoutIssue> s_layoutIssues;
        private static bool s_layoutIssuesDirty = true;

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new GFProjectSettingsProvider();
        }

        private GFProjectSettingsProvider() : base(ProviderPath, SettingsScope.Project)
        {
            label = "GameFrameworkX";
            keywords = new[]
            {
                "GF", "GF_X", "GameFrameworkX", "Path", "Runtime", "Editor", "Settings", "Config", "Build",
                "路径", "设置", "构建", "本地化",
            };
        }

        public override void OnGUI(string searchContext)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                int selected = GUILayout.Toolbar(_tab, new[] { RuntimeTab, EditorTab, ValidationTab }, EditorStyles.toolbarButton, GUILayout.Width(280f));
                if (selected != _tab)
                {
                    _tab = selected;
                    GUI.FocusControl(null);
                }

                GUILayout.FlexibleSpace();
                GUILayout.Label(_tab == 0 ? RuntimeAssetPath : "ProjectSettings/GameFrameworkX/*", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(4f);

            using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll, false, false, GUILayout.ExpandHeight(true)))
            {
                _scroll = scroll.scrollPosition;
                if (_tab == 0)
                {
                    DrawRuntime();
                }
                else if (_tab == 1)
                {
                    DrawEditor();
                }
                else
                {
                    DrawValidation();
                }
            }
        }

        private void DrawRuntime()
        {
            GFRuntimeSettings asset = AssetDatabase.LoadAssetAtPath<GFRuntimeSettings>(RuntimeAssetPath);
            if (asset == null)
            {
                EditorGUILayout.HelpBox($"运行时配置资产不存在：{RuntimeAssetPath}", MessageType.Warning);
                if (GUILayout.Button("创建 GFRuntimeSettings.asset", GUILayout.Width(260f)))
                {
                    CreateRuntimeAsset();
                }
                return;
            }

            if (_runtimeSettings == null || _runtimeSettings.targetObject != asset)
            {
                _runtimeSettings = new SerializedObject(asset);
            }

            _runtimeSettings.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.HelpBox(
                "约定优于配置：只有 GameRoot 可配置，其余目录布局是框架约定（不可改）。改动 GameRoot 后请自行迁移对应资产，并同步 ResourceRuleEditor / ResourceEditor.xml（校验页会提示）。",
                MessageType.None);

            DrawStringProperty(_runtimeSettings, "GameRoot", "业务根 GameRoot", true);

            if (EditorGUI.EndChangeCheck())
            {
                _runtimeSettings.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                s_layoutIssuesDirty = true;
            }

            EditorGUILayout.Space(4f);
            DrawSection("约定目录（不可配置）");
            DrawReadOnly("Generated", Combine(asset.GameRoot, GFRuntimeSettings.GeneratedDir));
            DrawReadOnly("Generated/Data", asset.GeneratedDataRoot);
            DrawReadOnly("DataTable / Config / Language", $"{asset.DataTableRoot} | {asset.ConfigRoot} | {asset.LanguageRoot}");
            DrawReadOnly("Settings", asset.SettingsRoot);
            DrawReadOnly("HotfixDlls", asset.HotfixDllDir);
            DrawReadOnly("Prefabs(Core/Entity/UI)", $"{asset.PrefabsRoot} | {asset.CoreRoot} | {asset.EntityRoot} | {asset.UIFormRoot}");
            DrawReadOnly("Scene / Audio / Textures / Sprites / Materials", $"{asset.SceneRoot} | {asset.AudioRoot} | {asset.TextureRoot} | {asset.SpritesRoot} | {asset.MaterialRoot}");
            DrawReadOnly("Resources/AOTDLLs（Resources 相对）", asset.AotDllResourcesDir);
            DrawReadOnly("Obfuz 密钥（Resources 相对）", GFRuntimeSettings.ObfuzSecretKeyResourcesPath);
            DrawReadOnly("version.json / HotfixFileList.txt", $"{GFRuntimeSettings.VersionFile} / {GFRuntimeSettings.HotfixFileList}");
        }

        private void DrawEditor()
        {
            AppBuilderEditorSettings appBuilder = AppBuilderEditorSettings.Instance;
            if (appBuilder == null)
            {
                EditorGUILayout.HelpBox("AppBuilderEditorSettings 加载失败。", MessageType.Error);
                return;
            }

            if (_appBuilder == null || _appBuilder.targetObject != appBuilder)
            {
                _appBuilder = new SerializedObject(appBuilder);
            }

            GameDataSettings gameData = GameDataSettings.Instance;
            if (gameData == null)
            {
                EditorGUILayout.HelpBox("GameDataSettings 加载失败。", MessageType.Error);
                return;
            }
            if (_gameData == null || _gameData.targetObject != gameData)
            {
                _gameData = new SerializedObject(gameData);
            }

            CompressionSettings compression = CompressionSettings.Instance;
            if (compression == null)
            {
                EditorGUILayout.HelpBox("CompressionSettings 加载失败。", MessageType.Error);
                return;
            }
            if (_compression == null || _compression.targetObject != compression)
            {
                _compression = new SerializedObject(compression);
            }

            _appBuilder.Update();
            _gameData.Update();
            _compression.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.HelpBox("仅环境/本地化/数据管线等“不在框架约定内”的字段可配置；其余只读派生展示。", MessageType.None);

            DrawSection("构建与签名（AppBuilderEditorSettings）");
            DrawStringProperty(_appBuilder, "KeystoreFile", "签名文件 Keystore", false, "keystore,jks,ks");
            DrawStringProperty(_appBuilder, "AssetBundles", "资源构建目录 AssetBundles", true);
            DrawStringProperty(_appBuilder, "Builds", "应用构建目录 Builds", true);
            DrawStringProperty(_appBuilder, "UpdatePrefixUri", "热更前缀 UpdatePrefixUri");

            DrawSection("压缩工具（CompressionSettings）");
            DrawStringProperty(_compression, "CompressImgToolOutputDir", "压缩输出目录", true);
            DrawStringProperty(_compression, "CompressImgToolBackupDir", "压缩备份目录", true);
            EditorGUILayout.IntField("压缩快等级", compression.CompressImgToolFastLv);

            DrawSection("派生路径预览（GameDataSettings）");
            DrawReadOnly("DataTable Excel Root", gameData.DataTableExcelPath);
            DrawReadOnly("Config Excel Root", gameData.ConfigExcelPath);
            DrawReadOnly("Language Excel Root", gameData.LanguageExcelPath);
            DrawReadOnly("Generated Code Root", gameData.GeneratedCodeRoot);
            DrawReadOnly("UIScripts / Item", $"{gameData.UIScriptsPath} | {gameData.UIItemScriptsPath}");
            DrawReadOnly("DataTable Code Template", gameData.DataTableCodeTemplatePath);
            DrawReadOnly("AppConfigs（运行时 SO）", appBuilder.GetResourceBuildAbsolutePath());
            DrawReadOnly("Package Runtime / Editor", $"{gameData.PackageRuntimeRoot} | {gameData.PackageEditorRoot}");
            DrawReadOnly("Templates: Form/Dialog/Item", "见 AppBuilderEditorSettings:Tools/UITemplates");
            DrawReadOnly("UGF Configs Root", "Assets/GameFramework/Configs（约定常量）");
            DrawReadOnly("UGF xml 配置",
                $"{appBuilder.ResourceCollectionConfigPath} | {appBuilder.ResourceEditorConfigPath} | {appBuilder.ResourceBuilderConfigPath}");

            if (EditorGUI.EndChangeCheck())
            {
                _appBuilder.ApplyModifiedProperties();
                _gameData.ApplyModifiedProperties();
                _compression.ApplyModifiedProperties();
                AppBuilderEditorSettings.Save();
                GameDataSettings.Save();
                CompressionSettings.Save();
                s_layoutIssuesDirty = true;
            }
        }

        private void DrawValidation()
        {
            EditorGUILayout.LabelField("校验", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "校验 GameRoot 与 UGF 配置（ResourceEditor.xml / ResourceBuilder.xml / ResourceCollection.xml）、资源规则资产一致性。构建产物目录允许暂时不存在。",
                MessageType.None);

            if (GUILayout.Button("重新检查布局", GUILayout.Width(140f)))
            {
                s_layoutIssuesDirty = true;
            }

            if (s_layoutIssuesDirty || s_layoutIssues == null)
            {
                s_layoutIssues = RuntimeLayoutValidator.Validate();
                s_layoutIssuesDirty = false;
            }

            if (s_layoutIssues.Count == 0)
            {
                EditorGUILayout.HelpBox("运行时目录、UGF 配置和资源规则一致性检查通过。", MessageType.Info);
                return;
            }

            var builder = new System.Text.StringBuilder();
            bool hasError = false;
            for (int i = 0; i < s_layoutIssues.Count; i++)
            {
                RuntimeLayoutIssue issue = s_layoutIssues[i];
                hasError |= issue.Severity == RuntimeLayoutIssueSeverity.Error;
                builder.Append("- [").Append(issue.Severity).Append("] ")
                    .Append(issue.Title).Append(": ").AppendLine(issue.Message);
            }

            EditorGUILayout.HelpBox(builder.ToString(), hasError ? MessageType.Error : MessageType.Warning);
        }

        private static void DrawSection(string title)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        private static void DrawStringProperty(SerializedObject settings, string propertyPath, string label, bool folder = false)
        {
            DrawStringProperty(settings, propertyPath, label, folder, null);
        }

        private static void DrawStringProperty(SerializedObject settings, string propertyPath, string label, bool folder, string extension)
        {
            SerializedProperty property = settings.FindProperty(propertyPath);
            if (property == null)
            {
                EditorGUILayout.HelpBox($"缺少配置字段：{propertyPath}", MessageType.Warning);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label), GUILayout.ExpandWidth(true));

                if (folder || !string.IsNullOrEmpty(extension))
                {
                    if (GUILayout.Button("...", GUILayout.Width(28f)))
                    {
                        string current = property.stringValue;
                        string selected = folder
                            ? EditorDialogUtility.OpenRelativeFolderPanel(label, current)
                            : EditorDialogUtility.OpenRelativeFilePanel(label, current, extension);

                        if (!string.IsNullOrWhiteSpace(selected))
                        {
                            property.stringValue = selected.Replace('\\', '/');
                            GUI.changed = true;
                        }
                    }
                }
            }
        }

        private static void DrawReadOnly(string label, string value)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(label, value ?? string.Empty);
            }
        }

        private static string Combine(string left, string right)
        {
            string normalizedLeft = string.IsNullOrWhiteSpace(left) ? string.Empty : left.Trim().TrimEnd('/');
            return normalizedLeft.Length == 0 ? right : normalizedLeft + "/" + right;
        }

        private static void CreateRuntimeAsset()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            GFRuntimeSettings asset = ScriptableObject.CreateInstance<GFRuntimeSettings>();
            AssetDatabase.CreateAsset(asset, RuntimeAssetPath);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
        }
    }
}
#endif
