using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace EMILtools.Core
{
    /// <summary>
    /// Value-types that can optionally and dynamically be promoted to reference types for stable configuration.
    /// 
    /// Directions:
    ///  - Use GetStack when the stability is known to be false; direct stack access is fast.
    ///  - Use heap (RefBox) when stabilized; gives reference semantics safely.
    ///  - Implicit conversion operator allows seamless use of Stablizable<T> as T.
    ///
    /// Useful when:
    /// - A system using value-types needs to be dynamically modified at runtime by another system.
    ///       + Need to edit your structs or value-types in the Editor? Use Stabilizable.
    /// - A system requires a reference-type, and that reference-type is frequently accessed in hot loops.
    ///       + 1 pointer de-ref ≈ 4 stack-local accesses.
    ///       + If you don't access this value often, the difference is negligible.
    ///       + If you access this variable a lot, stack-local access will outperform pointer de-ref by magnitudes.
    /// - You want selective reference-type sharing, meaning not all systems accessing this variable
    ///   necessarily need a reference type.
    ///       + Avoids unnecessary boxing. If the variable was a value-type, it would always be boxed when sent.
    ///       + "Pseudo-boxes" promote the T value to a Ref<T>, which is lighter than a boxed T.
    ///       + "Pseudo-boxes" only occur ONCE at Stabilization, rather than on every pass-through for value types.
    ///
    /// ------------------------------------- HOT LOOP TIMINGS -------------------------------------
    ///
    /// 50,000,000 (50 mil) iterations, single value reads/writes
    /// 
    ///  READ (ms):
    ///    Value-Type:...................... 25-37
    ///    Nested Value-Type:............... 19-24
    ///    Non-Stabilized - Get:............ 48-338
    ///    Non-Stabilized - GetStack:....... 40-407
    ///    Stabilized (RefBox<ValueType>):.. 12
    ///    Stabilized Implicit Conversion:.. 27-36
    ///    Ref<T> class:.................... 12-42
    /// 
    ///  WRITE (ms):
    ///    Value-Type (direct field):....... 28-108
    ///    Nested Value-Type:............... 24-108
    ///    Non-Stabilized - Get:............ 48-642
    ///    Non-Stabilized - GetStack:....... 40-407
    ///    Stabilized (RefBox<ValueType>):.. 12-108
    ///    Ref<T> class:.................... 12-108
    /// 
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

