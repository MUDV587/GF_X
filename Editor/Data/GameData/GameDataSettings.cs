using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UGF.EditorTools
{
    /// <summary>
    /// 游戏数据域设置（ProjectSettings/GameFrameworkX/GameDataSettings.asset）。
    /// 持有：Excel 根路径、生成代码/数据表路径、Unity.Languages 输出。
    /// </summary>
    [FilePath("ProjectSettings/GameFrameworkX/GameDataSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class GameDataSettings : EditorScriptableSingleton<GameDataSettings>
    {
        private const string DefaultDataTablesExcelRoot = "GF_X/DataTables";
        private const string DefaultConfigsExcelRoot = "GF_X/Configs";
        private const string DefaultLanguagesExcelRoot = "GF_X/Languages";

        // ── Excel 源（用户可配，通常保持框架默认）
        [Tooltip("数据表 Excel 根目录（工程相对；含子目录）")] public string DataTablesExcelRoot = DefaultDataTablesExcelRoot;
        [Tooltip("配置表 Excel 根目录（工程相对；含子目录）")] public string ConfigsExcelRoot = DefaultConfigsExcelRoot;
        [Tooltip("多语言表 Excel 根目录（工程相对；含子目录）")] public string LanguagesExcelRoot = DefaultLanguagesExcelRoot;

        // ── 生成代码输出（随 Hotfix 树）
        [Tooltip("框架自动生成 C# 文件的根目录（工程相对）")] public string GeneratedCodeRoot = "Assets/GF_X/Scripts/Runtime/Generated";

        // ── 包根（只读预览）
        private const string PackageRootPath = "Packages/com.final5788.ugfx";
        private const string UITemplatesDir = "UI/Templates";

        public string PackageRoot => PackageRootPath;
        public string PackageRuntimeRoot => Combine(PackageRootPath, "Runtime");
        public string PackageEditorRoot => Combine(PackageRootPath, "Editor");

        // ── 生成代码目录派生（按需外部访问）
        public string DataTableCodePath => Combine(GeneratedCodeRoot, "DataTable");
        public string ConstGroupScriptFileFullName => Combine(GeneratedCodeRoot, "Common/Const.Groups.cs");
        public string UIViewScriptFile => Combine(GeneratedCodeRoot, "UI/UIViews.cs");
        public string UISerializeFieldDir => Combine(GeneratedCodeRoot, "UI/UIVariables");
        public string UIItemSerializeFieldDir => Combine(GeneratedCodeRoot, "UI/UIItemVariables");
        public string UIScriptsPath => Combine(GetRuntimeGameRoot(), "Scripts/Runtime/UI");
        public string UIItemScriptsPath => Combine(UIScriptsPath, "Item");

        // ── UI 模板（包内只读）
        public string UIFormTemplatePath => Combine(Combine(PackageEditorRoot, UITemplatesDir), "UIFormTemplate.prefab");
        public string UIDialogTemplatePath => Combine(Combine(PackageEditorRoot, UITemplatesDir), "UIDialogTemplate.prefab");
        public string UIItemTemplatePath => Combine(Combine(PackageEditorRoot, UITemplatesDir), "UIItemTemplate.prefab");
        public string UIScriptFileTemplatePath => Combine(Combine(PackageEditorRoot, UITemplatesDir), "UIScriptFileTemplate.txt");
        public string UIItemScriptFileTemplatePath => Combine(Combine(PackageEditorRoot, UITemplatesDir), "UIItemScriptFileTemplate.txt");

        // ── 模板（框架打包进包内，只读；常量在 Core GameDataShared）
        public string DataTableCodeTemplatePath => GameDataShared.DataTableCodeTemplateRelativePath;

        public string GetRuntimeGameRoot()
        {
            GFRuntimeSettings runtime = GFRuntimeSettings.Instance;
            if (runtime == null || string.IsNullOrWhiteSpace(runtime.GameRoot))
            {
                return "Assets/GF_X";
            }

            return runtime.GameRoot.TrimEnd('/');
        }

        public static string ProjectRootPath =>
            Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;

        public string DataTableExcelPath => GetProjectAbsolutePath(DataTablesExcelRoot);
        public string ConfigExcelPath => GetProjectAbsolutePath(ConfigsExcelRoot);
        public string LanguageExcelPath => GetProjectAbsolutePath(LanguagesExcelRoot);

        public string GetDataTableExcelFullPath(string tableName)
        {
            return Path.Combine(DataTableExcelPath, tableName + ".xlsx");
        }

        public string GetConfigExcelFullPath(string configName)
        {
            return Path.Combine(ConfigExcelPath, configName + ".xlsx");
        }

        public string GetLanguageExcelFullPath(string languageName)
        {
            return Path.Combine(LanguageExcelPath, languageName + ".xlsx");
        }

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
