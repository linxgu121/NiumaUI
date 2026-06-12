using UnityEngine.UIElements;

namespace NiumaUI.Toolkit.Common
{
    /// <summary>
    /// UI Toolkit 常用元素操作工具。
    /// 业务 Binding 可以复用这里的显示、文本、样式和空状态规则，减少每个模块重复写样板代码。
    /// </summary>
    public static class ToolkitElementUtility
    {
        public static T Query<T>(VisualElement root, string elementName) where T : VisualElement
        {
            return root == null || string.IsNullOrWhiteSpace(elementName) ? null : root.Q<T>(elementName.Trim());
        }

        public static Label QueryLabel(VisualElement root, string elementName)
        {
            return Query<Label>(root, elementName);
        }

        public static Button QueryButton(VisualElement root, string elementName)
        {
            return Query<Button>(root, elementName);
        }

        public static ListView QueryListView(VisualElement root, string elementName)
        {
            return Query<ListView>(root, elementName);
        }

        public static void SetText(Label label, string text)
        {
            if (label != null)
                label.text = text ?? string.Empty;
        }

        public static void SetDisplay(VisualElement element, bool visible)
        {
            if (element != null)
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public static void SetPicking(VisualElement element, bool enabled)
        {
            if (element != null)
                element.pickingMode = enabled ? PickingMode.Position : PickingMode.Ignore;
        }

        public static void SetEnabled(VisualElement element, bool enabled)
        {
            element?.SetEnabled(enabled);
        }

        public static void SetClass(VisualElement element, string className, bool enabled)
        {
            if (element == null || string.IsNullOrWhiteSpace(className))
                return;

            className = className.Trim();
            if (enabled)
                element.AddToClassList(className);
            else
                element.RemoveFromClassList(className);
        }

        public static void ReplaceClass(VisualElement element, ref string activeClass, string nextClass)
        {
            if (element == null)
                return;

            if (!string.IsNullOrWhiteSpace(activeClass))
                element.RemoveFromClassList(activeClass);

            activeClass = string.IsNullOrWhiteSpace(nextClass) ? null : nextClass.Trim();
            if (!string.IsNullOrWhiteSpace(activeClass))
                element.AddToClassList(activeClass);
        }

        public static void ApplyEmptyState(VisualElement contentRoot, VisualElement emptyRoot, bool isEmpty)
        {
            SetDisplay(contentRoot, !isEmpty);
            SetDisplay(emptyRoot, isEmpty);
        }

        public static void ApplyErrorState(VisualElement normalRoot, VisualElement errorRoot, Label errorLabel, string errorMessage)
        {
            var hasError = !string.IsNullOrWhiteSpace(errorMessage);
            SetDisplay(normalRoot, !hasError);
            SetDisplay(errorRoot, hasError);
            SetText(errorLabel, hasError ? errorMessage : string.Empty);
        }
    }
}
