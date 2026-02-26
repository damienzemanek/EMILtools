using System;
using System.Linq;
using UnityEngine;

namespace EMILtools.Core
{
    public interface IPersistentAction<in TDelegate, out TPersistentCRTP> : IPersistentDelegate
        where TDelegate : Delegate
        where TPersistentCRTP : IPersistentAction<TDelegate, TPersistentCRTP>
    {
        TPersistentCRTP Add(TDelegate cb);
        TPersistentCRTP Remove(TDelegate cb);
        int Count { get; }
        void PrintInvokeListNames();
    }

    
    [Serializable]
    public sealed class PersistentAction<T, T2> : 
        IPersistentAction<Action<T,T2>, PersistentAction<T, T2>>,
        IPersistentAction<Action<T,T2>>
    {
        [NonSerialized] Action<T, T2> _action = delegate { };
        public void Invoke(T val1, T2 val2) => _action.Invoke(val1, val2);
        
        // CRTP
        public PersistentAction<T, T2> Add(Action<T, T2> cb) { _action += cb; return this; }
        public PersistentAction<T, T2> Remove(Action<T, T2> cb) { _action -= cb; return this; }
        
        // Non CRTP - Allows generic systems to store and cast the action w/out needing to know the type
        void IPersistentAction<Action<T, T2>>.Add(Action<T, T2> cb) => Add(cb);
        void IPersistentAction<Action<T, T2>>.Remove(Action<T, T2> cb) => Remove(cb);
        
        public int Count => _action.GetInvocationList().Length;

        public void PrintInvokeListNames()
        {
            var names = _action.GetInvocationList()
                .Select(d => d.Method.Name);
            Debug.Log($"PersistentAction has ({Count}) Subs, SUBS: [ {string.Join(" ], [ ", names)} ]");
        }

        public void API_Add(Delegate cb) => Add((Action<T, T2>)cb);
        public void API_Remove(Delegate cb) => Remove((Action<T, T2>)cb);
    }
    
    [Serializable]
    public sealed class PersistentAction<T> : 
            IPersistentAction<Action<T>, PersistentAction<T>>, 
            IPersistentAction<Action<T>>
    {
        [NonSerialized] Action<T> _action = delegate { };
        // CRTP
        public PersistentAction<T> Add(Action<T> cb) { _action += cb; return this; }
        public PersistentAction<T> Remove(Action<T> cb) { _action -= cb; return this; }
        
        // Non CRTP - Allows generic systems to store and cast the action w/out needing to know the type
        void IPersistentAction<Action<T>>.Add(Action<T> cb) => Add(cb);
        void IPersistentAction<Action<T>>.Remove(Action<T> cb) => Remove(cb);
        
        public void Invoke(T value) => _action.Invoke(value);
        public int Count => _action.GetInvocationList().Length;
        public void PrintInvokeListNames()
        {
            var names = _action.GetInvocationList()
                .Select(d => d.Method.Name);
            Debug.Log($"PersistentAction has ({Count}) Subs, SUBS: [ {string.Join(" ], [ ", names)} ]");
        }

        public void API_Add(Delegate cb) => Add((Action<T>)cb);

        public void API_Remove(Delegate cb) => Remove((Action<T>)cb);
    }

    /// <summary>
    /// Non-generic version for simple triggers
    /// </summary>
    [Serializable]
    public sealed class PersistentAction : 
        IPersistentAction<Action, PersistentAction>,
        IPersistentAction<Action>
    {
        [NonSerialized] Action _action = delegate { };
        public void Invoke() => _action.Invoke();
        
        // CRTP
        public PersistentAction Add(Action cb) { _action += cb; return this; }
        public PersistentAction Remove(Action cb) { _action -= cb; return this; }
        
        // Non CRTP - Allows generic systems to store and cast the action w/out needing to know the type
        void IPersistentAction<Action>.Add(Action cb) => Add(cb);
        void IPersistentAction<Action>.Remove(Action cb) => Remove(cb);
        public int Count => _action.GetInvocationList().Length;
        public void PrintInvokeListNames()
        {
            var names = _action.GetInvocationList()
                .Select(d => d.Method.Name);
            Debug.Log($"PersistentAction has ({Count}) Subs, SUBS: [ {string.Join(" ], [ ", names)} ]");
        }        
        public void Add(Action[] cbs)
            { foreach (var cb in cbs) Add(cb); }
        
        public void Remove(Action[] cbs)
            { foreach (var cb in cbs) Remove(cb); }

        public void API_Add(Delegate cb) => Add((Action)cb);

        public void API_Remove(Delegate cb) => Remove((Action)cb);
    }
}