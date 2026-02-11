using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;



namespace EMILtools.Core
{


    /// <summary>
    /// Reactions and Intercepts are Lazy Initialized
    /// Food for thought: Don't make ALL of your Value-Types Reactive intercepts
    ///                    Only the ones that actually need the behavior
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    [InlineProperty]
    public class ReactiveIntercept<T>
    {
        static readonly EqualityComparer<T> Comparer = EqualityComparer<T>.Default;


        [SerializeField, HideLabel] T _value;
        
        [NonSerialized] PersistentAction<T> _Reactions;
        [NonSerialized] PersistentFunc<T, T> _Intercepts;

        public PersistentFunc<T, T> Intercepts
        {
            get
            {
                if(_Intercepts == null) _Intercepts = new PersistentFunc<T, T>();
                return _Intercepts;
            }
            set => _Intercepts = value;
        }
        public PersistentAction<T> Reactions 
        {
            get
            {
                if (_Reactions == null) _Reactions = new PersistentAction<T>();
                return _Reactions;
            }
            set => _Reactions = value;
        }
        public T Value
        { 
            get => _value;
            set
            {
                T processed = (_Intercepts != null && _Intercepts.Count > 0) ? _Intercepts.ApplySequentially(value) : value;
                if(Comparer.Equals(_value, processed)) return;
                _value = processed;
                _Reactions?.Invoke(_value);
            }
        }

        public ReactiveIntercept(T initial)
        {
            _value = initial;
            _Intercepts = null;
            _Reactions = null;
        }
        public ReactiveIntercept(T initial,
            PersistentAction<T> reaction = null,
            PersistentFunc<T, T> intercept = null)
        {
            _value = initial;
            _Intercepts = intercept;
            _Reactions = reaction;
        }
        
#if UNITY_EDITOR
        void OnValidate() => _Reactions?.Invoke(_value);
#endif

        public static implicit operator T(ReactiveIntercept<T> intercept) => intercept.Value;
    }
    
}
