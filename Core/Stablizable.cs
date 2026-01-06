using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace EMILtools.Core
{


    [AttributeUsage(AttributeTargets.Field)]
    public class StabilizeAttribute : Attribute
    {
    }

    // Implications / Runtime Behavior
    //
    // Non-stable copies:
    //
    //      Can always move around; value semantics preserved.
    //
    //      Copies do not become stable if the owner is stabilized afterward.
    //
    // Stable copies (or post-stabilization access):
    //
    //      Reference semantics ensured.
    //
    //     refval guarantees safe access; no null references.
    //
    // Edge case safety:
    //
    //      Pre-stabilization copies remain value copies — safe.
    //
    //      Post-stabilization copies automatically share reference.
    //
    // Mental model:
    //      Intuitive for the developer: "stabilize your user → everything becomes reference; copies made before stabilization remain free value copies."
    //     


    // Critical Use Cases for Stablizable
    //
    //     Local fast-path computations
    //
    //     You have a struct that represents some gameplay state — like JumpSettings, MovementConfig, or PlayerStats.
    //
    //     For most of its lifetime, the struct is copied around locally, modified, and used in calculations.
    //
    //      Benefit: Zero heap allocations, super cache-friendly, extremely fast.
    //
    //      Example: AI agent runs multiple movement simulations using copies of JumpSettings — no interference, no GC churn.
    //
    //     Dynamic “switch to shared ref” when needed
    //
    // At certain points, a copy of the struct needs to be shared with other systems — for example, a HUD, animation controller, or physics engine.
    //
    //     Instead of boxing, you dynamically stabilize the struct to convert it into a heap-backed reference.
    //
    //     Benefit: Developer-intended boxing; you only pay the heap/GC cost when necessary.
    //
    //     Example: Player’s JumpSettings is simulated locally, then stabilized so animation + physics + AI all share the same data.

    public interface IStablizableUser
    {
    }

    public interface IStablizable
    {
        /// <summary>
        /// param fromStabilizer is not intended for manual Stabilization.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="fromStabilizer"></param>
        void Stabilize(IStablizableUser user, bool fromStabilizer = false);
    }

    public static class Stabilizer
    {
        public static HashSet<IStablizableUser> stabilizedUsers = new();

        public static void StabilizeAttributed(this IStablizableUser user)
        {
            //Debug.Log("Initializing StableValueTypes started...");
            var stableFields = user.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => typeof(IStablizable).IsAssignableFrom(f.FieldType)
                            && f.GetCustomAttribute<StabilizeAttribute>() != null)
                .ToList();
            //Debug.Log("Fields marked with [Stabilize]: " + stableFields.Count);

            user.StabilizeFields(stableFields);
        }

        public static void StabilizeAll(this IStablizableUser user)
        {
            //Debug.Log("Initializing StableValueTypes started...");
            var stableFields = user.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => typeof(IStablizable).IsAssignableFrom(f.FieldType))
                .ToList();
            //Debug.Log("Stabilizing All fields " + stableFields.Count);


            user.StabilizeFields(stableFields);
        }

        static void StabilizeFields(this IStablizableUser user, List<FieldInfo> stableFields)
        {
            foreach (var field in stableFields)
            {
                var value = field.GetValue(user);
                ((IStablizable)value).Stabilize(user);
                field.SetValue(user, value); // re-assining back struct value
                //Debug.Log($"Initialized reference on field {field.Name}");
            }

            // Avoid re-adding
            if (!Stabilizer.stabilizedUsers.Contains(user))
                stabilizedUsers.Add(user);
        }


        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static void Heap<T>(this Stablizable<T> s)
        //     where T : struct
        //     => s.RefreshAll();
        //
        //
        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static void Heap<T, T2>(this Stablizable<T> s, T2 _)
        //     where T : struct
        //     => s.RefreshAll();





        /// <summary>
        /// Value-types that can optionally and dynamically be promoted to reference types for stable configuration.
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
            [NonSerialized] public RefSync<T> sharedheap;
            public RefSync<T> inspectHeap => sharedheap;

            public bool isStable
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return isStablecached ||
                           (isStablecached = !(isNotStablecached = !(myUser != null && sharedheap != null)));
                }

            }

            public T Get
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => (isStablecached || isStable) ? _stack = sharedheap.val : _stack;
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
                    if (isStablecached || isStable) return ref sharedheap.val;
                    throw new InvalidOperationException(
                        "Trying to access heap when the variable isn't stabilized. Use SetLocal.");
                }
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
                if (sharedheap == null)
                {
                    sharedheap = new RefSync<T>(_stack);
                    sharedheap.OnValueChanged += PullStack;
                    Debug.Log($"Stabilized {_stack}, add sub, is now {sharedheap.OnValueChanged.GetInvocationList().Length}");
                }

                if (!fromStabilizer && !stabilizedUsers.Contains(myUser))
                    stabilizedUsers.Add(myUser);
            }

            public void PullStack()
            {
                Debug.Log($"Pulling value: {sharedheap.val}");
                _stack = sharedheap.val;
            }

           // public static implicit operator T(Stablizable<T> c) => c.Get;
           // public static implicit operator Stablizable<T>(T v) => new Stablizable<T> { _stack = v };
        }
    }
}

