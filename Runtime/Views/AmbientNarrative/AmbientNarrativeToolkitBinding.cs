using NiumaUI.Toolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace NiumaUI.Views.AmbientNarrative
{
    /// <summary>
    /// UI Toolkit 环境叙事 Binding 创建器。
    /// 挂到 UIRoot/UIToolkitRoot/BindingProviders，并拖入 UIToolkitViewFactory.Binding Provider Behaviours。
    /// </summary>
    public sealed class AmbientNarrativeToolkitBindingProvider : ToolkitViewBindingProviderBase
    {
        [Header("UXML 元素 Name")]
        [Tooltip("气泡根节点 VisualElement 的 name。Bubble 模式会显示它。")]
        [SerializeField] private string bubbleRootName = "AmbientBubbleRoot";

        [Tooltip("气泡说话人 Label 的 name。可留空。")]
        [SerializeField] private string bubbleSpeakerLabelName = "BubbleSpeakerText";

        [Tooltip("气泡正文 Label 的 name。")]
        [SerializeField] private string bubbleBodyLabelName = "BubbleBodyText";

        [Tooltip("字幕根节点 VisualElement 的 name。Subtitle / Monologue 模式会显示它。")]
        [SerializeField] private string subtitleRootName = "AmbientSubtitleRoot";

        [Tooltip("字幕说话人 Label 的 name。可留空。")]
        [SerializeField] private string subtitleSpeakerLabelName = "SubtitleSpeakerText";

        [Tooltip("字幕正文 Label 的 name。")]
        [SerializeField] private string subtitleBodyLabelName = "SubtitleBodyText";

        [Header("显示")]
        [Tooltip("没有环境叙事内容时是否隐藏整个 View 根节点。")]
        [SerializeField] private bool hideRootWhenEmpty = true;

        [Tooltip("Bubble 模式带屏幕坐标时，是否把气泡根节点设置为 absolute 并跟随坐标。")]
        [SerializeField] private bool positionBubbleByScreenPoint = true;

        [Header("调试")]
        [Tooltip("收到不支持的 ViewData 类型时是否输出警告。")]
        [SerializeField] private bool logWarnings = true;

        public override IToolkitViewBinding CreateBinding()
        {
            return new AmbientNarrativeToolkitBinding(
                bubbleRootName,
                bubbleSpeakerLabelName,
                bubbleBodyLabelName,
                subtitleRootName,
                subtitleSpeakerLabelName,
                subtitleBodyLabelName,
                hideRootWhenEmpty,
                positionBubbleByScreenPoint,
                logWarnings);
        }
    }

    /// <summary>
    /// UI Toolkit 环境叙事 Binding。
    /// 只负责写入 VisualElement，不负责触发环境叙事。
    /// </summary>
    public sealed class AmbientNarrativeToolkitBinding : ToolkitViewBindingBase
    {
        private readonly string _bubbleRootName;
        private readonly string _bubbleSpeakerLabelName;
        private readonly string _bubbleBodyLabelName;
        private readonly string _subtitleRootName;
        private readonly string _subtitleSpeakerLabelName;
        private readonly string _subtitleBodyLabelName;
        private readonly bool _hideRootWhenEmpty;
        private readonly bool _positionBubbleByScreenPoint;
        private readonly bool _logWarnings;

        private VisualElement _bubbleRoot;
        private Label _bubbleSpeakerLabel;
        private Label _bubbleBodyLabel;
        private VisualElement _subtitleRoot;
        private Label _subtitleSpeakerLabel;
        private Label _subtitleBodyLabel;

        public AmbientNarrativeToolkitBinding(
            string bubbleRootName,
            string bubbleSpeakerLabelName,
            string bubbleBodyLabelName,
            string subtitleRootName,
            string subtitleSpeakerLabelName,
            string subtitleBodyLabelName,
            bool hideRootWhenEmpty,
            bool positionBubbleByScreenPoint,
            bool logWarnings)
        {
            _bubbleRootName = bubbleRootName;
            _bubbleSpeakerLabelName = bubbleSpeakerLabelName;
            _bubbleBodyLabelName = bubbleBodyLabelName;
            _subtitleRootName = subtitleRootName;
            _subtitleSpeakerLabelName = subtitleSpeakerLabelName;
            _subtitleBodyLabelName = subtitleBodyLabelName;
            _hideRootWhenEmpty = hideRootWhenEmpty;
            _positionBubbleByScreenPoint = positionBubbleByScreenPoint;
            _logWarnings = logWarnings;
        }

        protected override void OnInitialize()
        {
            _bubbleRoot = QueryElement(_bubbleRootName);
            _bubbleSpeakerLabel = QueryLabel(_bubbleSpeakerLabelName);
            _bubbleBodyLabel = QueryLabel(_bubbleBodyLabelName);
            _subtitleRoot = QueryElement(_subtitleRootName);
            _subtitleSpeakerLabel = QueryLabel(_subtitleSpeakerLabelName);
            _subtitleBodyLabel = QueryLabel(_subtitleBodyLabelName);
            ApplyData(AmbientNarrativeToolkitViewData.Empty());
        }

        protected override void OnRefresh(object viewData)
        {
            if (viewData is AmbientNarrativeToolkitViewData data)
            {
                ApplyData(data);
                return;
            }

            if (viewData != null)
                Warn($"不支持的环境叙事 ViewData 类型：{viewData.GetType().Name}。");
        }

        protected override void OnClose()
        {
            ApplyData(AmbientNarrativeToolkitViewData.Empty());
        }

        private void ApplyData(AmbientNarrativeToolkitViewData data)
        {
            data ??= AmbientNarrativeToolkitViewData.Empty();

            if (_hideRootWhenEmpty && Root != null)
                Root.style.display = data.HasLine ? DisplayStyle.Flex : DisplayStyle.None;

            if (!data.HasLine)
            {
                SetDisplay(_bubbleRoot, false);
                SetDisplay(_subtitleRoot, false);
                return;
            }

            if (data.UseBubble)
                ApplyBubble(data);
            else
                ApplySubtitle(data);
        }

        private void ApplyBubble(AmbientNarrativeToolkitViewData data)
        {
            SetDisplay(_bubbleRoot, true);
            SetDisplay(_subtitleRoot, false);

            if (_bubbleSpeakerLabel != null)
                _bubbleSpeakerLabel.text = data.Speaker ?? string.Empty;

            if (_bubbleBodyLabel != null)
                _bubbleBodyLabel.text = data.Body ?? string.Empty;

            if (_positionBubbleByScreenPoint && _bubbleRoot != null && data.HasScreenPosition && Root?.panel != null)
            {
                var panelPosition = RuntimePanelUtils.ScreenToPanel(Root.panel, data.BubbleScreenPosition);
                _bubbleRoot.style.position = Position.Absolute;
                _bubbleRoot.style.left = panelPosition.x;
                _bubbleRoot.style.top = panelPosition.y;
            }
        }

        private void ApplySubtitle(AmbientNarrativeToolkitViewData data)
        {
            SetDisplay(_bubbleRoot, false);
            SetDisplay(_subtitleRoot, true);

            if (_subtitleSpeakerLabel != null)
                _subtitleSpeakerLabel.text = data.Speaker ?? string.Empty;

            if (_subtitleBodyLabel != null)
                _subtitleBodyLabel.text = data.Body ?? string.Empty;
        }

        private Label QueryLabel(string elementName)
        {
            return string.IsNullOrWhiteSpace(elementName) ? null : Query<Label>(elementName.Trim());
        }

        private VisualElement QueryElement(string elementName)
        {
            return string.IsNullOrWhiteSpace(elementName) ? null : Root?.Q<VisualElement>(elementName.Trim());
        }

        private static void SetDisplay(VisualElement element, bool visible)
        {
            if (element != null)
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Warn(string message)
        {
            if (_logWarnings && !string.IsNullOrWhiteSpace(message))
                Debug.LogWarning($"[AmbientNarrativeToolkitBinding] {message}");
        }
    }
}
