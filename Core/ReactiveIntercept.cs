using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;


namespace EMILtools.Core
{
    public struct ReactiveInterceptCore<T>
    {
        [SerializeField, HideLabel] internal T _value;
        public PersistentFunc<T, T> Intercepts;
        public PersistentAction<T> Reactions;
        public T Value
        { 
            get => _value;
            set
            {
                T processed = (Intercepts != null) ? Intercepts.ApplySequentially(value) : value;
                if(EqualityComparer<T>.Default.Equals(_value, processed)) return;
                _value = processed;
                Reactions?.Invoke(_value);
            }
        }

        public ReactiveInterceptCore(T initial)
        {
            _value = initial;
            Intercepts = null;
            Reactions = null;
        }
    }

    public interface IReactiveIntercept<T>
    {
        T Value { get; set; }
    }
    
    [Serializable]
    [InlineProperty]
    public struct ReactiveInterceptVT<T> : IReactiveIntercept<T>
        where T : struct
    {
        ReactiveInterceptCore<T> core;

        public T Value
        {
            get => core.Value;
            set => core.Value = value;
        }
        
        public ReactiveInterceptVT(T initial) => core = new ReactiveInterceptCore<T>(initial);

        // ----------------------------------------------------------------------------------
        //                              No Operator Overrides
        // ----------------------------------------------------------------------------------
    }
    
    [Serializable]
    [InlineProperty]
    public struct ReactiveInterceptRT<T> : IReactiveIntercept<T>
        where T : class
    {
        internal ReactiveInterceptCore<T> core;
        public T Value
        {
            get => core.Value;
            set => core.Value = value;
        }
        public ReactiveInterceptRT(T initial) => core = new ReactiveInterceptCore<T>(initial);

        // ----------------------------------------------------------------------------------
        //                              Operator Overrides
        //                      Func<T,T>: FUNCS += _ => Method();
        //                      Action<T>: ACTION += _ => { Method(); };
        // ----------------------------------------------------------------------------------
        public static ReactiveInterceptRT<T> operator +(ReactiveInterceptRT<T> ri, Func<T,T> cb)
        {
            if(ri.core.Intercepts == null) ri.core.Intercepts = new PersistentFunc<T, T>();
            ri.core.Intercepts.Add(cb);
            return ri;
        }
        
        public static ReactiveInterceptRT<T> operator -(ReactiveInterceptRT<T> ri, Func<T, T> cb)
        {
            ri.core.Intercepts?.Remove(cb);
            return ri;
        }
        public static ReactiveInterceptRT<T> operator +(ReactiveInterceptRT<T> ri, Action<T> cb)
        {
            if (ri.core.Reactions == null) ri.core.Reactions = new PersistentAction<T>();
            ri.core.Reactions.Add(cb);
            return ri;
        }

        public static ReactiveInterceptRT<T> operator -(ReactiveInterceptRT<T> ri, Action<T> cb)
        {
            ri.core.Reactions?.Remove(cb);
            return ri;
        }
    }
}
