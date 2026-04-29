namespace NiumaUI.Core
{
    /// <summary>
    /// UI 视图抽象基类 —— 纯契约，零状态，零实现
    /// 不感知 Unity，不感知预制体，不感知显隐状态
    /// 只定义生命周期钩子，由具体子类或外部管理器驱动
    /// </summary>
    public abstract class ViewBase
    {
        public abstract void Open();      // 展示自身（子类实现动画或瞬间显示）
        public abstract void Close();     // 隐藏自身
        public abstract void Refresh();   // 更新内容，不控制显隐
        
        /// <summary>
        /// 每帧驱动（仅视觉表现，如气泡跟随、进度条填充）
        /// 由 UIManager 通过黑板 Tick 列表统一调用，View 不自驱
        /// </summary>
        public virtual void Tick(float deltaTime) { }
    }
}