using NiumaUI.Core;
using TMPro;
using UnityEngine;

namespace NiumaUI.Views.Dialogue
{
    public sealed class DialogueWindowBinding : ViewBindingBase
    {
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private GameObject continueHint;

        protected override ViewBase CreateView()
        {
            return new DialogueWindowView(this);
        }

        public void SetContent(string speaker, string body, bool showContinueHint)
        {
            if (speakerText != null)
                speakerText.text = speaker ?? string.Empty;

            if (bodyText != null)
                bodyText.text = body ?? string.Empty;

            if (continueHint != null)
                continueHint.SetActive(showContinueHint);
        }
    }
}
