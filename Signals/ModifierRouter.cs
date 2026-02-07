using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using EMILtools.Core;
using static EMILtools.Core.ExpressionBinder;
using UnityEngine;
using static EMILtools.Signals.ModifierExtensions;
using static EMILtools.Signals.ModifierStrategies;

namespace EMILtools.Signals
{
    public static class ModiferRouting
    {
        
        public interface IStatHolder { }
        public interface IStatUser : IStatHolder
        {
            public Dictionary<Type, IStat> Stats { get; set; } 
        }

        /// <summary>
        /// Call in Awake { this.CacheStats(); } to cache the add modifiers for the IStatUser
        /// This penetrates and recurses into sub-systems that also implement IStatUser, so you can have a
        /// neat hierarchy of systems with their own stats and modifiers, and they will all be cached properly.
        /// </summary>
        /// <param name="statuser"></param>
        public static void CacheStats(this IStatUser user) => user.CacheStatsRecursive(out _);
        
        static void CacheStatsRecursive(this IStatHolder user, out List<(FieldInfo info, object instance)> nestedStats, bool nested = false)
        {
            nestedStats = new List<(FieldInfo info, object instance)>();
            if (user == null) return;
            Debug.Log($"[CacheStatFields] Starting cache for IStatUser: {user.GetType().Name}");
            
            var fields = user.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var statsFields = new List<(FieldInfo field, object instance)>();

            foreach (var field in fields)
            {
                var fieldType = field.FieldType;
                var instance = field.GetValue(user);
                if (instance == null) continue;
                
                if (typeof(IStat).IsAssignableFrom(fieldType))
                {
                    statsFields.Add((field, instance));
                    continue; 
                }

                // Wrappers
                if (field.FieldType.IsGenericType)
                {
                    var args = field.FieldType.GetGenericArguments();
                    if (args.Length <= 1) continue;
                    if (!typeof(IStat).IsAssignableFrom(args[0])) continue;
                    var property = instance.GetType().GetProperty("Value");
                    var value = property.GetValue(instance);
                    statsFields.Add((field, value));
                    continue;
                }

                if (typeof(IStatHolder).IsAssignableFrom(field.FieldType))
                {
                    CacheStatsRecursive(field.GetValue(user) as IStatHolder, out var newNestedStats, nested: true);
                    statsFields.AddRange(newNestedStats);
                }
            }
            if (nested) return; // Block the nested dudes from caching
            if (statsFields.Count <= 0) { Debug.Log("No IStat fields found to cache, Please declare your Stat fields in your IStatUser concrete implementation.");return;}
            if (user is not IStatUser mainUser) { Debug.LogError("Main user is not a IStatUSer, cannot cache"); return; }
            mainUser.Stats = new Dictionary<Type, IStat>(statsFields.Count);
            
            Debug.Log($"[CacheStatFields] Found {statsFields.Count} Stat fields: {string.Join(", ", statsFields.Select(f => f.field.Name))}");
            
            
            foreach (var f in statsFields)
            {
                var statArgs = f.instance.GetType().GetGenericArguments();
                Type ttag = statArgs.Length > 1 ? statArgs[1] : statArgs[0];
                
                mainUser.Stats[ttag] = f.instance as IStat;
                Debug.Log($"[CacheStatFields] Cached stat of TMod {ttag} in user {user}");
            }
        }
    }
}
