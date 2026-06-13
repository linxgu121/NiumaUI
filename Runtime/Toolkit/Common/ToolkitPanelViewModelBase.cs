using System;

namespace NiumaUI.Toolkit.Common
{
    /// <summary>
    /// ViewModel 清理原因。Binding 或 Receiver 需要根据原因决定是否释放本地资源、清空筛选、重置选中项。
    /// </summary>
    public enum UIViewModelClearReason
    {
        Unknown = 0,
        ViewClosed = 1,
        ViewCleared = 2,
        ContextChanged = 3,
        DataCleared = 4,
        Disposed = 5
    }

    /// <summary>
    /// UI Toolkit 面板 ViewModel 基类。
    /// 只保存 UI 局部状态，例如选中项、筛选、分页、临时输入、滚动位置和画板局部交互状态；不要保存背包数量、任务状态等业务事实。
    /// </summary>
    public abstract class ToolkitPanelViewModelBase : IDisposable
    {
        public string ViewId { get; private set; }
        public string ContextId { get; private set; }
        public int LocalRevision { get; private set; }
        public bool IsDirty { get; private set; }
        public bool IsInitialized { get; private set; }
        public bool IsDisposed { get; private set; }

        public void Initialize(string viewId)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().Name);

            ViewId = Normalize(viewId);
            IsInitialized = true;
            OnInitialize();
        }

        public void SetContext(string contextId)
        {
            if (IsDisposed)
                return;

            contextId = Normalize(contextId);
            if (string.Equals(ContextId, contextId, StringComparison.Ordinal))
                return;

            var previousContextId = ContextId;
            ContextId = contextId;
            Clear(UIViewModelClearReason.ContextChanged);
            OnContextChanged(previousContextId, contextId);
            MarkDirty();
        }

        public void Clear()
        {
            Clear(UIViewModelClearReason.ViewCleared);
        }

        public void Clear(UIViewModelClearReason reason)
        {
            if (IsDisposed)
                return;

            OnClear(reason);
            MarkDirty();
        }

        public void MarkDirty()
        {
            if (IsDisposed)
                return;

            LocalRevision = LocalRevision == int.MaxValue ? int.MaxValue : LocalRevision + 1;
            IsDirty = true;
        }

        public bool ConsumeDirty()
        {
            var wasDirty = IsDirty;
            IsDirty = false;
            return wasDirty;
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            Clear(UIViewModelClearReason.Disposed);
            OnDispose();
            ContextId = null;
            ViewId = null;
            IsInitialized = false;
            IsDirty = false;
            IsDisposed = true;
        }

        protected virtual void OnInitialize()
        {
        }

        protected virtual void OnClear(UIViewModelClearReason reason)
        {
            OnClear();
        }

        protected virtual void OnClear()
        {
        }

        protected virtual void OnContextChanged(string previousContextId, string nextContextId)
        {
            OnContextChanged();
        }

        protected virtual void OnContextChanged()
        {
        }

        protected virtual void OnDispose()
        {
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }

    /// <summary>
    /// 文档中的 UIPanelViewModel 命名别名。新代码推荐继承 ToolkitPanelViewModelBase；写文档或局部业务时可称为 UIPanelViewModel。
    /// </summary>
    public abstract class UIPanelViewModelBase : ToolkitPanelViewModelBase
    {
    }
}
