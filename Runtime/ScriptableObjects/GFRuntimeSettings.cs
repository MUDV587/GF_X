using System;
using UnityEngine;

/// <summary>
/// 运行时路径设置（Assets/Resources/GFRuntimeSettings.asset）。
/// 约定优于配置：只有 GameRoot 可配置，目录布局是框架约定（见 GF_X-PATCHES.md / Docs/MigrationLog.md T13）。
/// 编辑器专属路径见 UGF.EditorTools.GFEditorSettings。
/// </summary>
[CreateAssetMenu(fileName = "GFRuntimeSettings", menuName = "GF/GFRuntimeSettings [运行时路径配置]")]
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.All)]
public class GFRuntimeSettings : ScriptableObject
{
    private const string ResourcePath = "GFRuntimeSettings";
    public const string DefaultGameRoot = "Assets/GF_X";
    public const string RuntimeAssetPath = "Assets/Resources/GFRuntimeSettings.asset";
    private static GFRuntimeSettings s_Instance;

    public static GFRuntimeSettings Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = Resources.Load<GFRuntimeSettings>(ResourcePath);
                if (s_Instance == null)
                {
                    Debug.LogError($"加载运行时路径配置失败: Resources/{ResourcePath}.asset 不存在或未导入。");
                }
            }

            return s_Instance;
        }
    }

    [Header("业务根（工程相对路径，/ 分隔）")]
    [Tooltip("业务内容根，包含 Assets/ 前缀。其余目录布局是框架约定，不可配置。")]
    public string GameRoot = DefaultGameRoot;

    // ── 约定目录名（常量，不可配置）
    public const string GeneratedDir = "Generated";
    public const string GeneratedDataDir = "Data";
    public const string DataTableDir = "DataTable";
    public const string ConfigDir = "Config";
    public const string LanguageDir = "Language";
    public const string SettingsDir = "Settings";
    public const string HotfixDllsDir = "HotfixDlls";
    public const string PrefabsDir = "Prefabs";
    public const string SceneDir = "Scene";
    public const string AudioDir = "Audio";
    public const string CoreDir = "Core";
    public const string EntityDir = "Entity";
    public const string UIFormDir = "UI";
    public const string TextureDir = "Textures";
    public const string SpriteDir = "Sprites";
    public const string MaterialDir = "Materials";

    // ── Resources 相对（常量）
    public const string AotDllDir = "AOTDLLs";
    public const string ResourcesDir = "Assets/Resources";

    // ── 文件名（常量）
    public const string VersionFile = "version.json";
    public const string HotfixFileList = "HotfixFileList.txt";
    public const string ObfuzSecretKeyResourcesPath = "Obfuz/defaultStaticSecretKey";

    private static string GameRootValue => Normalize(string.IsNullOrWhiteSpace(InstanceOrNullGameRoot) ? DefaultGameRoot : InstanceOrNullGameRoot);

    // 防止 Unity 在序列化前调用属性导致空引用：实例化阶段读静态默认值即可。
    private static string InstanceOrNullGameRoot => s_Instance == null ? DefaultGameRoot : s_Instance.GameRoot;

    // ── 派生根（全部由 GameRoot + 约定常量派生，无配置副本）
    public string GeneratedRoot => Combine(GameRootValue, GeneratedDir);
    public string GeneratedDataRoot => Combine(GeneratedRoot, GeneratedDataDir);
    public string SettingsRoot => Combine(GameRootValue, SettingsDir);
    public string HotfixDllDir => Combine(GameRootValue, HotfixDllsDir);
    public string ResourcesRoot => ResourcesDir;
    public string AotDllResourcesDir => Combine(ResourcesDir, AotDllDir);
    public string PrefabsRoot => Combine(GameRootValue, PrefabsDir);
    public string SceneRoot => Combine(GameRootValue, SceneDir);
    public string AudioRoot => Combine(GameRootValue, AudioDir);
    public string CoreRoot => Combine(PrefabsRoot, CoreDir);
    public string EntityRoot => Combine(PrefabsRoot, EntityDir);
    public string UIFormRoot => Combine(PrefabsRoot, UIFormDir);
    public string TextureRoot => Combine(GameRootValue, TextureDir);
    public string SpritesRoot => Combine(GameRootValue, SpriteDir);
    public string MaterialRoot => Combine(GameRootValue, MaterialDir);
    public string DataTableRoot => Combine(GeneratedDataRoot, DataTableDir);
    public string ConfigRoot => Combine(GeneratedDataRoot, ConfigDir);
    public string LanguageRoot => Combine(GeneratedDataRoot, LanguageDir);

    public string GetDataTablePath(string name, bool useBytes)
    {
        return string.Concat(DataTableRoot, "/", name, useBytes ? ".bytes" : ".txt");
    }

    public string GetConfigPath(string name, bool useBytes)
    {
        return string.Concat(ConfigRoot, "/", name, useBytes ? ".bytes" : ".txt");
    }

    public string GetLanguagePath(string name, bool useBytes)
    {
        return string.Concat(LanguageRoot, "/", name, useBytes ? ".bytes" : ".json");
    }

    public string GetSoundPath(string name)
    {
        return string.Concat(AudioRoot, "/", name);
    }

    public string GetScenePath(string name)
    {
        return string.Concat(SceneRoot, "/", name, ".unity");
    }

    public string GetEntityPath(string name)
    {
        return string.Concat(EntityRoot, "/", name, ".prefab");
    }

    public string GetUIFormPath(string name)
    {
        return string.Concat(UIFormRoot, "/", name, ".prefab");
    }

    public string GetTexturePath(string fileName)
    {
        return string.Concat(TextureRoot, "/", fileName);
    }

    public string GetSpritesPath(string fileName)
    {
        return string.Concat(SpritesRoot, "/", fileName);
    }

    public string GetPrefab(string name)
    {
        return string.Concat(PrefabsRoot, "/", name, ".prefab");
    }

    public string GetMaterialPath(string name)
    {
        return string.Concat(MaterialRoot, "/", name, ".mat");
    }

    public string GetHotfixDll(string dllName)
    {
        return string.Concat(HotfixDllDir, "/", dllName, ".bytes");
    }

    public string GetScriptableAsset(string name)
    {
        return string.Concat(SettingsRoot, "/", name, ".asset");
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

        if (normalizedLeft.Length == 0)
        {
            return normalizedRight;
        }

        if (normalizedRight.Length == 0)
        {
            return normalizedLeft;
        }

        return string.Concat(normalizedLeft, "/", normalizedRight);
    }
}
