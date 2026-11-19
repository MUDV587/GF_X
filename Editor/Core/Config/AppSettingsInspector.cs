using UnityEditor;
using UnityEditor.SceneManagement;

namespace UGF.EditorTools
{
    [CustomEditor(typeof(AppSettings))]
    public class AppSettingsInspector : UnityEditor.Editor
    {
        private GUIContent designResolutionContent;
        private GUIContent designResolutionBtnContent;

        private void OnEnable()
        {
            designResolutionContent = new GUIContent("UI Design Resolution", "UI设计分辨率:");
            designResolutionBtnContent = new GUIContent("Conform", "确认修改");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
            EditorGUILayout.BeginHorizontal();
            {
                AppSettings.Instance.DesignResolution = EditorGUILayout.Vector2IntField(designResolutionContent, AppSettings.Instance.DesignResolution);
                if (GUILayout.Button(designResolutionBtnContent, GUILayout.Width(100)))
                {
                    SetDesignResolution(AppSettings.Instance.DesignResolution);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.EndDisabledGroup();
            serializedObject.ApplyModifiedProperties();
        }

        private static void SetDesignResolution(Vector2Int designResolution)
        {
            EditorUtility.SetDirty(AppSettings.Instance);
            var launchSceneName = GFUtility.AssetsPath.GetScenePath("Launch");
            var currentOpenScene = EditorSceneManager.GetActiveScene();
            if (currentOpenScene != null && currentOpenScene.isDirty)
            {
                int opIndex = EditorUtility.DisplayDialogComplex("警告", $"当前场景{currentOpenScene.name}未保存,是否保存?", "保存", "取消", "不保存");
                switch (opIndex)
                {
                    case 0:
                        if (!EditorSceneManager.SaveOpenScenes())
                            return;
                        break;
                    case 1:
                        return;
                }
            }
            var launchScene = EditorSceneManager.OpenScene(launchSceneName, OpenSceneMode.Single);
            UnityGameFramework.Runtime.UIComponent uiCom = null;
            foreach (var item in launchScene.GetRootGameObjects())
            {
                uiCom = item.GetComponentInChildren<UnityGameFramework.Runtime.UIComponent>();
                if (uiCom != null) break;
            }
            if (uiCom != null)
            {
                var instanceRoot = uiCom.InstanceRoot as RectTransform;
                if (instanceRoot != null)
                {
                    var canvasScaler = instanceRoot.GetComponent<CanvasScaler>();
                    if (canvasScaler != null && canvasScaler.referenceResolution != (Vector2)designResolution)
                    {
                        canvasScaler.referenceResolution = (Vector2)designResolution;
                        EditorSceneManager.SaveScene(launchScene);
                    }
                }
            }

            var gfExtension = GFUtility.AssetsPath.GetPrefab("Core/GFExtension");
            var gfExtensionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(gfExtension);
            if (gfExtensionPrefab != null)
            {
                var canvasScaler = gfExtensionPrefab.GetComponentInChildren<CanvasScaler>();
                if (canvasScaler != null && canvasScaler.referenceResolution != (Vector2)designResolution)
                {
                    canvasScaler.referenceResolution = (Vector2)designResolution;
                    EditorUtility.SetDirty(gfExtensionPrefab);
                    AssetDatabase.SaveAssetIfDirty(gfExtensionPrefab);
                }
            }
        }
    }
}
