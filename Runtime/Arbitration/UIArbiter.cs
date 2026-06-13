using NiumaUI.Enum;
using NiumaUI.RuntimeData;

namespace NiumaUI.Arbitration
{
    /// <summary>
    /// UI 浠茶鍣?
    /// 鎵€鏈?UI 鐘舵€佸彉鏇村繀椤荤粡杩囨澶勮鍐?
    /// </summary>
    public class UIArbiter
    {
        private readonly UIBlackboard _blackboard;

        public UIArbiter(UIBlackboard blackboard) => _blackboard = blackboard;

        /// <summary>
        /// 璇锋眰鍒囨崲鍏ㄥ眬 UI 妯″紡
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
        /// 璇锋眰鍘嬪叆瑙嗗浘
        /// </summary>
        public bool RequestPush(string viewId)
        {
            if (string.IsNullOrEmpty(viewId)) return false;
            if (_blackboard.IsPaused && _blackboard.CurrentMode != UIMode.Menu) return false;

            _blackboard.PushView(viewId);
            return true;
        }

        /// <summary>
        /// 璇锋眰寮瑰嚭鏍堥《瑙嗗浘
        /// </summary>
        public bool RequestPop()
        {
            if (_blackboard.ViewStack.Count == 0) return false;

            _blackboard.PopView();

            // 鏍堢┖鏃惰嚜鍔ㄥ洖鍒?Gameplay锛圖ialogue 妯″紡涓嬫爤绌?= 瀵硅瘽缁撴潫锛?
            if (_blackboard.ViewStack.Count == 0 && _blackboard.CurrentMode == UIMode.Dialogue)
                _blackboard.SetMode(UIMode.Gameplay);

            return true;
        }

        /// <summary>
        /// 璇锋眰鍏抽棴褰撳墠鐒︾偣瑙嗗浘
        /// 閬垮厤 Gal 鍏抽棴瀵硅瘽鏃惰鍏抽棴鏍堥《鐨勫叾浠?UI
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
        /// 璇锋眰鍏抽棴褰撳墠鐒︾偣瑙嗗浘
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
        /// 璇锋眰鍔犲叆 Tick 鍒楄〃
        /// </summary>
        public bool RequestAddTick(string viewId)
        {
            if (string.IsNullOrEmpty(viewId)) return false;
            _blackboard.AddTick(viewId);
            return true;
        }

        /// <summary>
        /// 璇锋眰绉诲嚭 Tick 鍒楄〃
        /// </summary>
        public bool RequestRemoveTick(string viewId)
        {
            _blackboard.RemoveTick(viewId);
            return true;
        }

        /// <summary>
        /// 璇锋眰娓呯┖鎵€鏈夎鍥惧苟鍥炲埌 Gameplay
        /// </summary>
        public bool RequestClearAll()
        {
            _blackboard.ClearStack();
            _blackboard.SetMode(UIMode.Gameplay);
            return true;
        }
    }
}


