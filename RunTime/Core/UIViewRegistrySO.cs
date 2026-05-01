using System;
using System.Collections.Generic;
using UnityEngine;

namespace NiumaUI.Core
{
    [CreateAssetMenu(fileName = "UIViewRegistry", menuName = "NiumaUI/View Registry")]
    public sealed class UIViewRegistrySO : ScriptableObject
    {
        [SerializeField] private List<UIViewEntry> views = new List<UIViewEntry>();

        public IReadOnlyList<UIViewEntry> Views => views;

        public bool TryGet(string viewId, out UIViewEntry entry)
        {
            for (int i = 0; i < views.Count; i++)
            {
                if (views[i] != null && views[i].ViewId == viewId)
                {
                    entry = views[i];
                    return true;
                }
            }

            entry = null;
            return false;
        }
    }

    [Serializable]
    public sealed class UIViewEntry
    {
        [SerializeField] private string viewId;
        [SerializeField] private ViewBindingBase prefab;
        [SerializeField] private string layerId = "Default";
        [SerializeField] private bool cacheInstance = true;
        [SerializeField] private bool startHidden = true;

        public string ViewId => viewId;
        public ViewBindingBase Prefab => prefab;
        public string LayerId => layerId;
        public bool CacheInstance => cacheInstance;
        public bool StartHidden => startHidden;
    }
}
