using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    /// <summary>
    /// 本地化工具设置（ProjectSettings/GameFrameworkX/LocalizationSettings.asset）。
    /// 数据持有：语言/翻译/AI Prompt 模板/扫描输出路径。
    /// </summary>
    [FilePath("ProjectSettings/GameFrameworkX/LocalizationSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class LocalizationSettings : EditorScriptableSingleton<LocalizationSettings>
    {
#if UNITY_EDITOR_WIN
        private const string LocalizationScannerToolName = "LocalizationCodeScanner.exe";
#else
        private const string LocalizationScannerToolName = "LocalizationCodeScanner";
#endif
        private const string DefaultPromptTemplate =
            ConstEditor.PackageRoot + "/Editor/Data/Localization/AIPrompts/LocalizationTranslatePrompt.md";

        // ── 语言
        public List<int> LanguagesSupport = new List<int>();

        // ── AI/翻译
        public int LocalizationTranslationProvider = (int)UGF.EditorTools.LocalizationTranslationProvider.ClaudeCodeCli;
        public bool LocalizationAiShowDebugCommandWindow;
        public string BaiduTransAppId = string.Empty;
        public string BaiduTransSecretKey = string.Empty;
        public int BaiduTransMaxLength = 2000;
        [HideInInspector] public string LocalizationAiPromptTemplateOverride = string.Empty;

        // ── 扫描
        public string LocalizationScanOutputFile = "Tools/LocalizationTextsScannerOutput.txt";
        public string LocalizationTranslateTempDirName = "Library/LocalizationTranslate";

        public static string ProjectRootPath =>
            Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;

        public string LocalizationScannerToolPath =>
            Combine(Combine(GetProjectAbsolutePath("Tools"), "LocalizationStringScanner"), LocalizationScannerToolName);

        public string LocalizationTranslateTempDir => GetProjectAbsolutePath(LocalizationTranslateTempDirName);

        public string LocalizationAiPromptTemplatePath
        {
            get => string.IsNullOrWhiteSpace(LocalizationAiPromptTemplateOverride)
                ? DefaultPromptTemplate
                : LocalizationAiPromptTemplateOverride;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    LocalizationAiPromptTemplateOverride = string.Empty;
                    return;
                }

                string normalized = Normalize(value);
                string defaultPath = Normalize(DefaultPromptTemplate);
                LocalizationAiPromptTemplateOverride = string.Equals(normalized, defaultPath, StringComparison.OrdinalIgnoreCase)
                    ? string.Empty
                    : value;
            }
        }

        public string GetProjectAbsolutePath(string projectRelativePath)
        {
            return Path.GetFullPath(projectRelativePath.Replace('/', Path.DirectorySeparatorChar), ProjectRootPath);
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Replace('\\', '/').Trim().TrimEnd('/');
        }

        private static string Combine(string left, string right)
        {
            string normalizedLeft = Normalize(left);
            string normalizedRight = Normalize(right);
            if (normalizedLeft.Length == 0) return normalizedRight;
            if (normalizedRight.Length == 0) return normalizedLeft;
            return string.Concat(normalizedLeft, "/", normalizedRight);
        }
    }
}
