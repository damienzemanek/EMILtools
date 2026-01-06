using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace EMILtools.Core
{
    /// <summary>
    /// Value-types that can optionally and dynamically be promoted to reference types for stable configuration.
    ///
    /// Advantages:
    /// - Reads are as fast as standard value-types:
    ///     + Before stabilization: use <see cref="GetStack"/>
    ///     + After stabilization: use implicit conversion or <see cref="GetStable"/>
    /// - Writes are as fast as reference-types after stabilization using a stabilized RefBox.
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
    ///                                 
    /// READ (ms):
    ///   Value-Type:........................ ~40
    ///   Nested Value-Type:................. ~20
    ///   Non-Stabilized - Get:.............. ~270
    ///   Non-Stabilized - GetStack:......... ~40   *** Recommended
    ///   Stabilized (RefBox<ValueType>):.... ~200
    ///   Stabilized Implicit Conversion:.... ~40   *** Recommended
    ///   Ref<T> class:...................... ~40
    ///
    /// WRITE (ms):
    ///   Value-Type (direct field):.......... ~108  (pass by value only)
    ///   Nested Value-Type:.................. ~108  (pass by value only)
    ///   Non-Stabilized - Get:................ ~650 (pass by value first, can be ref)
    ///   Non-Stabilized - GetStack:........... ~400 (pass by value first, can be ref)
    ///   Stabilized (RefBox<ValueType>):..... ~108  (pass by reference)
    ///   Ref<T> class:....................... ~108  (pass by ref only)
    ///
    /// Recommended usage:
    ///   - Use GetStack for non-stabilized hot loops.
    ///   - Use implicit conversion or GetStable for stabilized values.
    /// --------------------------------------------------------------------------------------------
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public struct Stablizable<T> : IStablizable where T : struct
    {
        public Stablizable(T initial)
        {
            _stack = initial;
            myUser = null;
            isStablecached = false;
            isNotStablecached = false;
            isUserNotStabilizedcached = false;
            sharedheap = null;
        }


        [SerializeField] T _stack;
        IStablizableUser myUser;
        [SerializeField] bool isNotStablecached;
        [SerializeField] bool isStablecached;
        [SerializeField] bool isUserNotStabilizedcached;
        [NonSerialized] public RefBox<T> sharedheap;
        public RefBox<T> inspectHeap => sharedheap;

        public bool isStable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get =>
                isStablecached ||
                (isStablecached = !(isNotStablecached = !(myUser != null && sharedheap != null)));
        }

        /// <summary>
        /// If the stability is known to be NOT stable during use. use GetStack
        /// to quickly retrive the value
        ///
        /// IS AS FAST as value-type access on the stack
        /// </summary>
        public T GetStack => _stack;
       

        /// <summary>
        /// If stability is known to be STABLE during use, use GetStable
        /// IS FASTER than pointer de-ref
        /// </summary>
        public T GetStable => sharedheap.unbox;

        /// <summary>
        /// If stability is UNKNOWN during use, use Get
        /// </summary>
        public T Get
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (isStablecached || isStable) ? _stack = sharedheap.unbox : _stack;
        }

        public T stack
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (!isUserNotStabilizedcached) ThrowIfUserWasStabilized();
                _stack = value;
            }
        }

        public ref T heap
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (isStablecached || isStable) return ref sharedheap.unbox;
                throw new InvalidOperationException(
                    "Trying to access heap when the variable isn't stabilized. Use SetLocal.");
            }
        }

        /// <summary>
        /// If using this in implicit conversion and the converted type is simple. is as fast as calling GetStack
        ///
        /// If not, just use GetStack
        /// </summary>
        public RefBox<T> param
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => sharedheap;
        }
        

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ThrowIfUserWasStabilized()
        {
            if (myUser != null && Stabilizer.stabilizedUsers.Contains(myUser))
                throw new Exception(
                    "Using a Stablizable outside of its owning user is forbidden. " +
                    "Do not copy the struct; pass its reference around."
                );
            else isUserNotStabilizedcached = true;
        }

        public void Stabilize(IStablizableUser user, bool fromStabilizer = false)
        {
            myUser = user;
            if (sharedheap == null) sharedheap = new RefBox<T>(_stack);
            if (!fromStabilizer && !Stabilizer.stabilizedUsers.Contains(myUser))
                Stabilizer.stabilizedUsers.Add(myUser);
        }

        public static implicit operator T(Stablizable<T> c) => c.Get;
        public static implicit operator Stablizable<T>(T v) => new Stablizable<T> { _stack = v };
    }
}

