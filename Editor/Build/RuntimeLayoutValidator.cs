using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using UGF.EditorTools.Build.ResourceRules;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    public enum RuntimeLayoutIssueSeverity
    {
        Error,
        Warning,
    }

    public sealed class RuntimeLayoutIssue
    {
        public RuntimeLayoutIssue(RuntimeLayoutIssueSeverity severity, string title, string message)
        {
            Severity = severity;
            Title = title;
            Message = message;
        }

        public RuntimeLayoutIssueSeverity Severity { get; }
        public string Title { get; }
        public string Message { get; }
    }

    /// <summary>
    /// 运行时布局一致性检查。只报告问题，不移动资产、不修改 UGF 配置或资源规则。
    /// 目录迁移由用户自行完成；这里负责在编辑器和构建前给出可操作的提示。
    /// </summary>
    public static class RuntimeLayoutValidator
    {
        public static IReadOnlyList<RuntimeLayoutIssue> Validate()
        {
            var issues = new List<RuntimeLayoutIssue>();
            GFRuntimeSettings runtime = GFRuntimeSettings.Instance;
            if (runtime == null)
            {
                AddError(issues, "运行时配置缺失", $"{GFRuntimeSettings.RuntimeAssetPath} 不存在，无法校验目录布局。");
                return issues;
            }

            ValidateGameRoot(runtime.GameRoot, issues);
            ValidateUgfConfiguration(runtime, issues);
            return issues;
        }

        public static bool LogWarnings(string context)
        {
            IReadOnlyList<RuntimeLayoutIssue> issues = Validate();
            if (issues.Count == 0)
            {
                return true;
            }

            var builder = new StringBuilder();
            builder.AppendLine($"[RuntimeLayout] {context}：发现 {issues.Count} 项布局提示。");
            builder.AppendLine("仅提示，不会自动迁移资产或修改 UGF 配置。");
            foreach (RuntimeLayoutIssue issue in issues)
            {
                builder.AppendLine($"- [{issue.Severity}] {issue.Title}: {issue.Message}");
            }

            Debug.LogWarning(builder.ToString());
            return !issues.Any(issue => issue.Severity == RuntimeLayoutIssueSeverity.Error);
        }

        private static void ValidateGameRoot(string gameRoot, List<RuntimeLayoutIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(gameRoot))
            {
                AddError(issues, "GameRoot 为空", "GameRoot 必须是非空的工程相对路径，例如 Assets/GF_X。");
                return;
            }

            string normalized = Normalize(gameRoot);
            if (Path.IsPathRooted(normalized)
                || !string.IsNullOrEmpty(Path.GetPathRoot(normalized))
                || normalized.Contains("://"))
            {
                AddError(issues, "GameRoot 非工程相对路径", $"GameRoot = {gameRoot}；必须使用 Assets/ 下的工程相对路径。");
                return;
            }

            if (normalized.Split('/').Any(segment => segment == "." || segment == ".." || segment.Length == 0))
            {
                AddError(issues, "GameRoot 含非法片段", $"GameRoot = {gameRoot}；不能包含空目录、. 或 ..。");
                return;
            }

            if (!string.Equals(normalized, "Assets", StringComparison.OrdinalIgnoreCase)
                && !normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                AddError(issues, "GameRoot 必须位于 Assets 下", $"GameRoot = {gameRoot}；AssetDatabase 无法稳定加载该目录之外的资源。");
            }

            CheckDirectory(issues, "GameRoot", normalized);
            CheckDirectory(issues, "GeneratedRoot", Combine(normalized, GFRuntimeSettings.GeneratedDir));
            CheckDirectory(issues, "SettingsRoot", Combine(normalized, GFRuntimeSettings.SettingsDir));
            CheckDirectory(issues, "HotfixDllDir", Combine(normalized, GFRuntimeSettings.HotfixDllsDir));
            CheckDirectory(issues, "PrefabsRoot", Combine(normalized, GFRuntimeSettings.PrefabsDir));
            CheckDirectory(issues, "SceneRoot", Combine(normalized, GFRuntimeSettings.SceneDir));
            CheckDirectory(issues, "AudioRoot", Combine(normalized, GFRuntimeSettings.AudioDir));
        }

        private static void ValidateUgfConfiguration(GFRuntimeSettings runtime, List<RuntimeLayoutIssue> issues)
        {
            AppBuilderEditorSettings editor = AppBuilderEditorSettings.Instance;

            ValidateResourceRuleAsset(runtime, editor, issues);
            ValidateResourceEditorXml(runtime, issues);
            ValidateResourceBuilderXml(editor, issues);
            ValidateResourceCollection(runtime, issues);

            if (!Directory.Exists(ToAbsolutePath(runtime.AotDllResourcesDir)))
            {
                AddWarning(issues, "AOT 资源目录缺失", $"{runtime.AotDllResourcesDir} 尚未生成；执行 HybridCLR Full Build 后会创建。");
            }

            string appConfigsPath = runtime.GetScriptableAsset("Core/AppConfigs");
            if (!File.Exists(ToAbsolutePath(appConfigsPath)))
            {
                AddWarning(issues, "AppConfigs 资产缺失", $"期望 {appConfigsPath}；如改动了目录布局，请手动迁移 Core/AppConfigs.asset 及其 .meta。");
            }
        }

        private static void ValidateResourceRuleAsset(GFRuntimeSettings runtime, AppBuilderEditorSettings editor, List<RuntimeLayoutIssue> issues)
        {
            string assetPath = editor.ResourceRuleEditorAssetPath;
            if (IsUnder(assetPath, runtime.GameRoot))
            {
                AddWarning(issues, "编辑器资产位于运行时扫描区",
                    $"资源规则资产 {assetPath} 位于 GameRoot({runtime.GameRoot}) 内；Settings 等规则刷新时可能把它重新加入 ResourceCollection。建议移到 GameRoot 之外。");
            }

            ResourceRuleEditorData configuration = AssetDatabase.LoadAssetAtPath<ResourceRuleEditorData>(assetPath);
            if (configuration == null)
            {
                AddWarning(issues, "资源规则资产缺失", $"未找到 {assetPath}；资源规则刷新和构建前检查无法验证规则路径。");
                return;
            }

            if (configuration.rules == null)
            {
                AddWarning(issues, "资源规则列表为空", $"{assetPath} 没有 rules 列表。");
                return;
            }

            foreach (ResourceRule rule in configuration.rules)
            {
                if (rule == null || !rule.valid)
                {
                    continue;
                }

                string layoutKey = rule.layoutKey;
                if (string.IsNullOrEmpty(layoutKey)
                    && ResourceLayoutResolver.TryGetLegacyLayoutKey(rule.name, out string legacyKey))
                {
                    layoutKey = legacyKey;
                }

                if (string.IsNullOrEmpty(layoutKey))
                {
                    continue;
                }

                if (!ResourceLayoutResolver.TryResolve(layoutKey, out string expectedPath))
                {
                    AddWarning(issues, "规则布局键无效", $"规则 {rule.name} 使用未知布局键 {layoutKey}。");
                    continue;
                }

                if (string.IsNullOrEmpty(rule.layoutKey)
                    && !ResourceLayoutResolver.PathsEqual(rule.assetsDirectoryPath, expectedPath))
                {
                    AddWarning(issues, "UGF 规则路径与布局不一致",
                        $"规则 {rule.name}: 当前 {rule.assetsDirectoryPath}，布局期望 {expectedPath}。请在规则编辑器中重新选择或同步该目录。");
                }
            }
        }

        private static void ValidateResourceEditorXml(GFRuntimeSettings runtime, List<RuntimeLayoutIssue> issues)
        {
            string absolutePath = ToAbsolutePath("Assets/GameFramework/Configs/ResourceEditor.xml");
            XmlDocument document = LoadXml(absolutePath, "ResourceEditor.xml", issues);
            if (document == null)
            {
                return;
            }

            XmlNode node = document.SelectSingleNode("/UnityGameFramework/ResourceEditor/Settings/SourceAssetRootPath");
            string sourceRoot = node?.InnerText?.Trim();
            if (!ResourceLayoutResolver.PathsEqual(sourceRoot, runtime.GameRoot))
            {
                AddWarning(issues, "ResourceEditor 根路径不一致",
                    $"ResourceEditor.xml.SourceAssetRootPath = {sourceRoot ?? "(空)"}，当前 GameRoot = {runtime.GameRoot}。请手动同步 UGF 的源资源根路径。");
            }
        }

        private static void ValidateResourceBuilderXml(AppBuilderEditorSettings editor, List<RuntimeLayoutIssue> issues)
        {
            string absolutePath = ToAbsolutePath("Assets/GameFramework/Configs/ResourceBuilder.xml");
            XmlDocument document = LoadXml(absolutePath, "ResourceBuilder.xml", issues);
            if (document == null)
            {
                return;
            }

            XmlNode node = document.SelectSingleNode("/UnityGameFramework/ResourceBuilder/Settings/OutputDirectory");
            string outputDirectory = node?.InnerText?.Trim();
            string expected = editor.GetResourceBuildAbsolutePath();
            string actual = string.IsNullOrWhiteSpace(outputDirectory) ? string.Empty : ToAbsolutePath(outputDirectory);
            if (!ResourceLayoutResolver.PathsEqual(actual, expected))
            {
                AddWarning(issues, "ResourceBuilder 输出目录不一致",
                    $"ResourceBuilder.xml.OutputDirectory = {outputDirectory ?? "(空)"}，当前资源构建目录 = {expected}。AppBuilder 构建会覆盖该值；独立使用 UGF 时请手动同步。");
            }
        }

        private static void ValidateResourceCollection(GFRuntimeSettings runtime, List<RuntimeLayoutIssue> issues)
        {
            string absolutePath = ToAbsolutePath("Assets/GameFramework/Configs/ResourceCollection.xml");
            XmlDocument document = LoadXml(absolutePath, "ResourceCollection.xml", issues);
            if (document == null)
            {
                return;
            }

            string ruleAssetPath = AppBuilderEditorSettings.Instance.ResourceRuleEditorAssetPath;
            XmlNodeList assetNodes = document.SelectNodes("/UnityGameFramework/ResourceCollection/Assets/Asset");
            if (assetNodes == null)
            {
                return;
            }

            foreach (XmlNode assetNode in assetNodes)
            {
                string guid = assetNode.Attributes?["Guid"]?.Value;
                if (string.IsNullOrWhiteSpace(guid))
                {
                    continue;
                }

                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrWhiteSpace(assetPath))
                {
                    AddWarning(issues, "ResourceCollection 引用了无效资产",
                        $"GUID {guid} 无法解析；请刷新资源集合或手动清理失效条目。");
                    continue;
                }

                if (ResourceLayoutResolver.PathsEqual(assetPath, ruleAssetPath))
                {
                    AddWarning(issues, "运行时资源集合包含编辑器资产",
                        $"{assetPath} 是编辑器专用资产，不应加入 ResourceCollection.xml。请从该文件的 Settings 资源中移除。");
                }
            }

            // 明确检查规则资产 GUID，避免只在规则刷新后才暴露问题。
            string ruleGuid = AssetDatabase.AssetPathToGUID(ruleAssetPath);
            if (!string.IsNullOrEmpty(ruleGuid))
            {
                foreach (XmlNode assetNode in assetNodes)
                {
                    if (string.Equals(assetNode.Attributes?["Guid"]?.Value, ruleGuid, StringComparison.OrdinalIgnoreCase))
                    {
                        AddWarning(issues, "ResourceCollection 包含资源规则资产",
                            $"资源规则资产 GUID {ruleGuid} 出现在 ResourceCollection.xml；请移除该条目，避免运行时资源集合被编辑器配置污染。");
                        break;
                    }
                }
            }
        }

        private static XmlDocument LoadXml(string absolutePath, string name, List<RuntimeLayoutIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                AddWarning(issues, $"{name} 路径为空", "无法读取 UGF 配置。");
                return null;
            }

            if (!File.Exists(absolutePath))
            {
                AddWarning(issues, $"{name} 不存在", $"{absolutePath} 不存在。");
                return null;
            }

            try
            {
                var document = new XmlDocument();
                document.Load(absolutePath);
                return document;
            }
            catch (Exception exception)
            {
                AddWarning(issues, $"{name} 解析失败", exception.Message);
                return null;
            }
        }

        private static void CheckDirectory(List<RuntimeLayoutIssue> issues, string name, string projectRelativePath)
        {
            if (!Directory.Exists(ToAbsolutePath(projectRelativePath)))
            {
                AddWarning(issues, "目录不存在", $"{name} = {projectRelativePath}；请确认目录名已同步，或手动迁移对应资产。");
            }
        }

        private static string Combine(string left, string right)
        {
            string normalizedLeft = Normalize(left);
            return normalizedLeft.Length == 0 ? right : normalizedLeft + "/" + right;
        }

        private static string ToAbsolutePath(string projectRelativePath)
        {
            if (string.IsNullOrWhiteSpace(projectRelativePath))
            {
                return string.Empty;
            }

            string path = projectRelativePath.Replace('\\', Path.DirectorySeparatorChar);
            try
            {
                return Path.IsPathRooted(path)
                    ? Path.GetFullPath(path)
                    : Path.GetFullPath(path, ConstEditor.ProjectRootPath);
            }
            catch (Exception)
            {
                // 非法路径由校验报告；这里返回空路径，避免校验页面因 Path.GetFullPath 抛异常而中断。
                return string.Empty;
            }
        }

        private static bool IsUnder(string path, string root)
        {
            string normalizedPath = Normalize(path);
            string normalizedRoot = Normalize(root);
            return normalizedPath.Length > 0
                && normalizedRoot.Length > 0
                && (string.Equals(normalizedPath, normalizedRoot, StringComparison.OrdinalIgnoreCase)
                    || normalizedPath.StartsWith(normalizedRoot + "/", StringComparison.OrdinalIgnoreCase));
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace('\\', '/').Trim().TrimEnd('/');
        }

        private static void AddError(List<RuntimeLayoutIssue> issues, string title, string message)
        {
            issues.Add(new RuntimeLayoutIssue(RuntimeLayoutIssueSeverity.Error, title, message));
        }

        private static void AddWarning(List<RuntimeLayoutIssue> issues, string title, string message)
        {
            issues.Add(new RuntimeLayoutIssue(RuntimeLayoutIssueSeverity.Warning, title, message));
        }
    }
}
