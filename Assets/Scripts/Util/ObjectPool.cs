using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Util
{
    /// <summary>
    /// 极简通用对象池：避免敌人/子弹频繁 Instantiate/Destroy 造成 GC 抖动。
    /// 工厂负责「创建一个全新对象」，池负责复用。
    /// </summary>
    public sealed class ObjectPool<T> where T : Component
    {
        private readonly Stack<T> _available = new Stack<T>();
        private readonly Func<T> _factory;

        public ObjectPool(Func<T> factory, int prewarmCount = 0)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            for (int i = 0; i < prewarmCount; i++)
            {
                var obj = _factory();
                obj.gameObject.SetActive(false);
                _available.Push(obj);
            }
        }

        public T Get()
        {
            T obj = _available.Count > 0 ? _available.Pop() : _factory();
            obj.gameObject.SetActive(true);
            return obj;
        }

        public void Release(T obj)
        {
            if (obj == null) return;
            obj.gameObject.SetActive(false);
            _available.Push(obj);
        }
    }
}
