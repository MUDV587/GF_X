using UnityEngine;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GameFramework;
using GameFramework.Resource;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "AppConfigs", menuName = "GF/AppConfigs [配置App运行时所需数据表、配置表、流程]")]
public class AppConfigs : ScriptableObject
{
    private static AppConfigs mInstance = null;

    [SerializeField] bool m_LoadFromBytes = false;
    public bool LoadFromBytes
    {
        get => m_LoadFromBytes;
        set => m_LoadFromBytes = value;
    }
    [Header("数据表")]
    [SerializeField] string[] mDataTables;
    public string[] DataTables => mDataTables;


    [Header("配置表")]
    [SerializeField] string[] mConfigs;
    public string[] Configs => mConfigs;

    [Header("多语言表")]
    [SerializeField] string[] mLanguages;
    public string[] Languages => mLanguages;

    [Header("已启用流程列表")]
    [SerializeField] string[] mProcedures;

    public string[] Procedures => mProcedures;

    private void Awake()
    {
        mInstance = this;
    }


#if UNITY_EDITOR
    /// <summary>
    /// 编辑器下获取实例
    /// </summary>
    /// <returns></returns>
    public static AppConfigs GetInstanceEditor()
    {
        if (mInstance == null)
        {
            var configAsset = GFUtility.AssetsPath.GetScriptableAsset("Core/AppConfigs");
            mInstance = AssetDatabase.LoadAssetAtPath<AppConfigs>(configAsset);
        }
        return mInstance;
    }
#endif
    /// <summary>
    /// 运行时获取实例
    /// </summary>
    /// <returns></returns>
    public static async Task<AppConfigs> GetInstanceSync()
    {
        var configAsset = GFUtility.AssetsPath.GetScriptableAsset("Core/AppConfigs");
        if (mInstance == null)
        {
            mInstance = await LoadAssetAwait(configAsset);
        }
        return mInstance;
    }

    /// <summary>
    /// 加载资源（可等待）——本程序集私有实现：
    /// AppConfigs 位于框架层（GF_X.Runtime），不可引用热更侧 AwaitExtension 的程序集扩展
    /// </summary>
    private static UniTask<AppConfigs> LoadAssetAwait(string assetName)
    {
        UniTaskCompletionSource<AppConfigs> loadAssetTcs = new UniTaskCompletionSource<AppConfigs>();
        GF_X.Resource.LoadAsset(assetName, typeof(AppConfigs), new LoadAssetCallbacks(
            (tempAssetName, asset, duration, userdata) =>
            {
                AppConfigs tAsset = asset as AppConfigs;
                if (tAsset != null)
                {
                    loadAssetTcs.TrySetResult(tAsset);
                }
                else
                {
                    Debug.LogError($"Load asset failure load type is {asset.GetType()} but asset type is {typeof(AppConfigs)}.");
                    loadAssetTcs.TrySetException(new GameFrameworkException(
                        $"Load asset failure load type is {asset.GetType()} but asset type is {typeof(AppConfigs)}."));
                }
            },
            (tempAssetName, status, errorMessage, userdata) =>
            {
                Debug.LogError(errorMessage);
                loadAssetTcs.TrySetException(new GameFrameworkException(errorMessage));
            }
        ));
        return loadAssetTcs.Task;
    }

}
