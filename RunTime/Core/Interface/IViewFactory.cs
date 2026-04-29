namespace NiumaUI.Core.Interface
{
    /// <summary>
    /// 视图工厂接口
    /// 负责 ViewBase 实例的创建与释放，UIManager 不感知具体实现
    /// </summary>
    public interface IViewFactory
    {
        /// <summary>获取或创建指定 ID 的视图实例</summary>
        ViewBase Get(string viewId);

        /// <summary>释放视图实例（对象池回收或销毁）</summary>
        void Release(string viewId);
    }
}