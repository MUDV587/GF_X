using UnityEngine;

public static partial class GFUtility
{
    /// <summary>
    /// 启动阶段或编辑器日志。
    /// 仅在 GameFramework.Log 初始化前使用；运行期初始化完成后请使用 UnityGameFramework.Runtime.Log。
    /// </summary>
    public static void Log(string format)
    {
        string colorfulFormat = $"<color=#2BD988>{format}</color>";
        Debug.Log(colorfulFormat);
    }
    /// <summary>
    /// 启动阶段或编辑器警告。
    /// 仅在 GameFramework.Log 初始化前使用；运行期初始化完成后请使用 UnityGameFramework.Runtime.Log。
    /// </summary>
    public static void LogWarning(string format)
    {
        string colorfulFormat = $"<color=#F2A20C>{format}</color>";
        Debug.LogWarning(colorfulFormat);
    }
    /// <summary>
    /// 启动阶段或编辑器错误。
    /// 仅在 GameFramework.Log 初始化前使用；运行期初始化完成后请使用 UnityGameFramework.Runtime.Log。
    /// </summary>
    public static void LogError(string format)
    {
        string colorfulFormat = $"<color=#F22E2E>{format}</color>";
        Debug.LogError(colorfulFormat);
    }
}