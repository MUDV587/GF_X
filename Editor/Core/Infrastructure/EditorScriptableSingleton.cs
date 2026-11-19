#if UNITY_EDITOR
using UnityEngine;

namespace UGF.EditorTools
{
    /// <summary>
    /// 可持久化编辑器单例（ProjectSettings/ 下的 .asset）。
    /// 基于 Unity 公开的 ScriptableSingleton&lt;T&gt; 实现（替代 UnityEditorInternal 私有 API）。
    /// 持久化路径通过 UnityEngine.FilePathAttribute 标注，Location 必须为 ProjectFolder：
    /// <code>[FilePath("ProjectSettings/Xxx.asset", FilePathAttribute.Location.ProjectFolder)]</code>
    /// </summary>
    public abstract class EditorScriptableSingleton<T> : ScriptableSingleton<T> where T : ScriptableObject
    {
        private static T s_Instance;

        public static T Instance
        {
            get
            {
                if (!s_Instance)
                {
                    s_Instance = instance; // Unity 公开 API：懒加载/创建，存取走 FilePathAttribute
                }
                return s_Instance;
            }
        }

        public static void Save(bool saveAsText = true)
        {
            if (!Instance)
            {
                Debug.LogWarning($"{nameof(EditorScriptableSingleton<T>)} save skipped: instance is null.");
                return;
            }

            Instance.Save(saveAsText); // Unity 公开 API
        }
    }
}
#endif
