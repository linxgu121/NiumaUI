namespace NiumaUI.Toolkit
{
    /// <summary>
    /// UI Toolkit View 关闭后的实例处理策略。
    /// </summary>
    public enum UIToolkitViewCachePolicy
    {
        /// <summary>关闭时销毁实例，下次打开重新创建。</summary>
        DestroyOnClose = 0,

        /// <summary>关闭时隐藏并保留实例，下次打开复用。</summary>
        HideAndCache = 1
    }

    /// <summary>
    /// UI Toolkit View 的模态策略。
    /// </summary>
    public enum UIToolkitViewModalPolicy
    {
        /// <summary>非模态，不阻塞下层 UI 点击。</summary>
        None = 0,

        /// <summary>模态，打开后阻塞下层 UI 点击。</summary>
        Modal = 1
    }

    /// <summary>
    /// UI Toolkit View 打开时对玩法输入的处理策略。
    /// </summary>
    public enum UIToolkitViewInputPolicy
    {
        /// <summary>不阻塞玩法输入，适合 HUD、提示、非交互状态条。</summary>
        None = 0,

        /// <summary>阻塞玩法输入，适合菜单、对话、弹窗、加载遮罩。</summary>
        BlockGameplayInput = 1
    }
}