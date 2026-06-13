using UnityEngine.UIElements;

namespace NiumaUI.Toolkit.Common
{
    /// <summary>
    /// ListView 条目绑定器。负责创建、绑定和回收单个列表条目。
    /// </summary>
    public interface IToolkitListItemBinder<in TData>
    {
        VisualElement CreateElement();
        void BindItem(VisualElement element, TData data, int index);
        void UnbindItem(VisualElement element, int index);
    }
}
