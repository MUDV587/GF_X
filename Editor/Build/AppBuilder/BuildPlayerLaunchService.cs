using GameFramework;
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UGF.EditorTools
{
    // ── Unity 引擎内部 API（Editor 私有成员，非公开接口）─────────────────────────
    // 本文件刻意依赖以下 Unity 内部实现，均不提供公开替代（截至 Unity 2022.3.62f1 验证可用）：
    //   BuildPlayerWindow.buildCompletionHandler（控制构建完成回调）
    //   BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptionsInternal（组装构建参数）
    //   UnityEditor.EditorUserBuildSettingsUtils.CalculateSelectedBuildTarget
    //   EditorUserBuildSettings.GetSelectedSubtargetFor
    //   UnityEditor.PostprocessBuildPlayer.GetExtensionForBuildTarget
    // 升级 Unity 版本后必须完整重测打包链（Build App / Build Resource / Jenkins 构建）。
    // 后续 E3-A 计划用 BuildPipeline.BuildPlayer(BuildPlayerOptions) 公开 API 重写本文件（见 UGFX-PATCHES.md）。
    internal static class BuildPlayerLaunchService
    {
        private const string PendingBuildTaskKey = "BUILD_TASK_TAG";

        public static bool HasPendingBuild()
        {
            return EditorPrefs.GetBool(PendingBuildTaskKey, false);
        }

        public static void SchedulePendingBuild()
        {
            EditorPrefs.SetBool(PendingBuildTaskKey, true);
        }

        public static void ExecutePendingBuild(Action<BuildReport> buildCompletionHandler)
        {
#if !UNITY_2022_3_OR_NEWER
            Debug.LogError("BuildPlayerLaunchService 仅支持 Unity 2022.3 及以上版本（依赖 Unity 内部构建 API，见文件头注释）。");
            return;
#else
            EditorPrefs.SetBool(PendingBuildTaskKey, false);

            var buildCompletionHandlerField = typeof(BuildPlayerWindow).GetField("buildCompletionHandler", BindingFlags.Static | BindingFlags.NonPublic);
            var getBuildPlayerOptions = typeof(BuildPlayerWindow.DefaultBuildMethods).GetMethod("GetBuildPlayerOptionsInternal", BindingFlags.NonPublic | BindingFlags.Static);
            var editorUserBuildSettingsUtilsType = Utility.Assembly.GetType("UnityEditor.EditorUserBuildSettingsUtils");
            var calculateSelectedBuildTargetMethod = editorUserBuildSettingsUtilsType?.GetMethod("CalculateSelectedBuildTarget", BindingFlags.Public | BindingFlags.Static);
            var getSelectedSubtargetMethod = typeof(EditorUserBuildSettings).GetMethod("GetSelectedSubtargetFor", BindingFlags.Static | BindingFlags.NonPublic);
            if (buildCompletionHandlerField == null
                || getBuildPlayerOptions == null
                || calculateSelectedBuildTargetMethod == null
                || getSelectedSubtargetMethod == null)
            {
                // 打 Unity 补丁级升级后最容易炸在这里：给出可直接对照的诊断信息。
                Debug.LogError($"[BuildPlayerLaunchService] Unity 内部构建 API 反射失败（引擎私有接口可能已变更，需重测/重写本文件）：\n" +
                    $"  BuildPlayerWindow.buildCompletionHandler = {(buildCompletionHandlerField != null ? "OK" : "MISSING")}\n" +
                    $"  DefaultBuildMethods.GetBuildPlayerOptionsInternal = {(getBuildPlayerOptions != null ? "OK" : "MISSING")}\n" +
                    $"  EditorUserBuildSettingsUtils.CalculateSelectedBuildTarget = {(calculateSelectedBuildTargetMethod != null ? "OK" : "MISSING")}\n" +
                    $"  EditorUserBuildSettings.GetSelectedSubtargetFor = {(getSelectedSubtargetMethod != null ? "OK" : "MISSING")}\n" +
                    $"  当前 Unity 版本：{Application.unityVersion}");
                return;
            }

            try
            {
                buildCompletionHandlerField.SetValue(null, buildCompletionHandler);
                var buildOptions = new BuildPlayerOptions
                {
                    options = BuildOptions.ShowBuiltPlayer,
                    targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup,
                    target = (BuildTarget)calculateSelectedBuildTargetMethod.Invoke(null, null)
                };
                buildOptions.subtarget = (int)getSelectedSubtargetMethod.Invoke(null, new object[] { buildOptions.target });

                var errBuildDir = string.IsNullOrWhiteSpace(AppBuilderEditorSettings.Instance.AppBuildDir);
                var locationPathName = GetBuildLocation(buildOptions.targetGroup, buildOptions.target, buildOptions.subtarget, buildOptions.options);
                if (string.IsNullOrWhiteSpace(locationPathName))
                {
                    Debug.LogError("[BuildPlayerLaunchService] 构建输出路径解析失败（GetBuildLocation 返回空）。");
                    return;
                }

                var locationDir = Path.GetDirectoryName(locationPathName);
                if (string.IsNullOrWhiteSpace(locationDir))
                {
                    Debug.LogError($"[BuildPlayerLaunchService] 构建输出目录无效：{locationPathName}");
                    return;
                }

                if (!Directory.Exists(locationDir))
                {
                    Directory.CreateDirectory(locationDir);
                }

                EditorUserBuildSettings.SetBuildLocation(buildOptions.target, locationPathName);
                buildOptions = (BuildPlayerOptions)getBuildPlayerOptions.Invoke(null, new object[] { errBuildDir, buildOptions });
                buildOptions.locationPathName = locationPathName;

                BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(buildOptions);
            }
            catch (Exception e)
            {
                Debug.LogError($"[BuildPlayerLaunchService] 调用 Unity 内部构建 API 抛异常（可能是 Unity 版本升级导致接口变更）：\n{e}");
            }
#endif
        }

        private static string GetBuildLocation(BuildTargetGroup targetGroup, BuildTarget target, int subtarget, BuildOptions options)
        {
            var defaultFolder = GFUtility.AssetsPath.GetCombinePath(AppBuilderEditorSettings.ProjectRootPath, AppBuilderEditorSettings.Instance.GetAppBuildDir(), target.ToString());
            var defaultName = Application.productName;
            string extension = null;
            var postprocessBuildPlayerType = Utility.Assembly.GetType("UnityEditor.PostprocessBuildPlayer");
#if UNITY_6000_0_OR_NEWER
            var getExtensionMethod = postprocessBuildPlayerType?.GetMethod("GetExtensionForBuildTarget", new Type[] { typeof(BuildTarget), typeof(int), typeof(BuildOptions) });
            if (getExtensionMethod != null)
            {
                extension = getExtensionMethod.Invoke(null, new object[] { target, subtarget, options }) as string;
            }
#else
            var getExtensionMethod = postprocessBuildPlayerType?.GetMethod("GetExtensionForBuildTarget", new Type[] { typeof(BuildTargetGroup), typeof(BuildTarget), typeof(int), typeof(BuildOptions) });
            if (getExtensionMethod != null)
            {
                extension = getExtensionMethod.Invoke(null, new object[] { targetGroup, target, subtarget, options }) as string;
            }
#endif
            var buildPath = defaultFolder;
            if (!string.IsNullOrEmpty(extension))
            {
                var appFileName = Utility.Text.Format("{0}.{1}", defaultName, extension);
                buildPath = GFUtility.AssetsPath.GetCombinePath(defaultFolder, appFileName);
            }

            return buildPath;
        }
    }
}
