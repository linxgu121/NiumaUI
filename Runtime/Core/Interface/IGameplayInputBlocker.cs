using NiumaUI.Enum;

namespace NiumaUI.Core.Interface
{
    /// <summary>
    /// 游戏输入阻塞接口
    /// 由 TPC 侧实现，UI 侧通过 UIManager 调用
    /// </summary>
    public interface IGameplayInputBlocker
    {
        /// <summary>
        /// 设置游戏输入阻塞状态
        /// </summary>
        /// <param name="blocked">是否阻塞</param>
        /// <param name="reason">阻塞原因（UIMode）</param>
        void SetBlocked(bool blocked, UIMode reason);
    }
}
