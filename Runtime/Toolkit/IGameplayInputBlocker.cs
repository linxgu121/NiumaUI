using NiumaUI.Enum;

namespace NiumaUI.Toolkit
{
    /// <summary>
    /// UI Toolkit 对玩法输入系统的最小阻塞接口。
    /// 由 TPC 或其他玩法输入模块实现，UIToolkitUIManager 只负责按 View 的输入策略调用它。
    /// </summary>
    public interface IGameplayInputBlocker
    {
        /// <summary>
        /// 设置玩法输入是否被 UI 阻塞。
        /// </summary>
        /// <param name="blocked">true 表示阻塞玩法输入，false 表示释放 UI 阻塞。</param>
        /// <param name="reason">阻塞原因，用于不同 UI 模式分别记录与释放。</param>
        void SetBlocked(bool blocked, UIMode reason);
    }
}
