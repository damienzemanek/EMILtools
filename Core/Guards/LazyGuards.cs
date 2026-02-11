using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;

namespace EMILtools.Core
{
    public class LazyGuardsMutable
    {
        public IReadOnlyList<LazyGuardCondition> Guards => guards;
        
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly, ListDrawerSettings(Expanded = true)]
        readonly List<LazyGuardCondition> guards;
    
        public LazyGuardsMutable(params (string name, PersistentAction onObservedChanged, Func<bool> observed)[] guards)
        {
            this.guards = new List<LazyGuardCondition>(guards.Length);
            foreach (var g in guards)
                this.guards.Add(new LazyGuardCondition(g.name, g.onObservedChanged, g.observed));
        }
    
        public LazyGuardsMutable AddGuard(LazyGuardCondition guard)
        {
            guards.Add(guard);
            return this;
        }
    
        public void AddGuard(params LazyGuardCondition[] guard)
            => guards.AddRange(guard);
    
        bool AnyBlocked
        {
            get
            {
                for (int i = 0; i < Guards.Count; i++)
                {
                    if (Guards[i].Blocked) return true;
                }
                return false;
            }
        }
    
        public static implicit operator bool(LazyGuardsMutable guards) => guards.AnyBlocked;
    
    }

    /// <summary>
    /// Intended to be set one in initialization to easily see what bools interact with what guards
    /// </summary>
    public readonly struct LazyGuardsImmutable
    {
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly, ListDrawerSettings(Expanded = true)] 
        LazyGuardCondition[] InspectGuards => guards;
        
        readonly LazyGuardCondition[] guards;

        public LazyGuardsImmutable(params (string name, PersistentAction onObservedChanged, Func<bool> observed)[] guards)
        {
            this.guards = new LazyGuardCondition[guards.Length];
            for (int i = 0; i < guards.Length; i++)
            {
                this.guards[i] = new LazyGuardCondition(guards[i].name, guards[i].onObservedChanged, guards[i].observed);
            }
        }
        
        bool AnyBlocked
        {
            get
            {
                for (int i = 0; i < guards.Length; i++)
                {
                    if (guards[i].Blocked) return true;
                }
                return false;
            }
        }
        
        public static implicit operator bool(LazyGuardsImmutable guards) => guards.AnyBlocked;
    }
}