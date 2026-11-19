using System;
using System.Reflection;
using GameFramework;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    // ── Unity 引擎内部 API（Editor 私有实现，截至 Unity 2022.3.62f1 验证可用）─────────────
    //   UnityEditor.TextureImportValidFormats.GetPlatformTextureFormatValuesAndStrings
    //   TextureImporter.GetImportWarnings（实例私有方法）
    //   UnityEditor.TextureUtil.GetStorageMemorySizeLong
    // 均无公开替代；Unity 升级后若签名/类型变化，这里会安静退回 fallback（已实现）。
    // 后续如改用公开 API 彻底摆脱内部依赖（见 UGFX-PATCHES.md）。
    internal static class TextureCompressionEditorBridge
    {
        private static MethodInfo s_GetTextureFormatOptionsMethod;
        private static MethodInfo s_GetImportWarningsMethod;
        private static MethodInfo s_GetStorageMemorySizeLongMethod;

        internal static void InitializeTextureFormatOptions(out int[] formatValues, out string[] formatDisplayOptions)
        {
            var parameters = new object[] { TextureImporterType.Sprite, EditorUserBuildSettings.activeBuildTarget, null, null };
            var getTextureFormatOptionsMethod = GetTextureFormatOptionsMethod();
            if (getTextureFormatOptionsMethod == null)
            {
                UseFallbackTextureFormats(out formatValues, out formatDisplayOptions);
                return;
            }

            try
            {
                getTextureFormatOptionsMethod.Invoke(null, parameters);
                formatValues = parameters[2] as int[];
                formatDisplayOptions = parameters[3] as string[];
            }
            catch (Exception e)
            {
                Debug.LogError($"[TextureCompressionEditorBridge] GetPlatformTextureFormatValuesAndStrings 调用失败（Unity 内部 API，可能随版本升级变化）：{e.Message}，将使用 fallback 格式列表。当前 Unity：{Application.unityVersion}");
                formatValues = null;
                formatDisplayOptions = null;
            }
            if (formatValues == null || formatDisplayOptions == null)
            {
                UseFallbackTextureFormats(out formatValues, out formatDisplayOptions);
            }
        }

        internal static bool HasImportWarnings(TextureImporter textureImporter, out string warning)
        {
            var getImportWarningsMethod = GetImportWarningsMethod(textureImporter);
            if (getImportWarningsMethod == null)
            {
                warning = string.Empty;
                return false;
            }

            try
            {
                warning = getImportWarningsMethod.Invoke(textureImporter, null) as string;
            }
            catch (Exception e)
            {
                Debug.LogError($"[TextureCompressionEditorBridge] GetImportWarnings 调用失败：{e.Message}，跳过警告检查。");
                warning = string.Empty;
            }
            return !string.IsNullOrWhiteSpace(warning);
        }

        internal static bool TryGetStorageMemorySize(Texture texture, out long storageSize)
        {
            var getStorageMemorySizeLongMethod = GetStorageMemorySizeLongMethod();
            if (getStorageMemorySizeLongMethod == null)
            {
                storageSize = 0L;
                return false;
            }

            try
            {
                storageSize = (long)getStorageMemorySizeLongMethod.Invoke(null, new object[] { texture });
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[TextureCompressionEditorBridge] GetStorageMemorySizeLong 调用失败：{e.Message}");
                storageSize = 0L;
                return false;
            }
        }

        private static MethodInfo GetTextureFormatOptionsMethod()
        {
            return s_GetTextureFormatOptionsMethod ??=
                Utility.Assembly.GetType("UnityEditor.TextureImportValidFormats")
                    ?.GetMethod("GetPlatformTextureFormatValuesAndStrings", BindingFlags.Static | BindingFlags.Public);
        }

        private static MethodInfo GetImportWarningsMethod(TextureImporter textureImporter)
        {
            return s_GetImportWarningsMethod ??=
                textureImporter.GetType().GetMethod("GetImportWarnings", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private static MethodInfo GetStorageMemorySizeLongMethod()
        {
            return s_GetStorageMemorySizeLongMethod ??=
                Utility.Assembly.GetType("UnityEditor.TextureUtil")
                    ?.GetMethod("GetStorageMemorySizeLong", BindingFlags.Public | BindingFlags.Static);
        }

        private static void UseFallbackTextureFormats(out int[] formatValues, out string[] formatDisplayOptions)
        {
            var fallbackFormats = (TextureImporterFormat[])Enum.GetValues(typeof(TextureImporterFormat));
            formatValues = Array.ConvertAll(fallbackFormats, item => (int)item);
            formatDisplayOptions = Array.ConvertAll(fallbackFormats, item => item.ToString());
        }
    }
}
