using UnityEngine.UIElements;

namespace NiumaUI.Toolkit
{
    /// <summary>
    /// UI Toolkit View Binding 基类。
    /// 子类在 OnInitialize 中缓存 VisualElement，在 OnRefresh 中把 ViewData 写入 UI。
    /// </summary>
    public abstract class ToolkitViewBindingBase : IToolkitViewBinding
    {
        public string ViewId { get; private set; }
        public VisualElement Root { get; private set; }
        public bool IsOpen { get; private set; }

        public void Initialize(string viewId, VisualElement root)
        {
            ViewId = viewId;
            Root = root;
            OnInitialize();
        }

        public void Open()
        {
            if (Root != null)
                Root.style.display = DisplayStyle.Flex;

            IsOpen = true;
            OnOpen();
        }

        public void Close()
        {
            OnClose();
            IsOpen = false;

            if (Root != null)
                Root.style.display = DisplayStyle.None;
        }

        public void Refresh(object viewData)
        {
            OnRefresh(viewData);
        }

        public void Tick(float deltaTime)
        {
            OnTick(deltaTime);
        }

        public void Dispose()
        {
            OnDispose();
            Root = null;
            ViewId = null;
            IsOpen = false;
        }

        protected T Query<T>(string elementName) where T : VisualElement
        {
            return Root?.Q<T>(elementName);
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnOpen() { }
        protected virtual void OnClose() { }
        protected virtual void OnRefresh(object viewData) { }
        protected virtual void OnTick(float deltaTime) { }
        protected virtual void OnDispose() { }
    }

    /// <summary>
    /// 无自定义逻辑的默认 Binding。用于测试或纯静态 UXML。
    /// </summary>
    public sealed class DefaultToolkitViewBinding : ToolkitViewBindingBase
    {
    }
}