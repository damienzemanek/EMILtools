using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;

namespace EMILtools.Core
{
    public readonly struct SimpleGuard 
    {
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly]
        public string If { get; }

        [ShowInInspector, Sirenix.OdinInspector.ReadOnly]
        public bool Blocked => observed();

        readonly Func<bool> observed;

        public SimpleGuard(string name, Func<bool> observed)
        {
            If = name;
            this.observed = observed;
        }
        
        public static implicit operator bool(SimpleGuard simpleGuards) => simpleGuards.Blocked;

    }
    
    public class SimpleGuarderMutable : IGuarder
    {
        public IReadOnlyList<SimpleGuard> Guards => guards;
        
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly, ListDrawerSettings(Expanded = true)]
        readonly List<SimpleGuard> guards;    
        
        public SimpleGuarderMutable(params (string name, Func<bool> method)[] guards)
        {
            this.guards = new List<SimpleGuard>(guards.Length);
            foreach (var g in guards)
                this.guards.Add(new SimpleGuard(g.name, g.method));
        }
    
        public SimpleGuarderMutable AddGuard(SimpleGuard simpleGuard)
        {
            guards.Add(simpleGuard);
            return this;
        }
    
        public void AddGuard(params SimpleGuard[] guard)
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
    
        public static implicit operator bool(SimpleGuarderMutable simpleGuarder) => simpleGuarder.AnyBlocked;

    }

    /// <summary>
    /// Intended to be set one in initialization to easily see what bools interact with what guards
    /// </summary>
    public readonly struct SimpleGuarderImmutable : IGuarder
    {
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly, ListDrawerSettings(Expanded = true)] 
        SimpleGuard[] InspectGuards => guards;
        
        readonly SimpleGuard[] guards;

        public SimpleGuarderImmutable(params (string name, Func<bool> method)[] guards)
        {
            this.guards = new SimpleGuard[guards.Length];
            for (int i = 0; i < guards.Length; i++)
            {
                this.guards[i] = new SimpleGuard(guards[i].name, guards[i].method);
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
    
        public static implicit operator bool(SimpleGuarderImmutable simpleGuarder) => simpleGuarder.AnyBlocked;
    }
}