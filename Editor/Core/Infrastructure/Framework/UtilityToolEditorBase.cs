using System;

namespace UGF.EditorTools
{
    /// <summary>
    /// 批处理操作工具
    /// </summary>
    public abstract class UtilityToolEditorBase : PersistentAssetSelectionToolEditorBase<UtilitySubToolBase>
    {
        protected override UtilitySubToolBase CreateSubPanelInstance(Type panelType)
        {
            var panel = Activator.CreateInstance(panelType) as UtilitySubToolBase;
            panel?.Initialize(this);
            return panel;
        }
    }
}
