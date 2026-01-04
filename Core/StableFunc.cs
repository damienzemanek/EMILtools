using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EMILtools.Core
{
    /// <summary>
    /// A stable container for a Func<T> delegate.
    /// Provides reference stability for value providers.
    ///
    /// Since these are immutable when passing around you would have to find the origin location, but this stable ref
    /// stores the location and auto assigns it.
    /// 
    /// </summary>
    public sealed class StableFunc<T>
    {
        Func<T> _func;
        public T Invoke() => (_func != null) ? _func() : default;
        public void Set(Func<T> cb) => _func = cb;
        public void Nullify() => _func = null;
        public void Add(Func<T> cb) => _func += cb;
        public void Remove(Func<T> cb) => _func -= cb;
    }

    /// <summary>
    /// A stable container for Func<T, T> delegates.
    /// Used for Intercepts to sequentially transform a value.
    /// </summary>
    public sealed class StableFuncList<T, TResult>
        where TResult : T
    {
        readonly List<Func<T, T>> _funcs = new();

        public T ApplySequentially(T val)
        {
            if(_funcs == null || _funcs.Count == 0) return val;
            T processed = val;
            for (int i = 0; i < _funcs.Count; i++)
                processed = _funcs[i](processed);
            return processed;
        }
    
        public void Add(Func<T, T> cb) => _funcs.Add(cb);
        public void Remove(Func<T, T> cb) => _funcs.Remove(cb);
        public void Clear() => _funcs.Clear();

        public void Add(params Func<T, T>[] cbs) {
            foreach (var cb in cbs) Add(cb); }
        public void Add(List<Func<T, T>> cbs) {
            foreach (var cb in cbs) Add(cb); }
    }
}

