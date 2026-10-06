using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Util
{
    /// <summary>
    /// 使用工厂创建组件，释放时停用并入池，获取时复用并启用。
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
