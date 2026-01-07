using System;
using System.Collections.Generic;
using System.Reflection;
using EMILtools.Core;
using UnityEngine;
using Sirenix.OdinInspector;
using static EMILtools.Signals.ModiferRouting;
using static EMILtools.Signals.ModifierExtensions;
using static EMILtools.Signals.ModifierStrategies;
using static EMILtools.Signals.StatTags;

namespace EMILtools.Signals
{
    
        /// <summary>
        /// A Multi-configurable event bus variable
        /// - Modifiers are functions that take in the base value and use FuncT T modifiers (that are 
        ///     not lambdas) to return a separate calculated value. The order matters
        /// - Intercepts are functions that intercept changes to the base value and Validate and Mutate it before it is stored.
        /// - Reactions are actions that are called when the base value changes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        [Serializable]
        [InlineProperty]
        public sealed class Stat<T, TTag> : IStat 
            where T : struct
            where TTag : struct, IStatTag
        {
            public struct ModifierSlot
            {
                public ModifierSlot(ulong hash)
                {
                    this.hash = hash;
                    listsOfModifiers = new();
                }
                
                public ulong hash; // For quick removal ops. moved from inside the mod to here so i dont have to iterate look for it
                
                // Type is TMod type (ex: tyepof MathMod, typeof ContextMod), Cannot be genericly constrainted
                // object is List<TMod> (ex: List<struct MathMod>)
                // decors corrosponds to the tmodlist it decorates
                public List<(Type tmodtype, object tmodlist, List<IStatModDecorator<T, TTag>> decors)> listsOfModifiers;
                
                /// <summary>
                /// Apply the decorator if it's there first
                /// </summary>
                /// <param name="val"></param>
                /// <returns></returns>
                public T SlotApply(T val)
                {
                    Debug.Log($"[SlotApply] Applying the Lists using val {val}");
                    foreach (var (tmodtype, tmodlist, decors) in listsOfModifiers)
                    {
                        if (decors != null && decors.Count > 0)
                            return ResolveList(tmodtype, tmodlist, decors.ApplyDecorators(val));
                        else
                            return ResolveList(tmodtype, tmodlist, val);
                    }
                    return val;
                }

                public void AddModifierToSlot<TMod>(TMod mod)
                    where TMod : struct, IStatModStrategy<T>
                {
                    if (listsOfModifiers == null) listsOfModifiers = new List<(Type, object, List<IStatModDecorator<T, TTag>>)>(); // Lazy init the list of (modifier lists)

                    bool tmodlistAlreadyExists = false;
                    foreach (var (tagtype, tmodlist, decs) in listsOfModifiers)
                    {
                        if (tagtype == typeof(TMod)) // If there is already a List<TMod> of this TMod Type, Add to this List<TMod> (which is a object atm)
                        {
                            (tmodlist as List<TMod>).Add(mod);
                            tmodlistAlreadyExists = true;
                            break;
                        }
                    }
                    if (tmodlistAlreadyExists == false)                  // If there isnt already a List<TMod> of this TMod type, Create the List<Tmod> and add it to the listsOfModifiers master list)
                    {
                        var newtmodList = new List<TMod>() { mod };// Lazy initialize specific modifier list, of this TMod type, and assign
                        listsOfModifiers.Add((typeof(TMod), newtmodList, null));
                    }
                }

                public void RemoveAllDecoratorsFromSlot()
                {
                    foreach (var (_, _, decors) in listsOfModifiers)
                    {
                        if (decors == null) continue;
                        foreach (var dec in decors)
                            dec?.OnRemove?.Invoke();
                    }
                }
            }

            [VerticalGroup("Split")] [HorizontalGroup("Split/Left")] [SerializeField] [HideLabel] ReactiveIntercept<T> ri;
            
            /// <summary>
            /// Used for MODIFIERS to store the final math value
            /// </summary>
            T calculated;

            /// <summary>
            /// Value is the pure value when there are no modifiers
            /// Value is the calculated value when there are modifiers
            /// </summary>
            [ShowInInspector, ReadOnly, HideLabel]
            [VerticalGroup("Split")]
            [HorizontalGroup("Split/Right")]
            [PropertyOrder(0)]
            public T Value
            {
                get => (HasModifiers && !blockIntercepts) ? calculated : ri.baseValue;
                set => ri.Value = value;
            }

            // Not initialized here to save on memory, When using lazy initialize
            List<ModifierSlot> _modSlots;
            public IReadOnlyList<ModifierSlot> ModSlots => _modSlots;
            bool HasModifiers => _modSlots != null && _modSlots.Count > 0;

            public Stat(T initialValue)
            {
                ri = new ReactiveIntercept<T>(initialValue);
                ri += _ => { Calculate(); }; //putting Calculate(); in { } forces a void return type (for Action<T>)
                Calculate();
            }
            
            // Settings
            bool _blockIntercepts = false;
            public bool blockIntercepts
            {
                get => _blockIntercepts;
                set
                {
                    if (_blockIntercepts == value) return;
                    _blockIntercepts = value;
                    Calculate();
                }
            }
            [HideInInspector] public bool notifyChanges = true;
            
            T Calculate()
            {
                if (!HasModifiers) return ri.baseValue;
                T beingModified = _modSlots.ApplyAll(ri.baseValue);
                Debug.Log("Old calculated value: " + ri.baseValue);
                Debug.Log("New calculated value: " + beingModified);
                return calculated = beingModified;
            }
            
            
            
            //--------------------------------------------------
            //                  Stat Modifiers
            //--------------------------------------------------
            /// <summary>
            /// Modifiers are functions that modify the base value and replace the Value getter with the calcualted value
            /// Example: Player picked up "Speed" ability. Player speed is increased
            /// Order of modifiers applied matters
            /// Do not use Inline Lambdas because they will be different instances and cannot be removed later
            /// </summary>
            /// <param name="modifier"></param>
            /// <returns></returns>
            public void AddModifier<TMod>(TMod mod) 
                where TMod: struct, IStatModStrategy<T>
            {
                Debug.Log("Adding Modifier: " + mod);
                
                if(ModSlots == null) _modSlots = new List<ModifierSlot>(); // Lazy init for the SLOTS
                ModifierSlot newSlot = new ModifierSlot(mod.hash);
                newSlot.AddModifierToSlot(mod); // Add the MOD into the slot
                _modSlots.Add(newSlot); // Add the slot with the new mod into the SLOTS
                
                Debug.Log($"Added Modifier : {mod}. Total Modifier Slots now: {_modSlots.Count}");
                Calculate();
            }
            
            public void RemoveModifier(ulong hash)
            {
                if (!_modSlots.RemoveModifierSlotEX(hash)) {
                    Debug.Log("[RemoveModifier] Removal failed. Could not find modifier with that func"); return; }
                
                Debug.Log("[RemoveModifier] Modifier Slot Removal Success. (Which includes the modifiers and decorators)");
                Calculate();
            }
            
            //--------------------------------------------------
            //                 Stat Decorators
            //--------------------------------------------------
            
            public void AddDecorator(IStatModDecorator<T, TTag> decorator)
            {
                Debug.Log("Appending Decorators: " + decorator);
                _modSlots.AddDecoratorEX(decorator, this);
                Debug.Log($"Added Decorators : {decorator}. Total Modifier Slots now: {_modSlots.Count}");
            
                Calculate();
            }
            
            public void RemoveDecorator(ulong hash, IStatModDecorator<T, TTag> deco)
            {
                if (!_modSlots.RemoveDecoOnModEX(hash, deco)) {
                    Debug.Log("[Removing Decorator] Removal failed. Could not find modifier with that func");
                    return; }
                
                Debug.Log($"[Removing Decorator] Decorator Removal Success on hash {hash}");
                Calculate();
            }
            
            /// <summary>
            /// Implicit conversion so we can treat this object like its raw value T in math or logic
            /// </summary>
            /// <param name="r"></param>
            /// <returns></returns>
            public static implicit operator T(Stat<T, TTag> r) => (r != null) ? r.Value : default;
            
        }
    
}
