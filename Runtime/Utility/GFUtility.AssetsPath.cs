using GameFramework;


public static partial class GFUtility
{
    /// <summary>
    /// 运行时路径门面：对外提供稳定的路径方法签名，
    /// 所有路径值/派生规则统一由 GFRuntimeSettings 提供。
    /// </summary>
    public static class AssetsPath
    {
        public static string GetCombinePath(params string[] args)
        {
            return Utility.Path.GetRegularPath(System.IO.Path.Combine(args));
        }

        public static string GetDataTablePath(string name, bool useBytes)
        {
            return GFRuntimeSettings.Instance.GetDataTablePath(name, useBytes);
        }

        public static string GetSoundPath(string name)
        {
            return GFRuntimeSettings.Instance.GetSoundPath(name);
        }

        public static string GetScenePath(string name)
        {
            return GFRuntimeSettings.Instance.GetScenePath(name);
        }

        public static string GetEntityPath(string name)
        {
            return GFRuntimeSettings.Instance.GetEntityPath(name);
        }

        public static string GetUIFormPath(string v)
        {
            return GFRuntimeSettings.Instance.GetUIFormPath(v);
        }

        public static string GetTexturePath(string fileName)
        {
            return GFRuntimeSettings.Instance.GetTexturePath(fileName);
        }

        public static string GetSpritesPath(string fileName)
        {
            return GFRuntimeSettings.Instance.GetSpritesPath(fileName);
        }

        public static string GetConfigPath(string v, bool useBytes)
        {
            return GFRuntimeSettings.Instance.GetConfigPath(v, useBytes);
        }

        public static string GetPrefab(string v)
        {
            return GFRuntimeSettings.Instance.GetPrefab(v);
        }

        public static string GetLanguagePath(string v, bool useBytes)
        {
            return GFRuntimeSettings.Instance.GetLanguagePath(v, useBytes);
        }

        public static string GetMaterialPath(string v)
        {
            return GFRuntimeSettings.Instance.GetMaterialPath(v);
        }

        public static string GetHotfixDll(string dllName)
        {
            return GFRuntimeSettings.Instance.GetHotfixDll(dllName);
        }

        public static string GetScriptableAsset(string v)
        {
            return GFRuntimeSettings.Instance.GetScriptableAsset(v);
        }
    }
}