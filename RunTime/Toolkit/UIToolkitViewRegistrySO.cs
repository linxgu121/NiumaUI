using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NiumaUI.Toolkit
{
    /// <summary>
    /// UI Toolkit View 注册表。
    /// 用于登记 ViewId、UXML、USS、BindingProviderId、层级和打开策略。
    /// </summary>
    [CreateAssetMenu(fileName = "UIToolkitViewRegistry", menuName = "NiumaUI/Toolkit View Registry")]
    public sealed class UIToolkitViewRegistrySO : ScriptableObject
    {
        [Tooltip("UI Toolkit View 注册项。每个 ViewId 必须唯一。")]
        [SerializeField] private List<UIToolkitViewEntry> views = new List<UIToolkitViewEntry>();

        public IReadOnlyList<UIToolkitViewEntry> Views => views;

        public bool TryGet(string viewId, out UIToolkitViewEntry entry)
        {
            for (var i = 0; i < views.Count; i++)
            {
                var candidate = views[i];
                if (candidate != null && string.Equals(candidate.ViewId, viewId, StringComparison.Ordinal))
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = null;
            return false;
        }
    }

    [Serializable]
    public sealed class UIToolkitViewEntry
    {
        [Header("基础信息")]
        [Tooltip("稳定窗口 ID。业务模块和 UIManager 都通过这个 ID 打开窗口，例如 DialogueWindow、InventoryPanel。")]
        [SerializeField] private string viewId;

        [Tooltip("UXML 资产。由 UI Builder 制作，是该窗口的结构。")]
        [SerializeField] private VisualTreeAsset visualTreeAsset;

        [Tooltip("USS 样式表。可配置多个，工厂创建 View 时会全部挂到根元素上。")]
        [SerializeField] private List<StyleSheet> styleSheets = new List<StyleSheet>();

        [Tooltip("Binding 创建器 ID。由程序提供，例如 Default、DialogueWindow、InventoryPanel；为空时使用 Default。")]
        [SerializeField] private string bindingProviderId = "Default";

        [Header("层级与策略")]
        [Tooltip("生成层级 ID。需要与 UIToolkitLayerRoot.LayerId 一致，例如 HUD、Prompt、Dialogue、Menu、Popup、Loading、Debug。")]
        [SerializeField] private string layerId = "Default";

        [Tooltip("关闭后的缓存策略。HideAndCache 适合常用窗口；DestroyOnClose 适合重型或一次性窗口。")]
        [SerializeField] private UIToolkitViewCachePolicy cachePolicy = UIToolkitViewCachePolicy.HideAndCache;

        [Tooltip("模态策略。Popup、Confirm、Loading 建议使用 Modal；HUD、Prompt 使用 None。")]
        [SerializeField] private UIToolkitViewModalPolicy modalPolicy = UIToolkitViewModalPolicy.None;

        [Tooltip("玩法输入策略。对话、菜单、弹窗、加载遮罩建议 BlockGameplayInput；HUD、提示使用 None。")]
        [SerializeField] private UIToolkitViewInputPolicy inputPolicy = UIToolkitViewInputPolicy.None;

        [Header("焦点")]
        [Tooltip("默认焦点元素 Name。用于键盘/手柄导航；可留空。")]
        [SerializeField] private string defaultFocusName;

        public string ViewId => viewId;
        public VisualTreeAsset VisualTreeAsset => visualTreeAsset;
        public IReadOnlyList<StyleSheet> StyleSheets => styleSheets;
        public string BindingProviderId => string.IsNullOrWhiteSpace(bindingProviderId) ? "Default" : bindingProviderId;
        public string LayerId => string.IsNullOrWhiteSpace(layerId) ? "Default" : layerId;
        public UIToolkitViewCachePolicy CachePolicy => cachePolicy;
        public UIToolkitViewModalPolicy ModalPolicy => modalPolicy;
        public UIToolkitViewInputPolicy InputPolicy => inputPolicy;
        public string DefaultFocusName => defaultFocusName;
    }

    [Serializable]
    public sealed class UIToolkitLayerRoot
    {
        [Tooltip("层级 ID。需要与 UIToolkitViewEntry.LayerId 完全一致，例如 HUD、Dialogue、Popup。")]
        [SerializeField] private string layerId = "Default";

        [Tooltip("该层级对应的 UIDocument。建议每个主要层级一个 UIDocument。")]
        [SerializeField] private UIDocument document;

        [Tooltip("该 UIDocument 内作为生成父节点的 VisualElement Name。为空时使用 document.rootVisualElement。")]
        [SerializeField] private string rootElementName;

        public string LayerId => string.IsNullOrWhiteSpace(layerId) ? "Default" : layerId;
        public UIDocument Document => document;
        public string RootElementName => rootElementName;
    }
}