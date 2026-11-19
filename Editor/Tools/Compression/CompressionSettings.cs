using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    /// <summary>
    /// 压缩工具设置（ProjectSettings/GameFrameworkX/CompressionSettings.asset）。
    /// 数据持有：图片/动画文件压缩、Atlas/Texture 处理参数。
    /// </summary>
    [FilePath("ProjectSettings/GameFrameworkX/CompressionSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class CompressionSettings : EditorScriptableSingleton<CompressionSettings>
    {
        private const string CompressImageToolsDir = "CompressImageTools";
        private const string TextureWarningsFileName = "TextureWarnings.txt";
#if UNITY_EDITOR_WIN
        private const string PngQuantSubPath = "pngquant_win/pngquant.exe";
#elif UNITY_EDITOR_OSX
        private const string PngQuantSubPath = "pngquant_mac/pngquant";
#else
        private const string PngQuantSubPath = "";
#endif

        // ── 图片压缩参数（用户可配）
        public bool CompressImgToolCoverRaw;
        public int CompressImgToolFastLv = 1;
        public float CompressImgToolQualityLv = 80;
        public float CompressImgToolQualityMinLv;

        // ── 图片目录（工程相对）
        public string CompressImgToolBackupDir = "CompressImageTool/ImgBackupDir";
        public string CompressImgToolOutputDir = "CompressImageTool/ImgCompressedDir";

        // ── PngQuant 工具路径（随平台安装目录，预设默认，可配置）
        public string PngQuantToolRelativePath = CompressImageToolsDir + "/" + PngQuantSubPath;

        public static string ProjectRootPath =>
            Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;

        public string PngQuantToolPath =>
            string.IsNullOrWhiteSpace(PngQuantToolRelativePath)
                ? string.Empty
                : GetProjectAbsolutePath(PngQuantToolRelativePath);

        public string TextureWarningsFile =>
            GetProjectAbsolutePath(CompressImageToolsDir + "/" + TextureWarningsFileName);

        public string GetProjectAbsolutePath(string projectRelativePath)
        {
            return Path.GetFullPath(projectRelativePath.Replace('/', Path.DirectorySeparatorChar), ProjectRootPath);
        }
    }
}
