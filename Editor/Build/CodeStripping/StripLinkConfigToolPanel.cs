using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    public enum ConfigEditorMode
    {
        StripLinkConfig,
        AotDllConfig,
        Netstandard2NetFrameworkConfig
    }

    /// <summary>
    /// Strip LinkConfig / AOT 泛型补充 / Netstandard→NetFramework 选择面板的共享实现。
    /// 子类提供 Mode 与面板元信息（Category/Title/Order）。
    /// </summary>
    public abstract class StripLinkConfigToolPanel : ToolboxPanelBase
    {
        private AssemblySelectionListView _assemblyListView;

        protected abstract ConfigEditorMode Mode { get; }

        public override void OnShow()
        {
            _assemblyListView ??= new AssemblySelectionListView();
            RefreshListData();
        }

        public override void OnGUI(Rect position)
        {
            GUILayout.BeginArea(position);
            DrawContent();
            GUILayout.EndArea();
        }

        private void DrawContent()
        {
            EditorGUILayout.BeginVertical();
            if (_assemblyListView.Count <= 0)
            {
                EditorGUILayout.HelpBox("未找到dll,请先Build项目以生成dll.", MessageType.Warning);
                if (GUILayout.Button("生成dll"))
                {
                    HybridCLR.Editor.Commands.StripAOTDllCommand.GenerateStripedAOTDlls();
                    RefreshListData();
                }
            }
            else
            {
                EditorGUILayout.HelpBox(GetHelpMessage(), MessageType.Info);
            }

            _assemblyListView.Draw();
            EditorGUILayout.BeginHorizontal();
            var btWidth = GUILayout.Width(100);
            var btHeight = GUILayout.Height(30);
            if (GUILayout.Button("全选", btWidth, btHeight))
            {
                _assemblyListView.SetAll(true);
            }
            if (GUILayout.Button("全不选", btWidth, btHeight))
            {
                _assemblyListView.SetAll(false);
            }
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("刷新列表", btWidth, btHeight))
            {
                RefreshListData();
            }
            if (GUILayout.Button("保存", btWidth, btHeight))
            {
                if (TrySaveCurrentMode())
                {
                    EditorUtility.DisplayDialog(GetDialogTitle(), GetSuccessMessage(), "OK");
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void RefreshListData()
        {
            string[] selectedDllList;
            switch (Mode)
            {
                case ConfigEditorMode.StripLinkConfig:
                    selectedDllList = StripLinkConfigRepository.GetSelectedAssemblyDlls();
                    break;
                case ConfigEditorMode.AotDllConfig:
                    selectedDllList = StripLinkConfigRepository.GetSelectedAotDlls();
                    break;
                case ConfigEditorMode.Netstandard2NetFrameworkConfig:
                    selectedDllList = StripLinkConfigRepository.GetSelectedNetframeworkDlls();
                    break;
                default:
                    selectedDllList = null;
                    break;
            }

            _assemblyListView.Reload(StripLinkConfigRepository.GetProjectAssemblyDlls(), selectedDllList);
        }

        private string GetHelpMessage()
        {
            switch (Mode)
            {
                case ConfigEditorMode.StripLinkConfig:
                    return "勾选需要添加到Link.xml的程序集,然后点击保存生效.";
                case ConfigEditorMode.AotDllConfig:
                    return "勾选需要添加到AOT元数据补充的dll,然后点击保存生效.";
                case ConfigEditorMode.Netstandard2NetFrameworkConfig:
                    return "勾选需要转换为NetFramework的dll";
                default:
                    return string.Empty;
            }
        }

        private bool TrySaveCurrentMode()
        {
            var selectedAssemblyNames = _assemblyListView.GetSelectedAssemblyNames();
            switch (Mode)
            {
                case ConfigEditorMode.StripLinkConfig:
                    return StripLinkConfigRepository.SaveLinkConfig(selectedAssemblyNames);
                case ConfigEditorMode.AotDllConfig:
                    return StripLinkConfigRepository.SaveAotDllList(selectedAssemblyNames);
                case ConfigEditorMode.Netstandard2NetFrameworkConfig:
                    return StripLinkConfigRepository.SaveNetstandard2NetFrameworkConfig(selectedAssemblyNames);
                default:
                    return false;
            }
        }

        private string GetDialogTitle()
        {
            switch (Mode)
            {
                case ConfigEditorMode.StripLinkConfig:
                    return "Strip LinkConfig Editor";
                case ConfigEditorMode.AotDllConfig:
                    return "AOT dlls Editor";
                case ConfigEditorMode.Netstandard2NetFrameworkConfig:
                    return "Netstandard to NetFramework Editor";
                default:
                    return "Strip Link Config";
            }
        }

        private string GetSuccessMessage()
        {
            switch (Mode)
            {
                case ConfigEditorMode.StripLinkConfig:
                    return "Update link.xml success!";
                case ConfigEditorMode.AotDllConfig:
                    return "Update AOT dll List success!";
                case ConfigEditorMode.Netstandard2NetFrameworkConfig:
                    return "Update config success!";
                default:
                    return "Update config success!";
            }
        }
    }

    /// <summary>
    /// 代码裁剪配置（Strip LinkConfig）
    /// </summary>
    public sealed class StripLinkConfigPanel : StripLinkConfigToolPanel
    {
        public override string Category => "打包";
        public override string Title => "代码裁剪配置";
        public override int Order => 1;
        protected override ConfigEditorMode Mode => ConfigEditorMode.StripLinkConfig;
    }

    /// <summary>
    /// AOT 泛型补充配置
    /// </summary>
    #if ENABLE_HYBRIDCLR
    public sealed class AotDllsPanel : StripLinkConfigToolPanel
    {
        public override string Category => "热更";
        public override string Title => "AOT泛型补充配置";
        public override int Order => 3;
        protected override ConfigEditorMode Mode => ConfigEditorMode.AotDllConfig;
    }
    #endif

    /// <summary>
    /// Netstandard → NetFramework DLL 配置
    /// </summary>
    public sealed class Netstandard2NetFrameworkPanel : StripLinkConfigToolPanel
    {
        public override string Category => "热更";
        public override string Title => "Netstandard to NetFramework";
        public override int Order => 4;
        protected override ConfigEditorMode Mode => ConfigEditorMode.Netstandard2NetFrameworkConfig;
    }
}
