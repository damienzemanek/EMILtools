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
    public sealed class PersistentFunc<T>
    {
        Func<T> _func;
        public T Invoke() => (_func != null) ? _func() : default;
        public void Nullify() => _func = null;
        public PersistentFunc<T> Set(Func<T> cb) { _func = cb; return this; }
        public PersistentFunc<T> Add(Func<T> cb) {_func += cb; return this; }
        public PersistentFunc<T> Remove(Func<T> cb) {_func -= cb; return this; }
    }

    /// <summary>
    /// A stable container for Func<T, T> delegates.
    /// Used for Intercepts to sequentially transform a value.
    /// </summary>
    public sealed class PersistentFunc<T, TResult>
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
    
        public PersistentFunc<T, TResult> Add(Func<T, T> cb) { _funcs.Add(cb); return this; }
        public PersistentFunc<T, TResult> Remove(Func<T, T> cb)  { _funcs.Remove(cb); return this; }
        public PersistentFunc<T, TResult> Clear()  { _funcs.Clear(); return this; }
        public PersistentFunc<T, TResult> Add(params Func<T, T>[] cbs) {
            foreach (var cb in cbs) Add(cb); return this; }
        public PersistentFunc<T, TResult> Add(List<Func<T, T>> cbs) {
            foreach (var cb in cbs) Add(cb); return this; }
    }
}

