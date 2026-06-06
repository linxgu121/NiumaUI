using System;
using NiumaAudio.Bridge;
using NiumaAudio.Controller;
using NiumaAudio.Data;
using NiumaAudio.Service;
using NiumaUI.Core;
using NiumaUI.Enum;
using NiumaUI.RunTimeData;
using UnityEngine;

namespace NiumaUI.AudioBridge
{
    /// <summary>
    /// NiumaUI 到 NiumaAudio 的桥接脚本。
    /// 建议挂在 UIRoot 或 UIManager 同物体上，绑定 UIManager 和 AudioRoot 上的 NiumaAudioController。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIAudioBridge : MonoBehaviour
    {
        [Header("控制器绑定")]
        [Tooltip("UI 根控制器。请拖入 UIRoot 上的 UIManager；为空时可自动查找。")]
        [SerializeField] private UIManager uiManager;

        [Tooltip("音频控制器。请拖入 AudioRoot 上的 NiumaAudioController；为空时可自动查找。")]
        [SerializeField] private NiumaAudioController audioController;

        [Tooltip("未手动绑定 UIManager 时是否自动查找场景中的 UIManager。正式场景建议手动绑定。")]
        [SerializeField] private bool autoFindUIManager = true;

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

        [Tooltip("UI 模式切换时播放的默认音效。CueId 填 AudioCueDefinition.CueId；为空则不播放。")]
        [SerializeField] private AudioCueBinding defaultModeChangedCue = new AudioCueBinding
        {
            SourceModule = "NiumaUI"
        };

        [Header("视图覆盖")]
        [Tooltip("按 ViewId 覆盖打开、关闭、焦点音效。ViewId 填 UIViewRegistrySO 或 UIManager.PushView 使用的 ID。")]
        [SerializeField] private UIViewAudioCueSet[] viewAudioCues = Array.Empty<UIViewAudioCueSet>();

        [Header("模式覆盖")]
        [Tooltip("按目标 UI 模式覆盖模式切换音效。例：切到 Dialogue 模式播放对话界面音效，切到 Inventory 模式播放背包界面音效。为空时使用默认模式切换音效。")]
        [SerializeField] private UIModeAudioCueSet[] modeAudioCues = Array.Empty<UIModeAudioCueSet>();

        [Header("播放规则")]
        [Tooltip("焦点清空时是否播放焦点变化音效。关闭时，FocusViewId 为空不会播放声音。")]
        [SerializeField] private bool playFocusCueWhenFocusCleared;

        [Tooltip("组件启用时是否把当前 UI 栈顶视为已聚焦状态，避免刚进场景就播放一次焦点音效。")]
        [SerializeField] private bool suppressInitialFocusCue = true;

        [Tooltip("自动查找 UIManager 失败后的重试间隔，避免 UIManager 缺失时每帧执行场景查找。0 表示每帧重试；正式场景建议手动绑定 UIManager。")]
        [SerializeField] private float autoBindRetryInterval = 0.5f;

        [Header("调试")]
        [Tooltip("缺少控制器、CueId 或播放失败时是否输出警告。")]
        [SerializeField] private bool logWarnings = true;

        private UIBlackboard _boundBlackboard;
        private IAudioCommand _runtimeCommand;
        private bool _initialFocusSuppressed;
        private float _nextBindRetryTime;

        public AudioOperationResult LastAudioResult { get; private set; }

        public void SetAudioCommand(IAudioCommand command)
        {
            _runtimeCommand = command;
        }

        public void SetUIManager(UIManager manager)
        {
            UnbindBlackboard();
            uiManager = manager;
            _nextBindRetryTime = 0f;
            TryBindBlackboard();
        }

        public void SetAudioController(NiumaAudioController controller)
        {
            audioController = controller;
        }

        private void OnEnable()
        {
            _initialFocusSuppressed = false;
            _nextBindRetryTime = 0f;
            TryBindBlackboard();
        }

        private void OnDisable()
        {
            UnbindBlackboard();
        }

        private void LateUpdate()
        {
            if (_boundBlackboard == null)
            {
                if (autoBindRetryInterval > 0f && Time.unscaledTime < _nextBindRetryTime)
                {
                    return;
                }

                TryBindBlackboard();

                if (_boundBlackboard == null && autoBindRetryInterval > 0f)
                {
                    _nextBindRetryTime = Time.unscaledTime + autoBindRetryInterval;
                }
            }
        }

        /// <summary>
        /// UnityEvent 调用入口：播放默认视图打开音效。
        /// </summary>
        public void PlayDefaultViewPushedCue()
        {
            PlayCue(defaultViewPushedCue);
        }

        /// <summary>
        /// UnityEvent 调用入口：播放默认视图关闭音效。
        /// </summary>
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
            {
                return;
            }

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
            {
                return;
            }

            var cueSet = FindModeCueSet(newMode);
            var cue = cueSet != null && cueSet.ModeChangedCue != null && cueSet.ModeChangedCue.HasPlayableKey
                ? cueSet.ModeChangedCue
                : defaultModeChangedCue;

            PlayCue(cue);
        }

