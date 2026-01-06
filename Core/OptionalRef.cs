using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using static EMILtools.Core.Box;

namespace EMILtools.Core
{
    /// <summary>
    /// Value-types that can optionally and dynamically be promoted to reference types for stable configuration.
    ///
    /// Advantages:
    /// - Reads are as fast as standard value-types using <see cref="stack"/>
    /// - Writes are as fast as reference-types using <see cref="stack"/>> after OptionalRef<T>.FastHeap()>
    /// 
    ///
    /// Use cases:
    /// - Systems using value-types that require dynamic runtime modification.
    ///     + Ideal for editing structs or value-types in the Editor.
    /// - Systems that need a reference-type for frequent access in hot loops.
    ///     + Stack-local access outperforms pointer de-references for frequently accessed variables.
    /// - Selective reference-type sharing:
    ///     + Avoid unnecessary boxing.
    ///     + Pseudo-boxes promote T to Ref<T> once at stabilization instead of every pass-through.
    ///
    /// ------------------------------------- HOT LOOP TIMINGS -------------------------------------
    ///
    /// 50,000,000 (50 mil) iterations, single value reads/writes
    ///
    /// READ (ms):
    ///----------------------------------------------------------
    /// Value-Type *                                   ~ 37  ms *   
    /// Nested Value-Type *                            ~ 37  ms *   
    /// OptionalRef (Nested) - Value                   ~ 120 ms    
    /// OptionalRef (Nested) - Stack *                 ~ 37  ms *   
    /// OptionalRef (Nested) - Heap                    ~ 110 ms    
    /// OptionalRef - Value                            ~ 120 ms    
    /// OptionalRef - Stack *                          ~ 37  ms *   
    /// OptionalRef - Heap (heapReadOnly)              ~ 98  ms 
    /// OptionalRef - After FastHeap (via stack) *     ~ 37  ms *   Read with Value-type speed
    /// Reference Type                                 ~ 43  ms  
    ///
    /// WRITE (ms):
    /// --------------------------------------------------------
    /// Value-Type                                     ~ 108 ms    
    /// Nested Value-Type                              ~ 108 ms   
    /// OptionalRef (Nested) - Value                   ~ 500 ms   
    /// OptionalRef (Nested) - Stack                   ~ 90  ms  
    /// OptionalRef (Nested) - Heap *                  ~ 108 ms *   
    /// OptionalRef - Value                              n/a    
    /// OptionalRef - Stack                            ~ 212 ms   
    /// OptionalRef - Heap *                           ~ 108 ms *  Write with reference-type speed
    /// Reference Type *                               ~ 108 ms *
    /// 
    /// --------------------------------------------------------------------------------------------
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public struct OptionalRef<T> : IBoxable where T : struct
    {
        public OptionalRef(T initial)
        {
            _stack = initial;
            isRef = false;
            sharedheap = null;
        }
        
        [SerializeField] T _stack;
        [SerializeField] public bool isRef;
        [NonSerialized] public RefBox<T> sharedheap;
        public RefBox<T> reference => isRef ? sharedheap : 
            throw new InvalidOperationException("Variable is not boxed");
        
        /// <summary>
        /// If box status is UNKNOWN during use, use Value
        /// </summary>
        public T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (isRef) 
                ? _stack = sharedheap.unbox 
                : _stack;
        }

        /// <summary>
        /// If the variable is known to be local and not boxed, use stack
        /// </summary>
        public T stack
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _stack;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (isRef) throw new InvalidOperationException(
                    "Trying to access stack on a boxed variable, Use .heap or .GetStable");
                _stack = value;
            }
        }

        /// <summary>
        /// If the variable is known to be boxed, use .heap
        /// </summary>
        public ref T heap
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (isRef) return ref sharedheap.unbox;
                throw new InvalidOperationException(
                    "Trying to set heap when the variable isn't boxed, Use .stack");
            }
        }
        
        public readonly T heapReadOnly => isRef ? sharedheap.unbox :
                throw new InvalidOperationException("Variable is not boxed");
        

        public void Box()
        {
            isRef = true;
            if (sharedheap == null) sharedheap = new RefBox<T>(_stack);
        }

        public void PullHeap()
        {
            if(!isRef) throw new InvalidOperationException("Variable is not boxed");
            _stack = sharedheap.unbox;
        }

        public static implicit operator T(OptionalRef<T> c) => c.Value;
        public static implicit operator OptionalRef<T>(T v) => new OptionalRef<T> { _stack = v };
    }
}

