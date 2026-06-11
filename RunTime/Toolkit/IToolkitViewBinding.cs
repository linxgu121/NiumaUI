using System;
using UnityEngine.UIElements;

namespace NiumaUI.Toolkit
{
    /// <summary>
    /// UI Toolkit View 的运行时绑定实例。
    /// 它只负责 VisualElement 查询、表现刷新和 UI 事件回调，不写业务规则。
    /// </summary>
    public interface IToolkitViewBinding : IDisposable
    {
        string ViewId { get; }
        VisualElement Root { get; }
        bool IsOpen { get; }

        void Initialize(string viewId, VisualElement root);
        void Open();
        void Close();
        void Refresh(object viewData);
        void Tick(float deltaTime);
    }

    /// <summary>
    /// Toolkit Binding 创建器。第二阶段工厂会按 BindingProviderId 查找并调用它。
    /// </summary>
    public interface IToolkitViewBindingProvider
    {
        string ProviderId { get; }
        IToolkitViewBinding CreateBinding();
    }
}