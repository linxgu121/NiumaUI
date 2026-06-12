using System;
using UnityEngine.UIElements;

namespace NiumaUI.Toolkit.Common
{
    /// <summary>
    /// UI Toolkit Button 绑定助手。
    /// 用于把按钮查询、点击注册、启用禁用、文本刷新和释放集中处理，避免每个 Binding 重复写事件解绑代码。
    /// </summary>
    public sealed class ToolkitButtonBinder : IDisposable
    {
        private Button _button;
        private Action _onClicked;
        private bool _isBound;

        public Button Button => _button;
        public bool HasButton => _button != null;

        public bool Bind(VisualElement root, string buttonName, Action onClicked)
        {
            Unbind();

            _button = ToolkitElementUtility.QueryButton(root, buttonName);
            _onClicked = onClicked;

            if (_button == null)
                return false;

            if (_onClicked != null)
            {
                _button.clicked += HandleClicked;
                _isBound = true;
            }

            return true;
        }

        public void Bind(Button button, Action onClicked)
        {
            Unbind();

            _button = button;
            _onClicked = onClicked;

            if (_button != null && _onClicked != null)
            {
                _button.clicked += HandleClicked;
                _isBound = true;
            }
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

        public void Unbind()
        {
            if (_button != null && _isBound)
                _button.clicked -= HandleClicked;

            _button = null;
            _onClicked = null;
            _isBound = false;
        }

        public void Dispose()
        {
            Unbind();
        }

        private void HandleClicked()
        {
            _onClicked?.Invoke();
        }
    }
}
