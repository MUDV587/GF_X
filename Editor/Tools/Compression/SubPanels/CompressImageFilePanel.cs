using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    [EditorToolMenu("图片文件压缩", typeof(CompressToolEditor), 1)]
    public class CompressImageFilePanel : CompressToolSubPanel
    {
        public override string AssetSelectorTypeFilter => "t:sprite t:texture2d t:folder";
        public override string DragAreaTips => "拖拽到此处添加文件夹或 PNG";
        public override string ReadmeText => "使用本地 pngquant 压缩 PNG 图片";

        private readonly string[] mSupportAssetFormats = { ".png" };
        private readonly Type[] mSupportAssetTypes = { typeof(Sprite), typeof(Texture), typeof(Texture2D) };
        protected override Type[] SupportAssetTypes => mSupportAssetTypes;

        public override bool IsSupportAsset(string assetPath)
        {
            var format = Path.GetExtension(assetPath);
            return Array.Exists(mSupportAssetFormats, item => string.Equals(item, format, StringComparison.OrdinalIgnoreCase));
        }
        public override void DrawBottomButtonsPanel()
        {
            EditorGUILayout.BeginHorizontal("box");
            {
                if (GUILayout.Button("开始压缩", GUILayout.Height(30)))
                {
                    StartCompress();
                }
                if (GUILayout.Button("备份图片", GUILayout.Height(30), GUILayout.MaxWidth(100)))
                {
                    BackupImages();
                }
                if (GUILayout.Button("还原备份", GUILayout.Height(30), GUILayout.MaxWidth(100)))
                {
                    RecoveryImages();
                }
                if (GUILayout.Button("保存设置", GUILayout.Height(30), GUILayout.MaxWidth(100)))
                {
                    CompressionSettings.Save();
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        public override void DrawSettingsPanel()
        {
            EditorGUILayout.BeginHorizontal("box");
            {
                CompressionSettings.Instance.CompressImgToolCoverRaw = EditorGUILayout.ToggleLeft("覆盖原图片", CompressionSettings.Instance.CompressImgToolCoverRaw, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.MinMaxSlider(Utility.Text.Format("压缩质量({0}%-{1}%)", (int)CompressionSettings.Instance.CompressImgToolQualityMinLv, (int)CompressionSettings.Instance.CompressImgToolQualityLv), ref CompressionSettings.Instance.CompressImgToolQualityMinLv, ref CompressionSettings.Instance.CompressImgToolQualityLv, 0, 100);

                CompressionSettings.Instance.CompressImgToolFastLv = EditorGUILayout.IntSlider(Utility.Text.Format("快压等级({0})", CompressionSettings.Instance.CompressImgToolFastLv), CompressionSettings.Instance.CompressImgToolFastLv, 1, 10);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.BeginHorizontal("box");
            {
                EditorGUI.BeginDisabledGroup(CompressionSettings.Instance.CompressImgToolCoverRaw);
                {
                    EditorGUILayout.LabelField("输出路径:", GUILayout.Width(80));
                    EditorGUILayout.SelectableLabel(CompressionSettings.Instance.CompressImgToolOutputDir, EditorStyles.selectionRect, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("选择", GUILayout.Width(80)))
                    {
                        var backupPath = EditorDialogUtility.OpenRelativeFolderPanel("选择图片输出路径", CompressionSettings.Instance.CompressImgToolOutputDir);
                        CompressionSettings.Instance.CompressImgToolOutputDir = backupPath;
                        CompressionSettings.Save();
                        CompressionSettings.Save();
                        GUIUtility.ExitGUI();
                    }
                    if (GUILayout.Button("打开", GUILayout.Width(80)))
                    {
                        EditorUtility.RevealInFinder(Path.Combine(ConstEditor.ProjectRootPath, CompressionSettings.Instance.CompressImgToolOutputDir));
                    }
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal("box");
            {
                EditorGUILayout.LabelField("备份路径:", GUILayout.Width(80));
                EditorGUILayout.SelectableLabel(CompressionSettings.Instance.CompressImgToolBackupDir, EditorStyles.selectionRect, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.ExpandWidth(true));
                if (GUILayout.Button("选择", GUILayout.Width(80)))
                {
                    var backupPath = EditorDialogUtility.OpenRelativeFolderPanel("选择备份路径", CompressionSettings.Instance.CompressImgToolBackupDir);

                    CompressionSettings.Instance.CompressImgToolBackupDir = backupPath;
                    CompressionSettings.Save();
                    CompressionSettings.Save();
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("打开", GUILayout.Width(80)))
                {
                    EditorUtility.RevealInFinder(Path.Combine(ConstEditor.ProjectRootPath, CompressionSettings.Instance.CompressImgToolBackupDir));
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void StartCompress()
        {
            if (!CompressionSettings.Instance.CompressImgToolCoverRaw && string.IsNullOrWhiteSpace(CompressionSettings.Instance.CompressImgToolOutputDir))
            {
                EditorUtility.DisplayDialog("错误", "图片输出路径无效!", "OK");
                return;
            }
            var imgList = GetSelectedAssets();
            CompressImages(imgList);
        }
        private void BackupImages()
        {
            var itmList = GetSelectedAssets();
            int totalImgCount = itmList.Count;
            if (0 != EditorUtility.DisplayDialogComplex("提示", $"确认开始备份已选 {totalImgCount} 张图片吗?", "确定备份", "取消", null))
            {
                return;
            }
            var projectRoot = ConstEditor.ProjectRootPath;
            var backupDir = GFUtility.AssetsPath.GetCombinePath(projectRoot, CompressionSettings.Instance.CompressImgToolBackupDir);

            if (string.IsNullOrWhiteSpace(CompressionSettings.Instance.CompressImgToolBackupDir))
            {
                EditorUtility.DisplayDialog("错误", $"当前选择的备份路径无效:{Environment.NewLine}{CompressionSettings.Instance.CompressImgToolBackupDir}", "OK");
                return;
            }
            var backupPath = GFUtility.AssetsPath.GetCombinePath(backupDir, DateTime.Now.ToString("yyyy-MM-dd-HHmmss"));
            int successCount = ImageFileBackupService.Backup(itmList, projectRoot, backupPath);

            if (0 == EditorUtility.DisplayDialogComplex("备份结束", $"共 {totalImgCount} 张图片{Environment.NewLine}成功备份  {successCount} 张{Environment.NewLine}备份失败 {totalImgCount - successCount} 张", "打开备份目录", "关闭", null))
            {
                EditorUtility.RevealInFinder(backupPath);
                GUIUtility.ExitGUI();
            }
        }
        private void RecoveryImages()
        {
            var projectRoot = ConstEditor.ProjectRootPath;
            var backupRoot = GFUtility.AssetsPath.GetCombinePath(projectRoot, CompressionSettings.Instance.CompressImgToolBackupDir);
            if (!Directory.Exists(backupRoot))
            {
                EditorUtility.DisplayDialog("提示", $"备份路径不存在:{backupRoot}", "OK");
                return;
            }
            var backupItems = Directory.GetDirectories(backupRoot, "*", SearchOption.TopDirectoryOnly);
            if (backupItems.Length < 1)
            {
                EditorUtility.DisplayDialog("提示", "没有备份记录", "OK");
                return;
            }
            var contents = new GUIContent[backupItems.Length];

            for (int i = 0; i < backupItems.Length; i++)
            {
                var item = Path.GetRelativePath(backupRoot, backupItems[i]);
                contents[i] = new GUIContent(item);
            }
            var dialogRect = new Rect(UnityEngine.Event.current.mousePosition, Vector2.zero);

            EditorUtility.DisplayCustomMenu(dialogRect, contents, -1, (object userData, string[] options, int selected) =>
            {
                string backupName = options[selected];
                if (0 != EditorUtility.DisplayDialogComplex("还原备份", $"是否还原此备份:[{backupName}]?", "还原备份", "取消", null))
                {
                    return;
                }
                var recoveryDir = GFUtility.AssetsPath.GetCombinePath(backupRoot, backupName);
                var imgList = GetAllBackupFilesByDir(recoveryDir, recoveryDir);
                int successCount = ImageFileBackupService.Restore(imgList, recoveryDir, projectRoot);
                EditorUtility.DisplayDialog("还原备份结束", $"共 {imgList.Count} 张图片{Environment.NewLine}成功还原 {successCount} 张{Environment.NewLine}还原失败 {imgList.Count - successCount} 张", "OK");
                AssetDatabase.Refresh();
            }, null);
        }
        private void CompressImages(List<string> imgList)
        {
            if (imgList.Count < 1) return;
            int clickBtIdx = EditorUtility.DisplayDialogComplex("请确认", Utility.Text.Format("共 {0} 张图片待压缩, 是否开始压缩?", imgList.Count), "开始压缩", "取消", null);
            if (clickBtIdx != 0)
            {
                //用户取消压缩
                return;
            }

            imgList.Reverse();

            var rootPath = ConstEditor.ProjectRootPath;
            string outputPath;
            if (CompressionSettings.Instance.CompressImgToolCoverRaw)
            {
                outputPath = rootPath;
            }
            else
            {
                outputPath = Path.GetFullPath(CompressionSettings.Instance.CompressImgToolOutputDir, rootPath);
            }

            if (!Directory.Exists(outputPath))
            {
                try
                {
                    Directory.CreateDirectory(outputPath);
                }
                catch (Exception)
                {
                    EditorUtility.DisplayDialog("错误", Utility.Text.Format("创建路径失败,请检查路径是否有效:{0}", outputPath), "OK");
                    return;
                }
            }

            try
            {
                var failedAssets = ImageFileCompressionRunner.Compress(imgList, rootPath, outputPath);
                OnCompressCompleted(failedAssets);
            }
            catch (Exception exception)
            {
                Debug.LogError($"图片压缩失败: {exception}");
                EditorUtility.ClearProgressBar();
            }
        }

        private void OnCompressCompleted(List<string> imgList)
        {
            if (imgList.Count <= 0)
            {
                EditorUtility.DisplayDialog("压缩完成!", "全部文件已压缩完成", "OK");
                return;
            }
            //提示是否再次压缩所有失败的图片
            var clickBtIdx = EditorUtility.DisplayDialogComplex("警告", Utility.Text.Format("有 {0} 张图片压缩失败, 是否继续压缩?", imgList.Count), "继续压缩", "取消", null);
            if (clickBtIdx == 0)
            {
                CompressImages(imgList);
            }
        }
    }
}