        private void TryBindBlackboard()
        {
            if (_boundBlackboard != null)
            {
                return;
            }

            if (!ResolveUIManager())
            {
                return;
            }

            var blackboard = uiManager.Blackboard;
            if (blackboard == null)
            {
                return;
            }

            _boundBlackboard = blackboard;
            _boundBlackboard.OnViewPushed += HandleViewPushed;
            _boundBlackboard.OnViewPopped += HandleViewPopped;
            _boundBlackboard.OnFocusChanged += HandleFocusChanged;
            _boundBlackboard.OnModeChanged += HandleModeChanged;
        }

        private void UnbindBlackboard()
        {
            if (_boundBlackboard == null)
            {
                return;
            }

            _boundBlackboard.OnViewPushed -= HandleViewPushed;
            _boundBlackboard.OnViewPopped -= HandleViewPopped;
            _boundBlackboard.OnFocusChanged -= HandleFocusChanged;
            _boundBlackboard.OnModeChanged -= HandleModeChanged;
            _boundBlackboard = null;
        }

        private UIViewAudioCueSet FindCueSet(string viewId)
        {
            if (string.IsNullOrWhiteSpace(viewId) || viewAudioCues == null)
            {
                return null;
            }

            for (var i = 0; i < viewAudioCues.Length; i++)
            {
                var item = viewAudioCues[i];
                if (item == null || string.IsNullOrWhiteSpace(item.ViewId))
                {
                    continue;
                }

                if (string.Equals(item.ViewId, viewId, StringComparison.Ordinal))
                {
                    return item;
                }
            }

            return null;
        }

        private UIModeAudioCueSet FindModeCueSet(UIMode mode)
        {
            if (modeAudioCues == null)
            {
                return null;
            }

            for (var i = 0; i < modeAudioCues.Length; i++)
            {
                var item = modeAudioCues[i];
                if (item == null || item.Mode != mode)
                {
                    continue;
                }

                return item;
            }

            return null;
        }

        private void PlayCue(AudioCueBinding cue)
        {
            if (cue == null || !cue.HasPlayableKey)
            {
                return;
            }

            if (!TryResolveCommand(out var command))
            {
                Warn("未找到 NiumaAudioController 或 IAudioCommand，无法播放 UI 音效。");
                return;
            }

            LastAudioResult = command.PlayCue(cue.ToPlayRequest("NiumaUI"));
            WarnFailure(LastAudioResult);
        }

        private bool ResolveUIManager()
        {
            if (uiManager != null)
            {
                return true;
            }

            if (!autoFindUIManager)
            {
                return false;
            }

#if UNITY_2023_1_OR_NEWER
            uiManager = FindFirstObjectByType<UIManager>();
#else
            uiManager = FindObjectOfType<UIManager>();
#endif
            return uiManager != null;
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
            {
                audioController = resolvedController;
            }

            return resolved;
        }

        private void WarnFailure(AudioOperationResult result)
        {
            if (result == null || result.Succeeded)
            {
                return;
            }

            Warn($"UI 音效播放失败：{result.FailureReason}，{result.Message}");
        }

        private void Warn(string message)
        {
            if (logWarnings)
            {
                Debug.LogWarning($"[NiumaUIAudioBridge] {message}", this);
            }
        }
    }

    /// <summary>
    /// 单个 UI 模式的音频覆盖配置。
    /// 用于让策划给不同 UI 模式配置不同的切换音效。
    /// </summary>
    [Serializable]
    public sealed class UIModeAudioCueSet
    {
        [Tooltip("目标 UI 模式。例：切换到 Dialogue / Inventory / Menu 等模式时使用本条音效。")]
        public UIMode Mode;

        [Tooltip("切换到该 UI 模式时播放的音效。CueId 填 AudioCueDefinition.CueId；为空时使用 UIAudioBridge 的默认模式切换音效。")]
        public AudioCueBinding ModeChangedCue = new AudioCueBinding
        {
            SourceModule = "NiumaUI"
        };
    }
}
