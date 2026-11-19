using System;
using System.IO;
using System.Text;
using GameFramework;
using UnityEngine;

namespace UGF.EditorTools
{
    [Flags]
    public enum GameDataType
    {
        DataTable = 1 << 0,
        Config = 1 << 1,
        Language = 1 << 2,
        All = DataTable | Config | Language,
    }

    internal static class GameDataShared
    {
        // 与 HybridCLRExtensionTool.ENABLE_OBFUZ 同值：Obfuz 加密 #if 标签
        public const string ENABLE_OBFUZ = "ENABLE_OBFUZ";
        // 框架约定常量（DataTable 域与 GameData 域共享，避免跨程序集引用设置）
        public const string DataTableCodeTemplateRelativePath =
            "Packages/com.final5788.ugfx/Editor/Data/DataTable/Templates/DataTableCodeTemplate/DataTableCodeTemplate.txt";
        public const string DefaultGeneratedDataTableDir = "Assets/GF_X/Scripts/Runtime/Generated/DataTable";

        public static readonly Encoding Utf8NoBom = new UTF8Encoding(false, true);

        public static string GetRelativeName(string fileName, string relativePath)
        {
            string path = Path.GetRelativePath(relativePath, fileName);
            return Utility.Path.GetRegularPath(Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path)));
        }

        public static void WriteTextFile(string fileName, string content, Encoding encoding)
        {
            if (Directory.GetParent(fileName) is { } parent)
            {
                Directory.CreateDirectory(parent.FullName);
            }

            File.WriteAllText(fileName, content, encoding ?? Encoding.UTF8);
        }
    }
}
