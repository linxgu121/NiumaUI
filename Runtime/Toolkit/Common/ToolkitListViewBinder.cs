using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace NiumaUI.Toolkit.Common
{
    /// <summary>
    /// UI Toolkit ListView 绑定助手。支持全量刷新与基础增量更新。
    /// </summary>
    public sealed class ToolkitListViewBinder<TItem> : IDisposable
    {
        private readonly List<TItem> _items = new List<TItem>();
        private ListView _listView;
        private VisualElement _emptyRoot;
        private Action<VisualElement, TItem, int> _bindItem;
        private Action<VisualElement, int> _unbindItem;
        private IToolkitListItemBinder<TItem> _itemBinder;

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

        public bool Bind(
            VisualElement root,
            string listViewName,
            IToolkitListItemBinder<TItem> itemBinder,
            string emptyRootName = null)
        {
            var listView = ToolkitElementUtility.QueryListView(root, listViewName);
            var emptyRoot = ToolkitElementUtility.Query<VisualElement>(root, emptyRootName);
            Bind(listView, itemBinder, emptyRoot);
            return listView != null;
        }

        public void Bind(
            ListView listView,
            Func<VisualElement> makeItem,
            Action<VisualElement, TItem, int> bindItem,
            VisualElement emptyRoot = null,
            Action<VisualElement, int> unbindItem = null)
        {
            DisposeListViewBinding();
            _listView = listView;
            _emptyRoot = emptyRoot;
            _bindItem = bindItem;
            _unbindItem = unbindItem;

            if (_listView == null)
                return;

            _listView.itemsSource = _items;
            _listView.makeItem = makeItem ?? MakeDefaultItem;
            _listView.bindItem = BindListItem;
            _listView.unbindItem = UnbindListItem;
            Refresh();
        }

        public void Bind(ListView listView, IToolkitListItemBinder<TItem> itemBinder, VisualElement emptyRoot = null)
        {
            DisposeListViewBinding();
            _listView = listView;
            _emptyRoot = emptyRoot;
            _itemBinder = itemBinder;

            if (_listView == null)
                return;

            _listView.itemsSource = _items;
            _listView.makeItem = itemBinder != null ? itemBinder.CreateElement : MakeDefaultItem;
            _listView.bindItem = BindListItem;
            _listView.unbindItem = UnbindListItem;
            Refresh();
        }

        public void SetItems(IEnumerable<TItem> items)
        {
            ReplaceAll(items);
        }

        public void ReplaceAll(IEnumerable<TItem> items)
        {
            _items.Clear();
            if (items != null)
            {
                foreach (var item in items)
                    _items.Add(item);
            }

            Refresh();
        }

        public void Append(TItem item)
        {
            _items.Add(item);
            RefreshItems();
        }

        public void Insert(int index, TItem item)
        {
            index = Math.Max(0, Math.Min(index, _items.Count));
            _items.Insert(index, item);
            RefreshItems();
        }

        public bool UpdateAt(int index, TItem item)
        {
            if (index < 0 || index >= _items.Count)
                return false;

            _items[index] = item;
            RefreshItems();
            return true;
        }

        public bool RemoveAt(int index)
        {
            if (index < 0 || index >= _items.Count)
                return false;

            _items.RemoveAt(index);
            RefreshItems();
            return true;
        }

        public void Clear()
        {
            _items.Clear();
            Refresh();
        }

        public void Refresh()
        {
            ApplyEmptyState();
            if (_listView == null)
                return;

            _listView.itemsSource = _items;
            _listView.Rebuild();
        }

        public void RefreshItems()
        {
            ApplyEmptyState();
            if (_listView == null)
                return;

            _listView.itemsSource = _items;
            _listView.RefreshItems();
        }

        public void Dispose()
        {
            DisposeListViewBinding();
            _items.Clear();
            _emptyRoot = null;
            _bindItem = null;
            _unbindItem = null;
            _itemBinder = null;
        }

        private void DisposeListViewBinding()
        {
            if (_listView != null)
            {
                _listView.itemsSource = null;
                _listView.makeItem = null;
                _listView.bindItem = null;
                _listView.unbindItem = null;
            }

            _listView = null;
        }

        private void ApplyEmptyState()
        {
            ToolkitElementUtility.ApplyEmptyState(_listView, _emptyRoot, _items.Count == 0);
        }

        private void BindListItem(VisualElement element, int index)
        {
            if (element == null || index < 0 || index >= _items.Count)
                return;

            if (_itemBinder != null)
                _itemBinder.BindItem(element, _items[index], index);
            else
                _bindItem?.Invoke(element, _items[index], index);
        }

        private void UnbindListItem(VisualElement element, int index)
        {
            if (element == null)
                return;

            if (_itemBinder != null)
                _itemBinder.UnbindItem(element, index);
            else
                _unbindItem?.Invoke(element, index);
        }

        private static VisualElement MakeDefaultItem()
        {
            return new Label();
        }
    }

    /// <summary>
    /// 文档中的 ToolkitListBinding 命名别名，方便后续业务 Binding 使用统一术语。
    /// </summary>
    public sealed class ToolkitListBinding<TItem> : IDisposable
    {
        private readonly ToolkitListViewBinder<TItem> _binder = new ToolkitListViewBinder<TItem>();

        public IReadOnlyList<TItem> Items => _binder.Items;
        public bool HasListView => _binder.HasListView;

        public bool Bind(VisualElement root, string listViewName, IToolkitListItemBinder<TItem> itemBinder, string emptyRootName = null)
        {
            return _binder.Bind(root, listViewName, itemBinder, emptyRootName);
        }

        public void ReplaceAll(IEnumerable<TItem> items)
        {
            _binder.ReplaceAll(items);
        }

        public void Append(TItem item)
        {
            _binder.Append(item);
        }

        public void Clear()
        {
            _binder.Clear();
        }

        public void Dispose()
        {
            _binder.Dispose();
        }
    }
}
