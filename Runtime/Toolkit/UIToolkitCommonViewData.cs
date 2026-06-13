using System;

namespace NiumaUI.Toolkit
{
    /// <summary>
    /// 单次打开 View 时覆盖玩法输入阻塞策略。
    /// 例如 Loading 有时只显示进度，有时需要阻塞玩家输入。
    /// </summary>
    public interface IUIToolkitInputBlockOverride
    {
        bool HasInputBlockOverride { get; }
        bool BlocksGameplayInput { get; }
    }

    /// <summary>
    /// Toast 短提示请求数据。
    /// </summary>
    [Serializable]
    public sealed class UIToolkitToastViewData
    {
        public string ToastId;
        public string Message;
        public float DurationSeconds = 2f;
        public string StyleKey;
        public Action OnExpired;
    }

    /// <summary>
    /// Confirm 确认弹窗请求数据。
    /// </summary>
    [Serializable]
    public sealed class UIToolkitConfirmViewData
    {
        public string RequestId;
        public string Title;
        public string Message;
        public string ConfirmText = "确定";
        public string CancelText = "取消";
        public bool ShowCancel = true;
        public bool AutoClose = true;
        public Action<bool> Callback;
    }

    /// <summary>
    /// Loading 遮罩请求数据。
    /// </summary>
    [Serializable]
    public sealed class UIToolkitLoadingViewData : IUIToolkitInputBlockOverride
    {
        public string LoadingId;
        public string Message;
        public float Progress01 = -1f;
        public bool IsBlocking = true;

        public bool HasInputBlockOverride => true;
        public bool BlocksGameplayInput => IsBlocking;
    }

    public interface IToolkitToastBinding
    {
        void ApplyToast(UIToolkitToastViewData data);
    }

    public interface IToolkitConfirmBinding
    {
        void ApplyConfirm(UIToolkitConfirmViewData data);
    }

    public interface IToolkitLoadingBinding
    {
        void ApplyLoading(UIToolkitLoadingViewData data);
    }
}