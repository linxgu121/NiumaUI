using UnityEngine.UIElements;

namespace NiumaUI.Toolkit
{
    /// <summary>
    /// UI Toolkit View 的运行时实例。
    /// 保存注册项、VisualElement 根节点、可选模态遮罩和 Binding 实例。
    /// </summary>
    public sealed class UIToolkitViewInstance
    {
        private readonly UIToolkitViewEntry _entry;
        private readonly VisualElement _root;
        private readonly VisualElement _modalBlocker;
        private readonly IToolkitViewBinding _binding;

        public UIToolkitViewInstance(UIToolkitViewEntry entry, VisualElement root, VisualElement modalBlocker, IToolkitViewBinding binding)
        {
            _entry = entry;
            _root = root;
            _modalBlocker = modalBlocker;
            _binding = binding;
        }

        public string ViewId => _entry?.ViewId;
        public string LayerId => _entry?.LayerId;
        public string DefaultFocusName => _entry?.DefaultFocusName;
        public UIToolkitViewCachePolicy CachePolicy => _entry != null ? _entry.CachePolicy : UIToolkitViewCachePolicy.DestroyOnClose;
        public UIToolkitViewModalPolicy ModalPolicy => _entry != null ? _entry.ModalPolicy : UIToolkitViewModalPolicy.None;
        public UIToolkitViewInputPolicy InputPolicy => _entry != null ? _entry.InputPolicy : UIToolkitViewInputPolicy.None;
        public UIToolkitViewBackPolicy BackPolicy => _entry != null ? _entry.BackPolicy : UIToolkitViewBackPolicy.None;
        public VisualElement Root => _root;
        public VisualElement ModalBlocker => _modalBlocker;
        public IToolkitViewBinding Binding => _binding;
        public bool IsOpen => _binding != null && _binding.IsOpen;

        public void Open(object viewData)
        {
            if (_modalBlocker != null)
                _modalBlocker.style.display = DisplayStyle.Flex;

            _binding?.Open();
            if (viewData != null)
                _binding?.Refresh(viewData);
        }

        public void Refresh(object viewData)
        {
            _binding?.Refresh(viewData);
        }

        public void Tick(float deltaTime)
        {
            _binding?.Tick(deltaTime);
        }

        public void Close()
        {
            _binding?.Close();
            if (_modalBlocker != null)
                _modalBlocker.style.display = DisplayStyle.None;
        }

        public void FocusDefaultElement()
        {
            if (_root == null || string.IsNullOrWhiteSpace(DefaultFocusName))
                return;

            _root.Q<VisualElement>(DefaultFocusName)?.Focus();
        }

        public void Dispose()
        {
            _binding?.Dispose();
            _root?.RemoveFromHierarchy();
            _modalBlocker?.RemoveFromHierarchy();
        }
    }
}