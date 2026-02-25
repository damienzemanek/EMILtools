using System;
using System.Linq;
using EMILtools.Timers;
using UnityEngine;

namespace EMILtools.Core
{
    /// <summary>
    /// A persistent container for multicast delegates that ensures reference stability across the utilizer's lifecycle.
    /// </summary>
    /// <remarks>
    /// Purpose:
    /// - Reference Stability: Standard C# events/actions are immutable; every subscription (+=) re-assigns the reference. 
    ///   This class provides a constant heap-allocated container so that internal tracking systems  
    ///   can maintain a permanent link to the event without needing to re-bind every time the underlying delegate is modified.
    /// - Lifecycle Decoupling: Allows users to manage subscriptions safely without knowing the internal state of the instance.
    /// - Null-Safety: Encapsulates the invocation logic with a default empty delegate to prevent NullReferenceExceptions during high-frequency ticks.
    /// 
    /// Use Cases:
    /// - Centralized Cleanup: Essential for <see cref="TimerUtility.ShutdownTimers"/>; it allows the utility to iterate through a list 
    ///   of Hooks and call Unsubscribe on a stable target, even if other objects have subscribed/unsubscribed in the meantime.
    /// - Fluent API Support: Enables the <c>.Sub().Sub()</c> chaining pattern by providing a consistent object to return and operate upon.
    /// </remarks>
    ///
    /// 

    public interface IPersistentDelegate
    {
        void API_Add(Delegate cb);
        void API_Remove(Delegate cb);
    }
    
    public interface IPersistentAction<in TDelegate> : IPersistentDelegate
        where TDelegate : Delegate
    {
        void Add(TDelegate cb);
        void Remove(TDelegate cb);
        int Count { get; }
        void PrintInvokeListNames();
    }
    
     [Serializable]
    public sealed class PersistentActionNonCRTP<T, T2> : IPersistentAction<Action<T,T2>>
    {
        [NonSerialized] Action<T, T2> _action = delegate { };
        public void Invoke(T val1, T2 val2) => _action.Invoke(val1, val2);
        public void Add(Action<T, T2> cb) => _action += cb; 
        public void Remove(Action<T, T2> cb) => _action -= cb; 
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
    public sealed class PersistentActionNonCRTP<T> : IPersistentAction<Action<T>>
    {
        [NonSerialized] Action<T> _action = delegate { };
        public void Add(Action<T> cb)  => _action += cb;
        public void Remove(Action<T> cb) => _action -= cb;
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
    public sealed class PersistentActionNonCRTP : IPersistentAction<Action>  
    {
        [NonSerialized] Action _action = delegate { };
        public void Invoke() => _action.Invoke();
        public void Add(Action cb) => _action += cb; 
        public void Remove(Action cb) => _action -= cb; 
        
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