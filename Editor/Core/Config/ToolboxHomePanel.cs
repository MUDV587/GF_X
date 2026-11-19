using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    /// <summary>
    /// 工具箱首页：GF_X 工具集中入口（环境信息 + 快捷操作 + 面板接入说明）。
    /// 作为 v1 试点面板验证 Hub 的面板协议 / 注册表 / 渲染链路。
    /// </summary>
    public sealed class ToolboxHomePanel : ToolboxPanelBase
    {
        public override string Category => "入口";
        public override string Title => "GF_X 工具箱";

        // Order 保持最小，保证首页始终排在各分类最前（Hub 默认选中第一项时即首页）。
        public override int Order => -100;

        private Vector2 _scroll;
        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;

        public override void OnGUI(Rect position)
        {
            InitStyles();

            _scroll = GUI.BeginScrollView(
                position, _scroll,
                new Rect(0f, 0f, position.width - 20f, 620f));

            Rect area = new Rect(12f, 12f, position.width - 44f, 600f);
            GUILayout.BeginArea(area);

            GUILayout.Label("GF_X 工具箱", _titleStyle);
            GUILayout.Label("GF_X 框架工具集中入口 · 开发中", _subtitleStyle);
            GUILayout.Space(16f);

            // ── 环境信息
            EditorGUILayout.LabelField("环境信息", EditorStyles.boldLabel);
            DrawInfoRow("GameRoot", TryGet(() => GFRuntimeSettings.Instance.GameRoot));
            DrawInfoRow("ProjectRoot", TryGet(() => ConstEditor.ProjectRootPath));
            DrawInfoRow("包目录", TryGet(() => GameDataSettings.Instance.PackageRoot));
            DrawInfoRow("生成代码目录", TryGet(() => GameDataSettings.Instance.GeneratedCodeRoot));
            DrawInfoRow("数据表输出", TryGet(() => GameDataSettings.Instance.DataTableCodePath));
            GUILayout.Space(12f);

            // ── 快捷操作
            EditorGUILayout.LabelField("快捷操作", EditorStyles.boldLabel);
            DrawMenuButton("资源规则编辑器", "Game Framework/Resource Tools/Resource Rule Editor");
            DrawMenuButton("刷新所有数据表（Excel → 代码/数据）", "Game Framework/GameTools/Refresh All Excels【刷新所有数据表】");
            DrawMenuButton("解决 AB 资源重复依赖冗余", "Game Framework/Resource Tools/Resolve Duplicate Assets【解决AB资源重复依赖冗余】");
            DrawMenuButton("清除工程所有 Prefab 丢失脚本", "Game Framework/GameTools/Clear All Prefabs Missing Scripts【清除工程所有Prefab丢失脚本】");
            GUILayout.Space(12f);

            // ── 已入驻的工具
            EditorGUILayout.LabelField("已入驻的工具", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("App Builder · 资源规则编辑器 · 代码裁剪配置 · AOT泛型补充配置 · Netstandard to NetFramework");
            EditorGUILayout.LabelField("语言国际化扫描工具 · 压缩（优化）工具五个子面板 · 批处理工具集两个子面板");
            EditorGUILayout.LabelField("一次性菜单命令保留原菜单（Game Framework/*、Assets/GF Tools/*）");
            GUILayout.Space(16f);

            // ── 面板接入说明
            EditorGUILayout.LabelField("如何加入新面板", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("继承 ToolboxPanelBase（或实现 IToolboxPanel），");
            EditorGUILayout.LabelField("实现 Category / Title / OnGUI；可选重写 Order / OnShow / OnHide。");
            EditorGUILayout.LabelField("GF_X.Editor 程序集内会被 ToolboxPanelRegistry 反射自动收集。");

            GUILayout.EndArea();
            GUI.EndScrollView();
        }

        private void InitStyles()
        {
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 26 };
                _subtitleStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.gray } };
            }
        }

        private static string TryGet(System.Func<string> read)
        {
            try
            {
                string value = read();
                return string.IsNullOrWhiteSpace(value) ? "（未配置）" : value;
            }
            catch (System.Exception)
            {
                return "（不可用）";
            }
        }

        private static void DrawInfoRow(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(120f));
                EditorGUILayout.LabelField(value);
            }
        }

        private static void DrawMenuButton(string label, string menuPath)
        {
            if (GUILayout.Button(label))
            {
                try
                {
                    EditorApplication.ExecuteMenuItem(menuPath);
                }
                catch (System.Exception exception)
                {
                    Debug.LogError($"[Toolbox] 无法执行菜单 {menuPath}: {exception.Message}");
                }
            }
        }
    }
}
