using NiumaUI.Core;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NiumaUI.Views.Dialogue
{
    /// <summary>
    /// Unity binding for the dialogue window. It owns scene/UI objects while DialogueWindowView stays pure C#.
    /// </summary>
    public sealed class DialogueWindowBinding : ViewBindingBase
    {
        [Header("Auto Build")]
        [SerializeField] private bool buildOnAwakeIfMissing = true;
        [SerializeField] private bool rebuildInReset = true;

        [Header("Style")]
        [SerializeField] private Color panelColor = new Color(0.035f, 0.039f, 0.05f, 0.92f);
        [SerializeField] private Color panelLineColor = new Color(0.62f, 0.78f, 1f, 0.9f);
        [SerializeField] private Color speakerPlateColor = new Color(0.1f, 0.16f, 0.24f, 0.96f);
        [SerializeField] private Color speakerTextColor = new Color(0.9f, 0.96f, 1f, 1f);
        [SerializeField] private Color bodyTextColor = new Color(0.95f, 0.95f, 0.92f, 1f);
        [SerializeField] private Color hintColor = new Color(0.9f, 0.72f, 0.32f, 1f);
        [SerializeField] private Color choiceButtonColor = new Color(0.12f, 0.16f, 0.23f, 0.94f);
        [SerializeField] private Color choiceButtonDisabledColor = new Color(0.12f, 0.12f, 0.13f, 0.72f);
        [SerializeField] private Color choiceTextColor = new Color(0.94f, 0.95f, 1f, 1f);
        [SerializeField] private Color choiceDisabledTextColor = new Color(0.55f, 0.57f, 0.62f, 1f);

        [Header("References")]
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private GameObject continueHint;
        [Tooltip("选项按钮父节点。UI 策划可手动绑定；为空时会自动创建。")]
        [SerializeField] private RectTransform choiceRoot;
        [Tooltip("选项按钮模板。需要包含 Button 和 TMP_Text；为空时会自动创建。")]
        [SerializeField] private Button choiceButtonTemplate;

        private CanvasGroup _canvasGroup;
        private readonly List<Button> _choiceButtons = new List<Button>();

        private void Reset()
        {
            if (rebuildInReset)
                EnsureBuilt();
        }

        private void Awake()
        {
            if (buildOnAwakeIfMissing)
                EnsureBuilt();
        }

        public override void Show()
        {
            EnsureBuilt();
            base.Show();

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
        }

        public override void Hide()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            base.Hide();
        }

        protected override ViewBase CreateView()
        {
            EnsureBuilt();
            return new DialogueWindowView(this);
        }

        public void SetContent(string speaker, string body, bool showContinueHint)
        {
            EnsureBuilt();

            // Keep all Unity component writes inside Binding so the View layer stays engine-agnostic.
            if (speakerText != null)
                speakerText.text = speaker ?? string.Empty;

            if (bodyText != null)
                bodyText.text = body ?? string.Empty;

            if (continueHint != null)
                continueHint.SetActive(showContinueHint);
        }

        /// <summary>
        /// 刷新对话选项按钮。
        /// 按钮点击只回传 ChoiceId，具体对话分支由 Gal 服务层处理。
        /// </summary>
        public void SetChoices(DialogueChoiceOptionData[] choices)
        {
            EnsureBuilt();

            if (choiceButtonTemplate != null)
                choiceButtonTemplate.gameObject.SetActive(false);

            var count = choices?.Length ?? 0;
            if (choiceRoot != null)
                choiceRoot.gameObject.SetActive(count > 0);

            for (var i = 0; i < count; i++)
            {
                var button = GetOrCreateChoiceButton(i);
                ApplyChoiceButton(button, choices[i]);
            }

            for (var i = count; i < _choiceButtons.Count; i++)
            {
                if (_choiceButtons[i] != null)
                    _choiceButtons[i].gameObject.SetActive(false);
            }
        }

        private void EnsureBuilt()
        {
            // Low-code mode: build a usable dialogue window if artists have not provided a prefab yet.
            if (transform is RectTransform selfRect)
                Stretch(selfRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (speakerText != null && bodyText != null && continueHint != null && choiceRoot != null && choiceButtonTemplate != null)
                return;

            var root = GetOrCreateRect("DialogueWindowRoot", transform);
            Stretch(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var panel = GetOrCreateRect("Panel", root);
            Stretch(panel, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.31f), Vector2.zero, Vector2.zero);
            SetImage(panel.gameObject, panelColor);
            SetShadow(panel.gameObject, new Color(0f, 0f, 0f, 0.38f), new Vector2(0f, -8f));

            var leftLine = GetOrCreateRect("AccentLine", panel);
            Stretch(leftLine, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(6f, 0f), new Vector2(6f, 0f));
            SetImage(leftLine.gameObject, panelLineColor);

            var speakerPlate = GetOrCreateRect("SpeakerPlate", panel);
            Stretch(speakerPlate, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -58f), new Vector2(280f, -14f));
            SetImage(speakerPlate.gameObject, speakerPlateColor);

            if (speakerText == null)
                speakerText = GetOrCreateText("SpeakerText", speakerPlate);

            Stretch((RectTransform)speakerText.transform, Vector2.zero, Vector2.one, new Vector2(18f, 2f), new Vector2(-18f, -2f));
            speakerText.alignment = TextAlignmentOptions.MidlineLeft;
            speakerText.fontSize = 22f;
            speakerText.fontStyle = FontStyles.Bold;
            speakerText.color = speakerTextColor;
            speakerText.raycastTarget = false;

            if (bodyText == null)
                bodyText = GetOrCreateText("BodyText", panel);

            Stretch((RectTransform)bodyText.transform, Vector2.zero, Vector2.one, new Vector2(36f, 28f), new Vector2(-36f, -72f));
            bodyText.alignment = TextAlignmentOptions.TopLeft;
            bodyText.fontSize = 25f;
            bodyText.lineSpacing = 8f;
            bodyText.enableWordWrapping = true;
            bodyText.overflowMode = TextOverflowModes.Ellipsis;
            bodyText.color = bodyTextColor;
            bodyText.raycastTarget = false;

            if (continueHint == null)
                continueHint = BuildContinueHint(panel);

            if (choiceRoot == null)
                choiceRoot = BuildChoiceRoot(root);

            if (choiceButtonTemplate == null)
                choiceButtonTemplate = BuildChoiceButtonTemplate(choiceRoot);
        }

        private GameObject BuildContinueHint(RectTransform parent)
        {
            var hintRoot = GetOrCreateRect("ContinueHint", parent);
            Stretch(hintRoot, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-148f, 22f), new Vector2(-32f, 54f));
            SetImage(hintRoot.gameObject, new Color(1f, 1f, 1f, 0.06f));

            var hintText = GetOrCreateText("HintText", hintRoot);
            Stretch((RectTransform)hintText.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            hintText.text = "NEXT >";
            hintText.alignment = TextAlignmentOptions.Center;
            hintText.fontSize = 18f;
            hintText.fontStyle = FontStyles.Bold;
            hintText.color = hintColor;
            hintText.raycastTarget = false;

            hintRoot.gameObject.SetActive(false);
            return hintRoot.gameObject;
        }

        private RectTransform BuildChoiceRoot(RectTransform parent)
        {
            var root = GetOrCreateRect("ChoiceRoot", parent);
            Stretch(root, new Vector2(0.56f, 0.32f), new Vector2(0.94f, 0.58f), Vector2.zero, Vector2.zero);

            var layout = root.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = root.gameObject.AddComponent<VerticalLayoutGroup>();

            layout.childAlignment = TextAnchor.LowerRight;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 8f;
            layout.padding = new RectOffset(0, 0, 0, 0);

            root.gameObject.SetActive(false);
            return root;
        }

        private Button BuildChoiceButtonTemplate(RectTransform parent)
        {
            var rect = GetOrCreateRect("ChoiceButtonTemplate", parent);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(0f, 42f);
            SetImage(rect.gameObject, choiceButtonColor);

            var image = rect.GetComponent<Image>();
            if (image != null)
                image.raycastTarget = true;

            var button = rect.GetComponent<Button>();
            if (button == null)
                button = rect.gameObject.AddComponent<Button>();

            var text = GetOrCreateText("ChoiceText", rect);
            Stretch((RectTransform)text.transform, Vector2.zero, Vector2.one, new Vector2(18f, 4f), new Vector2(-18f, -4f));
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.fontSize = 19f;
            text.fontStyle = FontStyles.Bold;
            text.color = choiceTextColor;
            text.raycastTarget = false;

            button.gameObject.SetActive(false);
            return button;
        }

        private Button GetOrCreateChoiceButton(int index)
        {
            while (_choiceButtons.Count <= index)
            {
                var button = Instantiate(choiceButtonTemplate, choiceRoot);
                button.name = $"ChoiceButton_{_choiceButtons.Count:00}";
                button.gameObject.SetActive(false);
                _choiceButtons.Add(button);
            }

            return _choiceButtons[index];
        }

        private void ApplyChoiceButton(Button button, DialogueChoiceOptionData choice)
        {
            if (button == null)
                return;

            var isAvailable = choice != null && choice.IsAvailable;
            var label = choice == null
                ? string.Empty
                : isAvailable
                    ? choice.DisplayText
                    : string.IsNullOrWhiteSpace(choice.DisabledText)
                        ? choice.DisplayText
                        : choice.DisabledText;

            button.gameObject.SetActive(true);
            button.interactable = isAvailable;
            button.onClick.RemoveAllListeners();

            if (isAvailable)
            {
                var choiceId = choice.ChoiceId;
                var callback = choice.OnSelected;
                button.onClick.AddListener(() => callback?.Invoke(choiceId));
            }

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = isAvailable ? choiceButtonColor : choiceButtonDisabledColor;
                image.raycastTarget = true;
            }

            var labelText = button.GetComponentInChildren<TMP_Text>(true);
            if (labelText != null)
            {
                labelText.text = label ?? string.Empty;
                labelText.color = isAvailable ? choiceTextColor : choiceDisabledTextColor;
            }
        }

        private static RectTransform GetOrCreateRect(string objectName, Transform parent)
        {
            var existing = parent.Find(objectName);
            if (existing != null && existing is RectTransform existingRect)
                return existingRect;

            var child = new GameObject(objectName, typeof(RectTransform));
            var rect = (RectTransform)child.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static TMP_Text GetOrCreateText(string objectName, RectTransform parent)
        {
            var existing = parent.Find(objectName);
            if (existing != null && existing.TryGetComponent<TMP_Text>(out var existingText))
                return existingText;

            var child = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = (RectTransform)child.transform;
            rect.SetParent(parent, false);
            return child.GetComponent<TMP_Text>();
        }

        private static void SetImage(GameObject target, Color color)
        {
            var image = target.GetComponent<Image>();
            if (image == null)
                image = target.AddComponent<Image>();

            image.color = color;
            image.raycastTarget = false;
        }

        private static void SetShadow(GameObject target, Color color, Vector2 distance)
        {
            var shadow = target.GetComponent<Shadow>();
            if (shadow == null)
                shadow = target.AddComponent<Shadow>();

            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }
    }
}
