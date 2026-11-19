#if UNITY_EDITOR
namespace UGF.EditorTools
{
    /// <summary>
    /// 编辑器侧非路径常量与表格约定。
    /// 路径定义统一在 GFRuntimeSettings / GFEditorSettings，本类不再承载任何路径派生。
    /// </summary>
    public static class ConstEditor
    {
        /// <summary>本包（com.final5788.ugfx）在 AssetsPath 中的根路径（工程根相对）。包内所有硬编码包路径均从这里派生，包名再改只改这一处。</summary>
        public const string PackageRoot = "Packages/com.final5788.ugfx";

        public const bool AutoScriptUTF8 = true;//新建脚本时自动修改脚本编码方式为utf-8以支持中文
        public const bool ResolveDuplicateAssets = true;

        public static string ProjectRootPath =>
            System.IO.Directory.GetParent(UnityEngine.Application.dataPath)?.FullName ?? UnityEngine.Application.dataPath;

        public const string UITableName = "Core/UITable";
        public const string EntityGroupTableName = "Core/EntityGroupTable";
        public const string SoundGroupTableName = "Core/SoundGroupTable";
        public const string UIGroupTableName = "Core/UIGroupTable";
        public const string UITableExcel = UITableName + ".xlsx";
        public const string EntityGroupTableExcel = EntityGroupTableName + ".xlsx";
        public const string SoundGroupTableExcel = SoundGroupTableName + ".xlsx";
        public const string UIGroupTableExcel = UIGroupTableName + ".xlsx";
        public static readonly string[] FrameworkRequiredDataTables =
        {
            EntityGroupTableName,
            SoundGroupTableName,
            UIGroupTableName,
            UITableName,
        };

        public const string SharedAssetBundleName = "SharedAssets";//AssetBundle分包共用资源
        // 热更程序集入口（常量，多个工具都依赖）
        public const string HotfixAssemblyPath = "Assets/GF_X/Scripts/Runtime/Hotfix.asmdef";
    }
}
#endif
