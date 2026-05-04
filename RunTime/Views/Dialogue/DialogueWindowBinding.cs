using NiumaUI.Core;
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

        [Header("References")]
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private GameObject continueHint;

        private CanvasGroup _canvasGroup;

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

        private void EnsureBuilt()
        {
            // Low-code mode: build a usable dialogue window if artists have not provided a prefab yet.
            if (transform is RectTransform selfRect)
                Stretch(selfRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (speakerText != null && bodyText != null && continueHint != null)
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
