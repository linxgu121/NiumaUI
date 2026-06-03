using System;
using NiumaUI.Core;

namespace NiumaUI.Views.Dialogue
{
    /// <summary>
    /// 对话选项 UI 数据。
    /// UI 模块只识别按钮显示与点击回调，不关心 Gal 的条件、行为和状态机。
    /// </summary>
    public sealed class DialogueChoiceOptionData
    {
        public string ChoiceId;
        public string DisplayText;
        public bool IsAvailable;
        public string DisabledText;
        public Action<string> OnSelected;
    }

    /// <summary>
    /// 对话窗口视图。
    /// 纯 C# 层只保存表现数据，所有 Unity 对象写入都交给 Binding。
    /// </summary>
    public sealed class DialogueWindowView : ViewBase
    {
        private readonly DialogueWindowBinding _binding;
        private string _speaker;
        private string _body;
        private bool _showContinueHint;
        private DialogueChoiceOptionData[] _choices = Array.Empty<DialogueChoiceOptionData>();

        public DialogueWindowView(DialogueWindowBinding binding)
        {
            _binding = binding;
        }

        public void SetLine(string speaker, string body, bool showContinueHint = false)
        {
            _speaker = speaker;
            _body = body;
            _showContinueHint = showContinueHint;
            Refresh();
        }

        /// <summary>
        /// 设置当前句子的选项按钮。
        /// 为空时隐藏选项区域，显示逻辑由 Binding 负责。
        /// </summary>
        public void SetChoices(DialogueChoiceOptionData[] choices)
        {
            _choices = choices ?? Array.Empty<DialogueChoiceOptionData>();
            Refresh();
        }

        public void SetContinueHintVisible(bool visible)
        {
            _showContinueHint = visible;
            Refresh();
        }

        protected override void OnRefresh()
        {
            _binding.SetContent(_speaker, _body, _showContinueHint);
            _binding.SetChoices(_choices);
        }

        protected override void OnClose()
        {
            _speaker = string.Empty;
            _body = string.Empty;
            _showContinueHint = false;
            _choices = Array.Empty<DialogueChoiceOptionData>();
            _binding.SetContent(_speaker, _body, _showContinueHint);
            _binding.SetChoices(_choices);
        }
    }
}
