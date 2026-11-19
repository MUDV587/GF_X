using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace UGF.EditorTools
{
    /// <summary>
    /// 工具箱面板注册表：反射扫描 Editor 程序集，收集所有 <see cref="IToolboxPanel"/> 实现，
    /// 按 Category -> Order -> Title（序数比较）排序并缓存实例。
    /// 面板在注册表构建时即实例化（面板都是轻量 IMGUI 组件，代价可忽略），避免排序期重复构造。
    /// </summary>
    public sealed class ToolboxPanelRegistry
    {
        private readonly List<IToolboxPanel> _panels = new List<IToolboxPanel>();
        private readonly Dictionary<Type, IToolboxPanel> _byType = new Dictionary<Type, IToolboxPanel>();

        /// <summary>全部面板实例（已按 Category -> Order -> Title 排序）。</summary>
        public IReadOnlyList<IToolboxPanel> Panels => _panels;

        /// <summary>按类型索引；未找到返回 -1。</summary>
        public int IndexOf(Type panelType) => _panels.FindIndex(p => p.GetType() == panelType);

        public ToolboxPanelRegistry()
        {
            Scan();
        }

        private void Scan()
        {
            // 用 TypeCache 扫全部 Editor 装载的程序集，并要求其子类注册为工具箱面板。
            TypeCache.TypeCollection allPanelTypes = TypeCache.GetTypesDerivedFrom<IToolboxPanel>();
            foreach (Type type in allPanelTypes)
            {
                if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
                {
                    continue;
                }

                try
                {
                    var panel = (IToolboxPanel)Activator.CreateInstance(type);
                    _panels.Add(panel);
                    _byType[type] = panel;
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogError($"[Toolbox] 面板 {type.FullName} 实例化失败，已跳过: {exception}");
                }
            }

            _panels.Sort(Compare);
        }

        /// <summary>按 Category、Order、Title 排序（序数比较，保证排序稳定可预期）。</summary>
        private static int Compare(IToolboxPanel left, IToolboxPanel right)
        {
            int category = string.CompareOrdinal(left.Category, right.Category);
            if (category != 0)
            {
                return category;
            }

            int order = left.Order.CompareTo(right.Order);
            if (order != 0)
            {
                return order;
            }

            return string.CompareOrdinal(left.Title, right.Title);
        }

        /// <summary>按类型取面板实例；未注册返回 null。</summary>
        public IToolboxPanel Get(Type panelType)
        {
            return _byType.TryGetValue(panelType, out IToolboxPanel panel) ? panel : null;
        }

        /// <summary>全部实例的 OnHide（Hub 关闭时统一调用）。</summary>
        public void HideAll()
        {
            foreach (IToolboxPanel panel in _panels)
            {
                try
                {
                    panel.OnHide();
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogError($"[Toolbox] 面板 {panel.GetType().FullName}.OnHide() 抛出异常: {exception}");
                }
            }
        }

        // 图标按面板类型集中映射（均为运行时已探测存在的内建图标名）。
        // 用类型名做 key，避免在工具程序集拆分后硬引用各工具具体类型。
        private static readonly Dictionary<string, string> s_IconNames = new Dictionary<string, string>
        {
            ["ToolboxHomePanel"] = "d_Settings",
            ["AppBuilderPanel"] = "d_BuildSettings.Standalone",
            ["StripLinkConfigPanel"] = "d_FilterSelectedOnly",
            ["AotDllsPanel"] = "d_Package Manager",
            ["Netstandard2NetFrameworkPanel"] = "d_RotateTool",
            ["ResourceRulePanel"] = "d_FilterByType",
            ["CompressToolEditor"] = "d_PreMatSphere",
            ["LocalizationScannerPanel"] = "d_Folder Icon",
            ["BatchOperateToolEditor"] = "d_Prefab Icon",
        };

        /// <summary>面板图标名（内建图标名，由调用方探测；失败回退 d_Folder Icon）。</summary>
        public static string IconNameOf(IToolboxPanel panel)
        {
            return s_IconNames.TryGetValue(panel.GetType().Name, out string name) ? name : "d_Folder Icon";
        }
    }
}
