using System;
using System.Collections.Generic;

namespace NeonHorde
{
    /// <summary>
    /// Minimal allocation-free object pool. Used for enemies, projectiles, gems,
    /// damage numbers and particles so a run converges to zero GC allocation.
    /// </summary>
    public sealed class ObjectPool<T> where T : class
    {
        readonly Stack<T> _stack = new();
        readonly Func<T> _factory;
        readonly Action<T> _onGet;
        readonly Action<T> _onRelease;

        public ObjectPool(Func<T> factory, Action<T> onGet = null, Action<T> onRelease = null, int prewarm = 0)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _onGet = onGet;
            _onRelease = onRelease;
            for (int i = 0; i < prewarm; i++) _stack.Push(_factory());
        }

        public int CountInactive => _stack.Count;

        public T Get()
        {
            T item = _stack.Count > 0 ? _stack.Pop() : _factory();
            _onGet?.Invoke(item);
            return item;
        }

        public void Release(T item)
        {
            _onRelease?.Invoke(item);
            _stack.Push(item);
        }
    }
}
