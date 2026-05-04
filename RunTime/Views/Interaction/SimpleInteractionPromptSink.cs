using NiumaInteract.Core.Data;
using NiumaInteract.Core.Interface;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NiumaUI.Views.Interaction
{
    /// <summary>
    /// 简单交互提示 UI。
    /// 用于测试交互链路：靠近目标显示提示，交互成功后隐藏提示。
    /// </summary>
    public sealed class SimpleInteractionPromptSink : MonoBehaviour, IInteractionPromptSink
    {
        [Header("UI 引用")]
        [Tooltip("需要显隐的提示根节点。建议绑定提示面板子物体，不要绑定挂载本脚本的物体。为空时仅使用 CanvasGroup 控制显隐。")]
        [SerializeField] private GameObject root;
        [Tooltip("提示面板的 CanvasGroup。用于透明隐藏且不阻挡射线；为空时只使用 Root.SetActive。")]
        [SerializeField] private CanvasGroup canvasGroup;
        [Tooltip("显示交互提示文字的 TMP_Text，例如“[E] 拾取 物品”。")]
        [SerializeField] private TMP_Text promptText;
        [Tooltip("长按进度图片。图片类型需要支持 Filled；短按交互可以不绑定。")]
        [SerializeField] private Image holdProgressImage;

        [Header("文本")]
        [Tooltip("显示给玩家看的交互按键名称，只影响 UI 文本，不影响真实输入绑定。")]
        [SerializeField] private string interactKeyLabel = "E";
        [Tooltip("提示文本格式。{0}=按键名称，{1}=提示文本，{2}=目标名称。")]
        [SerializeField] private string textFormat = "[{0}] {1} {2}";

        private void Awake()
        {
            ResolveReferences();
            HidePrompt();
        }

        /// <summary>
        /// 显示或刷新交互提示。
        /// </summary>
        public void ShowPrompt(in InteractionPromptData data)
        {
            ResolveReferences();

            if (!data.HasTarget)
            {
                HidePrompt();
                return;
            }

            SetVisible(true);

            if (promptText != null)
            {
                promptText.text = string.Format(
                    textFormat,
                    interactKeyLabel,
                    data.PromptText,
                    data.TargetName);
            }

            if (holdProgressImage != null)
                holdProgressImage.fillAmount = data.HoldProgress;
        }

        /// <summary>
        /// 隐藏交互提示。
        /// </summary>
        public void HidePrompt()
        {
            ResolveReferences();
            SetVisible(false);

            if (holdProgressImage != null)
                holdProgressImage.fillAmount = 0f;
        }

        private void ResolveReferences()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }

        private void SetVisible(bool visible)
        {
            if (root != null && root != gameObject && root.activeSelf != visible)
                root.SetActive(visible);

            if (canvasGroup == null)
                return;

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}
