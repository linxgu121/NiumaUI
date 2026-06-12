using NiumaUI.Enum;
using NiumaUI.RunTimeData;

namespace NiumaUI.Arbitration
{
    /// <summary>
    /// UI 仲裁器
    /// 所有 UI 状态变更必须经过此处裁决
    /// </summary>
    public class UIArbiter
    {
        private readonly UIBlackboard _blackboard;

        public UIArbiter(UIBlackboard blackboard) => _blackboard = blackboard;

        /// <summary>
        /// 请求切换全局 UI 模式
        /// </summary>
        public bool RequestMode(UIMode targetMode)
        {
            if (_blackboard.IsPaused && targetMode != UIMode.Menu) return false;

            var current = _blackboard.CurrentMode;
            if (current == targetMode) return true;

            bool allowed = (current, targetMode) switch
            {
                (UIMode.Gameplay, _) => true,
                (UIMode.Dialogue, UIMode.Menu) => true,
                (UIMode.Dialogue, UIMode.Gameplay) => true,
                (UIMode.Menu, UIMode.Gameplay) => true,
                (UIMode.Menu, UIMode.Dialogue) => _blackboard.PreviousMode == UIMode.Dialogue,
                (UIMode.Cinematic, UIMode.Gameplay) => true,
                _ => false
            };

            if (!allowed) return false;

            _blackboard.SetMode(targetMode);
            return true;
        }

        /// <summary>
        /// 请求压入视图
        /// </summary>
        public bool RequestPush(string viewId)
        {
            if (string.IsNullOrEmpty(viewId)) return false;
            if (_blackboard.IsPaused && _blackboard.CurrentMode != UIMode.Menu) return false;

            _blackboard.PushView(viewId);
            return true;
        }

        /// <summary>
        /// 请求弹出栈顶视图
        /// </summary>
        public bool RequestPop()
        {
            if (_blackboard.ViewStack.Count == 0) return false;

            _blackboard.PopView();

            // 栈空时自动回到 Gameplay（Dialogue 模式下栈空 = 对话结束）
            if (_blackboard.ViewStack.Count == 0 && _blackboard.CurrentMode == UIMode.Dialogue)
                _blackboard.SetMode(UIMode.Gameplay);

            return true;
        }

        /// <summary>
        /// 请求关闭当前焦点视图
        /// 避免 Gal 关闭对话时误关闭栈顶的其他 UI
        /// </summary>
        public bool RequestClose(string viewId)
        {
            if (string.IsNullOrEmpty(viewId)) return false;

            _blackboard.RemoveView(viewId);

            if (_blackboard.ViewStack.Count == 0 && _blackboard.CurrentMode == UIMode.Dialogue)
                _blackboard.SetMode(UIMode.Gameplay);

            return true;
        }

        /// <summary>
        /// 请求关闭当前焦点视图
        /// </summary>
        public bool RequestCloseFocus()
        {
            var focus = _blackboard.FocusViewId;
            if (string.IsNullOrEmpty(focus)) return false;

            _blackboard.RemoveView(focus);

            if (_blackboard.ViewStack.Count == 0 && _blackboard.CurrentMode == UIMode.Dialogue)
                _blackboard.SetMode(UIMode.Gameplay);

            return true;
        }

        /// <summary>
        /// 请求加入 Tick 列表
        /// </summary>
        public bool RequestAddTick(string viewId)
        {
            if (string.IsNullOrEmpty(viewId)) return false;
            _blackboard.AddTick(viewId);
            return true;
        }

        /// <summary>
        /// 请求移出 Tick 列表
        /// </summary>
        public bool RequestRemoveTick(string viewId)
        {
            _blackboard.RemoveTick(viewId);
            return true;
        }

        /// <summary>
        /// 请求清空所有视图并回到 Gameplay
        /// </summary>
        public bool RequestClearAll()
        {
            _blackboard.ClearStack();
            _blackboard.SetMode(UIMode.Gameplay);
            return true;
        }
    }
}
