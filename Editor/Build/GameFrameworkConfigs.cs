//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2020 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using UnityGameFramework.Editor;
using UnityGameFramework.Editor.ResourceTools;

namespace UGF.EditorTools
{
    /// <summary>
    /// UGF 编辑器配置路径适配器。
    /// 路径值统一来自 <see cref="AppBuilderEditorSettings"/>；这里只提供静态特性入口。
    /// </summary>
    public static class GameFrameworkConfigs
    {
        [BuildSettingsConfigPath]
        public static string BuildSettingsConfig => Resolve(AppBuilderEditorSettings.Instance.BuildSettingsConfigPath);

        [ResourceCollectionConfigPath]
        public static string ResourceCollectionConfig => Resolve(AppBuilderEditorSettings.Instance.ResourceCollectionConfigPath);

        [ResourceEditorConfigPath]
        public static string ResourceEditorConfig => Resolve(AppBuilderEditorSettings.Instance.ResourceEditorConfigPath);

        [ResourceBuilderConfigPath]
        public static string ResourceBuilderConfig => Resolve(AppBuilderEditorSettings.Instance.ResourceBuilderConfigPath);

        private static string Resolve(string projectRelativePath)
        {
            if (string.IsNullOrWhiteSpace(projectRelativePath))
            {
                return string.Empty;
            }

            return Utility.Path.GetRegularPath(AppBuilderEditorSettings.Instance.GetProjectAbsolutePath(projectRelativePath));
        }
    }
}
