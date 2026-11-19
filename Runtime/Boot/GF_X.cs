using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.All)]
[DefaultExecutionOrder(-10000)]
public class GF_X : MonoBehaviour
{
    public static GF_X Instance { get; private set; }
    public static BaseComponent Base { get; private set; }
    public static ConfigComponent Config { get; private set; }
    public static DataNodeComponent DataNode { get; private set; }
    public static DataTableComponent DataTable { get; private set; }
    public static DebuggerComponent Debugger { get; private set; }
    public static DownloadComponent Download { get; private set; }
    public static EntityComponent Entity { get; private set; }
    public static EventComponent Event { get; private set; }
    public static FsmComponent Fsm { get; private set; }
    public static FileSystemComponent FileSystem { get; private set; }
    public static LocalizationComponent Localization { get; private set; }
    public static NetworkComponent Network { get; private set; }
    public static ProcedureComponent Procedure { get; private set; }
    public static ResourceComponent Resource { get; private set; }
    public static SceneComponent Scene { get; private set; }
    public static SettingComponent Setting { get; private set; }
    public static SoundComponent Sound { get; private set; }
    public static UIComponent UI { get; private set; }
    public static ObjectPoolComponent ObjectPool { get; private set; }
    public static WebRequestComponent WebRequest { get; private set; }
    public static BuiltinViewComponent BuiltinView { get; private set; }
    public static Camera UICamera { get; private set; }

    public static Canvas RootCanvas { get; private set; } = null;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            GFUtility.Log($"[Bootstrap] AppSettings.ResourceMode={AppSettings.Instance.ResourceMode}");
            // 通过 UGF 公开 API（SetResourceModePreInit）应用资源模式（替代反射 m_ResourceMode）。
            // 必须在 ResourceComponent.Start 消费该字段之前写入，见 UGF fork 补丁 #14。
            var resCom = GameEntry.GetComponent<ResourceComponent>();
            if (resCom != null)
            {
                resCom.SetResourceModePreInit(AppSettings.Instance.ResourceMode);
                GFUtility.Log($"[Bootstrap] ResourceMode applied: {AppSettings.Instance.ResourceMode}");
            }
        }
    }

    private void Start()
    {
        Log.Info("[Bootstrap] GF_X.Start: GameFramework log is ready.");
        Base = GameEntry.GetComponent<BaseComponent>();
        Config = GameEntry.GetComponent<ConfigComponent>();
        DataNode = GameEntry.GetComponent<DataNodeComponent>();
        DataTable = GameEntry.GetComponent<DataTableComponent>();
        Debugger = GameEntry.GetComponent<DebuggerComponent>();
        Download = GameEntry.GetComponent<DownloadComponent>();
        Entity = GameEntry.GetComponent<EntityComponent>();
        Event = GameEntry.GetComponent<EventComponent>();
        Fsm = GameEntry.GetComponent<FsmComponent>();
        Procedure = GameEntry.GetComponent<ProcedureComponent>();
        Localization = GameEntry.GetComponent<LocalizationComponent>();
        Network = GameEntry.GetComponent<NetworkComponent>();
        Resource = GameEntry.GetComponent<ResourceComponent>();
        FileSystem = GameEntry.GetComponent<FileSystemComponent>();
        Scene = GameEntry.GetComponent<SceneComponent>();
        Setting = GameEntry.GetComponent<SettingComponent>();
        Sound = GameEntry.GetComponent<SoundComponent>();
        UI = GameEntry.GetComponent<UIComponent>();
        ObjectPool = GameEntry.GetComponent<ObjectPoolComponent>();
        WebRequest = GameEntry.GetComponent<WebRequestComponent>();
        BuiltinView = GameEntry.GetComponent<BuiltinViewComponent>();

        RootCanvas = UI != null ? UI.GetComponentInChildren<Canvas>(true) : null;
        if (RootCanvas == null)
        {
            Log.Error("[Bootstrap] 未找到根 Canvas（UI 实例根），UICamera/CanvasScaler 初始化跳过。");
        }
        else
        {
            UICamera = RootCanvas.worldCamera;
            UpdateCanvasScaler();
        }
    }
    public void UpdateCanvasScaler()
    {
        CanvasScaler canvasScaler = RootCanvas.GetComponent<CanvasScaler>();
        canvasScaler.referenceResolution = AppSettings.Instance.DesignResolution;
        var designRatio = canvasScaler.referenceResolution.x / (float)canvasScaler.referenceResolution.y;
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = Screen.width / (float)Screen.height > designRatio ? 1 : 0;
        Log.Info($"[GF_X] UI适配Match:{canvasScaler.matchWidthOrHeight}");
    }


    /// <summary>
    /// 退出或重启
    /// </summary>
    /// <param name="type"></param>
    public static void Shutdown(ShutdownType type)
    {
        GameEntry.Shutdown(type);
    }
}