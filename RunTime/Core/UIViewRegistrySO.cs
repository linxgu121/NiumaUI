using System;
using System.Collections.Generic;
using UnityEngine;

namespace NiumaUI.Core
{
    /// <summary>
    /// UI 视图注册表 ScriptableObject
    /// 设计为纯数据容器，编辑器中配置视图 ID、预制体、层级和缓存策略等信息，运行时由 DefaultViewFactory 读取并实例化视图
    /// </summary>
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
        [Tooltip("是否缓存实例，决定视图关闭后是销毁还是隐藏并保留实例以供下次快速显示")]
        [SerializeField] private bool cacheInstance = true;
        [Tooltip("是否初始状态隐藏，决定视图实例化后是立即显示还是保持隐藏状态，直到调用 Show() 方法")]
        [SerializeField] private bool startHidden = true;

        public string ViewId => viewId;
        public ViewBindingBase Prefab => prefab;
        public string LayerId => layerId;
        public bool CacheInstance => cacheInstance;
        public bool StartHidden => startHidden;
    }
}
