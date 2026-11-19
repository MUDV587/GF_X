using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    /// <summary>
    /// GF_X 工具箱 Hub：统一入口。
    /// 布局参照 Project Settings：顶部整栏 toolbar（标题+右侧搜索）、树形可折叠导航、
    /// 图标行、整行选中高亮（聚焦蓝/失焦灰）、键盘操作、可拖动分隔条。
    /// 分隔宽度与折叠状态按工程持久化（EditorPrefs）。
    /// </summary>
    public sealed class GFToolboxHubWindow : EditorWindow
    {
        private const float NavMinWidth = 160f;
        private const float NavDefaultWidth = 220f;
        private const float NavMaxWidth = 400f;
        private const float SplitterWidth = 5f;
        private const float ToolbarHeight = 21f; // EditorStyles.toolbar.fixedHeight
        private const float RowHeight = 21f;
        private const float HeaderHeight = 21f;
        private const float IconSize = 16f;

        private const string PinnedCategory = "入口";

        // 按工程持久化（key 含 Application.dataPath，跨重启/重开窗口均生效，互不影响）
        private static string PrefsNavWidthKey => $"GF_X.Toolbox.{Application.dataPath}.NavWidth";
        private static string PrefsCollapsedKey => $"GF_X.Toolbox.{Application.dataPath}.Collapsed";

        // 内建样式在此皮肤下均解析为空壳，统一手绘（Preferences 窗口风格）。
        private static GUIStyle s_SearchFieldStyle;
        private static GUIStyle s_SearchClearStyle;

        // 工具栏搜索框图标缓存（d_search_icon 放大镜、d_clear_uielements 清除按钮）
        private static Texture2D s_SearchIconTex;
        private static GUIContent s_SearchClearContent;

        private static void EnsureStyles()
        {
            if (s_SearchFieldStyle == null)
            {
                s_SearchFieldStyle = new GUIStyle(EditorStyles.textField)
                {
                    contentOffset = new Vector2(15f, 0f),
                };
                s_SearchClearStyle = new GUIStyle(GUIStyle.none)
                {
                    alignment = TextAnchor.MiddleCenter,
                };
            }
        }

        private static void EnsureIcons()
        {
            if (s_SearchIconTex == null)
            {
                s_SearchIconTex = EditorGUIUtility.IconContent("d_search_icon")?.image as Texture2D
                    ?? EditorGUIUtility.FindTexture("d_search_icon") as Texture2D;
            }

            if (s_SearchClearContent == null)
            {
                s_SearchClearContent = new GUIContent(EditorGUIUtility.IconContent("d_clear_uielements")?.image);
            }
        }

        /// <summary>上次选中的面板类型全名（跨域重载持久化）。</summary>
        [SerializeField] private string _selectedPanelName;

        private ToolboxPanelRegistry _registry;
        private string _search = string.Empty;
        private Vector2 _navScroll;
        private IToolboxPanel _current;
        private float _navWidth = NavDefaultWidth;
        private bool _draggingSplitter;
        private HashSet<string> _collapsedCategories = new HashSet<string>(StringComparer.Ordinal);

        // ── 颜色：选中行用 Project Settings 灰色聚焦态（无论窗口是否聚焦）
        private static readonly Color k_UnfocusedSelection = new Color(0.36f, 0.36f, 0.36f, 1f);
        private static readonly Color k_Hover = new Color(1f, 1f, 1f, 0.08f);

        [MenuItem("Game Framework/GF Tools/工具箱", false, 1999)]
        public static void Open()
        {
            var window = GetWindow<GFToolboxHubWindow>("GF_X 工具箱");
            window.minSize = new Vector2(760f, 560f);
            window.Show();
        }

        /// <summary>打开 Hub 并切换到指定面板（找不到该面板时只打开 Hub）。</summary>
        public static void OpenPanel<TPanel>() where TPanel : class, IToolboxPanel
        {
            OpenPanel(typeof(TPanel));
        }

        /// <summary>打开 Hub 并切换到指定面板类型（找不到该面板时只打开 Hub）。</summary>
        public static void OpenPanel(Type panelType)
        {
            var window = GetWindow<GFToolboxHubWindow>("GF_X 工具箱");
            window.minSize = new Vector2(760f, 560f);
            window.Show();
            window.SelectPanelByType(panelType);
        }

        /// <summary>按类型获取已注册面板实例（未注册返回 null）。</summary>
        public TPanel GetPanel<TPanel>() where TPanel : class, IToolboxPanel
        {
            return _registry.Get(typeof(TPanel)) as TPanel;
        }

        private void OnEnable()
        {
            _registry = new ToolboxPanelRegistry();
            titleContent = new GUIContent("GF_X 工具箱");
            minSize = new Vector2(760f, 560f);
            _navWidth = EditorPrefs.GetFloat(PrefsNavWidthKey, NavDefaultWidth);
            LoadCollapsed();
            EditorApplication.update += RepaintWhileFocused;
            RestoreSelection();
            foreach (var g in GroupPanels(false))
            {
                Debug.Log($"[Toolbox] on-open group: {g.Category} pinned={g.IsPinned} panels={g.Panels.Count}");
            }
        }

        private void OnDisable()
        {
            EditorApplication.update -= RepaintWhileFocused;
            _registry?.HideAll();
            _current = null;
        }

        private void RepaintWhileFocused()
        {
            if (focusedWindow == this)
            {
                // 转发可选的每帧更新（替代 EditorWindow.Update()，如 AppBuilder 的排队构建）。
                if (_current is IToolboxPanelUpdate updatable)
                {
                    updatable.Update();
                }

                Repaint();
            }
        }

        private void OnGUI()
        {
            EnsureStyles();

            if (focusedWindow == this && Event.current.type == EventType.KeyDown && !EditorGUIUtility.editingTextField)
            {
                HandleKeyDown(Event.current);
            }

            EnsureCurrentPanel();

            _navWidth = Mathf.Clamp(_navWidth, NavMinWidth, NavMaxWidth);

            DrawToolbar();

            float contentY = ToolbarHeight;
            float contentHeight = Mathf.Max(0f, position.height - ToolbarHeight);

            Rect left = new Rect(0f, contentY, _navWidth, contentHeight);
            Rect splitter = new Rect(_navWidth, contentY, SplitterWidth, contentHeight);
            Rect right = new Rect(_navWidth + SplitterWidth, contentY, Mathf.Max(0f, position.width - _navWidth - SplitterWidth), contentHeight);

            DrawNavigation(left);
            DrawSplitter(splitter);
            DrawContent(right);
        }

        // ───────────────────────── 顶部工具栏（仿 Project Settings） ─────────────────────────

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(ToolbarHeight));
            GUILayout.FlexibleSpace();

            // Preferences 风格搜索框：宽 300、高 15，上下/右 2px 边距；放大镜=d_search_icon
            const float SearchWidth = 300f;
            const float SearchHeight = 15f;
            Rect searchRect = GUILayoutUtility.GetRect(SearchWidth + 2f, ToolbarHeight, GUILayout.Width(SearchWidth + 2f), GUILayout.Height(ToolbarHeight));
            Rect fieldRect = new Rect(searchRect.x, searchRect.y + (ToolbarHeight - SearchHeight) * 0.5f, SearchWidth, SearchHeight);

            EnsureIcons();
            _search = GUI.TextField(fieldRect, _search ?? string.Empty, s_SearchFieldStyle);

            GUI.DrawTexture(
                new Rect(fieldRect.x + 2f, fieldRect.y + (fieldRect.height - 11f) * 0.5f, 11f, 11f),
                s_SearchIconTex,
                ScaleMode.ScaleToFit,
                true);

            if (!string.IsNullOrEmpty(_search))
            {
                Rect cancelRect = new Rect(fieldRect.xMax - 14f, fieldRect.y + (fieldRect.height - 13f) * 0.5f, 13f, 13f);
                if (GUI.Button(cancelRect, s_SearchClearContent, s_SearchClearStyle))
                {
                    _search = string.Empty;
                    GUI.FocusControl(null);
                }
            }

            GUILayout.EndHorizontal();
        }

        // ───────────────────────── 左栏树形导航 ─────────────────────────

        private void DrawNavigation(Rect area)
        {
            if (area.width <= 0f || area.height <= 0f)
            {
                return;
            }

            GUI.Box(area, GUIContent.none);
            Rect inner = new Rect(area.x + 2f, area.y + 1f, area.width - 4f, area.height - 2f);
            GUILayout.BeginArea(inner);

            bool searching = (_search ?? string.Empty).Trim().Length > 0;
            bool windowFocused = focusedWindow == this;

            _navScroll = EditorGUILayout.BeginScrollView(_navScroll, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

            foreach (var group in GroupPanels(searching))
            {
                if (group.IsPinned)
                {
                    // "入口"不作为分组，直接作为平铺项显示
                    foreach (IToolboxPanel panel in group.Panels)
                    {
                        DrawItem(panel, windowFocused);
                    }

                    continue;
                }

                bool collapsed = !searching && _collapsedCategories.Contains(group.Category);
                DrawGroupHeader(group.Category, collapsed, group.Panels.Count);
                if (collapsed)
                {
                    continue;
                }

                foreach (IToolboxPanel panel in group.Panels)
                {
                    DrawItem(panel, windowFocused);
                }
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private sealed class PanelGroup
        {
            public string Category;
            public bool IsPinned;
            public IReadOnlyList<IToolboxPanel> Panels;
        }

        /// <summary>按 Category 分组并保持稳定顺序：入口置顶，其余 ordinal 排序。</summary>
        private List<PanelGroup> GroupPanels(bool searching)
        {
            var result = new List<PanelGroup>();
            string keyword = (_search ?? string.Empty).Trim();

            var byCategory = new SortedDictionary<string, List<IToolboxPanel>>(StringComparer.Ordinal);
            foreach (IToolboxPanel panel in _registry.Panels)
            {
                if (keyword.Length > 0
                    && panel.Title.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) < 0
                    && panel.Category.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (!byCategory.TryGetValue(panel.Category, out List<IToolboxPanel> list))
                {
                    list = new List<IToolboxPanel>();
                    byCategory[panel.Category] = list;
                }

                list.Add(panel);
            }

            if (byCategory.TryGetValue(PinnedCategory, out List<IToolboxPanel> pinned))
            {
                result.Add(new PanelGroup { Category = PinnedCategory, IsPinned = true, Panels = pinned });
                byCategory.Remove(PinnedCategory);
            }

            foreach (var pair in byCategory)
            {
                result.Add(new PanelGroup { Category = pair.Key, IsPinned = false, Panels = pair.Value });
            }

            return result;
        }

        /// <summary>当前可见的面板顺序（键盘导航用）。</summary>
        private List<IToolboxPanel> VisiblePanels()
        {
            var result = new List<IToolboxPanel>();
            bool searching = (_search ?? string.Empty).Trim().Length > 0;
            foreach (var group in GroupPanels(searching))
            {
                if (group.IsPinned)
                {
                    result.AddRange(group.Panels);
                    continue;
                }

                if (!searching && _collapsedCategories.Contains(group.Category))
                {
                    continue;
                }

                result.AddRange(group.Panels);
            }

            return result;
        }

        private static readonly Dictionary<string, string> s_GroupIconNames = new Dictionary<string, string>
        {
            ["打包"] = "d_BuildSettings.Standalone",
            ["热更"] = "d_Package Manager",
            ["资源"] = "d_Folder Icon",
        };

        private static GUIContent GetGroupIcon(string category)
        {
            if (s_GroupIconNames.TryGetValue(category, out string name))
            {
                GUIContent content = EditorGUIUtility.IconContent(name);
                if (content?.image != null)
                {
                    return content;
                }
            }

            return EditorGUIUtility.IconContent("d_Folder Icon");
        }

        private void DrawGroupHeader(string category, bool collapsed, int count)
        {
            Rect row = GUILayoutUtility.GetRect(0f, HeaderHeight, GUILayout.ExpandWidth(true));
            bool hover = row.Contains(Event.current.mousePosition);
            if (hover)
            {
                EditorGUI.DrawRect(row, k_Hover);
            }

            Rect triangleRect = new Rect(row.x + 2f, row.y + (row.height - 12f) * 0.5f, 12f, 12f);
            GUI.DrawTexture(triangleRect, GetFoldoutTriangle(!collapsed));

            Rect iconRect = new Rect(row.x + 16f, row.y + (row.height - IconSize) * 0.5f, IconSize, IconSize);
            GUIContent iconContent = GetGroupIcon(category);
            if (iconContent?.image != null)
            {
                GUI.DrawTexture(iconRect, iconContent.image);
            }

            Rect labelRect = new Rect(row.x + 16f + IconSize + 4f, row.y, row.width - 16f - IconSize - 4f, row.height);
            var style = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
            GUI.Label(labelRect, category, style);

            if (Event.current.type == EventType.MouseDown && row.Contains(Event.current.mousePosition))
            {
                ToggleCollapsed(category);
                Event.current.Use();
            }
        }

        private void DrawItem(IToolboxPanel panel, bool windowFocused)
        {
            Rect row = GUILayoutUtility.GetRect(0f, RowHeight, GUILayout.ExpandWidth(true));

            bool selected = ReferenceEquals(panel, _current);
            bool hover = row.Contains(Event.current.mousePosition);
            if (selected)
            {
                EditorGUI.DrawRect(row, k_UnfocusedSelection);
            }
            else if (hover)
            {
                EditorGUI.DrawRect(row, k_Hover);
            }

            Rect indent = new Rect(row.x + 34f, row.y, row.width - 34f, row.height);

            Rect iconRect = new Rect(indent.x + 3f, indent.y + (row.height - IconSize) * 0.5f, IconSize, IconSize);
            GUIContent iconContent = GetPanelIcon(panel);
            if (iconContent?.image != null)
            {
                GUI.DrawTexture(iconRect, iconContent.image);
            }

            Rect textRect = new Rect(indent.x + 3f + IconSize + 3f, indent.y, indent.width - IconSize - 6f, indent.height);
            var textStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };
            GUI.Label(textRect, panel.Title, textStyle);

            if (Event.current.type == EventType.MouseDown && row.Contains(Event.current.mousePosition))
            {
                SelectPanel(panel);
                Event.current.Use();
            }
        }

        private static GUIContent GetPanelIcon(IToolboxPanel panel)
        {
            string iconName = ToolboxPanelRegistry.IconNameOf(panel);
            GUIContent content = EditorGUIUtility.IconContent(iconName);
            if (content?.image == null)
            {
                content = EditorGUIUtility.IconContent("d_Folder Icon");
            }

            return content;
        }

        private void ToggleCollapsed(string category)
        {
            if (!_collapsedCategories.Add(category))
            {
                _collapsedCategories.Remove(category);
            }

            SaveCollapsed();
        }

        private void LoadCollapsed()
        {
            _collapsedCategories = new HashSet<string>(StringComparer.Ordinal);
            string saved = EditorPrefs.GetString(PrefsCollapsedKey, string.Empty);
            foreach (string entry in saved.Split('\n'))
            {
                if (!string.IsNullOrWhiteSpace(entry))
                {
                    _collapsedCategories.Add(entry);
                }
            }
        }

        private void SaveCollapsed()
        {
            EditorPrefs.SetString(PrefsCollapsedKey, string.Join("\n", _collapsedCategories));
        }

        // ───────────────────────── 键盘操作 ─────────────────────────

        private void HandleKeyDown(Event current)
        {
            switch (current.keyCode)
            {
                case KeyCode.UpArrow:
                    MoveSelection(-1);
                    current.Use();
                    break;
                case KeyCode.DownArrow:
                    MoveSelection(1);
                    current.Use();
                    break;
                case KeyCode.PageUp:
                    MoveSelection(-10);
                    current.Use();
                    break;
                case KeyCode.PageDown:
                    MoveSelection(10);
                    current.Use();
                    break;
                case KeyCode.Home:
                    SelectAt(0);
                    current.Use();
                    break;
                case KeyCode.End:
                    SelectAt(VisiblePanels().Count - 1);
                    current.Use();
                    break;
                case KeyCode.LeftArrow:
                    SetCurrentGroupCollapsed(true);
                    current.Use();
                    break;
                case KeyCode.RightArrow:
                    SetCurrentGroupCollapsed(false);
                    current.Use();
                    break;
                case KeyCode.Escape:
                    if (!string.IsNullOrEmpty(_search))
                    {
                        _search = string.Empty;
                        current.Use();
                    }
                    break;
            }
        }

        private void MoveSelection(int offset)
        {
            var visible = VisiblePanels();
            if (visible.Count == 0)
            {
                return;
            }

            int index = visible.FindIndex(p => ReferenceEquals(p, _current));
            if (index < 0)
            {
                index = 0;
            }
            else
            {
                index = Mathf.Clamp(index + offset, 0, visible.Count - 1);
            }

            SelectPanel(visible[index]);
        }

        private void SelectAt(int index)
        {
            var visible = VisiblePanels();
            if (visible.Count == 0 || index < 0 || index >= visible.Count)
            {
                return;
            }

            SelectPanel(visible[index]);
        }

        private void SetCurrentGroupCollapsed(bool collapsed)
        {
            if (_current == null)
            {
                return;
            }

            string category = _current.Category;
            if (category == PinnedCategory)
            {
                return; // "入口"固定展开，不参与折叠
            }

            bool contains = _collapsedCategories.Contains(category);
            if (collapsed == contains)
            {
                return;
            }

            ToggleCollapsed(category);
        }

        // ───────────────────────── 分隔条 ─────────────────────────

        private void DrawSplitter(Rect area)
        {
            EditorGUIUtility.AddCursorRect(area, MouseCursor.ResizeHorizontal);

            Vector2 mousePosition = Event.current.mousePosition;
            if (Event.current.type == EventType.MouseDown && area.Contains(mousePosition))
            {
                _draggingSplitter = true;
                Event.current.Use();
            }

            if (_draggingSplitter)
            {
                if (Event.current.type == EventType.MouseDrag)
                {
                    _navWidth = Mathf.Clamp(mousePosition.x, NavMinWidth, NavMaxWidth);
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    _navWidth = Mathf.Clamp(mousePosition.x, NavMinWidth, NavMaxWidth);
                    EditorPrefs.SetFloat(PrefsNavWidthKey, _navWidth);
                    _draggingSplitter = false;
                    Event.current.Use();
                }
            }

            // 双击复位到默认宽度
            if (Event.current.type == EventType.MouseDown && area.Contains(mousePosition) && Event.current.clickCount == 2)
            {
                _navWidth = NavDefaultWidth;
                EditorPrefs.SetFloat(PrefsNavWidthKey, _navWidth);
                Event.current.Use();
            }

            // 分隔线 + 3 点 gripper
            EditorGUI.DrawRect(new Rect(area.x + area.width * 0.5f - 1f, area.y, 2f, area.height), new Color(0f, 0f, 0f, 0.35f));
            float gripY = area.y + area.height * 0.5f;
            for (int i = -1; i <= 1; i++)
            {
                EditorGUI.DrawRect(new Rect(area.x + area.width * 0.5f - 1.5f, gripY + i * 6f - 1.5f, 3f, 3f), new Color(1f, 1f, 1f, 0.45f));
            }
        }

        private static Texture2D s_TriangleClosed;
        private static Texture2D s_TriangleOpen;

        private static Texture2D GetFoldoutTriangle(bool open)
        {
            if (open ? s_TriangleOpen != null : s_TriangleClosed != null)
            {
                return open ? s_TriangleOpen : s_TriangleClosed;
            }

            const int Size = 16;
            // Unity 折叠三角的顶点（指向右=收起，指向下=展开）
            Vector2[] vertices = open
                ? new[] { new Vector2(3f, 5f), new Vector2(13f, 5f), new Vector2(8f, 13f) }
                : new[] { new Vector2(4f, 3f), new Vector2(4f, 13f), new Vector2(13f, 8f) };

            var tex = new Texture2D(Size, Size, TextureFormat.ARGB32, false);
            var pixels = new Color32[Size * Size];
            Color32 fill = new Color32(213, 213, 213, 255);   // 近白
            Color32 edge = new Color32(38, 38, 38, 255);      // 描边深色
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, Size - 1 - y - 0.5f); // IMGUI 行内坐标翻转
                    float inside = SignOfTriangle(p, vertices[0], vertices[1], vertices[2]);
                    if (inside < 0f) { pixels[y * Size + x] = new Color32(0, 0, 0, 0); continue; }
                    float edgeDist = EdgeDistance(p, vertices[0], vertices[1], vertices[2]);
                    pixels[y * Size + x] = edgeDist > 1.35f ? fill : edge;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            if (open) { s_TriangleOpen = tex; } else { s_TriangleClosed = tex; }
            return tex;
        }

        /// <summary>点是否在三角形内（同正方向=正，Winding 决定符号）。</summary>
        private static float SignOfTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float s1 = Cross(p, a, b);
            float s2 = Cross(p, b, c);
            float s3 = Cross(p, c, a);
            bool hasNeg = s1 < 0f || s2 < 0f || s3 < 0f;
            bool hasPos = s1 > 0f || s2 > 0f || s3 > 0f;
            return (hasNeg && hasPos) ? -1f : 1f;
        }

        private static float Cross(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p3.x - p2.x) * (p1.y - p2.y) - (p3.y - p2.y) * (p1.x - p2.x);
        }

        private static float PointToSegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Vector2.Dot(p - a, ab) / Vector2.Dot(ab, ab);
            t = Mathf.Clamp01(t);
            Vector2 closest = a + ab * t;
            return Vector2.Distance(p, closest);
        }

        private static float EdgeDistance(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d0 = PointToSegmentDistance(p, a, b);
            float d1 = PointToSegmentDistance(p, b, c);
            float d2 = PointToSegmentDistance(p, c, a);
            return Mathf.Min(d0, Mathf.Min(d1, d2));
        }

        // ───────────────────────── 右栏宿主 ─────────────────────────

        private void DrawContent(Rect area)
        {
            if (_current == null)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.LabelField(area, "（没有可用的面板）", EditorStyles.centeredGreyMiniLabel);
                }
                return;
            }

            _current.OnGUI(area);
        }

        // ───────────────────────── 选择/生命周期 ─────────────────────────

        private void RestoreSelection()
        {
            if (!string.IsNullOrEmpty(_selectedPanelName))
            {
                Type saved = Type.GetType(_selectedPanelName);
                IToolboxPanel panel = saved != null ? _registry.Get(saved) : null;
                if (panel != null)
                {
                    SelectPanel(panel);
                    return;
                }
            }

            // 默认选中首页（Category=入口），否则排序第一项。
            IToolboxPanel home = null;
            foreach (IToolboxPanel panel in _registry.Panels)
            {
                if (panel.Category == PinnedCategory)
                {
                    home = panel;
                    break;
                }
            }

            SelectPanel(home ?? (_registry.Panels.Count > 0 ? _registry.Panels[0] : null));
        }

        private void EnsureCurrentPanel()
        {
            // domain reload / 搜索不改变当前面板，仅保证非空。
            if (_current == null)
            {
                RestoreSelection();
            }
        }

        private void SelectPanelByType(Type panelType)
        {
            IToolboxPanel panel = _registry.Get(panelType);
            if (panel != null)
            {
                SelectPanel(panel);
            }
        }

        private void SelectPanel(IToolboxPanel next)
        {
            if (ReferenceEquals(_current, next))
            {
                return;
            }

            IToolboxPanel previous = _current;
            _current = next;
            _selectedPanelName = next?.GetType().FullName;

            try
            {
                previous?.OnHide();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Toolbox] 面板 {previous.GetType().FullName}.OnHide() 抛出异常: {exception}");
            }

            if (next != null)
            {
                try
                {
                    next.OnShow();
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[Toolbox] 面板 {next.GetType().FullName}.OnShow() 抛出异常: {exception}");
                }
            }
        }
    }
}
