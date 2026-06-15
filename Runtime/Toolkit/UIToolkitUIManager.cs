using System.Collections.Generic;
using NiumaUI.Enum;
using UnityEngine;

namespace NiumaUI.Toolkit
{
    /// <summary>
    /// UI Toolkit 根控制器。
    /// 负责通过 ViewId 打开、关闭、刷新 Toolkit View，并根据 View 策略处理输入阻塞、返回栈和通用 UI 请求。
    /// </summary>
    public sealed class UIToolkitUIManager : MonoBehaviour
    {
        [Header("工厂")]
        [Tooltip("UI Toolkit View 工厂。通常拖同物体或子物体上的 UIToolkitViewFactory。")]
        [SerializeField] private UIToolkitViewFactory viewFactory;

        [Header("输入阻塞")]
        [Tooltip("玩法输入阻塞脚本。使用 NiumaTPC 时拖 PlayerRoot/UIBridge 上的 TPCGameplayInputBlocker；没有需要阻塞的玩法输入时可留空。")]
        [SerializeField] private MonoBehaviour inputBlockerProvider;

        [Header("通用 ViewId")]
        [Tooltip("Toast 短提示 ViewId。需要在 UIToolkitViewRegistrySO 中注册，并实现 IToolkitToastBinding。")]
        [SerializeField] private string toastViewId = "Toast";

        [Tooltip("Confirm 确认弹窗 ViewId。需要在 UIToolkitViewRegistrySO 中注册，并实现 IToolkitConfirmBinding。")]
        [SerializeField] private string confirmViewId = "Confirm";

        [Tooltip("Loading 遮罩 ViewId。需要在 UIToolkitViewRegistrySO 中注册，并实现 IToolkitLoadingBinding。")]
        [SerializeField] private string loadingViewId = "Loading";

        [Header("返回键")]
        [Tooltip("是否在 Update 中监听返回键。关闭后可由外部输入系统调用 TryGoBack。")]
        [SerializeField] private bool enableKeyboardBack = true;

        [Tooltip("返回键。默认 Escape。")]
        [SerializeField] private KeyCode backKey = KeyCode.Escape;

        [Header("驱动")]
        [Tooltip("是否在 Update 中自动驱动 Toolkit View Tick。若由外部模块统一 Tick，则关闭。")]
        [SerializeField] private bool driveTickInUpdate = true;

        [Tooltip("Awake 时是否自动查找同物体或子物体上的 UIToolkitViewFactory。")]
        [SerializeField] private bool autoResolveFactory = true;

        [Header("调试")]
        [Tooltip("是否输出缺少工厂、输入阻塞器等警告。")]
        [SerializeField] private bool logWarnings = true;

        private readonly List<string> _openStack = new List<string>();
        private readonly List<string> _backStack = new List<string>();
        private IGameplayInputBlocker _inputBlocker;
        private bool _isGameplayInputBlocked;
        private UIMode _gameplayInputBlockMode = UIMode.Menu;

        public IReadOnlyList<string> OpenStack => _openStack;
        public IReadOnlyList<string> BackStack => _backStack;

        private void Awake()
        {
            ResolveReferences(false);
        }

        private void Update()
        {
            if (enableKeyboardBack && Input.GetKeyDown(backKey))
                TryGoBack();

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
            _backStack.Clear();
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

            PushOpenView(viewId, instance.BackPolicy);
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
            RemoveTrackedView(viewId);
            ApplyInputBlockState();
            return closed;
        }

        public bool CloseTopView()
        {
            if (_openStack.Count <= 0)
                return false;

            return CloseView(_openStack[_openStack.Count - 1]);
        }

        public bool TryGoBack()
        {
            while (_backStack.Count > 0)
            {
                var viewId = _backStack[_backStack.Count - 1];
                if (viewFactory != null && viewFactory.TryGetInstance(viewId, out var instance) && instance != null && instance.IsOpen)
                    return CloseView(viewId);

                _backStack.RemoveAt(_backStack.Count - 1);
            }

            return false;
        }

        public void CloseAllViews()
        {
            if (!EnsureReady())
                return;

            viewFactory.CloseAll();
            _openStack.Clear();
            _backStack.Clear();
            ApplyInputBlockState();
        }

