using System;
using System.Collections.Generic;
using NiumaUI.Enum;
using UnityEngine;
using UnityEngine.UIElements;

namespace NiumaUI.Toolkit
{
    /// <summary>
    /// UI Toolkit View 注册表。
    /// ViewId 是业务层打开 UI 的稳定协议 ID，不应因为 UXML 文件改名而变化。
    /// </summary>
    [CreateAssetMenu(fileName = "UIToolkitViewRegistry", menuName = "NiumaUI/Toolkit View Registry")]
    public sealed class UIToolkitViewRegistrySO : ScriptableObject
    {
        [Tooltip("UI Toolkit View 注册项。每个 ViewId 必须唯一。")]
        [SerializeField] private List<UIToolkitViewEntry> views = new List<UIToolkitViewEntry>();

        public IReadOnlyList<UIToolkitViewEntry> Views => views;

        public bool TryGet(string viewId, out UIToolkitViewEntry entry)
        {
            entry = FindDirect(viewId);
            if (entry == null)
                return false;

            if (!entry.IsDeprecated)
                return true;

            if (string.IsNullOrWhiteSpace(entry.ReplacementViewId) || string.Equals(entry.ViewId, entry.ReplacementViewId, StringComparison.Ordinal))
            {
                Debug.LogWarning($"[NiumaUI] ViewId={entry.ViewId} 已标记废弃，但没有配置有效替代 ViewId。{entry.MigrationNote}");
                return true;
            }

            var replacement = FindDirect(entry.ReplacementViewId);
            if (replacement == null)
            {
                Debug.LogWarning($"[NiumaUI] ViewId={entry.ViewId} 已废弃，但替代 ViewId={entry.ReplacementViewId} 未注册。{entry.MigrationNote}");
                return true;
            }

            Debug.LogWarning($"[NiumaUI] ViewId={entry.ViewId} 已废弃，已重定向到 {replacement.ViewId}。{entry.MigrationNote}");
            entry = replacement;
            return true;
        }

        private UIToolkitViewEntry FindDirect(string viewId)
        {
            if (string.IsNullOrWhiteSpace(viewId))
                return null;

            viewId = viewId.Trim();
            for (var i = 0; i < views.Count; i++)
            {
                var candidate = views[i];
                if (candidate != null && string.Equals(candidate.ViewId, viewId, StringComparison.Ordinal))
                    return candidate;
            }

            return null;
        }
    }

    [Serializable]
    public sealed class UIToolkitViewEntry
    {
        [Header("基础信息")]
        [Tooltip("稳定窗口 ID。业务模块和 UIManager 都通过这个 ID 打开窗口，例如 DialogueWindow、InventoryPanel。")]
        [SerializeField] private string viewId;

        [Tooltip("View 协议版本，遵循 SemVer 2.0，例如 2.1.0。ViewId 不变但 UXML/Binding 结构有破坏性变化时，必须提升主版本。")]
        [SerializeField] private string viewVersion = "2.1.0";

        [Tooltip("勾选后表示该 ViewId 已废弃。旧调用仍可被重定向到 Replacement ViewId，但 README 不再推荐使用。")]
        [SerializeField] private bool deprecated;

        [Tooltip("废弃 ViewId 的替代 ViewId。留空时只输出警告，不自动跳转。")]
        [SerializeField] private string replacementViewId;

        [Tooltip("计划移除该旧 ViewId 的版本号，例如 3.0.0。")]
        [SerializeField] private string removeAfterVersion;

        [Tooltip("迁移说明：写给策划和程序，说明旧 ViewId 为什么废弃、应改用哪个 ViewId。")]
        [TextArea]
        [SerializeField] private string migrationNote;

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

        [Tooltip("玩法输入阻塞原因。Input Policy 为 BlockGameplayInput 时生效；对话窗口选 Dialogue，菜单/弹窗/加载通常选 Menu，演出选 Cinematic。")]
        [SerializeField] private UIMode inputBlockMode = UIMode.Menu;

        [Tooltip("返回栈策略。菜单、弹窗建议 CloseOnBack；HUD、Prompt、Toast 建议 None。")]
        [SerializeField] private UIToolkitViewBackPolicy backPolicy = UIToolkitViewBackPolicy.CloseOnBack;

        [Header("焦点")]
        [Tooltip("默认焦点元素 Name。用于键盘/手柄导航；可留空。")]
        [SerializeField] private string defaultFocusName;

        public string ViewId => string.IsNullOrWhiteSpace(viewId) ? null : viewId.Trim();
        public string ViewVersion => string.IsNullOrWhiteSpace(viewVersion) ? "2.1.0" : viewVersion.Trim();
        public bool IsDeprecated => deprecated;
        public string ReplacementViewId => string.IsNullOrWhiteSpace(replacementViewId) ? null : replacementViewId.Trim();
        public string RemoveAfterVersion => string.IsNullOrWhiteSpace(removeAfterVersion) ? null : removeAfterVersion.Trim();
        public string MigrationNote => migrationNote;
        public VisualTreeAsset VisualTreeAsset => visualTreeAsset;
        public IReadOnlyList<StyleSheet> StyleSheets => styleSheets;
        public string BindingProviderId => string.IsNullOrWhiteSpace(bindingProviderId) ? "Default" : bindingProviderId.Trim();
        public string LayerId => string.IsNullOrWhiteSpace(layerId) ? "Default" : layerId.Trim();
        public UIToolkitViewCachePolicy CachePolicy => cachePolicy;
        public UIToolkitViewModalPolicy ModalPolicy => modalPolicy;
        public UIToolkitViewInputPolicy InputPolicy => inputPolicy;
        public UIMode InputBlockMode => inputBlockMode;
        public UIToolkitViewBackPolicy BackPolicy => backPolicy;
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

        public string LayerId => string.IsNullOrWhiteSpace(layerId) ? "Default" : layerId.Trim();
        public UIDocument Document => document;
        public string RootElementName => rootElementName;
    }
}
