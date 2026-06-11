using System.Collections.Generic;
using NiumaUI.Core.Interface;
using NiumaUI.Enum;
using UnityEngine;

namespace NiumaUI.Toolkit
{
    /// <summary>
    /// UI Toolkit 2.0 根控制器。
    /// 第一版负责通过 ViewId 打开、关闭、刷新 Toolkit View，并根据 InputPolicy 请求玩法输入阻塞。
    /// </summary>
    public sealed class UIToolkitUIManager : MonoBehaviour
    {
        [Header("工厂")]
        [Tooltip("UI Toolkit View 工厂。通常拖同物体或子物体上的 UIToolkitViewFactory。")]
        [SerializeField] private UIToolkitViewFactory viewFactory;

        [Header("输入阻塞")]
        [Tooltip("玩法输入阻塞脚本。使用 NiumaTPC 时拖 PlayerRoot/UIBridge 上的 TPCGameplayInputBlocker；没有需要阻塞的玩法输入时可留空。")]
        [SerializeField] private MonoBehaviour inputBlockerProvider;

        [Header("驱动")]
        [Tooltip("是否在 Update 中自动驱动 Toolkit View Tick。若由外部模块统一 Tick，则关闭。")]
        [SerializeField] private bool driveTickInUpdate = true;

        [Tooltip("Awake 时是否自动查找同物体或子物体上的 UIToolkitViewFactory。")]
        [SerializeField] private bool autoResolveFactory = true;

        [Header("调试")]
        [Tooltip("是否输出缺少工厂、缺少输入阻塞器等警告。")]
        [SerializeField] private bool logWarnings = true;

        private readonly List<string> _openStack = new List<string>();
        private IGameplayInputBlocker _inputBlocker;
        private bool _isGameplayInputBlocked;

        public IReadOnlyList<string> OpenStack => _openStack;

        private void Awake()
        {
            ResolveReferences(false);
        }

        private void Update()
        {
            if (driveTickInUpdate)
                Tick(Time.deltaTime);
        }

        private void OnDisable()
        {
            SetGameplayInputBlocked(false);
        }

        private void OnDestroy()
        {
            viewFactory?.DestroyAll();
            _openStack.Clear();
            SetGameplayInputBlocked(false);
        }

        public bool OpenView(string viewId)
        {
            return OpenView(viewId, null);
        }

        public bool OpenView(string viewId, object viewData)
        {
            if (!EnsureReady())
                return false;

            if (!viewFactory.TryOpen(viewId, viewData, out var instance) || instance == null)
                return false;

            _openStack.Remove(viewId);
            _openStack.Add(viewId);
            ApplyInputBlockState();
            return true;
        }

        public bool RefreshView(string viewId, object viewData)
        {
            return EnsureReady() && viewFactory.TryRefresh(viewId, viewData);
        }

        public bool CloseView(string viewId)
        {
            if (!EnsureReady())
                return false;

            var closed = viewFactory.TryClose(viewId);
            _openStack.Remove(viewId);
            ApplyInputBlockState();
            return closed;
        }

        public bool CloseTopView()
        {
            if (_openStack.Count <= 0)
                return false;

            return CloseView(_openStack[_openStack.Count - 1]);
        }

        public void CloseAllViews()
        {
            if (!EnsureReady())
                return;

            viewFactory.CloseAll();
            _openStack.Clear();
            ApplyInputBlockState();
        }

        public bool TryGetBinding<T>(string viewId, out T binding) where T : class, IToolkitViewBinding
        {
            binding = null;
            if (!EnsureReady() || !viewFactory.TryGetInstance(viewId, out var instance) || instance == null)
                return false;

            binding = instance.Binding as T;
            return binding != null;
        }

        public void Tick(float deltaTime)
        {
            viewFactory?.Tick(deltaTime);
        }

        private bool EnsureReady()
        {
            ResolveReferences(false);
            if (viewFactory != null)
                return true;

            Warn("UIToolkitViewFactory 未绑定，无法执行 UI Toolkit View 操作。");
            return false;
        }

        private void ResolveReferences(bool logMissing)
        {
            if (viewFactory == null && autoResolveFactory)
            {
                viewFactory = GetComponent<UIToolkitViewFactory>();
                if (viewFactory == null)
                    viewFactory = GetComponentInChildren<UIToolkitViewFactory>(true);
            }

            _inputBlocker = inputBlockerProvider as IGameplayInputBlocker;
            if (inputBlockerProvider != null && _inputBlocker == null && logMissing)
                Warn("InputBlockerProvider 没有实现 IGameplayInputBlocker。");
        }

        private void ApplyInputBlockState()
        {
            var shouldBlock = false;
            for (var i = 0; i < _openStack.Count; i++)
            {
                if (viewFactory != null
                    && viewFactory.TryGetInstance(_openStack[i], out var instance)
                    && instance != null
                    && instance.IsOpen
                    && instance.InputPolicy == UIToolkitViewInputPolicy.BlockGameplayInput)
                {
                    shouldBlock = true;
                    break;
                }
            }

            SetGameplayInputBlocked(shouldBlock);
        }

        private void SetGameplayInputBlocked(bool blocked)
        {
            if (_isGameplayInputBlocked == blocked)
                return;

            _isGameplayInputBlocked = blocked;
            _inputBlocker?.SetBlocked(blocked, UIMode.Menu);
        }

        private void Warn(string message)
        {
            if (logWarnings)
                Debug.LogWarning($"[UIToolkitUIManager] {message}", this);
        }
    }
}