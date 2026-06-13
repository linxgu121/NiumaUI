using UnityEngine;
using UnityEngine.UIElements;

namespace NiumaUI.Toolkit.Common
{
    public enum ToolkitPanelVisualState
    {
        Content = 0,
        Empty = 1,
        Error = 2,
        Loading = 3
    }

    /// <summary>
    /// UI Toolkit 常用元素操作工具。
    /// </summary>
    public static class ToolkitElementUtility
    {
        public static T Query<T>(VisualElement root, string elementName) where T : VisualElement
        {
            return QueryOptional<T>(root, elementName);
        }

        public static T QueryOptional<T>(VisualElement root, string elementName) where T : VisualElement
        {
            return root == null || string.IsNullOrWhiteSpace(elementName) ? null : root.Q<T>(elementName.Trim());
        }

        public static T QueryRequired<T>(VisualElement root, string elementName, string ownerName = null) where T : VisualElement
        {
            var element = QueryOptional<T>(root, elementName);
            if (element == null)
                Debug.LogWarning($"[NiumaUI] 找不到必要 UI 元素：Owner={ownerName ?? "Unknown"}, Name={elementName}, Type={typeof(T).Name}");
            return element;
        }

        public static Label QueryLabel(VisualElement root, string elementName)
        {
            return QueryOptional<Label>(root, elementName);
        }

        public static Button QueryButton(VisualElement root, string elementName)
        {
            return QueryOptional<Button>(root, elementName);
        }

        public static ListView QueryListView(VisualElement root, string elementName)
        {
            return QueryOptional<ListView>(root, elementName);
        }

        public static void SetText(Label label, string text)
        {
            if (label != null)
                label.text = text ?? string.Empty;
        }

        public static void SetText(VisualElement root, string labelName, string text)
        {
            SetText(QueryLabel(root, labelName), text);
        }

        public static void SetDisplay(VisualElement element, bool visible)
        {
            if (element != null)
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public static void SetVisible(VisualElement element, bool visible)
        {
            SetDisplay(element, visible);
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

        public static void ApplyPanelState(
            ToolkitPanelVisualState state,
            VisualElement contentRoot,
            VisualElement emptyRoot,
            VisualElement errorRoot,
            VisualElement loadingRoot,
            Label errorLabel = null,
            string errorMessage = null)
        {
            SetDisplay(contentRoot, state == ToolkitPanelVisualState.Content);
            SetDisplay(emptyRoot, state == ToolkitPanelVisualState.Empty);
            SetDisplay(errorRoot, state == ToolkitPanelVisualState.Error);
            SetDisplay(loadingRoot, state == ToolkitPanelVisualState.Loading);
            SetText(errorLabel, state == ToolkitPanelVisualState.Error ? errorMessage : string.Empty);
        }

        public static void ApplyIcon(VisualElement iconElement, string addressKey, IToolkitIconResolver resolver)
        {
            if (iconElement == null)
                return;

            Texture2D texture = null;
            if (resolver != null && !string.IsNullOrWhiteSpace(addressKey))
                resolver.TryResolve(addressKey, out texture);

            if (texture == null && resolver != null)
                texture = resolver.MissingIcon;

            iconElement.style.backgroundImage = new StyleBackground(texture);
        }
    }
}
