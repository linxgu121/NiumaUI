using System;

namespace NiumaUI.Views.Dialogue
{
    /// <summary>
    /// UI Toolkit 对话选项表现数据。
    /// 这是 NiumaUI 自己的 UI 数据契约，业务模块需要把自己的选项 DTO 转换到这里，不允许 UI 核心直接引用 Gal / Story 等业务类型。
    /// </summary>
    [Serializable]
    public sealed class DialogueToolkitChoiceData
    {
        public string ChoiceId;
        public string DisplayText;
        public string DisabledText;
        public bool IsAvailable = true;

        [NonSerialized]
        public Action<string> OnSelected;
    }

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
        public DialogueToolkitChoiceData[] Choices = Array.Empty<DialogueToolkitChoiceData>();

        public static DialogueToolkitViewData Empty()
        {
            return new DialogueToolkitViewData
            {
                Speaker = string.Empty,
                Body = string.Empty,
                ShowContinueHint = false,
                Choices = Array.Empty<DialogueToolkitChoiceData>()
            };
        }
    }
}