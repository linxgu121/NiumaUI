using System;
using System.Collections.Generic;
using NiumaAudio.Bridge;
using NiumaAudio.Controller;
using NiumaAudio.Data;
using NiumaAudio.Service;
using NiumaUI.Enum;
using NiumaUI.Toolkit;
using UnityEngine;

namespace NiumaUI.AudioBridge
{
    /// <summary>
    /// NiumaUI 到 NiumaAudio 的桥接脚本。
    /// 建议挂在 UIRoot 或 Toolkit UIManager 同物体上，绑定 UIToolkitUIManager 和 AudioRoot 上的 NiumaAudioController。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIAudioBridge : MonoBehaviour
    {
        [Header("UI Toolkit 绑定")]
        [Tooltip("UI Toolkit 根控制器。拖 UIRoot/UIManager 上的 UIToolkitUIManager；为空时可自动查找。")]
        [SerializeField] private UIToolkitUIManager toolkitUIManager;

        [Tooltip("音频控制器。拖 AudioRoot 上的 NiumaAudioController；为空时可自动查找。")]
        [SerializeField] private NiumaAudioController audioController;

        [Tooltip("未手动绑定 UIToolkitUIManager 时是否自动查找场景中的 UIToolkitUIManager。正式场景建议手动绑定。")]
        [SerializeField] private bool autoFindUIToolkitUIManager = true;

        [Tooltip("未手动绑定 AudioController 时是否自动查找场景中的 NiumaAudioController。正式场景建议手动绑定。")]
        [SerializeField] private bool autoFindAudioController = true;

        [Header("默认音效")]
        [Tooltip("任意 UI 视图打开时播放的默认音效。CueId 填 AudioCueDefinition.CueId；为空则不播放。")]
        [SerializeField] private AudioCueBinding defaultViewPushedCue = new AudioCueBinding
        {
            SourceModule = "NiumaUI"
        };

        [Tooltip("任意 UI 视图关闭时播放的默认音效。CueId 填 AudioCueDefinition.CueId；为空则不播放。")]
        [SerializeField] private AudioCueBinding defaultViewPoppedCue = new AudioCueBinding
        {
            SourceModule = "NiumaUI"
        };

        [Tooltip("UI 焦点变化时播放的默认音效。CueId 填 AudioCueDefinition.CueId；为空则不播放。")]
        [SerializeField] private AudioCueBinding defaultFocusChangedCue = new AudioCueBinding
        {
            SourceModule = "NiumaUI"
        };

        [Tooltip("UI 模式切换时播放的默认音效。CueId 填 AudioCueDefinition.CueId；UIToolkitUIManager 当前不主动广播模式变化，需要外部模式源调用 NotifyModeChanged。")]
        [SerializeField] private AudioCueBinding defaultModeChangedCue = new AudioCueBinding
        {
            SourceModule = "NiumaUI"
        };

        [Header("视图覆盖")]
        [Tooltip("按 ViewId 覆盖打开、关闭、焦点音效。ViewId 填 UIToolkitViewRegistrySO 或 UIToolkitUIManager.OpenView 使用的 ID。")]
        [SerializeField] private UIViewAudioCueSet[] viewAudioCues = Array.Empty<UIViewAudioCueSet>();

        [Header("模式覆盖（手动通知）")]
        [Tooltip("按目标 UI 模式覆盖模式切换音效。Toolkit 当前需要外部调用 NotifyModeChanged 后才会播放。")]
        [SerializeField] private UIModeAudioCueSet[] modeAudioCues = Array.Empty<UIModeAudioCueSet>();

        [Header("播放规则")]
        [Tooltip("焦点清空时是否播放焦点变化音效。关闭时，FocusViewId 为空不会播放声音。")]
        [SerializeField] private bool playFocusCueWhenFocusCleared;

        [Tooltip("组件启用时是否把当前 UI 栈顶视为已聚焦状态，避免刚进场景就播放一次焦点音效。")]
        [SerializeField] private bool suppressInitialFocusCue = true;

        [Tooltip("自动查找 UIToolkitUIManager 失败后的重试间隔，避免缺失时每帧执行场景查找。0 表示每帧重试；正式场景建议手动绑定。")]
        [SerializeField] private float autoBindRetryInterval = 0.5f;

        [Header("调试")]
        [Tooltip("缺少控制器、CueId 或播放失败时是否输出警告。")]
        [SerializeField] private bool logWarnings = true;

        private readonly List<string> _trackedOpenStack = new List<string>();
        private IAudioCommand _runtimeCommand;
        private string _trackedFocusViewId;
        private bool _hasToolkitSnapshot;
        private bool _initialFocusSuppressed;
        private float _nextBindRetryTime;

        public AudioOperationResult LastAudioResult { get; private set; }

        public void SetAudioCommand(IAudioCommand command)
        {
            _runtimeCommand = command;
        }

        public void SetUIToolkitUIManager(UIToolkitUIManager manager)
        {
            ClearToolkitSnapshot();
            toolkitUIManager = manager;
            _nextBindRetryTime = 0f;
            TryBindToolkitManager();
        }

        public void SetAudioController(NiumaAudioController controller)
        {
            audioController = controller;
        }

        private void OnEnable()
        {
            _initialFocusSuppressed = false;
            _nextBindRetryTime = 0f;
            ClearToolkitSnapshot();
            TryBindToolkitManager();
        }

        private void OnDisable()
        {
            ClearToolkitSnapshot();
        }

        private void LateUpdate()
        {
            if (toolkitUIManager == null)
            {
                ClearToolkitSnapshot();

                if (autoBindRetryInterval > 0f && Time.unscaledTime < _nextBindRetryTime)
                    return;

                TryBindToolkitManager();

                if (toolkitUIManager == null)
                {
                    if (autoBindRetryInterval > 0f)
                        _nextBindRetryTime = Time.unscaledTime + autoBindRetryInterval;

                    return;
                }
            }

            if (!_hasToolkitSnapshot)
                CaptureToolkitSnapshot();

            SyncToolkitState();
        }

        public void PlayDefaultViewPushedCue()
        {
            PlayCue(defaultViewPushedCue);
        }

        public void PlayDefaultViewPoppedCue()
        {
            PlayCue(defaultViewPoppedCue);
        }

        private void HandleViewPushed(string viewId)
        {
            var cueSet = FindCueSet(viewId);
            var cue = cueSet != null && cueSet.ViewPushedCue != null && cueSet.ViewPushedCue.HasPlayableKey
                ? cueSet.ViewPushedCue
                : defaultViewPushedCue;

            PlayCue(cue);
        }

        private void HandleViewPopped(string viewId)
        {
            var cueSet = FindCueSet(viewId);
            var cue = cueSet != null && cueSet.ViewPoppedCue != null && cueSet.ViewPoppedCue.HasPlayableKey
                ? cueSet.ViewPoppedCue
                : defaultViewPoppedCue;

            PlayCue(cue);
        }

        private void HandleFocusChanged(string viewId)
        {
            if (string.IsNullOrWhiteSpace(viewId) && !playFocusCueWhenFocusCleared)
                return;

            if (suppressInitialFocusCue && !_initialFocusSuppressed)
            {
                _initialFocusSuppressed = true;
                return;
            }

            var cueSet = FindCueSet(viewId);
            var cue = cueSet != null && cueSet.FocusChangedCue != null && cueSet.FocusChangedCue.HasPlayableKey
                ? cueSet.FocusChangedCue
                : defaultFocusChangedCue;

            PlayCue(cue);
        }

        private void HandleModeChanged(UIMode oldMode, UIMode newMode)
        {
            if (oldMode == newMode)
                return;

            var cueSet = FindModeCueSet(newMode);
            var cue = cueSet != null && cueSet.ModeChangedCue != null && cueSet.ModeChangedCue.HasPlayableKey
                ? cueSet.ModeChangedCue
                : defaultModeChangedCue;

            PlayCue(cue);
        }

        /// <summary>
        /// Toolkit 当前没有公开模式切换事件；如需模式音效，由外部模式源在切换后调用。
        /// </summary>
        public void NotifyModeChanged(UIMode oldMode, UIMode newMode)
        {
            HandleModeChanged(oldMode, newMode);
        }

        private void TryBindToolkitManager()
        {
            if (toolkitUIManager != null && _hasToolkitSnapshot)
                return;

            if (!ResolveUIToolkitUIManager())
                return;

            CaptureToolkitSnapshot();
        }

        private void CaptureToolkitSnapshot()
        {
            _trackedOpenStack.Clear();

            var openStack = toolkitUIManager != null ? toolkitUIManager.OpenStack : null;
            CopyStack(openStack, _trackedOpenStack);
            _trackedFocusViewId = GetTopViewId(openStack);
            _hasToolkitSnapshot = true;

            if (suppressInitialFocusCue && !string.IsNullOrWhiteSpace(_trackedFocusViewId))
                _initialFocusSuppressed = true;
        }

        private void ClearToolkitSnapshot()
        {
            _trackedOpenStack.Clear();
            _trackedFocusViewId = null;
            _hasToolkitSnapshot = false;
        }

        private void SyncToolkitState()
        {
            if (toolkitUIManager == null)
                return;

            var currentStack = toolkitUIManager.OpenStack;

            // UIToolkitUIManager 目前未公开打开/关闭事件，这里只基于公开栈做轻量同步。
            for (var i = 0; currentStack != null && i < currentStack.Count; i++)
            {
                var viewId = currentStack[i];
                if (!string.IsNullOrWhiteSpace(viewId) && !ContainsViewId(_trackedOpenStack, viewId))
                    HandleViewPushed(viewId);
            }

            for (var i = _trackedOpenStack.Count - 1; i >= 0; i--)
            {
                var viewId = _trackedOpenStack[i];
                if (!ContainsViewId(currentStack, viewId))
                    HandleViewPopped(viewId);
            }

            var focusViewId = GetTopViewId(currentStack);
            if (!string.Equals(_trackedFocusViewId, focusViewId, StringComparison.Ordinal))
            {
                _trackedFocusViewId = focusViewId;
                HandleFocusChanged(focusViewId);
            }

            _trackedOpenStack.Clear();
            CopyStack(currentStack, _trackedOpenStack);
        }

        private UIViewAudioCueSet FindCueSet(string viewId)
        {
            if (string.IsNullOrWhiteSpace(viewId) || viewAudioCues == null)
                return null;

            for (var i = 0; i < viewAudioCues.Length; i++)
            {
                var item = viewAudioCues[i];
                if (item == null || string.IsNullOrWhiteSpace(item.ViewId))
                    continue;

                if (string.Equals(item.ViewId, viewId, StringComparison.Ordinal))
                    return item;
            }

            return null;
        }

        private UIModeAudioCueSet FindModeCueSet(UIMode mode)
        {
            if (modeAudioCues == null)
                return null;

            for (var i = 0; i < modeAudioCues.Length; i++)
            {
                var item = modeAudioCues[i];
                if (item != null && item.Mode == mode)
                    return item;
            }

            return null;
        }

        private void PlayCue(AudioCueBinding cue)
        {
            if (cue == null || !cue.HasPlayableKey)
                return;

            if (!TryResolveCommand(out var command))
            {
                Warn("未找到 NiumaAudioController 或 IAudioCommand，无法播放 UI 音效。");
                return;
            }

            LastAudioResult = command.PlayCue(cue.ToPlayRequest("NiumaUI"));
            WarnFailure(LastAudioResult);
        }

        private bool ResolveUIToolkitUIManager()
        {
            if (toolkitUIManager != null)
                return true;

            if (!autoFindUIToolkitUIManager)
                return false;

            toolkitUIManager = GetComponent<UIToolkitUIManager>();
            if (toolkitUIManager == null)
                toolkitUIManager = GetComponentInParent<UIToolkitUIManager>();
            if (toolkitUIManager == null)
                toolkitUIManager = GetComponentInChildren<UIToolkitUIManager>(true);

#if UNITY_2023_1_OR_NEWER
            if (toolkitUIManager == null)
                toolkitUIManager = FindFirstObjectByType<UIToolkitUIManager>();
#else
            if (toolkitUIManager == null)
                toolkitUIManager = FindObjectOfType<UIToolkitUIManager>();
#endif
            return toolkitUIManager != null;
        }

        private static void CopyStack(IReadOnlyList<string> source, List<string> destination)
        {
            if (source == null || destination == null)
                return;

            for (var i = 0; i < source.Count; i++)
            {
                var viewId = source[i];
                if (!string.IsNullOrWhiteSpace(viewId))
                    destination.Add(viewId);
            }
        }

        private static bool ContainsViewId(IReadOnlyList<string> stack, string viewId)
        {
            if (stack == null || string.IsNullOrWhiteSpace(viewId))
                return false;

            for (var i = 0; i < stack.Count; i++)
            {
                if (string.Equals(stack[i], viewId, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static string GetTopViewId(IReadOnlyList<string> stack)
        {
            if (stack == null)
                return null;

            for (var i = stack.Count - 1; i >= 0; i--)
            {
                var viewId = stack[i];
                if (!string.IsNullOrWhiteSpace(viewId))
                    return viewId;
            }

            return null;
        }

        private bool TryResolveCommand(out IAudioCommand command)
        {
            var resolved = AudioBridgeResolver.TryResolveCommand(
                _runtimeCommand,
                null,
                audioController,
                autoFindAudioController,
                out command,
                out var resolvedController);

            if (resolvedController != null)
                audioController = resolvedController;

            return resolved;
        }

        private void WarnFailure(AudioOperationResult result)
        {
            if (result == null || result.Succeeded)
                return;

            Warn($"UI 音效播放失败：{result.FailureReason}，{result.Message}");
        }

        private void Warn(string message)
        {
            if (logWarnings)
                Debug.LogWarning($"[NiumaUIAudioBridge] {message}", this);
        }
    }

    /// <summary>
    /// 单个 UI 模式的音频覆盖配置。
    /// </summary>
    [Serializable]
    public sealed class UIModeAudioCueSet
    {
        [Tooltip("目标 UI 模式。例如切换到 Dialogue / Menu 等模式时使用本条音效。")]
        public UIMode Mode;

        [Tooltip("切换到该 UI 模式时播放的音效。CueId 填 AudioCueDefinition.CueId；Toolkit 当前需要外部调用 NotifyModeChanged 后才会播放。")]
        public AudioCueBinding ModeChangedCue = new AudioCueBinding
        {
            SourceModule = "NiumaUI"
        };
    }
}
