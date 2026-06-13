using System;
using NiumaUI.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NiumaUI.Views.Dialogue
{
    /// <summary>
    /// 对话窗口的 Unity 绑定组件。
    /// 只负责把纯 C# View 数据写入策划手动绑定的 UI 对象，不再自动创建保底对话框或保底按钮。
    /// </summary>
    public sealed class DialogueWindowBinding : ViewBindingBase
    {
        [Header("文本引用")]
        [Tooltip("说话人文本。通常绑定对话框左上角或姓名牌上的 TMP_Text。")]
        [SerializeField] private TMP_Text speakerText;

        [Tooltip("正文文本。用于显示当前对话句子内容。")]
        [SerializeField] private TMP_Text bodyText;

        [Tooltip("继续提示物体。没有选项且可以继续推进时显示，例如“点击继续”。")]
        [SerializeField] private GameObject continueHint;

        [Header("选项引用")]
        [Tooltip("选项父节点。当前句子有选项时显示，没有选项时隐藏。")]
        [SerializeField] private RectTransform choiceRoot;

        [Tooltip("策划手动摆放的选项按钮槽位。数组顺序对应选项显示顺序，不再运行时自动补按钮。")]
        [SerializeField] private DialogueChoiceButtonBinding[] choiceSlots = Array.Empty<DialogueChoiceButtonBinding>();

        [Header("调试")]
        [Tooltip("Awake 时检查必要引用。只输出警告，不会自动创建任何 UI。")]
        [SerializeField] private bool validateOnAwake = true;

        [Tooltip("绑定缺失或选项数量超过槽位数量时输出警告。")]
        [SerializeField] private bool logBindingWarnings = true;

        private CanvasGroup _canvasGroup;
        private bool _warnedMissingChoiceSlots;
        private bool _warnedChoiceOverflow;

        private void Awake()
        {
            CacheCanvasGroup();

            if (validateOnAwake)
                ValidateRequiredReferences();
        }

        private void OnDestroy()
        {
            var slots = choiceSlots ?? Array.Empty<DialogueChoiceButtonBinding>();
            for (var i = 0; i < slots.Length; i++)
            {
                slots[i]?.Dispose();
            }
        }

        public override void Show()
        {
            CacheCanvasGroup();
            base.Show();

            if (_canvasGroup == null)
                return;

            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        public override void Hide()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            base.Hide();
        }

        protected override ViewBase CreateView()
        {
            CacheCanvasGroup();
            ValidateRequiredReferences();
            return new DialogueWindowView(this);
        }

        public void SetContent(string speaker, string body, bool showContinueHint)
        {
            // 所有 Unity 对象写入都留在 Binding 层，ViewBase 保持纯 C#。
            if (speakerText != null)
                speakerText.text = speaker ?? string.Empty;

            if (bodyText != null)
                bodyText.text = body ?? string.Empty;

            if (continueHint != null)
                continueHint.SetActive(showContinueHint);
        }

        /// <summary>
        /// 刷新当前句子的选项。
        /// 选项按钮由策划预先摆放并绑定到 choiceSlots，业务选择仍统一回到 ChoiceId。
        /// </summary>
        public void SetChoices(DialogueChoiceOptionData[] choices)
        {
            var options = choices ?? Array.Empty<DialogueChoiceOptionData>();
            var optionCount = options.Length;
            var slots = choiceSlots ?? Array.Empty<DialogueChoiceButtonBinding>();

            if (choiceRoot != null)
                choiceRoot.gameObject.SetActive(optionCount > 0);

            if (optionCount > 0 && slots.Length == 0)
            {
                WarnOnce(ref _warnedMissingChoiceSlots, "当前对话存在选项，但 DialogueWindowBinding 没有绑定任何选项槽位。");
                return;
            }

            var applyCount = Mathf.Min(optionCount, slots.Length);
            for (var i = 0; i < applyCount; i++)
            {
                if (slots[i] == null)
                {
                    Warn($"选项槽位为空：Index={i}。");
                    continue;
                }

                slots[i].Bind(options[i]);
            }

            for (var i = applyCount; i < slots.Length; i++)
            {
                if (slots[i] != null)
                    slots[i].Clear();
            }

            if (optionCount > slots.Length)
            {
                WarnOnce(ref _warnedChoiceOverflow, $"当前对话选项数量为 {optionCount}，但只绑定了 {slots.Length} 个槽位，多余选项不会显示。");
            }
        }

        private void CacheCanvasGroup()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void ValidateRequiredReferences()
        {
            if (!logBindingWarnings)
                return;

            if (speakerText == null)
                Debug.LogWarning("[NiumaUI] DialogueWindowBinding 未绑定说话人文本 speakerText。", this);

            if (bodyText == null)
                Debug.LogWarning("[NiumaUI] DialogueWindowBinding 未绑定正文文本 bodyText。", this);

            if (choiceRoot == null)
                Debug.LogWarning("[NiumaUI] DialogueWindowBinding 未绑定选项父节点 choiceRoot。没有选项时可忽略。", this);

            if (choiceSlots == null || choiceSlots.Length == 0)
                Debug.LogWarning("[NiumaUI] DialogueWindowBinding 未绑定选项槽位 choiceSlots。有分支选项的对话将无法显示选项。", this);
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
            if (logBindingWarnings)
                Debug.LogWarning($"[NiumaUI] {message}", this);
        }
    }

    /// <summary>
    /// 单个对话选项按钮槽位。
    /// 策划可以为每个槽位配置独立样式和点击表现，但最终选项选择仍通过 ChoiceId 回到对话系统。
    /// </summary>
    [Serializable]
    public sealed class DialogueChoiceButtonBinding
    {
        [Header("按钮引用")]
        [Tooltip("选项槽位根物体。为空时使用 Button 所在物体作为显示隐藏目标。")]
        [SerializeField] private GameObject slotRoot;

        [Tooltip("选项按钮。点击后会触发本槽位表现事件，并把 ChoiceId 回传给对话系统。")]
        [SerializeField] private Button button;

        [Tooltip("选项文本。显示可选文本或不可选提示文本。")]
        [SerializeField] private TMP_Text labelText;

        [Header("状态物体")]
        [Tooltip("选项可点击时显示的物体。可为空。")]
        [SerializeField] private GameObject availableRoot;

        [Tooltip("选项不可点击时显示的物体。可为空。")]
        [SerializeField] private GameObject disabledRoot;

        [Header("策划事件")]
        [Tooltip("选项数据绑定到该按钮时触发，可用于播放刷新动画或设置额外表现。参数为 ChoiceId。")]
        [SerializeField] private DialogueChoiceUnityEvent onChoiceBound = new DialogueChoiceUnityEvent();

        [Tooltip("玩家点击可用选项时触发，可用于播放音效、动画、埋点等表现逻辑。不要在这里直接推进对话业务。参数为 ChoiceId。")]
        [SerializeField] private DialogueChoiceUnityEvent onChoiceClicked = new DialogueChoiceUnityEvent();

        [NonSerialized] private bool _referencesResolved;
        private DialogueChoiceOptionData _choice;
        private string _choiceId;
        private Button _registeredButton;

        /// <summary>
        /// 绑定一个选项到当前按钮槽位。
        /// </summary>
        public void Bind(DialogueChoiceOptionData choice)
        {
            ResolveReferences();
            RegisterClickListener();

            _choice = choice;
            _choiceId = choice?.ChoiceId ?? string.Empty;

            var isAvailable = choice != null && choice.IsAvailable;
            var displayText = ResolveDisplayText(choice, isAvailable);

            SetRootActive(true);

            if (button != null)
                button.interactable = isAvailable;

            if (labelText != null)
                labelText.text = displayText;

            if (availableRoot != null)
                availableRoot.SetActive(isAvailable);

            if (disabledRoot != null)
                disabledRoot.SetActive(!isAvailable);

            onChoiceBound?.Invoke(_choiceId);
        }

        /// <summary>
        /// 清空该槽位并隐藏按钮。
        /// </summary>
        public void Clear()
        {
            _choice = null;
            _choiceId = string.Empty;

            if (button != null)
                button.interactable = false;

            SetRootActive(false);
        }

        /// <summary>
        /// 释放运行时注册的按钮监听。
        /// </summary>
        public void Dispose()
        {
            UnregisterClickListener();
        }

        private void HandleClick()
        {
            if (_choice == null || !_choice.IsAvailable)
                return;

            var choiceId = _choiceId;
            onChoiceClicked?.Invoke(choiceId);
            _choice.OnSelected?.Invoke(choiceId);
        }

        private void ResolveReferences()
        {
            if (_referencesResolved)
                return;

            _referencesResolved = true;

            if (button == null && slotRoot != null)
                button = slotRoot.GetComponentInChildren<Button>(true);

            if (labelText == null && button != null)
                labelText = button.GetComponentInChildren<TMP_Text>(true);

            if (labelText == null && slotRoot != null)
                labelText = slotRoot.GetComponentInChildren<TMP_Text>(true);
        }

        private void RegisterClickListener()
        {
            if (button == null || _registeredButton == button)
                return;

            UnregisterClickListener();
            _registeredButton = button;
            _registeredButton.onClick.AddListener(HandleClick);
        }

        private void UnregisterClickListener()
        {
            if (_registeredButton == null)
                return;

            _registeredButton.onClick.RemoveListener(HandleClick);
            _registeredButton = null;
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

        private void SetRootActive(bool active)
        {
            if (slotRoot != null)
            {
                slotRoot.SetActive(active);
                return;
            }

            if (button != null)
                button.gameObject.SetActive(active);
        }
    }

    /// <summary>
    /// 对话选项 UnityEvent。
    /// 使用派生类型可以让 Inspector 稳定显示 string 参数事件。
    /// </summary>
    [System.Serializable]
    public sealed class DialogueChoiceUnityEvent : UnityEvent<string>
    {
    }
}
