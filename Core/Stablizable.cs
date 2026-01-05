using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace EMILtools.Core
{
    
    public interface IStablizableUser { }

    public interface IStablizable
    {
        void Stabilize(IStablizableUser user);
    }

    public static class ConfigInitializer
    {
        public static HashSet<IStablizableUser> stabilizedUsers = new();

        public static void Stabilize(this IStablizableUser user)
        {
            Debug.Log("Initializing StableValueTypes started...");
            var stableFields = user.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => typeof(IStablizable).IsAssignableFrom(f.FieldType))
                .ToList();
            Debug.Log("Retrieved list of stable value-types, size is " + stableFields.Count);

            foreach (var field in stableFields)
            {
                var value = field.GetValue(user);
                ((IStablizable)value).Stabilize(user);
                field.SetValue(user, value); // re-assining back struct value
                Debug.Log($"Initizalized reference on field {field.Name}");
            }
            
            stabilizedUsers.Add(user);
        }
    }

    
    /// <summary>
    /// Value-types that can be optionally set to be reference types for stable configuration
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
                return (isStable) ? reference.val : val;
            }
            set
            {
                if (isStable) reference.val = value;
                else
                {
                    ThrowIfUserWasStabilized();
                    val = value;
                }
            }
        }
        
        void ThrowIfUserWasStabilized()
        {
            if (ConfigInitializer.stabilizedUsers.Contains(myUser)) throw new Exception(
                "Using a Stablizable outside of its owning user is forbidden. " +
                    "Do not copy the struct; pass its Value instead."
                );
        }

        public void Stabilize(IStablizableUser user)
        {
            myUser = user;
            if(reference == null) reference = new Ref<T>(val);
            Debug.Log($"[Configurable] Successfully had my refernce initialized");
        }
        
        public static implicit operator T(Stablizable<T> c) => c.Value;
    }
}

