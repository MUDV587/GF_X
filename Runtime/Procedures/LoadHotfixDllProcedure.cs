using GameFramework.Event;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;
using GameFramework.Fsm;
using System;
using UnityEngine;
using System.Linq;
using GameFramework;
using GameFramework.Resource;
using System.IO;
using System.Text;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class LoadHotfixDllProcedure : ProcedureBase
{
    /// <summary>
    /// 全部的预加载热更脚本dll
    /// </summary>
    private System.Collections.Generic.List<string> hotfixDlls;
    private bool hotfixListIsLoaded;
    private int totalProgress;
    private int loadedProgress;

    /// <summary>
    /// 本次经 Assembly.Load 装入的热更程序集（编辑器资源模式下为空，模拟器不参与）
    /// </summary>
    private System.Collections.Generic.List<System.Reflection.Assembly> loadedHotfixAssemblies = new System.Collections.Generic.List<System.Reflection.Assembly>();

#if ENABLE_OBFUZ
    /// <summary>
    /// 初始化Obfuz
    /// </summary>、
    [Obfuz.ObfuzIgnore]
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void SetUpStaticSecretKey()
    {
        Obfuz.EncryptionService<Obfuz.DefaultStaticEncryptionScope>.Encryptor = new Obfuz.EncryptionVM.GeneratedEncryptionVirtualMachine(Resources.Load<TextAsset>(GFRuntimeSettings.ObfuzSecretKeyResourcesPath).bytes);
    }
#endif
    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
#if ENABLE_HYBRIDCLR
        GF_X.Event.Subscribe(LoadHotfixDllEventArgs.EventId, OnLoadHotfixDllCallback);
#endif
        PreloadAndInitData();
    }


    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
#if ENABLE_HYBRIDCLR
        GF_X.Event.Unsubscribe(LoadHotfixDllEventArgs.EventId, OnLoadHotfixDllCallback);
#endif
        base.OnLeave(procedureOwner, isShutdown);
    }


    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
        if (!hotfixListIsLoaded)
        {
            return;
        }
        //加载热更新Dll完成,进入热更逻辑
        if (loadedProgress >= totalProgress)
        {
            loadedProgress = -1;
            var entryFunc = Utility.Assembly.GetType("HotfixEntry")?.GetMethod("StartHotfixLogic", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (entryFunc == null)
            {
                Log.Fatal("游戏启动失败, 未找到HotfixEntry.StartHotfixLogic入口函数");
                return;
            }

            //热更 DLL 全部加载完成：补执行热更程序集中的 [RuntimeInitializeOnLoadMethod]（引擎不扫描运行期装入的程序集）。
            //只传入本次 Assembly.Load 装入的程序集；编辑器域内程序集由 Unity 原生调用，不重复执行。
            //用法：热更程序集中直接使用 [RuntimeInitializeOnLoadMethod]，由此处统一补执行（见 HotfixRuntimeInitializer）。
            try
            {
                HotfixRuntimeInitializer.ExecuteAll(loadedHotfixAssemblies);
            }
            catch (Exception e)
            {
                Log.Error($"热更 RuntimeInitializeOnLoadMethod 模拟执行异常:{e}");
            }
#if ENABLE_HYBRIDCLR
            entryFunc.Invoke(null, new object[] { true });
#else
            entryFunc.Invoke(null, new object[] { false });
#endif
        }
    }

    /// <summary>
    /// 加载热更新dll
    /// </summary>
    private void PreloadAndInitData()
    {
        //显示进度条
        GF_X.BuiltinView.ShowLoadingProgress();
        totalProgress = 0;
        loadedProgress = 0;
        hotfixListIsLoaded = true;

        //#if !UNITY_EDITOR && ENABLE_HYBRIDCLR
#if ENABLE_HYBRIDCLR
        if (!GF_X.Base.EditorResourceMode)
        {
            hotfixListIsLoaded = false;
            LoadAotDlls();
            LoadHotfixDlls();
        }
#endif
    }
