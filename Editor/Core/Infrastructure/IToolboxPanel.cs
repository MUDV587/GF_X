namespace UGF.EditorTools
{
    /// <summary>
    /// GF_X 工具箱面板协议。
    /// 面板的生命周期由 Hub 统一管理：选中时 <see cref="OnShow"/>，切换走/关窗时 <see cref="OnHide"/>。
    /// 面板自行负责绘制自身内容区域（<see cref="OnGUI(UnityEngine.Rect)"/>）。
    /// </summary>
    public interface IToolboxPanel
    {
        /// <summary>左侧分组名（如“资源”“打包”）。</summary>
        string Category { get; }

        /// <summary>左侧显示名。</summary>
        string Title { get; }

        /// <summary>同组内排序（从小到大）；用于同分类下的展示顺序。</summary>
        int Order { get; }

        /// <summary>面板被切换到前台（首次创建/恢复）。初始化重量资源放这里。</summary>
        void OnShow();

        /// <summary>面板被切换走或 Hub 关闭。保存设置放这里（替代 EditorWindow.OnDisable）。</summary>
        void OnHide();

        /// <summary>绘制面板内容区域。</summary>
        void OnGUI(UnityEngine.Rect position);
    }
}
