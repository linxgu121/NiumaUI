using NiumaUI.Core.Interface;

namespace NiumaUI.Core
{
    /// <summary>
    /// 纯 C# UI 视图，仅管理逻辑生命周期；
    /// Unity 对象通过 IViewBinding 访问，场景激活逻辑隔离在绑定层。
    /// </summary>
    public abstract class ViewBase
    {
        private IViewBinding _binding;

        public string ViewId { get; private set; }
        public bool IsOpen { get; private set; }
        protected IViewBinding Binding => _binding;

        public void Initialize(string viewId, IViewBinding binding)
        {
            ViewId = viewId;
            _binding = binding;
            OnInitialize();
        }

        public void Open()
        {
            if (IsOpen)
                return;

            IsOpen = true;
            _binding?.Show();
            OnOpen();
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen)
                return;

            IsOpen = false;
            OnClose();
            _binding?.Hide();
        }

        public virtual void Refresh()
        {
            OnRefresh();
            _binding?.Refresh();
        }

        public virtual void Tick(float deltaTime)
        {
        }

        protected T GetBinding<T>() where T : class, IViewBinding
        {
            return _binding as T;
        }

        protected virtual void OnInitialize()
        {
        }

        protected virtual void OnOpen()
        {
        }

        protected virtual void OnClose()
        {
        }

        protected virtual void OnRefresh()
        {
        }
    }
}
