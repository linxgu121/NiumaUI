using NiumaUI.Toolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace NiumaUI.Views.Interaction
{
    /// <summary>
    /// UI Toolkit 交互提示 Binding 创建器。
    /// </summary>
    public sealed class InteractionPromptToolkitBindingProvider : ToolkitViewBindingProviderBase
    {
        [Header("UXML 元素 Name")]
        [Tooltip("提示文本 Label 的 name。")]
        [SerializeField] private string promptLabelName = "PromptText";

        [Tooltip("目标名称 Label 的 name。可留空。")]
        [SerializeField] private string targetLabelName = "TargetName";

        [Tooltip("长按进度 ProgressBar 的 name。可留空。")]
        [SerializeField] private string progressBarName = "HoldProgress";

        [Tooltip("自定义进度填充 VisualElement 的 name。可留空；填写后脚本会修改 width 百分比。")]
        [SerializeField] private string progressFillName;

        [Header("显示")]
        [Tooltip("无目标时是否隐藏根节点。")]
        [SerializeField] private bool hideRootWhenEmpty = true;

        public override IToolkitViewBinding CreateBinding()
        {
            return new InteractionPromptToolkitBinding(
                promptLabelName,
                targetLabelName,
                progressBarName,
                progressFillName,
                hideRootWhenEmpty);
        }
    }

    /// <summary>
    /// UI Toolkit 交互提示 Binding。
    /// </summary>
    public sealed class InteractionPromptToolkitBinding : ToolkitViewBindingBase
    {
        private readonly string _promptLabelName;
        private readonly string _targetLabelName;
        private readonly string _progressBarName;
        private readonly string _progressFillName;
        private readonly bool _hideRootWhenEmpty;
        private Label _promptLabel;
        private Label _targetLabel;
        private ProgressBar _progressBar;
        private VisualElement _progressFill;

        public InteractionPromptToolkitBinding(
            string promptLabelName,
            string targetLabelName,
            string progressBarName,
            string progressFillName,
            bool hideRootWhenEmpty)
        {
            _promptLabelName = promptLabelName;
            _targetLabelName = targetLabelName;
            _progressBarName = progressBarName;
            _progressFillName = progressFillName;
            _hideRootWhenEmpty = hideRootWhenEmpty;
        }

        protected override void OnInitialize()
        {
            _promptLabel = QueryLabel(_promptLabelName);
            _targetLabel = QueryLabel(_targetLabelName);
            _progressBar = QueryProgressBar(_progressBarName);
            _progressFill = QueryElement(_progressFillName);
            ApplyData(InteractionPromptToolkitViewData.Empty());
        }

        protected override void OnRefresh(object viewData)
        {
            if (viewData is InteractionPromptToolkitViewData data)
                ApplyData(data);
        }

        protected override void OnClose()
        {
            ApplyData(InteractionPromptToolkitViewData.Empty());
        }

        private void ApplyData(InteractionPromptToolkitViewData data)
        {
            data ??= InteractionPromptToolkitViewData.Empty();

            if (_hideRootWhenEmpty && Root != null)
                Root.style.display = data.HasTarget ? DisplayStyle.Flex : DisplayStyle.None;

            if (_promptLabel != null)
                _promptLabel.text = data.DisplayText ?? string.Empty;

            if (_targetLabel != null)
                _targetLabel.text = data.TargetName ?? string.Empty;

            var progress = Mathf.Clamp01(data.HoldProgress);
            if (_progressBar != null)
            {
                _progressBar.lowValue = 0f;
                _progressBar.highValue = 1f;
                _progressBar.value = progress;
                _progressBar.style.display = data.IsHolding || progress > 0f ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_progressFill != null)
            {
                _progressFill.style.width = Length.Percent(progress * 100f);
                _progressFill.style.display = data.IsHolding || progress > 0f ? DisplayStyle.Flex : DisplayStyle.None;
            }
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