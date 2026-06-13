using System;

namespace NiumaUI.Views.Interaction
{
    /// <summary>
    /// UI Toolkit 交互提示表现数据。
    /// </summary>
    [Serializable]
    public sealed class InteractionPromptToolkitViewData
    {
        public bool HasTarget;
        public string TargetName;
        public string PromptText;
        public string DisplayText;
        public bool IsHolding;
        public float HoldProgress;

        public static InteractionPromptToolkitViewData Empty()
        {
            return new InteractionPromptToolkitViewData
            {
                HasTarget = false,
                TargetName = string.Empty,
                PromptText = string.Empty,
                DisplayText = string.Empty,
                IsHolding = false,
                HoldProgress = 0f
            };
        }
    }
}