#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    /// <summary>
    /// 可持久化编辑器单例（ProjectSettings/ 下的 .asset）。
    /// 基于 Unity 公开的 ScriptableSingleton&lt;T&gt; 实现（替代 UnityEditorInternal 私有 API）。
    /// 派生类仍须形如 <c>class XxxSettings : EditorScriptableSingleton&lt;XxxSettings&gt;</c>，
    /// 并用 <c>[FilePath("ProjectSettings/Xxx.asset", FilePathAttribute.Location.ProjectFolder)]</c> 标注。
    /// </summary>
    public abstract class EditorScriptableSingleton<T> : ScriptableSingleton<T> where T : EditorScriptableSingleton<T>
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

        /// <summary>
        /// 静态便捷入口（保持既有调用 <c>XxxSettings.Save()</c>）。
        /// </summary>
        public static void Save(bool saveAsText = true)
        {
            if (!Instance)
            {
                Debug.LogWarning($"{nameof(EditorScriptableSingleton<T>)} save skipped: instance is null.");
                return;
            }

            Instance.SaveInstance(saveAsText);
        }

        /// <summary>实例保存实现（内部调用 Unity 公开的受保护 Save）。</summary>
        public void SaveInstance(bool saveAsText = true)
        {
            base.Save(saveAsText); // 调用 Unity 公开的受保护 Save
        }
    }
}
#endif
