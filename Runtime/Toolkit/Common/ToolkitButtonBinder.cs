using System;
using UnityEngine.UIElements;

namespace NiumaUI.Toolkit.Common
{
    /// <summary>
    /// UI Toolkit Button 绑定助手。内部使用 ToolkitCallbackRegistry 统一注销回调。
    /// </summary>
    public sealed class ToolkitButtonBinder : IDisposable
    {
        private readonly ToolkitCallbackRegistry _callbacks = new ToolkitCallbackRegistry();
        private Button _button;

        public Button Button => _button;
        public bool HasButton => _button != null;

        public bool Bind(VisualElement root, string buttonName, Action onClicked)
        {
            return Bind(root, buttonName, onClicked, null);
        }

        public bool Bind(VisualElement root, string buttonName, Action onClicked, Func<bool> canExecute)
        {
            Unbind();
            _button = ToolkitElementUtility.QueryButton(root, buttonName);
            return _callbacks.RegisterButton(_button, onClicked, canExecute);
        }

        public void Bind(Button button, Action onClicked)
        {
            Bind(button, onClicked, null);
        }

        public void Bind(Button button, Action onClicked, Func<bool> canExecute)
        {
            Unbind();
            _button = button;
            _callbacks.RegisterButton(_button, onClicked, canExecute);
        }

        public void SetText(string text)
        {
            if (_button != null)
                _button.text = text ?? string.Empty;
        }

        public void SetDisplay(bool visible)
        {
            ToolkitElementUtility.SetDisplay(_button, visible);
        }

        public void SetEnabled(bool enabled)
        {
            ToolkitElementUtility.SetEnabled(_button, enabled);
        }

        public void SetBlocked(bool blocked)
        {
            _callbacks.SetBlocked(blocked);
        }

        public void Unbind()
        {
            _callbacks.UnregisterAll();
            _button = null;
        }

        public void Dispose()
        {
            _callbacks.Dispose();
            _button = null;
        }
    }
}
