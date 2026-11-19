namespace UGF.EditorTools
{
    /// <summary>
    /// 可选的面板每帧更新钩子。Hub 在 EditorApplication.update 中转发（焦点下）。
    /// 用于替代 EditorWindow.Update()（如 AppBuilder 的排队构建与 pending build 执行）。
    /// </summary>
    public interface IToolboxPanelUpdate
    {
        void Update();
    }
}
