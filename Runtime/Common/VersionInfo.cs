using System;

/// <summary>
/// 资源更新清单（热更 version.json）。
/// 由构建管线（Build App/Resource）生成，运行时（UpdateResourcesProcedure）消费；
/// 是编辑器/运行时共享的数据契约，必须位于 GF_X.Runtime 共享程序集。
/// </summary>
[Serializable]
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.All)]
public class VersionInfo
{
    public int InternalResourceVersion;//资源版本号
    public int VersionListLength;
    public int VersionListHashCode;
    public int VersionListCompressedLength;
    public int VersionListCompressedHashCode;
    public string ApplicableGameVersion;//资源适用的App版本
    public string UpdatePrefixUri;//热更资源地址

    public string LastAppVersion; //最新的App版本号
    public bool ForceUpdateApp;//是否强制更新App
    public string AppUpdateUrl;//强制更新App地址
    public string AppUpdateDesc;//强制更新说明文字,显示在对话框中
}
