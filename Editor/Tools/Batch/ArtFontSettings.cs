using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    /// <summary>
    /// 美术字体工具设置（ProjectSettings/GameFrameworkX/ArtFontSettings.asset）。
    /// </summary>
    [FilePath("ProjectSettings/GameFrameworkX/ArtFontSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class ArtFontSettings : EditorScriptableSingleton<ArtFontSettings>
    {
        private const string ImageFontAiTempDir = "Library/ImageFontAiGenerate";
        // 提示词模板包路径（框架自带只读）
        private const string ImageFontAiGeneratePromptRelativePath =
            ConstEditor.PackageRoot + "/Editor/Tools/Batch/ImageFontAtlasGeneratePrompt.md";

        public string DefaultImageFontTargetDir = "Assets/GF_X/Sprites/ImageFonts";

        // 自定义 Prompt 路径（自己带; 覆盖框架提示词后需要同步 AI 任务定义）
        public string ImageFontAiGeneratePromptPath = ImageFontAiGeneratePromptRelativePath;

        public static string ProjectRootPath =>
            Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;

        public string ImageFontAiGenerateTempDirName => ImageFontAiTempDir;

        public string ImageFontAiGenerateTempDir => GetProjectAbsolutePath(ImageFontAiGenerateTempDirName);

        public string GetProjectAbsolutePath(string projectRelativePath)
        {
            return Path.GetFullPath(projectRelativePath.Replace('/', Path.DirectorySeparatorChar), ProjectRootPath);
        }
    }
}
