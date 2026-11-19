using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

public static class SettingBuiltinExtension
{
    /// <summary>
    /// 设置A/B测试组
    /// </summary>
    /// <param name="com"></param>
    /// <param name="groupName"></param>
    public static void SetABTestGroup(this SettingComponent com, string groupName)
    {
        com.SetString(ConstBuiltin.Setting.ABTestGroup, groupName);
    }

    /// <summary>
    /// 获取A/B测试组
    /// </summary>
    /// <param name="com"></param>
    /// <returns></returns>
    public static string GetABTestGroup(this SettingComponent com)
    {
        return com.GetString(ConstBuiltin.Setting.ABTestGroup, string.Empty);
    }
}
