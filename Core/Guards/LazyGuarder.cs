using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Utilities;

namespace EMILtools.Core
{
    public readonly struct LazyGuard 
    {
        [ShowInInspector, ReadOnly]
        public string If { get; }

        [ShowInInspector, ReadOnly]
        public bool Blocked => observed;

        readonly LazyFuncLite<bool> observed;
        
        public LazyGuard(PersistentAction observedOnChanged,  Func<bool> @if, string ifName)
        {
            If = ifName;
            observed = new LazyFuncLite<bool>(observedOnChanged, @if);
        }
        
        public static implicit operator bool(LazyGuard guards) => guards.Blocked;

    }
    
    
    public class LazyGuarderMutable : IGuarder
    {
        public IReadOnlyList<LazyGuard> Guards => guards;
        
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly, ListDrawerSettings(Expanded = true)]
        readonly List<LazyGuard> guards;
    
        public LazyGuarderMutable(params LazyGuard[] guards)
        {
            this.guards = new List<LazyGuard>(guards.Length);
            this.guards.AddRange(guards);
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

        public LazyGuarderImmutable(params LazyGuard[] guards)
        {
            this.guards = new LazyGuard[guards.Length];
            this.guards.AddRange(guards);
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