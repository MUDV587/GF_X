using GameFramework.Resource;
using System;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Editor.ResourceTools;

namespace UGF.EditorTools
{
    public partial class AppBuilderPanel
    {
        private void RefreshObfuzEnable()
        {
            if (Obfuz.Settings.ObfuzSettings.Instance == null || Obfuz.Settings.ObfuzSettings.Instance.buildPipelineSettings == null)
            {
                return;
            }

            Obfuz.Settings.ObfuzSettings.Instance.buildPipelineSettings.enable = AppBuilderEditorSettings.Instance.EnableObfuz;
            if (AppBuilderEditorSettings.Instance.EnableObfuz)
            {
#if !ENABLE_OBFUZ
                HybridCLRExtensionTool.EnableObfuz();
#endif
            }
            else
            {
#if ENABLE_OBFUZ
                HybridCLRExtensionTool.DisableObfuz();
#endif
            }

            Obfuz.Settings.ObfuzSettings.Save();
            AppBuilderEditorSettings.Save();
        }

        private void RefreshHybridCLREnable()
        {
            if (AppSettings.Instance.ResourceMode == ResourceMode.Unspecified)
            {
                return;
            }

            if (AppSettings.Instance.ResourceMode == ResourceMode.Package)
            {
                HybridCLRExtensionTool.DisableHybridCLR();
            }
            else
            {
                HybridCLRExtensionTool.EnableHybridCLR();
            }
        }

        private string GetResourceOutputPathByMode(ResourceMode mode)
        {
            switch (mode)
            {
                case ResourceMode.Package:
                    return _controller.OutputPackagePath;
                case ResourceMode.Updatable:
                    return _controller.OutputFullPath;
                case ResourceMode.UpdatableWhilePlaying:
                    return _controller.OutputPackedPath;
                default:
                    return null;
            }
        }

        private void SetResourceMode(ResourceMode mode)
        {
            _controller.OutputPackageSelected = false;
            _controller.OutputFullSelected = false;
            _controller.OutputPackedSelected = false;
            switch (mode)
            {
                case ResourceMode.Package:
                    _controller.OutputPackageSelected = true;
                    break;
                case ResourceMode.Updatable:
                case ResourceMode.UpdatableWhilePlaying:
                    _controller.OutputFullSelected = true;
                    break;
            }
        }

        private bool HasPackedResource()
        {
            // 通过 UGF 公开 API 获取配置路径（替代反射 UnityGameFramework.Editor.Type）。
            string configPath = AppBuilderEditorSettings.Instance.GetProjectAbsolutePath(AppBuilderEditorSettings.Instance.ResourceCollectionConfigPath);
            string ugfConfigPath = UnityGameFramework.Editor.Type.GetConfigurationPath<ResourceCollectionConfigPathAttribute>();
            if (!string.IsNullOrEmpty(ugfConfigPath))
            {
                configPath = ugfConfigPath;
            }

            if (!File.Exists(configPath))
            {
                return false;
            }

            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(configPath);
                XmlNode xmlRoot = xmlDocument.SelectSingleNode("UnityGameFramework");
                XmlNode xmlCollection = xmlRoot?.SelectSingleNode("ResourceCollection");
                XmlNode xmlResources = xmlCollection?.SelectSingleNode("Resources");
                if (xmlResources == null)
                {
                    return false;
                }

                XmlNodeList xmlNodeList = xmlResources.ChildNodes;
                for (int i = 0; i < xmlNodeList.Count; i++)
                {
                    XmlNode xmlNode = xmlNodeList.Item(i);
                    if (!string.Equals(xmlNode.Name, "Resource", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    XmlNode packedNode = xmlNode.Attributes?.GetNamedItem("Packed");
                    if (packedNode != null && bool.TryParse(packedNode.Value, out bool packed) && packed)
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }

        private void OpenResourcesEditor()
        {
            // 通过 UGF 公开入口打开资源编辑器（替代反射 ResourceEditor）。
            ResourceEditorWindow.Open();
        }
    }
}
