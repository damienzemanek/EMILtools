using System;
using System.Collections.Generic;
using EMILtools.Extensions;
using EMILtools.Signals;
using EMILtools.Timers;
using UnityEngine;
using static EMILtools.Signals.ModiferRouting;
using static EMILtools.Signals.ModifierStrategies;
using static EMILtools.Signals.StatTags;

namespace EMILtools.Signals
{ 
    public static class ModifierExtensions
    {
        public interface IStat { }

        //---------------------------------------------------------------------------------------
        //                  Modify               [User] -> [Stat]
        //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
        
        //---------------------------------  FLOAT - MathMod  -----------------------------------
        public static (MathMod, TTag, IStatUser) Modify<TTag>(this IStatUser user, MathMod mod)
            where TTag : struct, IStatTag
        => user.Modify<float, MathMod, TTag>(mod);
        
        //--------------------------------  FLOAT  - GENERIC TMod --------------------------------
        public static (TMod, TTag, IStatUser) Modify<TMod, TTag>(this IStatUser user, TMod mod)
            where TMod : struct, IStatModStrategy<float>
            where TTag : struct, IStatTag
            => user.Modify<float, TMod, TTag>(mod);
        
        //------------------------------- Generic T - GENERIC TMod  -----------------------------
        public static (TMod, TTag, IStatUser) Modify<T, TMod, TTag>(this IStatUser user, TMod mod)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
            where TTag : struct, IStatTag
        {
            Stat<T, TTag> stat = (user.Stats[typeof(TTag)] as Stat<T, TTag>);
            stat.AddModifier(mod);
            return (mod, new TTag(), user);
        }
        
        //-----------------------------------------------------------------------------------
        //                 Remove Modifier      [User] -> [Stat]
        //------------------------------------------------------------------------------------
        //---------------------------------  FLOAT - MathMod  ----------------------------------
        public static void RemoveModifier<TTag>(this IStatUser user, MathMod mod)
            where TTag : struct, IStatTag
        => user.RemoveModifier<float, MathMod, TTag>(mod);
        
        //--------------------------------  FLOAT  - GENERIC TMod --------------------------------
        public static void RemoveModifier<TMod, TTag>(this IStatUser user, TMod mod)
            where TMod : struct, IStatModStrategy<float>
            where TTag : struct, IStatTag 
        => user.RemoveModifier<float, TMod, TTag>(mod);
        
        //------------------------------------  GENERIC  --------------------------------------
        public static void RemoveModifier<T, TMod, TTag>(this IStatUser user, TMod mod)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
            where TTag : struct, IStatTag
        {
            Stat<T, TTag> stat = (user.Stats[typeof(TTag)] as Stat<T, TTag>);
            stat.RemoveModifier(mod.hash);
        }
        
        
                
        //-----------------------------------------------------------------------------------
        //                 Decorator Add/Remove   [User] -> [Stat]
        //------------------------------------------------------------------------------------
        // Generic base Modify
        public static void AddDecorator<T, TMod, TTag>(this IStatUser user, IStatModDecorator<T, TTag> decorator)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
            where TTag : struct, IStatTag
        {
            Stat<T, TTag> stat = (user.Stats[typeof(TTag)] as Stat<T, TTag>);
            stat.AddDecorator(decorator);
        }
        
        public static void RemoveDecorator<T, TMod, TTag>(this IStatUser user,  TMod mod, IStatModDecorator<T, TTag> decorator)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
            where TTag : struct, IStatTag
        {
            Stat<T, TTag> stat = (user.Stats[typeof(TTag)] as Stat<T, TTag>);
            stat.RemoveDecorator(mod.hash, decorator);
        }
        
        

        
        
        
        
        //-----------------------------------------------------------------------------------
        //                         EX Modifier Slot Remove  
        //------------------------------------------------------------------------------------
        public static bool RemoveModifierSlotEX<T, TTag>(this List<Stat<T, TTag>.ModifierSlot> modslots, ulong hash)
            where T : struct
            where TTag : struct, IStatTag
        {
            Debug.Log("Removal STARTED");
        
            for (int i = 0; i < modslots.Count; i++)
            {
                if (modslots[i].hash != hash) { continue; } // must be same hash 
                modslots[i].RemoveAllDecoratorsFromSlot();
                modslots.RemoveAt(i); 
                Debug.Log("Sucessfully Removed Modifier Slot");
                return true;
            }
            return false;
        }
        
        //-----------------------------------------------------------------------------------
        //                        EX  Stat Decorator Add/Remove 
        //------------------------------------------------------------------------------------
        public static bool RemoveDecoOnModEX<T, TTag>(this List<Stat<T, TTag>.ModifierSlot> modslots, ulong hash, IStatModDecorator<T, TTag> dec)
            where T : struct
            where TTag : struct, IStatTag
        {
            Debug.Log("Removal STARTED");
        
            for (int i = 0; i < modslots.Count; i++)
            {
                Debug.Log($"Checking Mod slot {i} (continuing...) ");
                if (modslots[i].hash != hash) continue;                                                          // Correct Slot
                for (int j = 0; j < modslots[i].listsOfModifiers.Count; j++)
                {
                    if (modslots[i].listsOfModifiers[j].tmodtype != dec.tmodType) continue;                     // Correct TMod List (Meaning Correct Decor List)
                    if (modslots[i].listsOfModifiers[j].decors != null && modslots[i].listsOfModifiers[j].decors.Count > 0) // Not null
                    {
                       bool removed = modslots[i].listsOfModifiers[j].decors.Remove(dec);
                       if (removed) dec.OnRemove?.Invoke();
                    }
                }
            }
            return false;
        }
        
        public static List<Stat<T, TTag>.ModifierSlot> AddDecoratorEX<T, TTag>(this List<Stat<T,TTag>.ModifierSlot> modslots,
                                                                                    IStatModDecorator<T, TTag> decorator,
                                                                                    Stat<T, TTag> stat )
            where T : struct
            where TTag : struct, IStatTag
        {
            bool added = false;
            for (int i = 0; i < modslots.Count; i++)
            {
                if (modslots[i].hash != decorator.hash) continue; // Finding the correct slot by hash

                for (int j = 0; j < modslots[i].listsOfModifiers.Count; j++)  // for loop cause foreach is immutable list (bruhhh)
                {
                    var tmodtype = modslots[i].listsOfModifiers[j].tmodtype;
                    var decList = modslots[i].listsOfModifiers[j].decors;
                    
                    if (tmodtype != decorator.tmodType) continue;
                    
                    // only quick copy for the value-type tuple
                    // will most likely occur early when there are not that many modifiers to copy as decs will be common
                    if (decList == null)
                    {
                        modslots[i].listsOfModifiers[j] = (modslots[i].listsOfModifiers[j].tmodtype, 
                                                            modslots[i].listsOfModifiers[j].tmodlist, 
                                                            decList = new List<IStatModDecorator<T, TTag>>());
                    }
    
                    // set its stat for use by the dec
                    decorator.stat = stat;
                    decList.AddGet(decorator).OnAdd?.Invoke();
                    Debug.Log($"[AddDecorator] Added new decorator to slot {i}"); added = true;
                }
                
            }
            if(added) Debug.Log($"[AddDecorator] Added Decorator Success");
            else Debug.Log($"[AddDecorator] Added Decorator FAILED [!] ");
            return modslots;
        }
        
    }
}


