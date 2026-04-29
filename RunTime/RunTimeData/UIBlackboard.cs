using System;
using System.Collections.Generic;
using NiumaUI.Enum;

namespace NiumaUI.RunTimeData
{
    /// <summary>
    /// UI 数据黑板
    /// 仅承载状态快照，不做合法性判断
    /// 写入接口仅由 UIArbiter 调用
    /// </summary>
    public class UIBlackboard
    {
        // 模式快照
        public UIMode CurrentMode { get; private set; } = UIMode.Gameplay;
        // 上一个模式
        public UIMode PreviousMode { get; private set; } = UIMode.Gameplay;

        // 视图栈（内部可变，外部只读）
        private readonly List<string> _viewStack = new List<string>();
        public IReadOnlyList<string> ViewStack => _viewStack;

        // 当前焦点视图 ID
        public string FocusViewId { get; private set; }

        // 需要每帧 Tick 的视图 ID 列表
        private readonly List<string> _tickList = new List<string>();
        public IReadOnlyList<string> TickList => _tickList;

        // 外部暂停标志（由 PauseManager 写入）
        public bool IsPaused { get; set; } = false;

        // 状态变更事件
        public event Action<UIMode, UIMode> OnModeChanged;      // (oldMode, newMode)
        public event Action<string> OnViewPushed;
        public event Action<string> OnViewPopped;
        public event Action<string> OnFocusChanged;
        public event Action<string, bool> OnTickChanged;        // (viewId, isAdded)

        // 写入接口（仅 UIArbiter 调用）
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
        /// 弹出栈顶视图
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
        /// 清空视图栈
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
