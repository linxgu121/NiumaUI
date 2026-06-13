using System;
using System.Collections.Generic;
using NiumaUI.Toolkit;
using NiumaUI.Toolkit.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace NiumaUI.Views.Dialogue
{
    /// <summary>
    /// UI Toolkit 对话窗口 Binding 创建器。
    /// 挂到 UIRoot/UIToolkitRoot/BindingProviders，并拖入 UIToolkitViewFactory.Binding Provider Behaviours。
    /// ProviderId 需要与 UIToolkitViewRegistrySO 中 DialogueWindow 条目的 BindingProviderId 完全一致。
    /// </summary>
    public sealed class DialogueToolkitBindingProvider : ToolkitViewBindingProviderBase
    {
        [Header("UXML 元素 Name")]
        [Tooltip("说话人 Label 的 name。为空则不写入说话人。")]
        [SerializeField] private string speakerLabelName = "SpeakerText";

        [Tooltip("正文 Label 的 name。为空则不写入正文。")]
        [SerializeField] private string bodyLabelName = "BodyText";

        [Tooltip("继续提示 VisualElement 的 name。为空则不控制继续提示。")]
        [SerializeField] private string continueHintName = "ContinueHint";

        [Tooltip("选项父节点 VisualElement 的 name。为空则直接在根节点下查找选项按钮。")]
        [SerializeField] private string choiceRootName = "ChoiceRoot";

        [Header("选项按钮")]
        [Tooltip("手动指定选项 Button 的 name 列表。数组顺序就是选项显示顺序。若为空，则按 Choice Button Class 查找。")]
        [SerializeField] private string[] choiceButtonNames = Array.Empty<string>();

        [Tooltip("未手动指定按钮名时，查找带有该 USS class 的 Button。默认 niuma-dialogue-choice。")]
        [SerializeField] private string choiceButtonClass = "niuma-dialogue-choice";

        [Tooltip("没有选项时是否隐藏 ChoiceRoot。")]
        [SerializeField] private bool hideChoiceRootWhenEmpty = true;

        [Header("调试")]
        [Tooltip("缺少元素或选项数量超过按钮数量时是否输出警告。")]
        [SerializeField] private bool logWarnings = true;

        public override IToolkitViewBinding CreateBinding()
        {
            return new DialogueToolkitBinding(
                speakerLabelName,
                bodyLabelName,
                continueHintName,
                choiceRootName,
                choiceButtonNames,
                choiceButtonClass,
                hideChoiceRootWhenEmpty,
                logWarnings);
        }
    }

    /// <summary>
    /// 对话窗口局部 ViewModel。只保存表现层临时状态，不保存 Gal 剧情进度事实。
    /// </summary>
    public sealed class DialogueToolkitViewModel : UIPanelViewModelBase
    {
        public string LastSpeaker { get; private set; }
        public string LastBody { get; private set; }
        public bool LastShowContinueHint { get; private set; }
        public DialogueChoiceOptionData[] CurrentChoices { get; private set; } = Array.Empty<DialogueChoiceOptionData>();

        public void Apply(DialogueToolkitViewData data)
        {
            data ??= DialogueToolkitViewData.Empty();
            LastSpeaker = data.Speaker ?? string.Empty;
            LastBody = data.Body ?? string.Empty;
            LastShowContinueHint = data.ShowContinueHint;
            CurrentChoices = data.Choices ?? Array.Empty<DialogueChoiceOptionData>();
            MarkDirty();
        }

        protected override void OnClear(UIViewModelClearReason reason)
        {
            LastSpeaker = string.Empty;
            LastBody = string.Empty;
            LastShowContinueHint = false;
            CurrentChoices = Array.Empty<DialogueChoiceOptionData>();
        }
    }

    /// <summary>
    /// UI Toolkit 对话窗口 Binding。
    /// 只操作 VisualElement，不直接推进 Gal / Story / Quest 业务。
    /// </summary>
    public sealed class DialogueToolkitBinding : ToolkitViewBindingBase<DialogueToolkitViewData, DialogueToolkitViewModel>
    {
        private readonly string _speakerLabelName;
        private readonly string _bodyLabelName;
        private readonly string _continueHintName;
        private readonly string _choiceRootName;
        private readonly string[] _choiceButtonNames;
        private readonly string _choiceButtonClass;
        private readonly bool _hideChoiceRootWhenEmpty;
        private readonly bool _logWarnings;
        private readonly List<Button> _choiceButtons = new List<Button>();
        private readonly List<Action> _choiceClickHandlers = new List<Action>();
        private Label _speakerLabel;
        private Label _bodyLabel;
        private VisualElement _continueHint;
        private VisualElement _choiceRoot;
        private bool _warnedMissingChoiceButtons;
        private bool _warnedChoiceOverflow;

        public DialogueToolkitBinding(
            string speakerLabelName,
            string bodyLabelName,
            string continueHintName,
            string choiceRootName,
            string[] choiceButtonNames,
            string choiceButtonClass,
            bool hideChoiceRootWhenEmpty,
            bool logWarnings)
        {
            _speakerLabelName = speakerLabelName;
            _bodyLabelName = bodyLabelName;
            _continueHintName = continueHintName;
            _choiceRootName = choiceRootName;
            _choiceButtonNames = choiceButtonNames ?? Array.Empty<string>();
            _choiceButtonClass = string.IsNullOrWhiteSpace(choiceButtonClass) ? "niuma-dialogue-choice" : choiceButtonClass.Trim();
            _hideChoiceRootWhenEmpty = hideChoiceRootWhenEmpty;
            _logWarnings = logWarnings;
        }

        protected override void OnInitializeTyped()
        {
            _speakerLabel = Query<Label>(_speakerLabelName);
            _bodyLabel = Query<Label>(_bodyLabelName);
            _continueHint = QueryElement(_continueHintName);
            _choiceRoot = QueryElement(_choiceRootName);
            RebuildChoiceButtons();
            ApplyData(DialogueToolkitViewData.Empty(), ViewModel);
        }

        protected override void OnRefreshTyped(DialogueToolkitViewData viewData, DialogueToolkitViewModel viewModel)
        {
            ApplyData(viewData, viewModel);
        }

        protected override void OnClearTyped(UIViewModelClearReason reason)
        {
            ApplyVisualState(string.Empty, string.Empty, false, Array.Empty<DialogueChoiceOptionData>());
        }

        protected override void OnDisposeTyped()
        {
            UnregisterChoiceHandlers();
        }

        private void ApplyData(DialogueToolkitViewData data, DialogueToolkitViewModel viewModel)
        {
            data ??= DialogueToolkitViewData.Empty();
            viewModel.Apply(data);
            ApplyVisualState(
                viewModel.LastSpeaker,
                viewModel.LastBody,
                viewModel.LastShowContinueHint,
                viewModel.CurrentChoices);
        }

        private void ApplyVisualState(string speaker, string body, bool showContinueHint, DialogueChoiceOptionData[] choices)
        {
            if (_speakerLabel != null)
                _speakerLabel.text = speaker ?? string.Empty;

            if (_bodyLabel != null)
                _bodyLabel.text = body ?? string.Empty;

            SetDisplay(_continueHint, showContinueHint);
            ApplyChoices(choices ?? Array.Empty<DialogueChoiceOptionData>());
        }

        private void ApplyChoices(DialogueChoiceOptionData[] choices)
        {
            var optionCount = choices?.Length ?? 0;
            var hasChoices = optionCount > 0;

            if (_choiceRoot != null && _hideChoiceRootWhenEmpty)
                SetDisplay(_choiceRoot, hasChoices);

            if (hasChoices && _choiceButtons.Count == 0)
            {
                WarnOnce(ref _warnedMissingChoiceButtons, "当前句子存在选项，但没有找到任何 Toolkit 选项 Button。请配置 Choice Button Names，或给按钮添加 niuma-dialogue-choice USS class。");
                return;
            }

            var applyCount = Mathf.Min(optionCount, _choiceButtons.Count);
            for (var i = 0; i < applyCount; i++)
                BindChoiceButton(i, choices[i]);

            for (var i = applyCount; i < _choiceButtons.Count; i++)
                ClearChoiceButton(_choiceButtons[i]);

            if (optionCount > _choiceButtons.Count)
                WarnOnce(ref _warnedChoiceOverflow, $"当前句子选项数量为 {optionCount}，但 Toolkit 只找到 {_choiceButtons.Count} 个按钮，多余选项不会显示。");
        }

        private void BindChoiceButton(int index, DialogueChoiceOptionData choice)
        {
            var button = _choiceButtons[index];
            if (button == null)
                return;

            var isAvailable = choice != null && choice.IsAvailable;
            button.text = ResolveDisplayText(choice, isAvailable);
            button.SetEnabled(isAvailable);
            SetDisplay(button, true);
        }

        private static void ClearChoiceButton(Button button)
        {
            if (button == null)
                return;

            button.text = string.Empty;
            button.SetEnabled(false);
            SetDisplay(button, false);
        }

        private void RebuildChoiceButtons()
        {
            UnregisterChoiceHandlers();
            _choiceButtons.Clear();

            if (_choiceButtonNames.Length > 0)
            {
                for (var i = 0; i < _choiceButtonNames.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(_choiceButtonNames[i]))
                        continue;

                    var button = Root?.Q<Button>(_choiceButtonNames[i].Trim());
                    if (button != null)
                        _choiceButtons.Add(button);
                    else
                        Warn($"找不到选项 Button：{_choiceButtonNames[i]}。");
                }
            }
            else
            {
                var searchRoot = _choiceRoot ?? Root;
                searchRoot?.Query<Button>(className: _choiceButtonClass).ToList(_choiceButtons);
            }

            RegisterChoiceHandlers();
        }

        private void RegisterChoiceHandlers()
        {
            for (var i = 0; i < _choiceButtons.Count; i++)
            {
                var index = i;
                Action handler = () => HandleChoiceClicked(index);
                _choiceClickHandlers.Add(handler);
                _choiceButtons[i].clicked += handler;
            }
        }

        private void UnregisterChoiceHandlers()
        {
            for (var i = 0; i < _choiceButtons.Count && i < _choiceClickHandlers.Count; i++)
            {
                if (_choiceButtons[i] != null && _choiceClickHandlers[i] != null)
                    _choiceButtons[i].clicked -= _choiceClickHandlers[i];
            }

            _choiceClickHandlers.Clear();
        }

        private void HandleChoiceClicked(int index)
        {
            var choices = ViewModel?.CurrentChoices;
            if (choices == null || index < 0 || index >= choices.Length)
                return;

            var choice = choices[index];
            if (choice == null || !choice.IsAvailable)
                return;

            choice.OnSelected?.Invoke(choice.ChoiceId);
        }

        private VisualElement QueryElement(string elementName)
        {
            return string.IsNullOrWhiteSpace(elementName) ? null : Root?.Q<VisualElement>(elementName.Trim());
        }

        private static string ResolveDisplayText(DialogueChoiceOptionData choice, bool isAvailable)
        {
            if (choice == null)
                return string.Empty;

            if (isAvailable)
                return choice.DisplayText ?? string.Empty;

            return string.IsNullOrWhiteSpace(choice.DisabledText)
                ? choice.DisplayText ?? string.Empty
                : choice.DisabledText;
        }

        private static void SetDisplay(VisualElement element, bool visible)
        {
            if (element != null)
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void WarnOnce(ref bool flag, string message)
        {
            if (flag)
                return;

            flag = true;
            Warn(message);
        }

        private void Warn(string message)
        {
            if (_logWarnings && !string.IsNullOrWhiteSpace(message))
                Debug.LogWarning($"[DialogueToolkitBinding] {message}");
        }
    }
}
