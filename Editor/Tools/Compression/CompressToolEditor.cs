using System;

namespace UGF.EditorTools
{
    /// <summary>
    /// 压缩（优化）资源工具（Hub 面板）
    /// </summary>
    public class CompressToolEditor : PersistentAssetSelectionToolEditorBase<CompressToolSubPanel>
    {
        public override string Category => "资源";
        public override string Title => "压缩(优化)工具";
        public override int Order => 2;

        protected override string ClearListButtonText => "清除列表";
        protected override bool UseRootVerticalLayout => true;
        protected override bool WrapSelectionAreaInHelpBox => false;
        protected override bool WrapSettingsAreaInHelpBox => false;
        protected override bool ShowReadmeText => true;

        protected override CompressToolSubPanel CreateSubPanelInstance(Type panelType)
        {
            var panel = Activator.CreateInstance(panelType) as CompressToolSubPanel;
            panel?.Initialize(this);
            return panel;
        }
    }
}
