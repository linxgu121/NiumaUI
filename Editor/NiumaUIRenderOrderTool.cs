using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UGUIText = UnityEngine.UI.Text;

namespace NiumaUI.EditorTools
{
    /// <summary>
    /// UI 层级整理工具。
    /// Unity UI 的显示优先级主要由 Canvas 排序和同父级 Hierarchy 顺序决定：同一父节点下越靠后的 Graphic 越后绘制，也就是越显示在上面。
    /// </summary>
    public sealed class NiumaUIRenderOrderTool : EditorWindow
    {
        private bool includeInactive = true;
        private bool includeTmpText = true;
        private bool includeLegacyText = true;
        private Vector2 scroll;

        [MenuItem("Tools/Niuma/UI/打开 UI 层级工具")]
        public static void Open()
        {
            GetWindow<NiumaUIRenderOrderTool>("Niuma UI 层级");
        }

        [MenuItem("Tools/Niuma/UI/选中根节点/文字置顶")]
        public static void BringSelectedRootTextsToFront()
        {
            BringTextsToFront(Selection.gameObjects, true, true, true);
        }

        [MenuItem("Tools/Niuma/UI/选中根节点/扫描可能遮挡文字的图片")]
        public static void ScanSelectedRootsForCoverImages()
        {
            ScanPotentialCoverImages(Selection.gameObjects, true, true, true);
        }

        [MenuItem("Tools/Niuma/UI/选中物体/置顶")]
        public static void BringSelectedObjectsToFront()
        {
            ReorderSelectedObjects(true);
        }

        [MenuItem("Tools/Niuma/UI/选中物体/置底")]
        public static void SendSelectedObjectsToBack()
        {
            ReorderSelectedObjects(false);
        }

        [MenuItem("Tools/Niuma/UI/选中根节点/文字置顶", true)]
        [MenuItem("Tools/Niuma/UI/选中根节点/扫描可能遮挡文字的图片", true)]
        [MenuItem("Tools/Niuma/UI/选中物体/置顶", true)]
        [MenuItem("Tools/Niuma/UI/选中物体/置底", true)]
        public static bool HasSelection()
        {
            return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.LabelField("Niuma UI 层级工具", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Unity UI 内置规则：同一个 Canvas 下，Hierarchy 中越靠后的 Graphic 越显示在上面；不同 Canvas 之间看 Sorting Layer / Order in Layer。\n\n" +
                "如果图片遮住文字，优先检查：文字和图片是否在同一个 Canvas、同父级顺序是否正确、是否有子 Canvas 覆盖了排序。",
                MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("处理选项", EditorStyles.boldLabel);
            includeInactive = EditorGUILayout.ToggleLeft("包含未激活物体", includeInactive);
            includeTmpText = EditorGUILayout.ToggleLeft("处理 TextMeshPro 文本", includeTmpText);
            includeLegacyText = EditorGUILayout.ToggleLeft("处理 Unity UI Text 文本", includeLegacyText);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"当前选中：{Selection.gameObjects.Length} 个物体", EditorStyles.miniBoldLabel);

            using (new EditorGUI.DisabledScope(Selection.gameObjects.Length == 0))
            {
                if (GUILayout.Button("把选中根节点下的文字置顶", GUILayout.Height(32)))
                {
                    BringTextsToFront(Selection.gameObjects, includeInactive, includeTmpText, includeLegacyText);
                }

                if (GUILayout.Button("扫描可能遮挡文字的图片", GUILayout.Height(28)))
                {
                    ScanPotentialCoverImages(Selection.gameObjects, includeInactive, includeTmpText, includeLegacyText);
                }

                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("选中物体置顶"))
                {
                    ReorderSelectedObjects(true);
                }

                if (GUILayout.Button("选中物体置底"))
                {
                    ReorderSelectedObjects(false);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "推荐用法：选中一个窗口根节点或 Panel 根节点，点击“把选中根节点下的文字置顶”。\n" +
                "如果仍然被遮住，说明遮挡物可能在不同 Canvas 或父级层级更高，使用扫描功能查看 Console 提示。",
                MessageType.None);

            EditorGUILayout.EndScrollView();
        }

        private static void BringTextsToFront(GameObject[] roots, bool includeInactive, bool includeTmp, bool includeLegacy)
        {
            if (roots == null || roots.Length == 0)
            {
                return;
            }

            var moved = 0;
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                Undo.RegisterFullObjectHierarchyUndo(root, "Niuma UI 文字置顶");
                var textTransforms = CollectTextTransforms(root, includeInactive, includeTmp, includeLegacy);
                for (var j = 0; j < textTransforms.Count; j++)
                {
                    var target = textTransforms[j];
                    if (target == null || target.parent == null)
                    {
                        continue;
                    }

                    var before = target.GetSiblingIndex();
                    target.SetAsLastSibling();
                    if (target.GetSiblingIndex() != before)
                    {
                        moved++;
                    }
                }

                MarkDirty(root);
            }