        public bool ShowToast(string message, float durationSeconds = 2f, string styleKey = null)
        {
            var data = new UIToolkitToastViewData
            {
                ToastId = System.Guid.NewGuid().ToString("N"),
                Message = message,
                DurationSeconds = durationSeconds,
                StyleKey = styleKey
            };

            return ShowToast(data);
        }

        public bool ShowToast(UIToolkitToastViewData data)
        {
            if (data == null)
                return false;

            data.OnExpired ??= () => CloseView(toastViewId);

            if (!OpenView(toastViewId, data))
                return false;

            if (TryGetBinding<IToolkitToastBinding>(toastViewId, out var binding))
                binding.ApplyToast(data);

            return true;
        }

        public bool ShowConfirm(UIToolkitConfirmViewData data)
        {
            if (data == null)
                return false;

            var effectiveData = data;
            if (data.AutoClose)
            {
                var callback = data.Callback;
                effectiveData = new UIToolkitConfirmViewData
                {
                    RequestId = data.RequestId,
                    Title = data.Title,
                    Message = data.Message,
                    ConfirmText = data.ConfirmText,
                    CancelText = data.CancelText,
                    ShowCancel = data.ShowCancel,
                    AutoClose = data.AutoClose,
                    Callback = confirmed =>
                    {
                        CloseView(confirmViewId);
                        callback?.Invoke(confirmed);
                    }
                };
            }

            if (!OpenView(confirmViewId, effectiveData))
                return false;

            if (TryGetBinding<IToolkitConfirmBinding>(confirmViewId, out var binding))
                binding.ApplyConfirm(effectiveData);

            return true;
        }

        public bool ShowLoading(string message = null, float progress01 = -1f, bool isBlocking = true)
        {
            return ShowLoading(new UIToolkitLoadingViewData
            {
                LoadingId = "default",
                Message = message,
                Progress01 = progress01,
                IsBlocking = isBlocking
            });
        }

        public bool ShowLoading(UIToolkitLoadingViewData data)
        {
            if (data == null)
                return false;

            if (!OpenView(loadingViewId, data))
                return false;

            if (TryGetBinding<IToolkitLoadingBinding>(loadingViewId, out var binding))
                binding.ApplyLoading(data);

            return true;
        }

        public bool HideLoading()
        {
            return CloseView(loadingViewId);
        }

        public bool TryGetBinding<T>(string viewId, out T binding) where T : class
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

        private void PushOpenView(string viewId, UIToolkitViewBackPolicy backPolicy)
        {
            _openStack.Remove(viewId);
            _openStack.Add(viewId);

            _backStack.Remove(viewId);
            if (backPolicy == UIToolkitViewBackPolicy.CloseOnBack)
                _backStack.Add(viewId);
        }

        private void RemoveTrackedView(string viewId)
        {
            _openStack.Remove(viewId);
            _backStack.Remove(viewId);
        }

        private bool EnsureReady()
        {
            ResolveReferences(false);
            if (viewFactory != null)
                return true;

            Warn("未绑定 UIToolkitViewFactory，无法执行 UI Toolkit View 操作。");
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
            var blockMode = UIMode.Menu;
            for (var i = _openStack.Count - 1; i >= 0; i--)
            {
                if (viewFactory != null
                    && viewFactory.TryGetInstance(_openStack[i], out var instance)
                    && instance != null
                    && instance.IsOpen
                    && instance.BlocksGameplayInput)
                {
                    shouldBlock = true;
                    blockMode = instance.InputBlockMode;
                    break;
                }
            }

            SetGameplayInputBlocked(shouldBlock, blockMode);
        }

        private void SetGameplayInputBlocked(bool blocked)
        {
            SetGameplayInputBlocked(blocked, _gameplayInputBlockMode);
        }

        private void SetGameplayInputBlocked(bool blocked, UIMode blockMode)
        {
            if (_isGameplayInputBlocked == blocked && _gameplayInputBlockMode == blockMode)
                return;

            _isGameplayInputBlocked = blocked;
            _gameplayInputBlockMode = blockMode;
            _inputBlocker?.SetBlocked(blocked, blockMode);
        }

        private void Warn(string message)
        {
            if (logWarnings)
                Debug.LogWarning($"[UIToolkitUIManager] {message}", this);
        }
    }
}
