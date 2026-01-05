using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace EMILtools.Core
{
    
    
    [AttributeUsage(AttributeTargets.Field)]
    public class StabilizeAttribute : Attribute { }
    
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
    
    
    public interface IStablizableUser { }

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
            Debug.Log("Initializing StableValueTypes started...");
            var stableFields = user.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => typeof(IStablizable).IsAssignableFrom(f.FieldType)
                                    && f.GetCustomAttribute<StabilizeAttribute>() != null)
                .ToList();
            Debug.Log("Fields marked with [Stabilize]: " + stableFields.Count);

            user.StabilizeFields(stableFields);
        }
        
        public static void StabilizeAll(this IStablizableUser user)
        {
            Debug.Log("Initializing StableValueTypes started...");
            var stableFields = user.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => typeof(IStablizable).IsAssignableFrom(f.FieldType))
                .ToList();
            Debug.Log("Stabilizing All fields " + stableFields.Count);
            
            user.StabilizeFields(stableFields);

        }

        static void StabilizeFields(this IStablizableUser user, List<FieldInfo> stableFields)
        {
            foreach (var field in stableFields)
            {
                var value = field.GetValue(user);
                ((IStablizable)value).Stabilize(user);
                field.SetValue(user, value); // re-assining back struct value
                Debug.Log($"Initialized reference on field {field.Name}");
            }
            stabilizedUsers.Add(user);
        }
        
    }

    
    /// <summary>
    /// Value-types that can be optionally set to be reference types for stable configuration dynamically
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public struct Stablizable<T> : IStablizable
        where T : struct
    {
        [SerializeField] T val; 
        [NonSerialized] Ref<T> reference; 
        [SerializeField] IStablizableUser myUser;
        public bool isStable { get => (myUser != null && reference != null); }
        
        public T Value
        {
            get
            {
                // Only matters for non-stable users
                if (!isStable) ThrowIfUserWasStabilized();
                return (isStable) ? refval : val;
            }
            set
            {
                if (isStable) refval = value;
                else
                {
                    ThrowIfUserWasStabilized();
                    val = value;
                }
            }
        }

        public T refval
        {
            get { EnsureStabilization(); return reference.val; }
            set { EnsureStabilization(); reference.val = value; }
        }
        

        void ThrowIfUserWasStabilized()
        {
            if (Stabilizer.stabilizedUsers.Contains(myUser)) throw new Exception(
                "Using a Stablizable outside of its owning user is forbidden. " +
                    "Do not copy the struct; pass its Value instead."
                );
        }

        /// <summary>
        /// Only intended for use by the Stabilizer static class.
        /// Stabilizing v
        /// </summary>
        /// <param name="user"></param>
        public void Stabilize(IStablizableUser user, bool fromStabilizer = false)
        {
            myUser = user;
            if(reference == null) reference = new Ref<T>(val);
            Debug.Log($"[Stablizable] Successfully Stabilized");

            // Individual Stabilization, optionally incrementally and dynamically stabilize
            // Check if this is from the stabilizer, avoids checking the hashset for every add in the stabilizer
            if(!fromStabilizer)
                if (!Stabilizer.stabilizedUsers.Contains(myUser)) Stabilizer.stabilizedUsers.Add(myUser);
        }

        void EnsureStabilization() => reference ??= new Ref<T>(val);
        
        public static implicit operator T(Stablizable<T> c) => c.Value;
    }
}

