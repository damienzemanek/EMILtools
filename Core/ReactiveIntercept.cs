using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;


namespace EMILtools.Core
{
    [Serializable]
    [InlineProperty]
    public struct ReactiveIntercept<T>
        where T : struct
    {
        [SerializeField, HideLabel] public T baseValue;
        public T Value
        { 
            get => baseValue;
            set
            {
                T processed = (Intercepts != null) ? Intercepts.ApplySequentially(value) : value;
                if(EqualityComparer<T>.Default.Equals(baseValue, processed)) return;
                Reactions?.Invoke(baseValue);
            }
        }
        [HideInInspector] public PersistentFunc<T, T> Intercepts;
        [HideInInspector] public PersistentAction<T> Reactions;
        
        public ReactiveIntercept(T initial)
        {
            baseValue = initial;
            Intercepts = null;
            Reactions = null;
        }

        // ----------------------------------------------------------------------------------
        //                              Operator Overrides
        //                      Func<T,T>: FUNCS += _ => Method();
        //                      Action<T>: ACTION += _ => { Method(); };
        // ----------------------------------------------------------------------------------
        public static ReactiveIntercept<T> operator +(ReactiveIntercept<T> ri, Func<T,T> cb)
        {
            if(ri.Intercepts == null) ri.Intercepts = new PersistentFunc<T, T>();
            ri.Intercepts.Add(cb);
            return ri;
        }
        
        public static ReactiveIntercept<T> operator -(ReactiveIntercept<T> ri, Func<T, T> cb)
        {
            ri.Intercepts?.Remove(cb);
            return ri;
        }
        public static ReactiveIntercept<T> operator +(ReactiveIntercept<T> ri, Action<T> cb)
        {
            if (ri.Reactions == null) ri.Reactions = new PersistentAction<T>();
            ri.Reactions.Add(cb);
            return ri;
        }

        public static ReactiveIntercept<T> operator -(ReactiveIntercept<T> ri, Action<T> cb)
        {
            ri.Reactions?.Remove(cb);
            return ri;
        }
    }
}
