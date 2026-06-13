using System;
using NiumaUI.Toolkit.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace NiumaUI.Toolkit
{
    /// <summary>
    /// UI Toolkit View Binding 基类。
    /// 子类只负责 VisualElement 查询、表现刷新和用户输入回调，不写业务规则。
    /// </summary>
    public abstract class ToolkitViewBindingBase : IToolkitViewBinding
    {
        private readonly ToolkitCallbackRegistry _callbacks = new ToolkitCallbackRegistry();

        public string ViewId { get; private set; }
        public VisualElement Root { get; private set; }
        public bool IsOpen { get; private set; }

        protected ToolkitCallbackRegistry Callbacks => _callbacks;

        public void Initialize(string viewId, VisualElement root)
        {
            ViewId = viewId;
            Root = root;
            OnInitialize();
        }

        public void Open()
        {
            if (Root != null)
                Root.style.display = DisplayStyle.Flex;

            IsOpen = true;
            _callbacks.SetBlocked(false);
            OnOpen();
        }

        public void Close()
        {
            OnClose();
            OnClear(UIViewModelClearReason.ViewClosed);
            _callbacks.SetBlocked(true);
            IsOpen = false;

            if (Root != null)
                Root.style.display = DisplayStyle.None;
        }

        public void Refresh(object viewData)
        {
            try
            {
                OnRefresh(viewData);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[NiumaUI] Toolkit Binding 刷新失败：ViewId={ViewId}, {exception.Message}\n{exception.StackTrace}");
            }
        }

        public void Tick(float deltaTime)
        {
            OnTick(deltaTime);
        }

        public void Dispose()
        {
            OnClear(UIViewModelClearReason.Disposed);
            OnDispose();
            _callbacks.Dispose();
            Root = null;
            ViewId = null;
            IsOpen = false;
        }

        protected T Query<T>(string elementName) where T : VisualElement
        {
            return ToolkitElementUtility.QueryOptional<T>(Root, elementName);
        }

        protected T QueryRequired<T>(string elementName) where T : VisualElement
        {
            return ToolkitElementUtility.QueryRequired<T>(Root, elementName, GetType().Name);
        }

        protected Label QLabel(string elementName)
        {
            return Query<Label>(elementName);
        }

        protected Button QButton(string elementName)
        {
            return Query<Button>(elementName);
        }

        protected ListView QListView(string elementName)
        {
            return Query<ListView>(elementName);
        }

        protected void SetText(Label label, string text)
        {
            ToolkitElementUtility.SetText(label, text);
        }

        protected void SetElementVisible(VisualElement element, bool visible)
        {
            ToolkitElementUtility.SetDisplay(element, visible);
        }

        protected void SetElementEnabled(VisualElement element, bool enabled)
        {
            ToolkitElementUtility.SetEnabled(element, enabled);
        }

        protected void ShowContent(VisualElement contentRoot, VisualElement emptyRoot = null, VisualElement errorRoot = null, VisualElement loadingRoot = null)
        {
            ToolkitElementUtility.ApplyPanelState(ToolkitPanelVisualState.Content, contentRoot, emptyRoot, errorRoot, loadingRoot);
        }

        protected void ShowEmpty(VisualElement contentRoot, VisualElement emptyRoot, VisualElement errorRoot = null, VisualElement loadingRoot = null)
        {
            ToolkitElementUtility.ApplyPanelState(ToolkitPanelVisualState.Empty, contentRoot, emptyRoot, errorRoot, loadingRoot);
        }

        protected void ShowError(VisualElement contentRoot, VisualElement emptyRoot, VisualElement errorRoot, VisualElement loadingRoot, Label errorLabel, string message)
        {
            ToolkitElementUtility.ApplyPanelState(ToolkitPanelVisualState.Error, contentRoot, emptyRoot, errorRoot, loadingRoot, errorLabel, message);
        }

        protected void ShowLoading(VisualElement contentRoot, VisualElement emptyRoot, VisualElement errorRoot, VisualElement loadingRoot)
        {
            ToolkitElementUtility.ApplyPanelState(ToolkitPanelVisualState.Loading, contentRoot, emptyRoot, errorRoot, loadingRoot);
        }

        protected void ApplyIcon(VisualElement iconElement, string addressKey, IToolkitIconResolver resolver)
        {
            ToolkitElementUtility.ApplyIcon(iconElement, addressKey, resolver);
        }

        protected void LogWarning(string message)
        {
            Debug.LogWarning($"[{GetType().Name}] {message}");
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnOpen() { }
        protected virtual void OnClose() { }
        protected virtual void OnRefresh(object viewData) { }
        protected virtual void OnTick(float deltaTime) { }
        protected virtual void OnClear(UIViewModelClearReason reason) { }
        protected virtual void OnDispose() { }
    }

    /// <summary>
    /// 带强类型 ViewData 和 ViewModel 的 Binding 基类。
    /// </summary>
    public abstract class ToolkitViewBindingBase<TViewData, TViewModel> : ToolkitViewBindingBase
        where TViewModel : ToolkitPanelViewModelBase, new()
    {
        protected TViewModel ViewModel { get; private set; }
        protected TViewData CurrentViewData { get; private set; }

        protected override void OnInitialize()
        {
            ViewModel = new TViewModel();
            ViewModel.Initialize(ViewId);
            OnInitializeTyped();
        }

        protected sealed override void OnRefresh(object viewData)
        {
            if (!(viewData is TViewData typedData))
            {
                LogWarning($"ViewData 类型不匹配。期望={typeof(TViewData).Name}，实际={viewData?.GetType().Name ?? "null"}");
                return;
            }

            CurrentViewData = typedData;
            OnRefreshTyped(typedData, ViewModel);
        }

        protected override void OnClear(UIViewModelClearReason reason)
        {
            ViewModel?.Clear(reason);
            CurrentViewData = default;
            OnClearTyped(reason);
        }

        protected override void OnDispose()
        {
            ViewModel?.Dispose();
            ViewModel = null;
            CurrentViewData = default;
            OnDisposeTyped();
        }

        protected virtual void OnInitializeTyped() { }
        protected abstract void OnRefreshTyped(TViewData viewData, TViewModel viewModel);
        protected virtual void OnClearTyped(UIViewModelClearReason reason) { }
        protected virtual void OnDisposeTyped() { }
    }

    /// <summary>
    /// 无自定义逻辑的默认 Binding。用于测试或纯静态 UXML。
    /// </summary>
    public sealed class DefaultToolkitViewBinding : ToolkitViewBindingBase
    {
    }
}
