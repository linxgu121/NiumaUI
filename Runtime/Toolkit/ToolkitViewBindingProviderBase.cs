using UnityEngine;

namespace NiumaUI.Toolkit
{
    /// <summary>
    /// UI Toolkit BindingProvider 的 MonoBehaviour 基类。
    /// 把子类挂到场景物体上，再拖入 UIToolkitViewFactory.Binding Provider Behaviours。
    /// </summary>
    public abstract class ToolkitViewBindingProviderBase : MonoBehaviour, IToolkitViewBindingProvider
    {
        [Tooltip("Binding 创建器 ID。需要与 UIToolkitViewRegistrySO 条目中的 BindingProviderId 完全一致。")]
        [SerializeField] private string providerId = "Default";

        public string ProviderId => string.IsNullOrWhiteSpace(providerId) ? "Default" : providerId;

        public abstract IToolkitViewBinding CreateBinding();
    }

    /// <summary>
    /// 默认 BindingProvider。适合纯静态 UXML 或早期连通性测试。
    /// </summary>
    public sealed class DefaultToolkitViewBindingProvider : ToolkitViewBindingProviderBase
    {
        public override IToolkitViewBinding CreateBinding()
        {
            return new DefaultToolkitViewBinding();
        }
    }
}