            Debug.Log($"[NiumaUIRenderOrderTool] 文字置顶完成，移动 {moved} 个文本节点。");
        }

        private static void ReorderSelectedObjects(bool toFront)
        {
            var selected = Selection.transforms;
            if (selected == null || selected.Length == 0)
            {
                return;
            }

            var moved = 0;
            for (var i = 0; i < selected.Length; i++)
            {
                var target = selected[i];
                if (target == null || target.parent == null)
                {
                    continue;
                }

                Undo.RegisterCompleteObjectUndo(target.parent, toFront ? "Niuma UI 置顶" : "Niuma UI 置底");
                var before = target.GetSiblingIndex();
                if (toFront)
                {
                    target.SetAsLastSibling();
                }
                else
                {
                    target.SetAsFirstSibling();
                }

                if (target.GetSiblingIndex() != before)
                {
                    moved++;
                    MarkDirty(target.gameObject);
                }
            }

            Debug.Log($"[NiumaUIRenderOrderTool] {(toFront ? "置顶" : "置底")}完成，移动 {moved} 个节点。");
        }

        private static void ScanPotentialCoverImages(GameObject[] roots, bool includeInactive, bool includeTmp, bool includeLegacy)
        {
            if (roots == null || roots.Length == 0)
            {
                return;
            }

            var warnings = 0;
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                var textTransforms = CollectTextTransforms(root, includeInactive, includeTmp, includeLegacy);
                for (var j = 0; j < textTransforms.Count; j++)
                {
                    var text = textTransforms[j];
                    if (text == null || text.parent == null)
                    {
                        continue;
                    }

                    var parent = text.parent;
                    var textIndex = text.GetSiblingIndex();
                    for (var childIndex = textIndex + 1; childIndex < parent.childCount; childIndex++)
                    {
                        var sibling = parent.GetChild(childIndex);
                        if (sibling == null || (!includeInactive && !sibling.gameObject.activeInHierarchy))
                        {
                            continue;
                        }

                        if (!HasCoverGraphic(sibling.gameObject))
                        {
                            continue;
                        }

                        warnings++;
                        Debug.LogWarning(
                            $"[NiumaUIRenderOrderTool] 可能遮挡文字：{GetPath(sibling)} 位于文字 {GetPath(text)} 后面。同父级后绘制会盖在前者上方。",
                            sibling.gameObject);
                    }
                }
            }

            if (warnings == 0)
            {
                Debug.Log("[NiumaUIRenderOrderTool] 未发现同父级中位于文字后面的 Image/RawImage。若仍遮挡，请检查不同 Canvas 的 Sorting Order。");
            }
            else
            {
                Debug.Log($"[NiumaUIRenderOrderTool] 扫描完成，发现 {warnings} 个可能遮挡项。点击 Console 日志可定位对象。");
            }
        }

        private static List<Transform> CollectTextTransforms(GameObject root, bool includeInactive, bool includeTmp, bool includeLegacy)
        {
            var result = new List<Transform>();
            var added = new HashSet<Transform>();

            if (includeTmp)
            {
                var tmpTexts = root.GetComponentsInChildren<TMP_Text>(includeInactive);
                for (var i = 0; i < tmpTexts.Length; i++)
                {
                    AddTransform(result, added, tmpTexts[i] == null ? null : tmpTexts[i].transform);
                }
            }

            if (includeLegacy)
            {
                var legacyTexts = root.GetComponentsInChildren<UGUIText>(includeInactive);
                for (var i = 0; i < legacyTexts.Length; i++)
                {
                    AddTransform(result, added, legacyTexts[i] == null ? null : legacyTexts[i].transform);
                }
            }

            return result;
        }

        private static void AddTransform(List<Transform> result, HashSet<Transform> added, Transform target)
        {
            if (target == null || added.Contains(target))
            {
                return;
            }

            added.Add(target);
            result.Add(target);
        }

        private static bool HasCoverGraphic(GameObject target)
        {
            if (target == null)
            {
                return false;
            }

            var image = target.GetComponent<Image>();
            if (image != null && image.enabled)
            {
                return true;
            }

            var rawImage = target.GetComponent<RawImage>();
            return rawImage != null && rawImage.enabled;
        }

        private static void MarkDirty(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            EditorUtility.SetDirty(target);
            var scene = target.scene;
            if (scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        private static string GetPath(Transform target)
        {
            if (target == null)
            {
                return string.Empty;
            }

            var path = target.name;
            var parent = target.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    }
}
