using NiumaUI.Core.Interface;
using UnityEngine;

namespace NiumaUI.Core
{
    /// <summary>
    /// 面向 Unity 引擎的纯 C# ViewBase 绑定组件
    /// 挂载在 UI 预制体上，负责管理 GameObject 显隐与序列化组件引用
    /// </summary>
    public abstract class ViewBindingBase : MonoBehaviour, IViewBinding
    {
        public string ViewId { get; private set; }
        public ViewBase View { get; private set; }

        public ViewBase CreateAndBindView(string viewId)
        {
            ViewId = viewId;
            View = CreateView();
            View.Initialize(viewId, this);
            return View;
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }

        public virtual void Refresh()
        {
        }

        protected abstract ViewBase CreateView();
    }
}
