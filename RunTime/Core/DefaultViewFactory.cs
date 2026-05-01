using System;
using System.Collections.Generic;
using NiumaUI.Core.Interface;
using UnityEngine;

namespace NiumaUI.Core
{
    public sealed class DefaultViewFactory : MonoBehaviour, IViewFactory
    {
        [SerializeField] private UIViewRegistrySO registry;
        [SerializeField] private RectTransform defaultRoot;
        [SerializeField] private List<UILayerRoot> layerRoots = new List<UILayerRoot>();

        private readonly Dictionary<string, ViewBase> _instances = new Dictionary<string, ViewBase>();
        private readonly Dictionary<string, ViewBindingBase> _bindings = new Dictionary<string, ViewBindingBase>();
        private readonly Dictionary<string, bool> _cachePolicies = new Dictionary<string, bool>();

        public ViewBase Get(string viewId)
        {
            if (string.IsNullOrEmpty(viewId))
                return null;

            if (_instances.TryGetValue(viewId, out var cached) && cached != null)
                return cached;

            if (registry == null || !registry.TryGet(viewId, out var entry))
            {
                Debug.LogError($"[DefaultViewFactory] View is not registered: {viewId}", this);
                return null;
            }

            if (entry.Prefab == null)
            {
                Debug.LogError($"[DefaultViewFactory] View prefab is missing: {viewId}", registry);
                return null;
            }

            var parent = ResolveParent(entry.LayerId);
            var binding = Instantiate(entry.Prefab, parent);

            if (entry.StartHidden)
                binding.Hide();

            var view = binding.CreateAndBindView(viewId);

            _instances[viewId] = view;
            _bindings[viewId] = binding;
            _cachePolicies[viewId] = entry.CacheInstance;

            return view;
        }

        public void Release(string viewId)
        {
            if (string.IsNullOrEmpty(viewId))
                return;

            if (!_instances.TryGetValue(viewId, out var view) || view == null)
                return;

            bool cache = _cachePolicies.TryGetValue(viewId, out var policy) && policy;
            if (cache)
            {
                view.Close();
            }
            else
            {
                _instances.Remove(viewId);
                _bindings.Remove(viewId, out var binding);
                _cachePolicies.Remove(viewId);
                if (binding != null)
                    Destroy(binding.gameObject);
            }
        }

        private Transform ResolveParent(string layerId)
        {
            for (int i = 0; i < layerRoots.Count; i++)
            {
                var layer = layerRoots[i];
                if (layer != null && layer.LayerId == layerId && layer.Root != null)
                    return layer.Root;
            }

            return defaultRoot != null ? defaultRoot : transform;
        }
    }

    [Serializable]
    public sealed class UILayerRoot
    {
        [SerializeField] private string layerId = "Default";
        [SerializeField] private RectTransform root;

        public string LayerId => layerId;
        public RectTransform Root => root;
    }
}
