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
    
        [Serializable]
        [InlineProperty]
        public class Ref<T> where T : struct
        {
            /// <summary>
            /// Backing field for the base value, Specifically for the custom drawer
            /// </summary>
            [HorizontalGroup("Split", Width = 0.3f)]
            [SerializeField, HideLabel] [VerticalGroup("Split/Right")] protected T _val;
            
            /// <summary>
            /// Overridable for Intercept and Notify on change
            /// </summary>
            protected virtual T val { get => _val; set => _val = value; }
            
            /// <summary>
            /// Public facing getter and setters for the actual value
            /// </summary>
            [ShowInInspector]
            public virtual T Value { get => val; set => val = value;}
            
            public virtual ref T ValueRef => ref _val;
            
            public Ref(T initialValue) => val = initialValue;
            public Ref(ref T initialValue) => val = initialValue;
        }
    
        
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
        public sealed class Stat<T, TTag> : Ref<T>, IStat 
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
            
            /// <summary>
            /// Used for INTERCEPTS when changed but before NOTIFY.
            /// Validates and Mutates the base value when changed to subscribed specifications
            /// 
            /// Used for NOTIFY when changed
            /// (Has nothing to do with Modifiers, except re-calculate them when the base is changed)
            /// </summary>
            protected override T val
            {
                get => base.val;
                set
                {
                    if (HasIntercepts) value = Intercept(value);
                    
                    if (EqualityComparer<T>.Default.Equals(base.val, value)) return;
                    base.val = value;
                    if(notifyChanges) Reactions?.Invoke(value);
                    Calculate(); //Refresh modifiers to new base value for math
                }
            }
            
            /// <summary>
            /// Used for MODIFIERS to store the final math value
            /// </summary>
            T calculated;

            /// <summary>
            /// Value is the pure value when there are no modifiers
            /// Value is the calculated value when there are modifiers
            /// </summary>
            [ShowInInspector, ReadOnly, HideLabel]
            [VerticalGroup("Split/Left")]
            [PropertyOrder(0)]
            public override T Value
            {
                get => (HasModifiers && !blockIntercepts) ? calculated : val;
                set => val = value;
            }

            // Not initialized here to save on memory, When using lazy initialize
            private List<ModifierSlot> _modSlots;
            private List<Func<T, T>> _intercepts;
            
            // Access for Configurations
            public IReadOnlyList<ModifierSlot> ModSlots => _modSlots;
            public IReadOnlyList<Func<T, T>> Intercepts => _intercepts;
            public event Action<T> Reactions; //Kept as event so outside scripts can't invoke it directly
            
            
            bool HasModifiers => _modSlots != null && _modSlots.Count > 0;
            bool HasIntercepts => _intercepts != null && _intercepts.Count > 0;

            public Stat(T initialValue) : base(initialValue) => Calculate();
            
            // Settings
            [VerticalGroup("Split/Right")]
            [LabelWidth(100)]
            [PropertyOrder(1)]
            [SerializeField] private bool _blockIntercepts = false;
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
            [VerticalGroup("Split/Right")]
            [LabelWidth(100)]
            [PropertyOrder(2)]
            public bool notifyChanges = true;
            
            T Calculate()
            {
                if (!HasModifiers) return val;
                T beingModified = _modSlots.ApplyAll(val);
                Debug.Log("Old calculated value: " + val);
                Debug.Log("New calculated value: " + beingModified);
                return calculated = beingModified;
            }

            T Intercept(T newValue)
            {
                for(int i = 0; i < Intercepts.Count; i++) 
                    newValue = Intercepts[i].Invoke(newValue);
                return newValue;
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
            
            
            //--------------------------------------------------
            //                  Stat Interecepts
            //--------------------------------------------------

            /// <summary>
            /// Intercepts are called before the base value is changed.
            /// Example: Player looses health, health is clamped to a minimum of 0
            /// 1st T is the input value, 2nd T is the output value
            /// </summary>
            /// <param name="intercept"></param>
            /// <returns></returns>
            public Stat<T, TTag> AddIntercept(Func<T, T> intercept)
            {
                if(Intercepts == null) _intercepts = new List<Func<T, T>>();
                if (_intercepts.Contains(intercept)) return this;
                _intercepts.Add(intercept);
                RefreshIntercept();
                return this;

            }
            
            public Stat<T, TTag> RemoveIntercept(Func<T, T> intercept)
            {
                _intercepts.Remove(intercept);
                RefreshIntercept();
                return this;
            }
            
            void RefreshIntercept() => val = val;
            

            /// <summary>
            /// Reactions events that are called when the base value changes
            /// Example: Player looses health, an "On Hit" event is called
            /// </summary>
            /// <param name="reaction"></param>
            /// <returns></returns>
            public Stat<T, TTag> AddReaction(Action<T> reaction)
            {
                if(Reactions == null) Reactions = reaction;
                Reactions += reaction;
                return this;
            }

            public Stat<T, TTag> RemoveReaction(Action<T> reaction)
            {
                Reactions -= reaction;
                return this;
            }
            
            
            /// <summary>
            /// Implicit conversion so we can treat this object like its raw value T in math or logic
            /// </summary>
            /// <param name="r"></param>
            /// <returns></returns>
            public static implicit operator T(Stat<T, TTag> r) => (r != null) ? r.Value : default;
            
        }
    
}
