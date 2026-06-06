using System;
using NiumaAudio.Bridge;
using UnityEngine;

namespace NiumaUI.AudioBridge
{
    /// <summary>
    /// 单个 UI 视图的音频覆盖配置。
    /// ViewId 填 UIManager.PushView 使用的视图 ID，例如 DialogueWindow、InventoryPanel。
    /// </summary>
    [Serializable]
    public sealed class UIViewAudioCueSet
    {
        [Tooltip("UI 视图 ID。填写 UIManager.PushView / UIViewRegistrySO 中配置的 ViewId，例如 DialogueWindow。")]
        public string ViewId;

        [Tooltip("该视图打开时播放的音效。为空时使用 UIAudioBridge 的默认打开音效。")]
        public AudioCueBinding ViewPushedCue = new AudioCueBinding
        {
            SourceModule = "NiumaUI"
        };

        [Tooltip("该视图关闭时播放的音效。为空时使用 UIAudioBridge 的默认关闭音效。")]
        public AudioCueBinding ViewPoppedCue = new AudioCueBinding
        {
            SourceModule = "NiumaUI"
        };

        [Tooltip("该视图成为焦点时播放的音效。为空时使用 UIAudioBridge 的默认焦点变化音效。")]
        public AudioCueBinding FocusChangedCue = new AudioCueBinding
        {
            SourceModule = "NiumaUI"
        };
    }
}
