using System;
using UnityEngine;

namespace NiumaUI.Views.AmbientNarrative
{
    /// <summary>
    /// UI Toolkit 环境叙事表现数据。
    /// 只保存 UI 需要显示的事实，不引用 Gal / Story 等业务模块。
    /// </summary>
    [Serializable]
    public sealed class AmbientNarrativeToolkitViewData
    {
        public bool HasLine;
        public bool UseBubble;
        public string Speaker;
        public string Body;
        public string ModeKey;
        public bool HasScreenPosition;
        public Vector2 BubbleScreenPosition;

        public static AmbientNarrativeToolkitViewData Empty()
        {
            return new AmbientNarrativeToolkitViewData
            {
                HasLine = false,
                UseBubble = false,
                Speaker = string.Empty,
                Body = string.Empty,
                ModeKey = string.Empty,
                HasScreenPosition = false,
                BubbleScreenPosition = Vector2.zero
            };
        }
    }
}
