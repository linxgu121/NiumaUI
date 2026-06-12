namespace NiumaUI.Toolkit.Common
{
    /// <summary>
    /// UI Toolkit 面板 ViewModel 基类。
    /// ViewModel 只保存 UI 局部状态，例如选中项、筛选、临时输入、滚动位置和画板本地交互状态。
    /// 不要把背包数量、任务状态、房间权威状态等业务事实放进 ViewModel。
    /// </summary>
    public abstract class ToolkitPanelViewModelBase
    {
        /// <summary>
        /// 当前 UI 上下文 ID。可填 ActorId、ShopId、RoomId、StationId 等。
        /// 切换上下文时会调用 ClearForContextChanged，避免旧选择带入新界面。
        /// </summary>
        public string ContextId { get; private set; }

        /// <summary>
        /// UI 局部状态版本。只表示 ViewModel 自己变化，不等同业务模块 Revision。
        /// </summary>
        public int LocalRevision { get; private set; }

        /// <summary>
        /// 是否存在尚未消费的 UI 局部状态变化。
        /// Binding 可用它决定是否需要重新刷新局部表现。
        /// </summary>
        public bool IsDirty { get; private set; }

        public void SetContext(string contextId)
        {
            contextId = Normalize(contextId);
            if (string.Equals(ContextId, contextId, System.StringComparison.Ordinal))
                return;

            ContextId = contextId;
            ClearForContextChanged();
            MarkDirty();
        }

        public void Clear()
        {
            ContextId = null;
            OnClear();
            MarkDirty();
        }

        public void MarkDirty()
        {
            LocalRevision = LocalRevision == int.MaxValue ? int.MaxValue : LocalRevision + 1;
            IsDirty = true;
        }

        public bool ConsumeDirty()
        {
            var wasDirty = IsDirty;
            IsDirty = false;
            return wasDirty;
        }

        protected virtual void OnClear()
        {
        }

        protected virtual void OnContextChanged()
        {
        }

        private void ClearForContextChanged()
        {
            OnContextChanged();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
