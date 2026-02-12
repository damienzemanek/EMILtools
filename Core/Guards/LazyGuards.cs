using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;

namespace EMILtools.Core
{
    public class LazyGuarderMutable : IGuarder
    {
        public IReadOnlyList<LazyGuard> Guards => guards;
        
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly, ListDrawerSettings(Expanded = true)]
        readonly List<LazyGuard> guards;
    
        public LazyGuarderMutable(params (string name, PersistentAction onObservedChanged, Func<bool> observed)[] guards)
        {
            this.guards = new List<LazyGuard>(guards.Length);
            foreach (var g in guards)
                this.guards.Add(new LazyGuard(g.name, g.onObservedChanged, g.observed));
        }
    
        public LazyGuarderMutable AddGuard(LazyGuard guard)
        {
            guards.Add(guard);
            return this;
        }
    
        public void AddGuard(params LazyGuard[] guard)
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
    
        public static implicit operator bool(LazyGuarderMutable guarder) => guarder.AnyBlocked;
    
    }

    /// <summary>
    /// Intended to be set one in initialization to easily see what bools interact with what guards
    /// </summary>
    public readonly struct LazyGuarderImmutable : IGuarder
    {
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly, ListDrawerSettings(Expanded = true)] 
        LazyGuard[] InspectGuards => guards;
        
        readonly LazyGuard[] guards;

        public LazyGuarderImmutable(params (string name, PersistentAction onObservedChanged, Func<bool> observed)[] guards)
        {
            this.guards = new LazyGuard[guards.Length];
            for (int i = 0; i < guards.Length; i++)
            {
                this.guards[i] = new LazyGuard(guards[i].name, guards[i].onObservedChanged, guards[i].observed);
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
        
        public static implicit operator bool(LazyGuarderImmutable guarder) => guarder.AnyBlocked;
    }
}