using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NiumaUI.Toolkit.Common
{
    /// <summary>
    /// UI Toolkit 回调注册表。统一管理按钮和值变化回调，避免 View 关闭后遗留事件。
    /// </summary>
    public sealed class ToolkitCallbackRegistry : IDisposable
    {
        private readonly List<Action> _unregisterActions = new List<Action>();
        private readonly List<VisualElement> _registeredElements = new List<VisualElement>();
        private bool _blocked;
        private bool _isInvoking;
        private bool _disposed;

        public bool IsBlocked => _blocked;

        public bool RegisterButton(VisualElement root, string buttonName, Action callback, Func<bool> canExecute = null)
        {
            var button = ToolkitElementUtility.QueryButton(root, buttonName);
            return RegisterButton(button, callback, canExecute);
        }

        public bool RegisterButton(Button button, Action callback, Func<bool> canExecute = null)
        {
            if (_disposed || button == null)
                return false;

            void Handler()
            {
                Invoke(callback, canExecute);
            }

            button.clicked += Handler;
            _unregisterActions.Add(() => button.clicked -= Handler);
            _registeredElements.Add(button);
            button.SetEnabled(!_blocked);
            return true;
        }

        public bool RegisterValueChanged<TValue>(BaseField<TValue> field, Action<TValue> callback, Func<bool> canExecute = null)
        {
            if (_disposed || field == null)
                return false;

            void Handler(ChangeEvent<TValue> evt)
            {
                Invoke(() => callback?.Invoke(evt.newValue), canExecute);
            }

            field.RegisterValueChangedCallback(Handler);
            _unregisterActions.Add(() => field.UnregisterValueChangedCallback(Handler));
            _registeredElements.Add(field);
            field.SetEnabled(!_blocked);
            return true;
        }

        public void SetBlocked(bool blocked)
        {
            _blocked = blocked;
            for (var i = 0; i < _registeredElements.Count; i++)
            {
                if (_registeredElements[i] != null)
                    _registeredElements[i].SetEnabled(!blocked);
            }
        }

        public void UnregisterAll()
        {
            for (var i = _unregisterActions.Count - 1; i >= 0; i--)
            {
                try
                {
                    _unregisterActions[i]?.Invoke();
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[NiumaUI] 注销 UI 回调时发生异常：{exception.Message}");
                }
            }

            _unregisterActions.Clear();
            _registeredElements.Clear();
            _isInvoking = false;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            UnregisterAll();
            _disposed = true;
        }

        private void Invoke(Action callback, Func<bool> canExecute)
        {
            if (_blocked || callback == null || _isInvoking)
                return;

            if (canExecute != null && !canExecute())
                return;

            _isInvoking = true;
            try
            {
                callback.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[NiumaUI] UI 回调执行失败：{exception.Message}\n{exception.StackTrace}");
            }
            finally
            {
                _isInvoking = false;
            }
        }
    }
}
