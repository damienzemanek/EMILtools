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
    
    public interface IPersistenAction { }

    
    [Serializable]
    public sealed class PersistentAction<T, T2> : IPersistenAction
    {
        Action<T, T2> _action = delegate { };
        
        public void Invoke(T val1, T2 val2) => _action.Invoke(val1, val2);
        public PersistentAction<T, T2> Add(Action<T, T2> cb) { _action += cb; return this; }
        public PersistentAction<T, T2> Remove(Action<T, T2> cb) { _action -= cb; return this; }
        
        public int Count => _action.GetInvocationList().Length;
        
        public void PrintInvokeListNames() => Debug.Log("Invoking PersistentAction with " + Count + " subscribers: " + string.Join(", NAME >>>>>>>>>>>>>> ", _action.GetInvocationList().Select(d => d.Method.Name)));

    }
    
    [Serializable]
    public sealed class PersistentAction<T> : IPersistenAction
    {
        Action<T> _action = delegate { };
        
        public void Invoke(T value) => _action.Invoke(value);
        public PersistentAction<T> Add(Action<T> cb) { _action += cb; return this; }
        public PersistentAction<T> Remove(Action<T> cb) { _action -= cb; return this; }
        
        public int Count => _action.GetInvocationList().Length;
        public void PrintInvokeListNames() => Debug.Log("Invoking PersistentAction with " + Count + " subscribers: " + string.Join(", NAME >>>>>>>>>>>>>> ", _action.GetInvocationList().Select(d => d.Method.Name)));

    }

    /// <summary>
    /// Non-generic version for simple triggers
    /// </summary>
    [Serializable]
    public sealed class PersistentAction : IPersistenAction
    {
        Action _action = delegate { };

        public void Invoke() => _action.Invoke();
        public PersistentAction Add(Action cb) { _action += cb; return this; }

        public PersistentAction Remove(Action cb) { _action -= cb; return this; }
        
        public int Count => _action.GetInvocationList().Length;
        public void PrintInvokeListNames() => Debug.Log("Invoking PersistentAction with " + Count + " subscribers: " + string.Join(", NAME >>>>>>>>>>>>>> ", _action.GetInvocationList().Select(d => d.Method.Name)));

        
        public void Add(Action[] cbs)
            { foreach (var cb in cbs) Add(cb); }
        
        public void Remove(Action[] cbs)
            { foreach (var cb in cbs) Remove(cb); }
    }
}