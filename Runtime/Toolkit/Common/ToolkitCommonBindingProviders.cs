using NiumaUI.Toolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace NiumaUI.Toolkit.Common
{
    /// <summary>
    /// 默认 Toast Binding 创建器。
    /// ProviderId 建议填 Toast，并在注册表 Toast 条目中使用同名 BindingProviderId。
    /// </summary>
    public sealed class ToolkitToastBindingProvider : ToolkitViewBindingProviderBase
    {
        [Header("UXML 元素 Name")]
        [Tooltip("消息 Label 的 name。")]
        [SerializeField] private string messageLabelName = "Message";

        [Tooltip("Toast 可选样式根节点。为空时使用根节点。")]
        [SerializeField] private string styleRootName;

        public override IToolkitViewBinding CreateBinding()
        {
            return new ToolkitToastBinding(messageLabelName, styleRootName);
        }
    }

    /// <summary>
    /// 默认 Toast Binding。支持 DurationSeconds 到期后自动回调关闭。
    /// </summary>
    public sealed class ToolkitToastBinding : ToolkitViewBindingBase, IToolkitToastBinding
    {
        private readonly string _messageLabelName;
        private readonly string _styleRootName;
        private Label _messageLabel;
        private VisualElement _styleRoot;
        private UIToolkitToastViewData _data;
        private string _activeStyleKey;
        private float _remainingSeconds;

        public ToolkitToastBinding(string messageLabelName, string styleRootName)
        {
            _messageLabelName = messageLabelName;
            _styleRootName = styleRootName;
        }

        protected override void OnInitialize()
        {
            _messageLabel = QueryLabel(_messageLabelName);
            _styleRoot = string.IsNullOrWhiteSpace(_styleRootName) ? Root : Root?.Q<VisualElement>(_styleRootName.Trim());
        }

        protected override void OnRefresh(object viewData)
        {
            if (viewData is UIToolkitToastViewData data)
                ApplyToast(data);
        }

        protected override void OnClose()
        {
            ClearStyleKey();
            _data = null;
            _remainingSeconds = 0f;
        }

        protected override void OnTick(float deltaTime)
        {
            if (_data == null || _data.DurationSeconds <= 0f)
                return;

            _remainingSeconds -= deltaTime;
            if (_remainingSeconds <= 0f)
            {
                var callback = _data.OnExpired;
                _data = null;
                callback?.Invoke();
            }
        }

        public void ApplyToast(UIToolkitToastViewData data)
        {
            _data = data;
            _remainingSeconds = data != null ? Mathf.Max(0f, data.DurationSeconds) : 0f;

            if (_messageLabel != null)
                _messageLabel.text = data?.Message ?? string.Empty;

            ApplyStyleKey(data?.StyleKey);
        }

        private void ApplyStyleKey(string styleKey)
        {
            ClearStyleKey();
            if (_styleRoot == null || string.IsNullOrWhiteSpace(styleKey))
                return;

            _activeStyleKey = styleKey.Trim();
            _styleRoot.AddToClassList(_activeStyleKey);
        }

        private void ClearStyleKey()
        {
            if (_styleRoot != null && !string.IsNullOrWhiteSpace(_activeStyleKey))
                _styleRoot.RemoveFromClassList(_activeStyleKey);

            _activeStyleKey = null;
        }

        private Label QueryLabel(string elementName)
        {
            return string.IsNullOrWhiteSpace(elementName) ? null : Query<Label>(elementName.Trim());
        }
    }

    /// <summary>
    /// 默认 Confirm Binding 创建器。
    /// </summary>
    public sealed class ToolkitConfirmBindingProvider : ToolkitViewBindingProviderBase
    {
        [Header("UXML 元素 Name")]
        [SerializeField] private string titleLabelName = "Title";
        [SerializeField] private string messageLabelName = "Message";
        [SerializeField] private string confirmButtonName = "ConfirmButton";
        [SerializeField] private string cancelButtonName = "CancelButton";

        public override IToolkitViewBinding CreateBinding()
        {
            return new ToolkitConfirmBinding(titleLabelName, messageLabelName, confirmButtonName, cancelButtonName);
        }
    }

    /// <summary>
    /// 默认 Confirm Binding。
    /// </summary>
    public sealed class ToolkitConfirmBinding : ToolkitViewBindingBase, IToolkitConfirmBinding
    {
        private readonly string _titleLabelName;
        private readonly string _messageLabelName;
        private readonly string _confirmButtonName;
        private readonly string _cancelButtonName;
        private Label _titleLabel;
        private Label _messageLabel;
        private Button _confirmButton;
        private Button _cancelButton;
        private UIToolkitConfirmViewData _data;

        public ToolkitConfirmBinding(string titleLabelName, string messageLabelName, string confirmButtonName, string cancelButtonName)
        {
            _titleLabelName = titleLabelName;
            _messageLabelName = messageLabelName;
            _confirmButtonName = confirmButtonName;
            _cancelButtonName = cancelButtonName;
        }

        protected override void OnInitialize()
        {
            _titleLabel = QueryLabel(_titleLabelName);
            _messageLabel = QueryLabel(_messageLabelName);
            _confirmButton = QueryButton(_confirmButtonName);
            _cancelButton = QueryButton(_cancelButtonName);

            if (_confirmButton != null)
                _confirmButton.clicked += HandleConfirm;

            if (_cancelButton != null)
                _cancelButton.clicked += HandleCancel;
        }

        protected override void OnRefresh(object viewData)
        {
            if (viewData is UIToolkitConfirmViewData data)
                ApplyConfirm(data);
        }

        protected override void OnClose()
        {
            _data = null;
        }

        protected override void OnDispose()
        {
            if (_confirmButton != null)
                _confirmButton.clicked -= HandleConfirm;

            if (_cancelButton != null)
                _cancelButton.clicked -= HandleCancel;
        }

        public void ApplyConfirm(UIToolkitConfirmViewData data)
        {
            _data = data;

            if (_titleLabel != null)
                _titleLabel.text = data?.Title ?? string.Empty;

            if (_messageLabel != null)
                _messageLabel.text = data?.Message ?? string.Empty;

            if (_confirmButton != null)
            {
                _confirmButton.text = string.IsNullOrWhiteSpace(data?.ConfirmText) ? "确定" : data.ConfirmText;
                _confirmButton.style.display = DisplayStyle.Flex;
            }

            if (_cancelButton != null)
            {
                _cancelButton.text = string.IsNullOrWhiteSpace(data?.CancelText) ? "取消" : data.CancelText;
                _cancelButton.style.display = data != null && data.ShowCancel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void HandleConfirm()
        {
            _data?.Callback?.Invoke(true);
        }

        private void HandleCancel()
        {
            _data?.Callback?.Invoke(false);
        }

        private Label QueryLabel(string elementName)
        {
            return string.IsNullOrWhiteSpace(elementName) ? null : Query<Label>(elementName.Trim());
        }

        private Button QueryButton(string elementName)
        {
            return string.IsNullOrWhiteSpace(elementName) ? null : Query<Button>(elementName.Trim());
        }
    }

    /// <summary>
    /// 默认 Loading Binding 创建器。
    /// </summary>
    public sealed class ToolkitLoadingBindingProvider : ToolkitViewBindingProviderBase
    {
        [Header("UXML 元素 Name")]
        [SerializeField] private string messageLabelName = "Message";
        [SerializeField] private string progressBarName = "Progress";
        [SerializeField] private string progressFillName = "ProgressFill";
        [SerializeField] private string blockingRootName = "BlockingRoot";

        public override IToolkitViewBinding CreateBinding()
        {
            return new ToolkitLoadingBinding(messageLabelName, progressBarName, progressFillName, blockingRootName);
        }
    }

    /// <summary>
    /// 默认 Loading Binding。
    /// </summary>
    public sealed class ToolkitLoadingBinding : ToolkitViewBindingBase, IToolkitLoadingBinding
    {
        private readonly string _messageLabelName;
        private readonly string _progressBarName;
        private readonly string _progressFillName;
        private readonly string _blockingRootName;
        private Label _messageLabel;
        private ProgressBar _progressBar;
        private VisualElement _progressFill;
        private VisualElement _blockingRoot;

        public ToolkitLoadingBinding(string messageLabelName, string progressBarName, string progressFillName, string blockingRootName)
        {
            _messageLabelName = messageLabelName;
            _progressBarName = progressBarName;
            _progressFillName = progressFillName;
            _blockingRootName = blockingRootName;
        }

        protected override void OnInitialize()
        {
            _messageLabel = QueryLabel(_messageLabelName);
            _progressBar = QueryProgressBar(_progressBarName);
            _progressFill = QueryElement(_progressFillName);
            _blockingRoot = QueryElement(_blockingRootName) ?? Root;
        }

        protected override void OnRefresh(object viewData)
        {
            if (viewData is UIToolkitLoadingViewData data)
                ApplyLoading(data);
        }

        public void ApplyLoading(UIToolkitLoadingViewData data)
        {
            data ??= new UIToolkitLoadingViewData();

            if (_messageLabel != null)
                _messageLabel.text = data.Message ?? string.Empty;

            var hasProgress = data.Progress01 >= 0f;
            var progress = Mathf.Clamp01(data.Progress01);

            if (_progressBar != null)
            {
                _progressBar.lowValue = 0f;
                _progressBar.highValue = 1f;
                _progressBar.value = progress;
                _progressBar.style.display = hasProgress ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_progressFill != null)
            {
                _progressFill.style.width = Length.Percent(hasProgress ? progress * 100f : 0f);
                _progressFill.style.display = hasProgress ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_blockingRoot != null)
                _blockingRoot.pickingMode = data.IsBlocking ? PickingMode.Position : PickingMode.Ignore;
        }

        private Label QueryLabel(string elementName)
        {
            return string.IsNullOrWhiteSpace(elementName) ? null : Query<Label>(elementName.Trim());
        }

        private ProgressBar QueryProgressBar(string elementName)
        {
            return string.IsNullOrWhiteSpace(elementName) ? null : Query<ProgressBar>(elementName.Trim());
        }

        private VisualElement QueryElement(string elementName)
        {
            return string.IsNullOrWhiteSpace(elementName) ? null : Root?.Q<VisualElement>(elementName.Trim());
        }
    }
}