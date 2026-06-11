using System;
using System.Collections.Generic;
using NiumaUI.Core.Interface;
using NiumaUI.Views.Dialogue;
using UnityEngine;
using UnityEngine.UI;

namespace NiumaUI.Core
{
    public sealed class DefaultViewFactory : MonoBehaviour, IViewFactory
    {
        private const string BuiltInDialogueViewId = "DialogueWindow";

        [Header("视图注册")]
        [Tooltip("View 注册表资产。把 DialogueWindow、InventoryPanel 等 ViewId 与对应 Binding 预制体登记在这里。")]
        [SerializeField] private UIViewRegistrySO registry;

        [Header("生成父节点")]
        [Tooltip("默认生成父节点。通常拖 UIRoot/Windows 或 Canvas 下的默认窗口层；注册表没有指定 LayerId 时会生成到这里。")]
        [SerializeField] private RectTransform defaultRoot;

        [Tooltip("按 LayerId 指定生成父节点。例如 Default、Dialogue、Popup、Toast。UIViewRegistrySO 条目里的 LayerId 会匹配这里。")]
        [SerializeField] private List<UILayerRoot> layerRoots = new List<UILayerRoot>();

        [Header("开发期兜底")]
        [Tooltip("未在 Registry 中配置 DialogueWindow 时，是否自动创建一个内置对话窗口。开发期可开；正式 UI 配好预制体后建议关闭。")]
        [SerializeField] private bool enableBuiltInDialogueWindow = true;

        [Tooltip("Default Root 为空时，是否自动创建运行时 Canvas。开发期可开；正式核心场景建议手动绑定 Canvas/Windows 根节点。")]
        [SerializeField] private bool autoCreateRuntimeCanvasRoot = true;

        private readonly Dictionary<string, ViewBase> _instances = new Dictionary<string, ViewBase>();
        private readonly Dictionary<string, ViewBindingBase> _bindings = new Dictionary<string, ViewBindingBase>();
        private readonly Dictionary<string, bool> _cachePolicies = new Dictionary<string, bool>();
        private RectTransform _runtimeCanvasRoot;

        /// <summary>
        /// 根据视图ID获取或创建视图。
        /// 注册路径为正式环境的标准加载路径。
        /// 内置路径用于预制体配置前，保证早期低代码场景可用。
        /// </summary>
        public ViewBase Get(string viewId)
        {
            if (string.IsNullOrEmpty(viewId))
                return null;

            if (_instances.TryGetValue(viewId, out var cached) && cached != null)
                return cached;

            if (registry == null || !registry.TryGet(viewId, out var entry))
            {
                if (TryCreateBuiltInView(viewId, out var builtInView))
                    return builtInView;

                Debug.LogError($"[DefaultViewFactory] View is not registered: {viewId}", this);
                return null;
            }

            if (entry.Prefab == null)
            {
                if (TryCreateBuiltInView(viewId, out var builtInView))
                    return builtInView;

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

        /// <summary>
        /// 释放视图实例，根据注册表配置决定是隐藏还是销毁
        /// </summary>
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

        /// <summary>
        /// 根据层级ID解析父节点，优先使用注册表配置的层级根节点，其次是默认根节点，最后根据设置自动创建运行时画布根节点或使用工厂自身节点。
        /// </summary>
        private Transform ResolveParent(string layerId)
        {
            for (int i = 0; i < layerRoots.Count; i++)
            {
                var layer = layerRoots[i];
                if (layer != null && layer.LayerId == layerId && layer.Root != null)
                    return layer.Root;
            }

            if (defaultRoot != null)
                return defaultRoot;

            return autoCreateRuntimeCanvasRoot ? GetOrCreateRuntimeCanvasRoot() : transform;
        }

        /// <summary>
        /// 尝试创建内置视图（如对话框），仅在注册表未配置时启用。
        /// </summary>
        private bool TryCreateBuiltInView(string viewId, out ViewBase view)
        {
            view = null;

            // 低代码兜底方案：在注册预制体配置完成前，保证对话UI可用
            if (!enableBuiltInDialogueWindow || viewId != BuiltInDialogueViewId)
                return false;

            var root = new GameObject(BuiltInDialogueViewId, typeof(RectTransform), typeof(DialogueWindowBinding));
            root.transform.SetParent(ResolveParent("Dialogue"), false);

            var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;

            var binding = root.GetComponent<DialogueWindowBinding>();
            view = binding.CreateAndBindView(viewId);

            _instances[viewId] = view;
            _bindings[viewId] = binding;
            _cachePolicies[viewId] = true;

            return true;
        }

        /// <summary>
        /// 获取或创建运行时画布根节点。
        /// </summary>
        private RectTransform GetOrCreateRuntimeCanvasRoot()
        {
            if (_runtimeCanvasRoot != null)
                return _runtimeCanvasRoot;

            // 低代码兜底方案：未指定画布根节点时，自动创建可渲染的UI根节点
            var canvasObject = new GameObject("NiumaUI_RuntimeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            DontDestroyOnLoad(canvasObject);

            _runtimeCanvasRoot = (RectTransform)canvasObject.transform;
            _runtimeCanvasRoot.anchorMin = Vector2.zero;
            _runtimeCanvasRoot.anchorMax = Vector2.one;
            _runtimeCanvasRoot.offsetMin = Vector2.zero;
            _runtimeCanvasRoot.offsetMax = Vector2.zero;

            return _runtimeCanvasRoot;
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
