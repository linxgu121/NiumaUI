using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace NiumaUI.Toolkit.Common
{
    /// <summary>
    /// UI Toolkit ListView 绑定助手。
    /// 后续业务面板的物品列表、商品列表、玩家列表、聊天列表都应优先使用 ListView，避免大量手动创建子节点。
    /// </summary>
    public sealed class ToolkitListViewBinder<TItem> : IDisposable
    {
        private readonly List<TItem> _items = new List<TItem>();
        private ListView _listView;
        private VisualElement _emptyRoot;
        private Action<VisualElement, TItem, int> _bindItem;

        public IReadOnlyList<TItem> Items => _items;
        public bool HasListView => _listView != null;

        public bool Bind(
            VisualElement root,
            string listViewName,
            Func<VisualElement> makeItem,
            Action<VisualElement, TItem, int> bindItem,
            string emptyRootName = null)
        {
            var listView = ToolkitElementUtility.QueryListView(root, listViewName);
            var emptyRoot = ToolkitElementUtility.Query<VisualElement>(root, emptyRootName);
            Bind(listView, makeItem, bindItem, emptyRoot);
            return listView != null;
        }

        public void Bind(
            ListView listView,
            Func<VisualElement> makeItem,
            Action<VisualElement, TItem, int> bindItem,
            VisualElement emptyRoot = null)
        {
            _listView = listView;
            _emptyRoot = emptyRoot;
            _bindItem = bindItem;

            if (_listView == null)
                return;

            _listView.itemsSource = _items;
            _listView.makeItem = makeItem ?? MakeDefaultItem;
            _listView.bindItem = BindListItem;
            Refresh();
        }

        public void SetItems(IEnumerable<TItem> items)
        {
            _items.Clear();
            if (items != null)
            {
                foreach (var item in items)
                    _items.Add(item);
            }

            Refresh();
        }

        public void Clear()
        {
            _items.Clear();
            Refresh();
        }

        public void Refresh()
        {
            var isEmpty = _items.Count == 0;
            ToolkitElementUtility.ApplyEmptyState(_listView, _emptyRoot, isEmpty);

            if (_listView == null)
                return;

            _listView.itemsSource = _items;
            _listView.Rebuild();
        }

        public void Dispose()
        {
            if (_listView != null)
            {
                _listView.itemsSource = null;
                _listView.makeItem = null;
                _listView.bindItem = null;
            }

            _items.Clear();
            _listView = null;
            _emptyRoot = null;
            _bindItem = null;
        }

        private void BindListItem(VisualElement element, int index)
        {
            if (_bindItem == null || element == null || index < 0 || index >= _items.Count)
                return;

            _bindItem(element, _items[index], index);
        }

        private static VisualElement MakeDefaultItem()
        {
            return new Label();
        }
    }
}
