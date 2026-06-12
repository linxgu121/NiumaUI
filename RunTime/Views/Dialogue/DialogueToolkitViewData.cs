using System;

namespace NiumaUI.Views.Dialogue
{
    /// <summary>
    /// UI Toolkit 对话窗口表现数据。
    /// 业务模块只把当前句子和选项转换到这里，具体 UXML 元素由 DialogueToolkitBinding 写入。
    /// </summary>
    [Serializable]
    public sealed class DialogueToolkitViewData
    {
        public string Speaker;
        public string Body;
        public bool ShowContinueHint;
        public DialogueChoiceOptionData[] Choices = Array.Empty<DialogueChoiceOptionData>();

        public static DialogueToolkitViewData Empty()
        {
            return new DialogueToolkitViewData
            {
                Speaker = string.Empty,
                Body = string.Empty,
                ShowContinueHint = false,
                Choices = Array.Empty<DialogueChoiceOptionData>()
            };
        }
    }
}