using System;
using System.Collections.Generic;

namespace TowerDefense.Util
{
    /// <summary>
    /// 按 keySelector 排序的二叉最小堆：Rebuild 为 O(n)，Push/Pop 为 O(log n)。
    /// 过期项的判断与懒删除由调用方负责。
    /// </summary>
    public sealed class MinHeap<T> where T : class
    {
        private readonly List<T> _items = new List<T>();
        private readonly Func<T, float> _keySelector;

        public MinHeap(Func<T, float> keySelector)
        {
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
        }

        public int Count => _items.Count;

        public void Clear()
        {
            _items.Clear();
        }

        /// <summary>清空后把 items 全部入堆并 Heapify（O(n)）。</summary>
        public void Rebuild(IEnumerable<T> items)
        {
            _items.Clear();
            foreach (var item in items)
            {
                _items.Add(item);
            }

            for (int i = _items.Count / 2 - 1; i >= 0; i--)
            {
                SiftDown(i);
            }
        }

        public void Push(T item)
        {
            _items.Add(item);
            SiftUp(_items.Count - 1);
        }

        public T Peek()
        {
            return _items.Count > 0 ? _items[0] : null;
        }

        public T Pop()
        {
            if (_items.Count == 0) return null;

            var root = _items[0];
            int last = _items.Count - 1;
            _items[0] = _items[last];
            _items.RemoveAt(last);

            if (_items.Count > 0)
            {
                SiftDown(0);
            }

            return root;
        }

        private void SiftUp(int index)
        {
            var item = _items[index];
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (_keySelector(item) >= _keySelector(_items[parent])) break;
                _items[index] = _items[parent];
                index = parent;
            }
            _items[index] = item;
        }

        private void SiftDown(int index)
        {
            var item = _items[index];
            int n = _items.Count;

            while (true)
            {
                int left = index * 2 + 1;
                if (left >= n) break;

                int right = left + 1;
                int child = (right < n && _keySelector(_items[right]) < _keySelector(_items[left])) ? right : left;

                if (_keySelector(_items[child]) >= _keySelector(item)) break;

                _items[index] = _items[child];
                index = child;
            }

            _items[index] = item;
        }
    }
}
