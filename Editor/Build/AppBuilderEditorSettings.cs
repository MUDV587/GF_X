using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UGF.EditorTools
{
    /// <summary>
    /// 构建域设置（ProjectSettings/GameFrameworkX/AppBuilderEditorSettings.asset）。
    /// 汇集一切打包/打热更/资源构建/Jenkins CI 所需路径与参数；跨工具参数见各自 SO。
    /// </summary>
    [FilePath("ProjectSettings/GameFrameworkX/AppBuilderEditorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class AppBuilderEditorSettings : UGF.EditorTools.EditorScriptableSingleton<AppBuilderEditorSettings>
    {
        public const string DefaultResourceBuildDir = "AssetBundles";
        public const string DefaultAppBuildDir = "Builds";
        public const string Netstandard2NetFrameworkDir = "Assets/Netstandard2NetFramework";
        /// <summary>Unity 识别的固定 link.xml 锚点（不可随 GameRoot 迁移）。</summary>
        public const string UnityLinkXmlPath = "Assets/link.xml";
        private const string PackageRuntimeRoot = "Packages/com.final5788.ugfx/Runtime";
        private const string FrameworkConfigsRoot = "Assets/GameFramework/Configs";
        private const string ToolsJenkinsDir = "Tools/Jenkins";
        private const string EditorScriptableObjectsRoot = "Assets/GF_X_Editor/Settings";

        // ── 用户可配（构建输出与签名）
        [Header("构建输出与签名")]
        [Tooltip("签名 keystore（工程相对路径）")] public string KeystoreFile = "Keystore/user.keystore";
        [Tooltip("资源构建输出目录（工程相对；CI 可传绝对路径覆盖）")] public string ResourceBuildDir = "AssetBundles";
        [Tooltip("应用构建输出目录（工程相对）")] public string AppBuildDir = "Builds";

        // ── 更新参数（构建期 / 热更期共用）
        public string UpdatePrefixUri = "http://localhost/1_0_0_1/";
        public bool Netstandard2NetFramework = true;
        public string ApplicableGameVersion = "1.0.0|1.0.1|1.0.2";
        public bool ForceUpdateApp = false;
        public string AppUpdateUrl = "https://play.google.com/store/apps/details?id=";
        public string AppUpdateDesc = "1. bug fix.\n2. add xxx";
        public bool RevealFolder = true;
        public bool EnableResourceRuleEditor = true;
        public bool AndroidUseKeystore = false;
        public string KeystorePass = "topgames";
        public string AndroidKeyAliasName = "release";
        public string KeyAliasPass = "topgames";
        public string[] Netstandard2NetFrameworkList = new string[0];
        public bool EnableObfuz = false;

        public static string ProjectRootPath =>
            Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;

        // ── 派生路径（约定常量推导，无序列化副本）
        public string BuiltinAssemblyPath => Combine(PackageRuntimeRoot, "GF_X.Runtime.asmdef");
        public string HotfixAssembly => "Assets/GF_X/Scripts/Runtime/Hotfix.asmdef";
        public string HotfixAssemblyDir => Path.GetDirectoryName(HotfixAssembly) ?? string.Empty;
        public string HotfixLinkFile => Combine(HotfixAssemblyDir, "link.xml");
        public string HotfixLinkFileAbsolute => GetProjectAbsolutePath(HotfixLinkFile);

        // ── 框架配置文件（UGF 默认布局）
        public string ResourceCollectionConfigPath => Combine(FrameworkConfigsRoot, "ResourceCollection.xml");
        public string ResourceEditorConfigPath => Combine(FrameworkConfigsRoot, "ResourceEditor.xml");
        public string ResourceBuilderConfigPath => Combine(FrameworkConfigsRoot, "ResourceBuilder.xml");
        public string BuildSettingsConfigPath => Combine(FrameworkConfigsRoot, "BuildSettings.xml");

        public string ResourceRuleEditorAssetPath => Combine(EditorScriptableObjectsRoot, "ResourceRuleEditor.asset");

        public string GetResourceBuildDir()
        {
            return string.IsNullOrWhiteSpace(ResourceBuildDir) ? DefaultResourceBuildDir : ResourceBuildDir;
        }

        public string GetAppBuildDir()
        {
            return string.IsNullOrWhiteSpace(AppBuildDir) ? DefaultAppBuildDir : AppBuildDir;
        }

        // ── 派生
        public string GetResourceBuildAbsolutePath()
        {
            return GetProjectAbsolutePath(GetResourceBuildDir());
        }

        public string GetAppBuildAbsolutePath()
        {
            return GetProjectAbsolutePath(GetAppBuildDir());
        }

        public string GetKeystoreAbsolutePath()
        {
            return GetProjectAbsolutePath(KeystoreFile);
        }

        public string ResolveResourceBuildOutputDir(string overrideDir)
        {
            if (string.IsNullOrWhiteSpace(overrideDir))
            {
                return GetResourceBuildAbsolutePath();
            }

            return Path.IsPathRooted(overrideDir)
                ? Path.GetFullPath(overrideDir)
                : GetProjectAbsolutePath(overrideDir);
        }

        // ── Jenkins CI 配置路径（固定用 Tools/Jenkins；override 可入参数）
        public string GetJenkinsConfigPath(string relativeConfigFile)
        {
            return Path.Combine(ProjectRootPath, ToolsJenkinsDir, relativeConfigFile);
        }

        public string GetJenkinsConfigRoot() => ToolsJenkinsDir;

        public string GetProjectAbsolutePath(string projectRelativePath)
        {
            return Path.GetFullPath(projectRelativePath.Replace('/', Path.DirectorySeparatorChar), ProjectRootPath);
        }

        private static string Combine(string left, string right)
        {
            string leftNorm = Normalize(left);
            string rightNorm = Normalize(right);
            if (leftNorm.Length == 0) return rightNorm;
            if (rightNorm.Length == 0) return leftNorm;
            return string.Concat(leftNorm, "/", rightNorm);
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Replace('\\', '/').Trim().TrimEnd('/');
        }
    }
}
