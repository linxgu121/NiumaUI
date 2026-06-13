using System;
using System.Collections.Generic;
using NiumaUI.Enum;

namespace NiumaUI.RuntimeData
{
    /// <summary>
    /// UI 鏁版嵁榛戞澘
    /// 浠呮壙杞界姸鎬佸揩鐓э紝涓嶅仛鍚堟硶鎬у垽鏂?
    /// 鍐欏叆鎺ュ彛浠呯敱 UIArbiter 璋冪敤
    /// </summary>
    public class UIBlackboard
    {
        // 妯″紡蹇収
        public UIMode CurrentMode { get; private set; } = UIMode.Gameplay;
        // 涓婁竴涓ā寮?
        public UIMode PreviousMode { get; private set; } = UIMode.Gameplay;

        // 瑙嗗浘鏍堬紙鍐呴儴鍙彉锛屽閮ㄥ彧璇伙級
        private readonly List<string> _viewStack = new List<string>();
        public IReadOnlyList<string> ViewStack => _viewStack;

        // 褰撳墠鐒︾偣瑙嗗浘 ID
        public string FocusViewId { get; private set; }

        // 闇€瑕佹瘡甯?Tick 鐨勮鍥?ID 鍒楄〃
        private readonly List<string> _tickList = new List<string>();
        public IReadOnlyList<string> TickList => _tickList;

        // 澶栭儴鏆傚仠鏍囧織锛堢敱 PauseManager 鍐欏叆锛?
        public bool IsPaused { get; set; } = false;

        // 鐘舵€佸彉鏇翠簨浠?
        public event Action<UIMode, UIMode> OnModeChanged;      // (oldMode, newMode)
        public event Action<string> OnViewPushed;
        public event Action<string> OnViewPopped;
        public event Action<string> OnFocusChanged;
        public event Action<string, bool> OnTickChanged;        // (viewId, isAdded)

        // 鍐欏叆鎺ュ彛锛堜粎 UIArbiter 璋冪敤锛?
        public void SetMode(UIMode mode)
        {
            if (CurrentMode == mode) return;
            var old = CurrentMode;
            PreviousMode = old;
            CurrentMode = mode;
            OnModeChanged?.Invoke(old, mode);
        }

        public void PushView(string viewId)
        {
            if (string.IsNullOrEmpty(viewId) || _viewStack.Contains(viewId)) return;
            _viewStack.Add(viewId);
            FocusViewId = viewId;
            OnViewPushed?.Invoke(viewId);
            OnFocusChanged?.Invoke(viewId);
        }

        /// <summary>
        /// 寮瑰嚭鏍堥《瑙嗗浘
        /// </summary>
        public void PopView()
        {
            if (_viewStack.Count == 0) return;
            var viewId = _viewStack[_viewStack.Count - 1];
            _viewStack.RemoveAt(_viewStack.Count - 1);
            OnViewPopped?.Invoke(viewId);

            FocusViewId = _viewStack.Count > 0 ? _viewStack[_viewStack.Count - 1] : null;
            OnFocusChanged?.Invoke(FocusViewId);
        }

        public void RemoveView(string viewId)
        {
            if (!_viewStack.Remove(viewId)) return;
            OnViewPopped?.Invoke(viewId);
            if (FocusViewId == viewId)
            {
                FocusViewId = _viewStack.Count > 0 ? _viewStack[_viewStack.Count - 1] : null;
                OnFocusChanged?.Invoke(FocusViewId);
            }
        }

        /// <summary>
        /// 娓呯┖瑙嗗浘鏍?
        /// </summary>
        public void ClearStack()
        {
            while (_viewStack.Count > 0)
                PopView();
        }

        public void AddTick(string viewId)
        {
            if (string.IsNullOrEmpty(viewId) || _tickList.Contains(viewId)) return;
            _tickList.Add(viewId);
            OnTickChanged?.Invoke(viewId, true);
        }

        public void RemoveTick(string viewId)
        {
            if (!_tickList.Remove(viewId)) return;
            OnTickChanged?.Invoke(viewId, false);
        }
    }
}


