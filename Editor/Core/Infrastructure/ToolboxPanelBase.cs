namespace UGF.EditorTools
{
    /// <summary>
    /// 工具箱面板基类：提供 <see cref="Category"/>/<see cref="Title"/> 抽象与
    /// <see cref="Order"/> 默认值，并把 <see cref="OnShow"/>/<see cref="OnHide"/> 做成空实现，减少样板代码。
    /// </summary>
    public abstract class ToolboxPanelBase : IToolboxPanel
    {
        public abstract string Category { get; }
        public abstract string Title { get; }

        /// <summary>同组排序，默认 0；同分类内按 Order 升序展示。</summary>
        public virtual int Order => 0;

        public virtual void OnShow() { }
        public virtual void OnHide() { }

        public abstract void OnGUI(UnityEngine.Rect position);
    }
}
