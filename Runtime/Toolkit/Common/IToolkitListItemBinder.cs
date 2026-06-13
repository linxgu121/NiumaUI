using System;
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

    /// <summary>
    /// 通用文本行数据。用于业务面板的 MVP 列表：显示文本、稳定 ID、选中态和可点击状态。
    /// </summary>
    public sealed class ToolkitTextRowData
    {
        public string Id { get; }
        public string Text { get; }
        public bool IsSelected { get; }
        public bool IsEnabled { get; }
        public object Payload { get; }

        public ToolkitTextRowData(string id, string text, bool isSelected = false, bool isEnabled = true, object payload = null)
        {
            Id = string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
            Text = text ?? string.Empty;
            IsSelected = isSelected;
            IsEnabled = isEnabled;
            Payload = payload;
        }
    }

    /// <summary>
    /// 通用文本行 ListView 条目绑定器。条目使用 Button，支持选中态、禁用态和点击回调。
    /// </summary>
    public sealed class ToolkitTextRowItemBinder : IToolkitListItemBinder<ToolkitTextRowData>
    {
        private readonly string _rowClass;
        private readonly string _selectedClass;
        private readonly string _disabledClass;
        private readonly Action<ToolkitTextRowData> _clicked;

        public ToolkitTextRowItemBinder(string rowClass, string selectedClass, string disabledClass, Action<ToolkitTextRowData> clicked)
        {
            _rowClass = string.IsNullOrWhiteSpace(rowClass) ? "niuma-list-row" : rowClass.Trim();
            _selectedClass = string.IsNullOrWhiteSpace(selectedClass) ? "is-selected" : selectedClass.Trim();
            _disabledClass = string.IsNullOrWhiteSpace(disabledClass) ? "is-disabled" : disabledClass.Trim();
            _clicked = clicked;
        }

        public VisualElement CreateElement()
        {
            var button = new Button();
            button.AddToClassList(_rowClass);
            button.clicked += () =>
            {
                if (button.userData is ToolkitTextRowData row && row.IsEnabled)
                    _clicked?.Invoke(row);
            };
            return button;
        }

        public void BindItem(VisualElement element, ToolkitTextRowData data, int index)
        {
            if (!(element is Button button))
                return;

            data ??= new ToolkitTextRowData(string.Empty, string.Empty, false, false);
            button.userData = data;
            button.text = data.Text;
            button.SetEnabled(data.IsEnabled);
            ToolkitElementUtility.SetClass(button, _selectedClass, data.IsSelected);
            ToolkitElementUtility.SetClass(button, _disabledClass, !data.IsEnabled);
        }

        public void UnbindItem(VisualElement element, int index)
        {
            if (!(element is Button button))
                return;

            button.userData = null;
            button.text = string.Empty;
            button.SetEnabled(true);
            ToolkitElementUtility.SetClass(button, _selectedClass, false);
            ToolkitElementUtility.SetClass(button, _disabledClass, false);
        }
    }
}
