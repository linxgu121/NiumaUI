namespace NiumaUI.Core.Interface
{
    /// <summary>
    /// 视图绑定接口
    /// 定义纯 C# 视图与 Unity 视图对象之间的交互契约
    /// </summary>
    public interface IViewBinding
    {
        string ViewId { get; }

        void Show();
        void Hide();
        void Refresh();
    }
}
