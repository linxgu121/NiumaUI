using NiumaUI.Core;

namespace NiumaUI.Views.Dialogue
{
    /// <summary>
    /// 对话窗口视图
    /// </summary>
    public sealed class DialogueWindowView : ViewBase
    {
        private readonly DialogueWindowBinding _binding;
        private string _speaker;
        private string _body;
        private bool _showContinueHint;

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

        public void SetContinueHintVisible(bool visible)
        {
            _showContinueHint = visible;
            Refresh();
        }

        protected override void OnRefresh()
        {
            _binding.SetContent(_speaker, _body, _showContinueHint);
        }

        protected override void OnClose()
        {
            _speaker = string.Empty;
            _body = string.Empty;
            _showContinueHint = false;
            _binding.SetContent(_speaker, _body, _showContinueHint);
        }
    }
}
