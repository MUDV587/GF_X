using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 手动调度热更程序集中的 [RuntimeInitializeOnLoadMethod]（引擎不会自动执行）。
/// <para>
/// 热更 DLL 经 Assembly.Load 运行期装入，晚于 Unity 启动期的回调扫描，因此其
/// [RuntimeInitializeOnLoadMethod] 不会被引擎调用；由 AOT 侧在“全部热更 DLL 加载完成、
/// 调用 HotfixEntry.StartHotfixLogic 之前”（见 LoadHotfixDllProcedure.OnUpdate）统一补执行。
/// </para>
/// <para>
/// 语义说明（近似模拟）：Unity 的五个阶段统一在热更 DLL 加载完成点按枚举顺序依次调用；
/// SubsystemRegistration/BeforeSplashScreen 等“已过”阶段按补执行处理，AfterSceneLoad 等
/// “已到”阶段立即执行，不还原各阶段相对引擎启动的真实时序。
/// </para>
/// <para>
/// 使用契约：调用方只应传入运行期经 <see cref="Assembly.Load(byte[])"/> 装入的程序集；
/// 编辑器域内经 Unity 常规编译加载的程序集由引擎原生调用，重复传入将导致回调执行两次。
/// </para>
/// </summary>
internal static class HotfixRuntimeInitializer
{
    /// <summary>Unity 官方执行顺序（从早到晚）</summary>
    private static readonly RuntimeInitializeLoadType[] LoadOrder =
    {
        RuntimeInitializeLoadType.SubsystemRegistration,
        RuntimeInitializeLoadType.AfterAssembliesLoaded,
        RuntimeInitializeLoadType.BeforeSplashScreen,
        RuntimeInitializeLoadType.BeforeSceneLoad,
        RuntimeInitializeLoadType.AfterSceneLoad,
    };

    /// <summary>已执行过的程序集（内存引用比较），保证每次启动仅执行一次</summary>
    private static readonly HashSet<Assembly> Executed = new HashSet<Assembly>();

    /// <summary>
    /// 执行指定热更程序集中的所有 [RuntimeInitializeOnLoadMethod]。
    /// </summary>
    /// <param name="assembly">热更程序集（运行期 Assembly.Load 装入）</param>
    /// <param name="startFrom">从哪个阶段开始执行，默认从最早阶段开始（补执行全部阶段）</param>
    public static void Execute(Assembly assembly, RuntimeInitializeLoadType startFrom = RuntimeInitializeLoadType.SubsystemRegistration)
    {
        if (assembly == null)
        {
            Log.Error("[HotfixInit] 程序集为 null，跳过。");
            return;
        }

        if (!Executed.Add(assembly))
        {
            Log.Warning($"[HotfixInit] 程序集 {assembly.GetName().Name} 已执行过，跳过。");
            return;
        }

        Dictionary<RuntimeInitializeLoadType, List<Action>> actions;
        try
        {
            actions = Collect(assembly);
        }
        catch (Exception e)
        {
            Log.Error($"[HotfixInit] 扫描程序集 {assembly.GetName().Name} 失败：{e}");
            return;
        }

        int startIndex = Array.IndexOf(LoadOrder, startFrom);
        if (startIndex < 0)
        {
            Log.Warning($"[HotfixInit] 未知起始阶段 {startFrom}，按最早阶段执行。");
            startIndex = 0;
        }

        int totalCount = 0;
        for (int i = startIndex; i < LoadOrder.Length; i++)
        {
            RuntimeInitializeLoadType loadType = LoadOrder[i];
            if (!actions.TryGetValue(loadType, out var list) || list.Count == 0)
            {
                continue;
            }

            for (int j = 0; j < list.Count; j++)
            {
                totalCount++;
                try
                {
                    list[j]();
                }
                catch (Exception e)
                {
                    //单个方法失败不影响其他方法
                    Log.Error($"[HotfixInit] 执行 {loadType} 阶段回调失败：{e}");
                }
            }
        }

        Log.Info($"[HotfixInit] 程序集 {assembly.GetName().Name} 回调执行完成，共 {totalCount} 个。");
    }

    /// <summary>
    /// 一次性执行多个热更程序集（按传入顺序，即加载顺序）。
    /// </summary>
    public static void ExecuteAll(IEnumerable<Assembly> assemblies, RuntimeInitializeLoadType startFrom = RuntimeInitializeLoadType.SubsystemRegistration)
    {
        if (assemblies == null)
        {
            Log.Error("[HotfixInit] 程序集集合为 null，跳过。");
            return;
        }

        foreach (Assembly assembly in assemblies)
        {
            Execute(assembly, startFrom);
        }
    }

    /// <summary>
    /// 扫描程序集，将 [RuntimeInitializeOnLoadMethod] 方法按阶段收集为 Action 委托。
    /// 用委托而非 MethodInfo.Invoke：收集期即可暴露 AOT/解释器桥接问题，且执行无装箱。
    /// </summary>
    private static Dictionary<RuntimeInitializeLoadType, List<Action>> Collect(Assembly assembly)
    {
        var actions = new Dictionary<RuntimeInitializeLoadType, List<Action>>();
        foreach (RuntimeInitializeLoadType loadType in LoadOrder)
        {
            actions[loadType] = new List<Action>();
        }

        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            //部分类型加载失败时，过滤 null 继续处理
            types = e.Types.Where(t => t != null).ToArray();
            Log.Warning($"[HotfixInit] 程序集 {assembly.GetName().Name} 部分类型加载失败，已过滤：{e.Message}");
        }

        foreach (Type type in types)
        {
            //只扫描静态方法（含 private；DeclaredOnly：继承来的方法不在子类重复扫描）
            MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            foreach (MethodInfo method in methods)
            {
                try
                {
                    var attr = method.GetCustomAttribute<RuntimeInitializeOnLoadMethodAttribute>();
                    if (attr == null)
                    {
                        continue;
                    }

                    //官方要求：必须是无参静态方法
                    if (method.GetParameters().Length != 0)
                    {
                        Log.Warning($"[HotfixInit] 忽略带参数的方法：{type.FullName}.{method.Name}");
                        continue;
                    }

                    //泛型方法/开放泛型类型上的方法无法创建委托
                    if (method.ContainsGenericParameters)
                    {
                        Log.Warning($"[HotfixInit] 忽略泛型方法：{type.FullName}.{method.Name}");
                        continue;
                    }

                    if (!actions.TryGetValue(attr.loadType, out var list))
                    {
                        Log.Warning($"[HotfixInit] 未知 LoadType({(int)attr.loadType})：{type.FullName}.{method.Name}");
                        continue;
                    }

                    //非 void 返回类型无法绑定到 Action，用 Invoke 兜底（与引擎“忽略返回值”的行为保持一致）
                    Action action = method.ReturnType == typeof(void)
                        ? (Action)Delegate.CreateDelegate(typeof(Action), method)
                        : () => method.Invoke(null, null);
                    list.Add(action);
                }
                catch (Exception e)
                {
                    Log.Error($"[HotfixInit] 处理 {type.FullName}.{method.Name} 失败：{e}");
                }
            }
        }

        return actions;
    }

    /// <summary>
    /// 清除已执行记录（仅用于测试）。
    /// </summary>
    public static void Reset()
    {
        Executed.Clear();
    }
}
