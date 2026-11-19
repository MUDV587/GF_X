using System;

namespace UGF.EditorTools.Build.ResourceRules
{
    /// <summary>
    /// 把资源规则中的逻辑布局键解析为当前 <see cref="GFRuntimeSettings"/> 的目录。
    /// 规则只保存布局键和固定路径回退值；迁移目录时不需要再改一份规则路径。
    /// </summary>
    public static class ResourceLayoutResolver
    {
        public const string GameRoot = "GameRoot";
        public const string Generated = "Generated";
        public const string GeneratedData = "GeneratedData";
        public const string Settings = "Settings";
        public const string HotfixDlls = "HotfixDlls";
        public const string Prefabs = "Prefabs";
        public const string Core = "Core";
        public const string Entity = "Entity";
        public const string UI = "UI";
        public const string Scene = "Scene";
        public const string Audio = "Audio";
        public const string Textures = "Textures";
        public const string Sprites = "Sprites";
        public const string Materials = "Materials";
        public const string DataTable = "DataTable";
        public const string Config = "Config";
        public const string Language = "Language";

        /// <summary>可在规则编辑器中选择的布局键（不包含“自定义”）。</summary>
        public static readonly string[] LayoutKeys =
        {
            Entity,
            Core,
            UI,
            Config,
            DataTable,
            HotfixDlls,
            Language,
            Scene,
            Settings,
            Audio,
            Textures,
            Sprites,
            Materials,
        };

        public static string GetDisplayName(string layoutKey)
        {
            switch (layoutKey)
            {
                case Entity:
                    return "Entity (Prefabs/Entity)";
                case Core:
                    return "Core (Prefabs/Core)";
                case UI:
                    return "UI (Prefabs/UI)";
                case Config:
                    return "Config (Generated/Data/Config)";
                case DataTable:
                    return "DataTable (Generated/Data/DataTable)";
                case HotfixDlls:
                    return "HotfixDlls";
                case Language:
                    return "Language (Generated/Data/Language)";
                case Scene:
                    return "Scene";
                case Settings:
                    return "Settings";
                case Audio:
                    return "Audio";
                case Textures:
                    return "Textures";
                case Sprites:
                    return "Sprites";
                case Materials:
                    return "Materials";
                default:
                    return string.IsNullOrEmpty(layoutKey) ? "自定义目录" : layoutKey;
            }
        }

        public static bool TryResolve(string layoutKey, out string assetsDirectoryPath)
        {
            assetsDirectoryPath = string.Empty;
            if (string.IsNullOrEmpty(layoutKey))
            {
                return false;
            }

            GFRuntimeSettings runtime = GFRuntimeSettings.Instance;
            if (runtime == null)
            {
                return false;
            }

            switch (layoutKey)
            {
                case GameRoot:
                    assetsDirectoryPath = runtime.GameRoot;
                    return true;
                case Generated:
                    assetsDirectoryPath = runtime.GeneratedRoot;
                    return true;
                case GeneratedData:
                    assetsDirectoryPath = runtime.GeneratedDataRoot;
                    return true;
                case Settings:
                    assetsDirectoryPath = runtime.SettingsRoot;
                    return true;
                case HotfixDlls:
                    assetsDirectoryPath = runtime.HotfixDllDir;
                    return true;
                case Prefabs:
                    assetsDirectoryPath = runtime.PrefabsRoot;
                    return true;
                case Core:
                    assetsDirectoryPath = runtime.CoreRoot;
                    return true;
                case Entity:
                    assetsDirectoryPath = runtime.EntityRoot;
                    return true;
                case UI:
                    assetsDirectoryPath = runtime.UIFormRoot;
                    return true;
                case Scene:
                    assetsDirectoryPath = runtime.SceneRoot;
                    return true;
                case Audio:
                    assetsDirectoryPath = runtime.AudioRoot;
                    return true;
                case Textures:
                    assetsDirectoryPath = runtime.TextureRoot;
                    return true;
                case Sprites:
                    assetsDirectoryPath = runtime.SpritesRoot;
                    return true;
                case Materials:
                    assetsDirectoryPath = runtime.MaterialRoot;
                    return true;
                case DataTable:
                    assetsDirectoryPath = runtime.DataTableRoot;
                    return true;
                case Config:
                    assetsDirectoryPath = runtime.ConfigRoot;
                    return true;
                case Language:
                    assetsDirectoryPath = runtime.LanguageRoot;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>返回规则实际使用的目录；布局键无效时回退到规则保存的固定路径。</summary>
        public static string Resolve(ResourceRule rule)
        {
            if (rule == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(rule.layoutKey) && TryResolve(rule.layoutKey, out string path))
            {
                return path;
            }

            return rule.assetsDirectoryPath ?? string.Empty;
        }

        /// <summary>
        /// 兼容尚未写入 layoutKey 的旧规则：按规则名映射到已知布局键。
        /// 只用于校验提示，不会在编译时静默改变用户自定义规则。
        /// </summary>
        public static bool TryGetLegacyLayoutKey(string ruleName, out string layoutKey)
        {
            layoutKey = string.Empty;
            if (string.IsNullOrEmpty(ruleName))
            {
                return false;
            }

            switch (ruleName)
            {
                case Entity:
                case Core:
                case UI:
                case Config:
                case DataTable:
                case HotfixDlls:
                case Language:
                case Scene:
                case Settings:
                    layoutKey = ruleName;
                    return true;
                default:
                    return false;
            }
        }

        public static bool PathsEqual(string left, string right)
        {
            return string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);
        }

        private static string Normalize(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Replace('\\', '/').Trim().TrimEnd('/');
        }
    }
}