#if ENABLE_HYBRIDCLR
    /// <summary>
    /// 补充元数据
    /// </summary>
    private void LoadAotDlls()
    {
        var aotMetaDlls = Resources.LoadAll<TextAsset>(GFRuntimeSettings.AotDllDir);
        totalProgress += aotMetaDlls.Length;
        LoadMetadata(aotMetaDlls);
    }
    private void LoadMetadata(TextAsset[] aotMetaDlls)
    {
        var encryptDllList = AppSettings.Instance.EncryptAOTDlls;
        var encryptCode = UTF8Encoding.UTF8.GetBytes(ConstBuiltin.AOT_DLLS_KEY);
        foreach (var dll in aotMetaDlls)
        {
            var dllBytes = dll.bytes;
            if (encryptDllList != null && encryptDllList.Contains(dll.name))
            {
                dllBytes = Utility.Encryption.GetQuickXorBytes(dllBytes, encryptCode);
            }
            var resultCode = LoadMetadataForAOT(dllBytes);
            Log.Info("补充元数据:{0}. ret:{1}", dll.name, resultCode);
            if (resultCode == HybridCLR.LoadImageErrorCode.OK)
            {
                loadedProgress++;
            }
        }
    }
    private void LoadHotfixDlls()
    {
        Log.Info("Start load hotfix dlls...");
        var hotfixListFile = GFUtility.AssetsPath.GetCombinePath(GFRuntimeSettings.Instance.HotfixDllDir, GFRuntimeSettings.HotfixFileList);
        if (GF_X.Resource.HasAsset(hotfixListFile) == GameFramework.Resource.HasAssetResult.NotExist)
        {
            Log.Fatal("HotfixFileList Not Exist :{0}", hotfixListFile);
            return;
        }
        GF_X.Resource.LoadAsset(hotfixListFile, new GameFramework.Resource.LoadAssetCallbacks((string assetName, object asset, float duration, object userData) =>
        {
            var textAsset = asset as TextAsset;
            if (textAsset != null)
            {
                hotfixListIsLoaded = true;
                hotfixDlls = GFUtility.Json.ToObject<System.Collections.Generic.List<string>>(textAsset.text);
                totalProgress += hotfixDlls.Count;
                if (hotfixDlls.Count == 1)
                {
                    var mainDll = GFUtility.AssetsPath.GetHotfixDll(hotfixDlls.Last());
                    LoadHotfixDll(mainDll, this);
                }
                else
                {
                    for (int i = 0; i < hotfixDlls.Count - 1; i++)
                    {
                        var dllName = hotfixDlls[i];
                        var dllAsset = GFUtility.AssetsPath.GetHotfixDll(dllName);
                        LoadHotfixDll(dllAsset, this);
                    }
                }
            }
        }));
    }


    private void OnLoadHotfixDllCallback(object sender, GameEventArgs e)
    {
        var args = e as LoadHotfixDllEventArgs;
        if (args.UserData != this)
        {
            return;
        }
        if (args.Assembly == null)
        {
            Log.Error($"加载dll失败:{args.DllName}");
            return;
        }

        //记录本次经 Assembly.Load 装入的热更程序集，供 RuntimeInitializeOnLoadMethod 模拟器使用
        loadedHotfixAssemblies.Add(args.Assembly);

        loadedProgress++;
        GF_X.BuiltinView.SetLoadingProgress(loadedProgress / (float)totalProgress);

        //所有依赖dll加载完成后再加载Hotfix.dll
        if (hotfixDlls.Contains(args.DllName))
        {
            hotfixDlls.Remove(args.DllName);
            if (hotfixDlls.Count == 1)
            {
                var mainDll = GFUtility.AssetsPath.GetHotfixDll(hotfixDlls.Last());
                LoadHotfixDll(mainDll, this);
            }
        }
    }

    /// <summary>
    /// 加载热更文件
    /// </summary>
    /// <param name="dllAssetName"></param>
    /// <param name="userData"></param>
    public void LoadHotfixDll(string dllAssetName, object userData)
    {
        GF_X.Resource.LoadAsset(dllAssetName, typeof(TextAsset), new LoadAssetCallbacks(OnLoadDllSuccess, OnLoadDllFail), userData);
    }

    private void OnLoadDllFail(string assetName, LoadResourceStatus status, string errorMessage, object userData)
    {
        Log.Error("加载{0}失败! Error:{1}", assetName, errorMessage);
        GF_X.Event.Fire(this, ReferencePool.Acquire<LoadHotfixDllEventArgs>().Fill(Path.GetFileNameWithoutExtension(assetName), null, userData));
    }

    private void OnLoadDllSuccess(string assetName, object asset, float duration, object userData)
    {
        var dllTextAsset = asset as TextAsset;
        System.Reflection.Assembly dllAssembly = null;
        if (dllTextAsset != null)
        {
            try
            {
                dllAssembly = System.Reflection.Assembly.Load(dllTextAsset.bytes);
            }
            catch (Exception e)
            {
                Log.Error("Assembly.Load加载热更dll失败:{0},Error:{1}", assetName, e.Message);
                throw;
            }

        }
        var dllName = Path.GetFileNameWithoutExtension(assetName);
        GF_X.Event.Fire(this, ReferencePool.Acquire<LoadHotfixDllEventArgs>().Fill(dllName, dllAssembly, userData));
    }
    /// <summary>
    /// 为aot assembly加载原始metadata， 这个代码放aot或者热更新都行。
    /// 一旦加载后，如果AOT泛型函数对应native实现不存在，则自动替换为解释模式执行
    /// </summary>
    private HybridCLR.LoadImageErrorCode LoadMetadataForAOT(byte[] dllBytes)
    {
        return HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, HybridCLR.HomologousImageMode.SuperSet);
    }
#endif
}
