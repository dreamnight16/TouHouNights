using System;
using System.Collections.Generic;

namespace TowerDefense.Util
{
    /// <summary>
    /// 懒删除二叉最小堆（参考算法笔记「懒删除堆 Lazy Deletion Heap」）。
    /// 按 <paramref name="keySelector"/> 维护最小元素；Pop 时跳过已死（IsAlive=false）的过期项。
    /// 用于「血量最低 / 距终点最近」等全局索敌策略：堆中元素按全局 key 排序，
    /// 塔取目标时「弹出出界项、命中即停、再把弹出的项回插」，保证堆状态不变。
    /// 复杂度：建堆 Heapify O(n)，Push/Pop O(log n)。
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
