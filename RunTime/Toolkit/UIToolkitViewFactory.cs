using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NiumaUI.Toolkit
{
    /// <summary>
    /// UI Toolkit View 工厂。
    /// 根据 UIToolkitViewRegistrySO 创建、缓存、关闭和销毁 Toolkit View。
    /// </summary>
    public sealed class UIToolkitViewFactory : MonoBehaviour
    {
        [Header("注册表")]
        [Tooltip("UI Toolkit View 注册表。创建方式：Create / NiumaUI / Toolkit View Registry。")]
        [SerializeField] private UIToolkitViewRegistrySO registry;

        [Header("层级根节点")]
        [Tooltip("按 LayerId 配置 UIDocument 根节点。View 条目的 LayerId 会匹配这里。")]
        [SerializeField] private List<UIToolkitLayerRoot> layerRoots = new List<UIToolkitLayerRoot>();

        [Header("Binding 创建器")]
        [Tooltip("实现 IToolkitViewBindingProvider 的 MonoBehaviour。BindingProviderId 会在这里匹配；为空或未匹配时使用默认 Binding。")]
        [SerializeField] private List<MonoBehaviour> bindingProviderBehaviours = new List<MonoBehaviour>();

        [Header("调试")]
        [Tooltip("是否输出缺失注册、缺失 UIDocument、BindingProvider 未匹配等警告。")]
        [SerializeField] private bool logWarnings = true;

        private readonly Dictionary<string, UIToolkitViewInstance> _instances = new Dictionary<string, UIToolkitViewInstance>();
        private readonly Dictionary<string, IToolkitViewBindingProvider> _providers = new Dictionary<string, IToolkitViewBindingProvider>();
        private readonly IToolkitViewBindingProvider _fallbackProvider = new RuntimeDefaultBindingProvider();

        public IReadOnlyDictionary<string, UIToolkitViewInstance> Instances => _instances;

        private void Awake()
        {
            RebuildProviderIndex();
        }

        public void SetRegistry(UIToolkitViewRegistrySO value)
        {
            registry = value;
        }

        public void SetLayerRoots(IEnumerable<UIToolkitLayerRoot> roots)
        {
            layerRoots.Clear();
            if (roots == null)
                return;

            foreach (var root in roots)
            {
                if (root != null)
                    layerRoots.Add(root);
            }
        }

        public void RebuildProviderIndex()
        {
            _providers.Clear();

            for (var i = 0; i < bindingProviderBehaviours.Count; i++)
            {
                if (bindingProviderBehaviours[i] is not IToolkitViewBindingProvider provider)
                    continue;

                var providerId = NormalizeProviderId(provider.ProviderId);
                if (_providers.ContainsKey(providerId))
                {
                    Warn($"BindingProviderId 重复：{providerId}，后出现的 Provider 已忽略。");
                    continue;
                }

                _providers.Add(providerId, provider);
            }
        }

        public bool TryOpen(string viewId, out UIToolkitViewInstance instance)
        {
            return TryOpen(viewId, null, out instance);
        }

        public bool TryOpen(string viewId, object viewData, out UIToolkitViewInstance instance)
        {
            instance = null;
            if (string.IsNullOrWhiteSpace(viewId))
            {
                Warn("ViewId 为空，无法打开 UI Toolkit View。");
                return false;
            }

            if (_instances.TryGetValue(viewId, out instance) && instance != null)
            {
                instance.Open(viewData);
                instance.FocusDefaultElement();
                return true;
            }

            if (!TryCreateInstance(viewId, out instance))
                return false;

            _instances[viewId] = instance;
            instance.Open(viewData);
            instance.FocusDefaultElement();
            return true;
        }

        public bool TryRefresh(string viewId, object viewData)
        {
            if (!_instances.TryGetValue(viewId, out var instance) || instance == null)
                return false;

            instance.Refresh(viewData);
            return true;
        }

        public bool TryClose(string viewId)
        {
            if (string.IsNullOrWhiteSpace(viewId))
                return false;

            if (!_instances.TryGetValue(viewId, out var instance) || instance == null)
                return false;

            instance.Close();
            if (instance.CachePolicy == UIToolkitViewCachePolicy.DestroyOnClose)
            {
                instance.Dispose();
                _instances.Remove(viewId);
            }

            return true;
        }

        public bool TryGetInstance(string viewId, out UIToolkitViewInstance instance)
        {
            return _instances.TryGetValue(viewId, out instance) && instance != null;
        }

        public void CloseAll()
        {
            var keys = new List<string>(_instances.Keys);
            for (var i = 0; i < keys.Count; i++)
                TryClose(keys[i]);
        }

        public void DestroyAll()
        {
            foreach (var pair in _instances)
                pair.Value?.Dispose();

            _instances.Clear();
        }

        public void Tick(float deltaTime)
        {
            foreach (var pair in _instances)
            {
                if (pair.Value != null && pair.Value.IsOpen)
                    pair.Value.Tick(deltaTime);
            }
        }

        private bool TryCreateInstance(string viewId, out UIToolkitViewInstance instance)
        {
            instance = null;

            if (registry == null)
            {
                Warn("UIToolkitViewRegistrySO 未绑定，无法创建 Toolkit View。");
                return false;
            }

            if (!registry.TryGet(viewId, out var entry) || entry == null)
            {
                Warn($"ViewId 未注册：{viewId}");
                return false;
            }

            if (entry.VisualTreeAsset == null)
            {
                Warn($"ViewId={viewId} 的 VisualTreeAsset 为空。");
                return false;
            }

            var parent = ResolveParent(entry.LayerId);
            if (parent == null)
            {
                Warn($"LayerId={entry.LayerId} 找不到可用 UIDocument 根节点，ViewId={viewId} 创建失败。");
                return false;
            }

            var root = entry.VisualTreeAsset.CloneTree();
            root.name = string.IsNullOrWhiteSpace(root.name) ? viewId : root.name;
            root.style.display = DisplayStyle.None;
            root.AddToClassList("niuma-view");
            root.AddToClassList($"niuma-view-{viewId}");

            var styleSheets = entry.StyleSheets;
            for (var i = 0; styleSheets != null && i < styleSheets.Count; i++)
            {
                if (styleSheets[i] != null)
                    root.styleSheets.Add(styleSheets[i]);
            }

            parent.Add(root);

            var binding = CreateBinding(entry.BindingProviderId);
            binding.Initialize(viewId, root);
            instance = new UIToolkitViewInstance(entry, root, binding);
            return true;
        }

        private VisualElement ResolveParent(string layerId)
        {
            var normalizedLayerId = string.IsNullOrWhiteSpace(layerId) ? "Default" : layerId;
            for (var i = 0; i < layerRoots.Count; i++)
            {
                var layer = layerRoots[i];
                if (layer == null || !string.Equals(layer.LayerId, normalizedLayerId, StringComparison.Ordinal))
                    continue;

                var document = layer.Document;
                if (document == null || document.rootVisualElement == null)
                    return null;

                if (string.IsNullOrWhiteSpace(layer.RootElementName))
                    return document.rootVisualElement;

                var root = document.rootVisualElement.Q<VisualElement>(layer.RootElementName);
                return root ?? document.rootVisualElement;
            }

            return null;
        }

        private IToolkitViewBinding CreateBinding(string providerId)
        {
            var normalized = NormalizeProviderId(providerId);
            if (_providers.TryGetValue(normalized, out var provider) && provider != null)
            {
                var binding = provider.CreateBinding();
                if (binding != null)
                    return binding;

                Warn($"BindingProviderId={normalized} 返回了空 Binding，已回退到 Default Binding。");
            }
            else if (!string.Equals(normalized, "Default", StringComparison.Ordinal))
            {
                Warn($"未找到 BindingProviderId={normalized}，已回退到 Default Binding。");
            }

            return _fallbackProvider.CreateBinding();
        }

        private static string NormalizeProviderId(string providerId)
        {
            return string.IsNullOrWhiteSpace(providerId) ? "Default" : providerId.Trim();
        }

        private void Warn(string message)
        {
            if (logWarnings)
                Debug.LogWarning($"[UIToolkitViewFactory] {message}", this);
        }

        private sealed class RuntimeDefaultBindingProvider : IToolkitViewBindingProvider
        {
            public string ProviderId => "Default";

            public IToolkitViewBinding CreateBinding()
            {
                return new DefaultToolkitViewBinding();
            }
        }
    }
}