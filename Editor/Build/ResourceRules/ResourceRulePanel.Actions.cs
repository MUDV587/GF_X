using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace UGF.EditorTools.Build.ResourceRules
{
    public partial class ResourceRulePanel : IToolboxPanelUpdate
    {
        [MenuItem("Game Framework/Resource Tools/Resource Rule Editor", false, 50)]
        public static void Open()
        {
            GFToolboxHubWindow.OpenPanel<ResourceRulePanel>();
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
#if UNITY_6000_3_OR_NEWER
            var config = EditorUtility.EntityIdToObject(instanceID) as ResourceRuleEditorData;
#else
            var config = EditorUtility.InstanceIDToObject(instanceID) as ResourceRuleEditorData;
#endif
            if (config == null)
            {
                return false;
            }

            GFToolboxHubWindow.OpenPanel<ResourceRulePanel>();
            var window = EditorWindow.GetWindow<GFToolboxHubWindow>("GF_X 工具箱");
            window.minSize = new Vector2(900f, 600f);
            ResourceRulePanel panel = window.GetPanel<ResourceRulePanel>();
            if (panel != null)
            {
                panel.OpenWithConfig(AssetDatabase.GetAssetPath(config));
            }

            return true;
        }

        /// <summary>Hub 每帧更新（焦点下）：选中资源规则资产时自动切换配置。</summary>
        public void Update()
        {
            var config = Selection.activeObject as ResourceRuleEditorData;
            if (config != null && config != _configuration)
            {
                _currentConfigPath = AssetDatabase.GetAssetPath(config);
                Load();
            }
        }

        public void RefreshResourceCollection()
        {
            if (_configuration == null)
            {
                Load();
            }

            if (ResourceRuleCompiler.RefreshResourceCollection(_configuration))
            {
                Debug.Log("Refresh ResourceCollection.xml success");
            }
            else
            {
                Debug.Log("Refresh ResourceCollection.xml fail");
            }
        }

        public void RefreshResourceCollection(string configPath)
        {
            if (_configuration == null || !_currentConfigPath.Equals(configPath))
            {
                _currentConfigPath = configPath;
                Load();
            }

            if (ResourceRuleCompiler.RefreshResourceCollection(_configuration))
            {
                Debug.Log("Refresh ResourceCollection.xml success");
            }
            else
            {
                Debug.Log("Refresh ResourceCollection.xml fail");
            }
        }
    }
}
